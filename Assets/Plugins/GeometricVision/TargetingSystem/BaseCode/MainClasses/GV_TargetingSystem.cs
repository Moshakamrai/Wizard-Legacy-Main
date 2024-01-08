// Copyright © 2020-2022 Mikael Korpinen (Finland). All Rights Reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Debugging;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Factory;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingJobs;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Plane = UnityEngine.Plane;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses
{
    /// <summary>
    /// Class that shows up as a targeting system controller for the user.
    /// Main class for using the targeting system plugin.
    ///
    /// Usage: Add to objects you want to act as a targeting system. The component will handle the rest. 
    /// A lot of the settings are meant to be adjusted from the inspector UI. Do not add this to camera object
    /// since it creates its own camera and hides it from the user. Create empty gameobject instead and 
    ///add that as a child object to the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class GV_TargetingSystem : MonoBehaviour, IComparable<GV_TargetingSystem>
    {
        private int targetingSystemGroupIndex = Int32.MaxValue;

        public int CompareTo(GV_TargetingSystem other)
        {
            return this.targetingSystemGroupIndex.CompareTo(other.targetingSystemGroupIndex);
        }

        //X = actual index, w = any of the filters is null
        internal int4 targetingSystemIndex;


        internal bool indexIsSet = false;

        [SerializeField, Tooltip("Enables editor drawings for seeing targeting data.")]
        private TargetingDebugInstruction targetingDebugOptions = new TargetingDebugInstruction();

        [Header("Targeting area settings: ")]
        [SerializeField, Tooltip("Area view width used in filtering targets. Targets only exists inside this angle.")]
        private float fieldOfView = 25f;

        [SerializeField, Tooltip("How near the component sees targets. Targets only exists outside this distance.")]
        private float nearDistanceOfView = 0.5f;

        [field: SerializeField]
        [field: Tooltip("How far the component sees targets. Targets only exists inside this distance.")]
        public float TargetingDistance { get; set; } = 1500f;

        [Header("Targeting behaviour settings: ")]
        [SerializeField, Tooltip("How objects are going to be culled, when out of sight.")]
        private TargetingSystemDataModels.CullingBehaviour cullingBehaviour;

        [SerializeField, Tooltip("List of instructions that define what is targeted.")]
        private List<TargetingInstruction> targetingInstructions = new List<TargetingInstruction>();

        [Header("Tech stack support settings: ")]
        [SerializeField, Tooltip("Will enable the system to use GameObjects.")]
        private BoolReactiveProperty gameObjectProcessing = new BoolReactiveProperty(false);

        [Header("Targeting indicator settings:")]
        [SerializeField]
        [Tooltip("You can put transform here and it will get its position from closest target.")]
        private Transform targetingIndicatorObject;

        [SerializeField] [Tooltip("Inside this distance the locator will be shown.")]
        private float indicatorVisibilityDistance = 1f;

        [SerializeField] [Tooltip("Only target inside this distance will be returned")]
        private bool alsoBlockTargetFetchOutsideLocatorVisibilityDistance = false;


        private SortedSet<ITargetVisibilityProcessor> targetVisibilityProcessors =
            new SortedSet<ITargetVisibilityProcessor>(
                new SortedSet<ITargetVisibilityProcessor>(new TargetingSystemComparers.VisibilityProcessorComparer()));

        internal Camera hiddenUnityCamera;
        private Plane[] planes = new Plane[6];
        private Vector3 forwardWorldCoordinate = Vector3.zero;
        private Transform cachedTransform;
        private NativeList<Target> closestTargetsContainer;
        internal NativeSlice<Target> closestTargets;

        private TargetingSystemsRunner targetingSystemsRunner;

        //Should system use target objects bounds or center point when determining visibility. useBounds is set to true, if system uses line/edge or vertex targeting
        private bool useBounds = false;

        //This variable is used to make a slice of the targets container. It should not be altered by developer or user.
        private int amountOfTargets = 0;

        //Cached containers to avoid gc
        private readonly HashSet<string> usedTags = new HashSet<string>();
        private readonly HashSet<GeometryType> usedGeoTypes = new HashSet<GeometryType>();

        [SerializeField, HideInInspector]
        private List<ActionsTemplateTriggerActionElement> collectedTriggerActionElements =
            new List<ActionsTemplateTriggerActionElement>();

        //If the system belongs to group this is the system that contains keys. Done to avoid expensive calculations
        private GV_TargetingSystem targetingSystemWithKeys;
        internal bool NativeMemoryInitialized { get; set; } = false;
        private TargetingSystemFactory factory;
        private bool closestTargetFetchedForCurrentFrame = false;
        private bool mostDistantTargetFetchedForCurrentFrame = false;
        private bool refreshSystemState;

        private List<TargetingSystemDataModels.GeoInfo> seenGeometryInfos =
            new List<TargetingSystemDataModels.GeoInfo>();

        internal TargetingSystemMemory targetingSystemMemory;

        #region GettersAndSetters

        public TargetingSystemMemory TargetingSystemMemory
        {
            get { return this.targetingSystemMemory; }
            set { this.targetingSystemMemory = value; }
        }

        internal bool UpdateTargetingIndicator { get; private set; }

        //Caching variable to prevent multiple costly accesses to native array/slice
        public Target ClosestTarget { get; internal set; }
        public Target MostDistantTarget { get; internal set; }

        // ReSharper disable once ConvertToAutoProperty
        internal List<TargetingSystemDataModels.GeoInfo> SeenGeometryInfos
        {
            get { return this.seenGeometryInfos; }
            set { this.seenGeometryInfos = value; }
        }

        /// <summary>
        /// Targeting locator or indicator GameObject made to visualise the closest target, if wanted.
        /// </summary>
        /// <remarks>
        /// Setter that also checks, if gameObject is in the scene and then spawns, if not.
        /// </remarks>
        public Transform TargetingIndicatorObject
        {
            get { return this.targetingIndicatorObject; }
            set
            {
                this.targetingIndicatorObject = value;
                if (value != null)
                {
                    var nameComponent = this.TargetingIndicatorObject.gameObject.GetComponent<AssignName>();
                    if (nameComponent != null)
                    {
                        nameComponent.TargetingSystem = this;
                    }

                    InstantiateAndTurnUpdateOn();
                }
                else
                {
                    this.UpdateTargetingIndicator = false;
                }

                void InstantiateAndTurnUpdateOn()
                {
                    //Checks if the game object is prefab and does not exist in any scene
                    if (value.gameObject.scene.IsValid() == false)
                    {
                        this.TargetingIndicatorObject = Instantiate(value.gameObject).transform;
                    }

                    this.UpdateTargetingIndicator = true;
                }
            }
        }

        public TargetingSystemDataModels.CullingBehaviour CullingBehaviour
        {
            get { return this.cullingBehaviour; }
            set { this.cullingBehaviour = value; }
        }

        internal bool UseBounds
        {
            get { return this.useBounds; }
            set { this.useBounds = value; }
        }

        public float FieldOfView
        {
            get { return this.fieldOfView; }
            set { this.fieldOfView = value; }
        }
        
        /// <summary>
        /// Has refresh functionality added to the getter. Use this for adding new instruction in non performance critical scenarios.
        /// It re updates cache and fixes missing target processors if any.
        /// </summary>
        public List<TargetingInstruction> TargetingInstructionsWithRefresh
        {
            get
            {
                var count = TargetingSystemUtilities.CountTargetingActionsFromTargetingInstructions(
                    this.targetingInstructions);

                if (count != this.LastTriggerActionsCount)
                {
                    this.LastTriggerActionsCount = this.collectedTriggerActionElements.Count;
                    this.collectedTriggerActionElements =
                        TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                            this.targetingInstructions, this.collectedTriggerActionElements, this);
                }
         
                for (int i = 0; i< this.targetingInstructions.Count; i++)
                {
                    this.TryFixMissingGameObjectTargetProcessor(this.targetingInstructions[i], i);
                }

                return this.targetingInstructions;
            }
        }

        public List<TargetingInstruction> TargetingInstructions
        {
            get { return this.targetingInstructions; }
        }

        /// <summary>
        /// Targeting systems with the same query have their keys only calculated once and for other the reference to that system is stored here
        /// </summary>
        public GV_TargetingSystem TargetingSystemWithKeys
        {
            get { return this.targetingSystemWithKeys; }
            set { this.targetingSystemWithKeys = value; }
        }

        private int LastTriggerActionsCount { get; set; }

        private Camera HiddenUnityCamera
        {
            get { return this.hiddenUnityCamera; }
            set { this.hiddenUnityCamera = value; }
        }

        internal TargetingDebugInstruction TargetingDebugOptions
        {
            get { return this.targetingDebugOptions; }
            set { this.targetingDebugOptions = value; }
        }

        public BoolReactiveProperty GameObjectBasedProcessing
        {
            get { return this.gameObjectProcessing; }
        }

        public SortedSet<ITargetVisibilityProcessor> TargetVisibilityProcessors
        {
            get { return this.targetVisibilityProcessors; }
            set { this.targetVisibilityProcessors = value; }
        }

        /// <summary>
        /// Link to targetingSystemsRunner that is shared across all targeting systems
        /// </summary>
        public TargetingSystemsRunner TargetingSystemsRunner
        {
            get { return this.targetingSystemsRunner; }
            set { this.targetingSystemsRunner = value; }
        }

        public NativeList<Target> ClosestTargetsContainer
        {
            get { return this.closestTargetsContainer; }
            set { this.closestTargetsContainer = value; }
        }

        public int AmountOfTargets
        {
            get { return this.amountOfTargets; }
            set { this.amountOfTargets = value; }
        }

        public float IndicatorVisibilityDistance
        {
            get { return this.indicatorVisibilityDistance; }
            set { this.indicatorVisibilityDistance = value; }
        }
        

        internal bool ClosestTargetFetchedForCurrentFrame
        {
            get { return this.closestTargetFetchedForCurrentFrame; }
            set { this.closestTargetFetchedForCurrentFrame = value; }
        }

        internal bool MostDistantTargetFetchedForCurrentFrame
        {
            get { return this.mostDistantTargetFetchedForCurrentFrame; }
            set { this.mostDistantTargetFetchedForCurrentFrame = value; }
        }

        internal List<ActionsTemplateTriggerActionElement> CollectedTriggerActionElements
        {
            get { return this.collectedTriggerActionElements; }
            set { this.collectedTriggerActionElements = value; }
        }

        public int TargetingSystemGroupIndex
        {
            get { return this.targetingSystemGroupIndex; }
            set { this.targetingSystemGroupIndex = value; }
        }

        #endregion

        //Awake is called when script is instantiated.
        //Call initialize on Awake to init systems in case Component is created on the factory method.
        void Awake()
        {

            this.gameObject.layer = TargetingSystemSettings.IgnoreRayCastLayer;
            //Init with -1(outside the normal range of 0-n), the final index is calculated in the targetingSystemsRunner
            this.targetingSystemIndex.x = -1;
            if (this.targetingSystemMemory == null)
            {
                this.targetingSystemMemory = new TargetingSystemMemory();
            }
            
            if (this.ClosestTargetsContainer.IsCreated == false)
            {
                this.ClosestTargetsContainer = new NativeList<Target>(TargetingSystemSettings.MaxTargetsPerSystem, Allocator.Persistent);
                this.closestTargets = new NativeSlice<Target>(this.ClosestTargetsContainer, 0, 0);
            }

            this.cachedTransform = this.transform;
            this.targetingSystemWithKeys = this;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        //When scene is loaded and the scene contains systems, it is important to do a clean start.
        //Creating the targetingSystemsRunner on scene load dodges some memory management issues with the blob asset storage.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            this.AwakeSystem();
        }

        //Gets called when script is created in editor
        void Reset()
        {
            this.AwakeSystem();
        }

        private void AwakeSystem()
        {
            this.factory = TargetingSystemUtilities.TryReCreateFactory(this.factory);
            this.TargetingSystemsRunner = FindObjectOfType<TargetingSystemsRunner>();

            FirstOneCreatesTargetingSystemsRunner();
            CreateIndexForActiveTargetingSystem();

            //
            // Locals
            // 

            void FirstOneCreatesTargetingSystemsRunner()
            {
                if (this.TargetingSystemsRunner == null)
                {
                    this.factory.CreateTargetingSystemRunner(this);
                }
            }

            void CreateIndexForActiveTargetingSystem()
            {
                if (this.transform.root.gameObject.activeSelf && this.gameObject.activeSelf &&
                    this.isActiveAndEnabled)
                {
                    this.CreateIndexForTargetingSystem();
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            this.InitPersistentNativeArrays();
            this.InitUnityCamera();
            this.InitializeTargetingSystem(new List<GeometryType>());
            InitTargetingIndicator();
            this.RefreshInstructions(true);

            void InitTargetingIndicator()
            {
                if (this.TargetingIndicatorObject != null &&
                    this.TargetingIndicatorObject.gameObject.scene.IsValid() == false)
                {
                    this.TargetingIndicatorObject = Instantiate(this.TargetingIndicatorObject.gameObject).transform;
                }

                if (this.TargetingIndicatorObject != null &&
                    this.TargetingIndicatorObject.gameObject.scene.IsValid() == true)
                {
                    this.UpdateTargetingIndicator = true;
                }
            }
        }

        void OnValidate()
        {
            if (this.HiddenUnityCamera && this.targetingSystemMemory != null && Application.isPlaying)
            {
                this.TryInitRegenerateVisionArea(this.FieldOfView);
            }

            this.refreshSystemState = true;
        }

        private void OnDestroy()
        {
            this.GameObjectBasedProcessing.Value = false;
            this.GameObjectBasedProcessing.Dispose();
            this.FlushPersistentMemory();

            this.TargetVisibilityProcessors.Clear();
            
            if (this.TargetingSystemsRunner != null && this.TargetingSystemsRunner.TargetingSystems != null)
            {
                this.TargetingSystemsRunner.TargetingSystems.Remove(this);
            }
            Destroy(this.GetComponent<Camera>());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }


        internal void InitPersistentNativeArrays()
        {
            if (this.NativeMemoryInitialized == false)
            {
                this.NativeMemoryInitialized = true;
                this.targetingSystemMemory.InitPersistentNativeArrays();
            }
        }


        /// <summary>
        /// Should be run before start or in case making changes to GUI values from code and want the changes to happen before next frame(instantly).
        /// </summary>
        private void InitializeTargetingSystem(List<GeometryType> additionalGeometryTypesToProcess)
        {
            this.cachedTransform = this.transform;
            //needs to try initialize here for cold start
            this.InitPersistentNativeArrays();
            StartFactory(additionalGeometryTypesToProcess);
            this.InitTargetingActionsEntityPrefabs();
            this.collectedTriggerActionElements =
                TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                    this.targetingInstructions, this.collectedTriggerActionElements, this);

            //
            //Locals
            //

            //Runs all the required procedures for initializing targeting system with factory
            //Also add geometry types given as parameter to targeting
            void StartFactory(List<GeometryType> geometryTypesIn)
            {
                this.factory = new TargetingSystemFactory();

                if (geometryTypesIn == null)
                {
                    this.factory.AddAdditionalGeometryTypeTargetProcessors(this, geometryTypesIn);
                }

                this.targetingInstructions =
                    this.factory.InitializeTargeting(this.targetingInstructions, this);
                this.factory.CreateTargetingSystemRunner(this);

                this.targetingSystemIndex.x =
                    this.factory.CreateIndexForTargetingSystem(this.targetingSystemsRunner, this);
                this.SetGameObjectBasedProcessingOnOrOff(this.GameObjectBasedProcessing.Value);
            }
        }

        private void CreateIndexForTargetingSystem()
        {
            this.targetingSystemIndex.x = -1;
            int upperLimitForIndex = int.MaxValue;

            for (int i = 0; i < upperLimitForIndex; i++)
            {
                bool indexFound = this.targetingSystemsRunner.TargetingSystems.All(targetingSystem => targetingSystem.TargetingSystemIndex.x != i);
                if (indexFound)
                {
                    this.targetingSystemIndex.x = i;
                    this.indexIsSet = true;
                    break;
                }
            }
        }

        private void InitTargetingActionsEntityPrefabs()
        {
            foreach (var targetingInstruction in this.targetingInstructions)
            {
                targetingInstruction.TargetingActions.InitActionElementsForTargetingSystem(this);
            }
        }

        /// <summary>
        /// Applies changes made to entity filter type by the user from editor.
        /// </summary>
        /// <param name="instructions">Instructions to get changes to applied to</param>
        private void ApplyEntityFilterChanges(List<TargetingInstruction> instructions)
        {
            foreach (var targetingInstruction in instructions)
            {
                targetingInstruction.SetCurrentEntityFilterType(targetingInstruction.entityQueryFilter);
            }
        }

        internal void ApplyActionsTemplateObject(TargetingActionsTemplateObject template)
        {
            foreach (var targetingInstruction in this.targetingInstructions)
            {
                targetingInstruction.TargetingActions = template;
            }
        }

        /// <summary>
        /// Inits unity camera which provides some needed features for geometric vision like Gizmos, planes and matrices.
        /// </summary>
        internal void InitUnityCamera()
        {
            this.HiddenUnityCamera = this.gameObject.GetComponent<Camera>();

            if (this.HiddenUnityCamera == null)
            {
                this.HiddenUnityCamera = this.gameObject.AddComponent<Camera>();
            }

            this.HiddenUnityCamera.usePhysicalProperties = true;
            this.HiddenUnityCamera.aspect = 1f;

            this.HiddenUnityCamera.cameraType = CameraType.Game;
            this.HiddenUnityCamera.clearFlags = CameraClearFlags.Nothing;

            this.HiddenUnityCamera.enabled = false;
            this.TryInitRegenerateVisionArea(this.FieldOfView);
        }

        /// <summary>
        /// Gets closest entity or gameObject as target according to parameters set by the user from GV_TargetingSystem component.
        /// </summary>
        /// <remarks>Normally update is done every frame so calling this with forceUpdate set to true will impact performance when using multiple systems.
        /// Use case where this might be needed, is when something is destroyed on the main thread and the new target search is required immediately for next function
        /// or when measuring performance</remarks>
        /// <param name="forceUpdate">Schedules a job to instantly update targets.</param>
        /// <returns>TargetingSystemDataModels.Target that contain information about gameObject or entity</returns>
        public Target GetClosestTarget(bool forceUpdate)
        {
            if (forceUpdate)
            {
                this.UpdateTargetingData();
            }

            if (this.closestTargets.Length == 0)
            {
                return new Target();
            }

            this.ClosestTarget = TargetingSystemUtilities.GetAndUpdateTarget(this.ClosestTarget, this.closestTargets, 0,
                ref this.closestTargetFetchedForCurrentFrame);


            return TargetingSystemUtilities.CheckTargetAgainstPickingRadius(this.ClosestTarget,
                this.alsoBlockTargetFetchOutsideLocatorVisibilityDistance, this.indicatorVisibilityDistance);
        }

        /// <summary>
        /// Gets closest gameObject, if none return null.
        /// </summary>
        /// <param name="forceUpdate">Schedules a job to instantly update targets.</param>
        /// <returns>TargetingSystemDataModels.Target that contain information about gameObject or entity</returns>
        public GameObject GetClosestTargetAsGameObject(bool forceUpdate)
        {
            if (forceUpdate)
            {
                this.UpdateTargetingData();
            }

            if (this.closestTargets.Length > 0 && this.ClosestTargetFetchedForCurrentFrame == false)
            {
                this.ClosestTarget = TargetingSystemUtilities.SquareTargetDistances(this.closestTargets[0]);
                this.ClosestTargetFetchedForCurrentFrame = true;
                return TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(this.ClosestTarget.geoInfoHashCode,
                    this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos).gameObject;
            }
            else if (this.closestTargets.Length > 0 && this.ClosestTargetFetchedForCurrentFrame == true)
            {
                return TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(this.ClosestTarget.geoInfoHashCode,
                    this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos).gameObject;
            }

            return null;
        }

        /// <summary>
        /// Gets most distant entity or gameObject based on the view angle of the system as target according to parameters set by the user from GV_TargetingSystem component.
        /// </summary>
        /// <remarks>Normally update is done every frame so manually calling this at the middle of frame might impact performance.
        /// Use case where this might be needed, is when something is destroyed on the main thread and the new target search is required immediately for next function
        /// or when measuring performance</remarks>
        /// <param name="forceUpdate">Schedules a job to instantly update targets instead of using chunkKeysForTargeting from the update loop</param>
        /// <returns>TargetingSystemDataModels.Target that contain information about gameObject or entity</returns>
        public Target GetMostDistantTarget(bool forceUpdate)
        {
            if (forceUpdate)
            {
                this.UpdateTargetingData();
            }

            this.MostDistantTarget = TargetingSystemUtilities.GetAndUpdateTarget(this.MostDistantTarget,
                this.closestTargets, this.amountOfTargets - 1, ref this.mostDistantTargetFetchedForCurrentFrame);

            return TargetingSystemUtilities.CheckTargetAgainstPickingRadius(this.MostDistantTarget,
                this.alsoBlockTargetFetchOutsideLocatorVisibilityDistance, this.indicatorVisibilityDistance);
        }

        /// <summary>
        /// Calculate forward vector from position and forward vector.
        /// </summary>
        [BurstCompile]
        public Vector3 CalculateTargetingSystemDirectionInWorldSpace()
        {
            var transform1 = this.transform;
            this.forwardWorldCoordinate = transform1.position + transform1.forward;
            return this.forwardWorldCoordinate;
        }

        /// <summary>
        /// Gets targets for both entities and GameObject. Then sorts them.
        /// </summary>
        /// <remarks>Normally update is done every frame so manually calling this at the middle of frame with forceUpdate = true might impact performance.
        /// Case where this might be needed is when something is destroyed on the main thread and the new target search is required immediately for next function
        /// or when measuring performance</remarks>
        /// <param name="forceUpdate">Schedules a job to instantly update targets instead of using chunkKeysForTargeting from the update loop</param>
        /// <returns>New list of sorted targets </returns>
        public NativeSlice<Target> GetClosestTargets(bool forceUpdate)
        {
            if (forceUpdate)
            {
                this.GetAndGenerateClosetsTargetsArray();
            }

            return this.closestTargets;
        }

        /// <summary>
        /// Gets targets for both entities and GameObject. Then sorts them.
        /// </summary>
        /// <remarks>Normally update is done every frame so manually calling this at the middle of frame with forceUpdate = true impacts performance.
        /// Case where this might be needed is when something is destroyed on the main thread and the new target search is required immediately for next function
        /// or when measuring performance</remarks>
        /// <param name="forceUpdate">Schedules a job to instantly update targets instead of using chunkKeysForTargeting from the update loop</param>
        /// <param name="coldStart">Also initialize System. This enables the system to work before Start() is called. This means that you can ask the system to provide data during game object to entity conversion phase.
        /// For example to provide data for projectile authoring.</param>
        /// <returns>New list of sorted targets </returns>
        public NativeSlice<Target> GetClosestTargets(bool forceUpdate, bool coldStart)
        {
            //coldStart means the targeting system is called before Start().
            //This enables the system to work before Start() is called.
            //There is a quite rare case where this is needed. GameObject entity conversion phase and tests
            if (coldStart == true)
            {
                this.InitializeTargetingSystem(new List<GeometryType>());
                this.TargetingSystemsRunner.CheckSceneChangesAndInitializeTargetingData();
            }

            if (forceUpdate)
            {
                this.closestTargets = this.GetAndGenerateClosetsTargetsArray();
            }

            return this.closestTargets;
        }

        private NativeSlice<Target> GetAndGenerateClosetsTargetsArray()
        {
            this.UpdateTargetingData();

            var sortJob = new TargetingVisibilityJobs.SortTargetsSliceJob
            {
                newClosestTargets = this.closestTargets,
                favorDistanceToCameraInsteadDistanceToPointer = false,
                comparerToViewDirection = new TargetingSystemComparers.DistanceComparerToViewDirection()
            };

            sortJob.Run();
            return sortJob.newClosestTargets;
        }

        private void UpdateTargetingData()
        {
            this.TargetingSystemsRunner.RunTargetingSystems(this);
        }

        /// <summary>
        /// Makes a new slice of the targets container containing currently seen targets and then sorts them.
        /// </summary>
        /// <remarks>You need to call returnValue.Complete() to apply the jobs</remarks>
        internal JobHandle UpdateTargetsSlice(JobHandle jobHandle)
        {
            this.closestTargets = new NativeSlice<Target>(this.closestTargetsContainer, 0, this.amountOfTargets);

            var sortJob = new TargetingVisibilityJobs.SortTargetsSliceJob
            {
                newClosestTargets = this.closestTargets,
                favorDistanceToCameraInsteadDistanceToPointer = false,
                comparerToViewDirection = new TargetingSystemComparers.DistanceComparerToViewDirection(),
            };

            jobHandle = sortJob.Schedule(jobHandle);

            return jobHandle;
        }

        /// <summary>
        /// Instantly schedules update job for both entities and/or GameObject.
        /// </summary>
        /// <remarks>Normally update is done every frame so manually calling this at the middle of frame might impact performance.
        /// Case where this might be needed is when something is destroyed on the main thread and the new target search is required immediately for next function
        /// or when measuring performance</remarks>
        /// <param name="updateGameObjects">Should game objects be included?</param>
        /// <param name="updateEntities">Should entities be included?</param>
        internal JobHandle UpdateClosestTargets(bool updateGameObjects, bool updateEntities)
        {
            var jobHandle = new JobHandle();

            this.ClosestTargetsContainer = this.GetTargetsForGameObjectsAndEntities(this.ClosestTargetsContainer, updateGameObjects, updateEntities);
            return jobHandle;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public List<Type> GetQueryComponentsFromTargetingInstructions(List<Type> filterTypes)
        {
            foreach (var targetingInstruction in this.TargetingInstructions)
            {
                filterTypes.Add(targetingInstruction.EntityFilterComponentType);
            }

            return filterTypes;
        }


        /// <summary>
        /// Use to get list of targets containing data from entities and gameObjects. 
        /// </summary>
        /// <returns>List of target objects that can be used to find out closest target.</returns>
        private NativeList<Target> GetTargetsForGameObjectsAndEntities(NativeList<Target> closestTargetsContainerIn,
            bool updateGameObjects,
            bool updateEntities)
        {
            this.usedTags.Clear();
            this.usedGeoTypes.Clear();
            for (var index = 0; index < this.TargetingInstructions.Count; index++)
            {
                var targetingInstruction = this.TargetingInstructions[index];
                if (targetingInstruction.IsTargetingEnabled == false)
                {
                    continue;
                }
                FetchGameObjectTargets(closestTargetsContainerIn, updateGameObjects, targetingInstruction, index);
            }
            
            return closestTargetsContainerIn;

            //
            // Local functions//
            // TODO:Refactor this
            void FetchGameObjectTargets(NativeList<Target> closestTargetsIn, bool updateGameObjects,
                TargetingInstruction targetingInstruction, int index)
            {
                if (this.GameObjectBasedProcessing.Value == true
                    && (this.usedTags.Contains(targetingInstruction.TargetTag) == false ||
                        this.usedGeoTypes.Contains(targetingInstruction.GeometryType) == false))
                {
                    this.AmountOfTargets += GetTargetsFromGameObjects(closestTargetsIn, updateGameObjects,
                        targetingInstruction, index);
                    this.usedTags.Add(targetingInstruction.TargetTag);
                    this.usedGeoTypes.Add(targetingInstruction.GeometryType);
                }
            }
            
            int GetTargetsFromGameObjects(NativeList<Target> closestTargetsIn1, bool updateGameObjectsIn,
                TargetingInstruction targetingInstruction, int index)
            {
                int gameObjectsCount = 0;
                if (updateGameObjectsIn == true)
                {
                    var listOfTargets = GetGameObjectTargets(targetingInstruction, index);
                    gameObjectsCount = AddGameObjectTargetsToClosestTargets(listOfTargets, closestTargetsIn1);
                    
                }

                return gameObjectsCount;
            }


            //Runs the gameObject implementation of the ITargetProcessor interface 
            NativeList<Target> GetGameObjectTargets(TargetingInstruction targetingInstruction, int index)
            {
                this.targetingSystemMemory.GameObjectProcessorTargets = new NativeList<Target>(Allocator.TempJob);
                if (this.gameObjectProcessing.Value == true &&
                    targetingInstruction.TargetProcessorForGameObjects != null)
                {
                    return targetingInstruction.TargetProcessorForGameObjects.GetTargets(this.cachedTransform.position,
                        this.CalculateTargetingSystemDirectionInWorldSpace(), this, targetingInstruction);
                }
                else if (targetingInstruction.TargetProcessorForGameObjects == null)
                {
                    this.RefreshInstructions(true);
                    this.TryFixMissingGameObjectTargetProcessor(targetingInstruction, index);
                }
                return new NativeList<Target>(Allocator.Temp);
            }

            int AddGameObjectTargetsToClosestTargets(NativeList<Target> listOfTargets, NativeList<Target> closestTargetsIn1)
            {
                int gameObjectsCount;
                var targetsFromGameObjects = new NativeArray<Target>(listOfTargets, Allocator.TempJob);

                gameObjectsCount = targetsFromGameObjects.Length;

                TargetingSystemUtilities.AddSpaceToContainerIfNeeded(this.AmountOfTargets + gameObjectsCount, closestTargetsIn1);
                TargetingSystemUtilities.AddTargetsToContainer(closestTargetsIn1, targetsFromGameObjects, this.AmountOfTargets)
                    .Complete();
                targetsFromGameObjects.Dispose();
                this.targetingSystemMemory.GameObjectProcessorTargets.Dispose();
                
                return gameObjectsCount;
            }
        }

        private void TryFixMissingGameObjectTargetProcessor(TargetingInstruction targetingInstructionIn, int index)
        {
            var targetProcessorFromInstructions =
                InterfaceUtilities.GetGameObjectTargetProcessorFromTargetingInstructionsList(this.TargetingInstructions,
                    targetingInstructionIn);
            if (targetProcessorFromInstructions != null)
            {
                targetingInstructionIn.TargetProcessorForGameObjects = targetProcessorFromInstructions;
            }
            else
            {
                if (targetingInstructionIn.GeometryType == GeometryType.Objects)
                {
                    targetingInstructionIn.TargetProcessorForGameObjects = new GameObjectTargetProcessor();
                }
            }
        }

        /// <summary>
        /// Uses given targets position to calculate targeting data from this systems point of view for single target.
        /// Use case: You can have 1 system that calculates a target list and from that take 1 target and recalculate from another point of view
        /// This is used in a turret example project, where 1 system provides targets for multiple turrets, which then recalculate the targeting data from another point of view.
        /// In the turret example this target is used then to provide lock on confirmation (turret is looking close enough).
        /// Another way would be to get closest target from both systems, but this would be highly inefficient for many turrets.
        /// </summary>
        /// <param name="target">target to get data for</param>
        /// <param name="sqrt">square root results</param>
        /// <returns>target containing the the calculated data</returns>
        public Target GetTargetDataForTarget(Target target, bool sqrt)
        {
            if (sqrt)
            {
                return TargetingSystemUtilities.GetTargetingDataForTarget(target,
                    new float4(this.cachedTransform.position, 1),
                    new float4(this.CalculateTargetingSystemDirectionInWorldSpace(), 1));
            }

            return TargetingSystemUtilities.GetTargetingDataForTargetNoSqrt(target,
                new float4(this.cachedTransform.position, 1),
                new float4(this.CalculateTargetingSystemDirectionInWorldSpace(), 1));
        }

        /// <summary>
        /// Moves closest target with give instructions
        /// </summary>
        /// <param name="newPosition">Position to move</param>
        /// <param name="speedMultiplier">Gives extra speed</param>
        /// <param name="distanceToStop">0 value means it will travel to target. 1 value means it will stop 1 unit before reaching destination. If distance to stop is larger than distance to travel the target will not move.</param>
        public void MoveClosestTargetToPosition(Vector3 newPosition, float speedMultiplier, float distanceToStop)
        {
            var closestTarget = this.GetClosestTarget(false);
            float movementSpeed = closestTarget.distanceFromTargetToCastOrigin * Time.deltaTime * speedMultiplier;

            //Since target component is multi threading friendly it cannot store transform, so this uses the geoInfoObject that is made for the game objects
            var geoInfo = TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(closestTarget.geoInfoHashCode,
                this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos);
            MainThreadDispatcher.StartUpdateMicroCoroutine(
                TargetingSystemUtilities.MoveTarget(geoInfo.transform, newPosition, movementSpeed, distanceToStop));
        
        }

        /// <summary>
        /// Moves closest target with give instructions
        /// </summary>
        public void MoveClosestTargetToPosition(Vector3 newPosition)
        {
            var closestTarget = this.GetClosestTarget(false);
            if (closestTarget.distanceFromTargetToCastOrigin == 0f)
            {
                return;
            }

            //Since target component is multi threading friendly it cannot store transforms, so this just uses the geoInfoObject that is made for the game objects
            var geoInfo = TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(closestTarget.geoInfoHashCode,
                this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos);
            geoInfo.transform.position = newPosition;
            
        }

        /// <summary>
        /// Function that can be used to destroy game object or entity target.
        /// </summary>
        /// <param name="target">Target wanted to be destroyed</param>
        /// <param name="time">Time after target is destroyed</param>
        public IEnumerator DestroyTargetAfter(Target target, float time)
        {
            float timer = 0;
            while (timer < time)
            {
               
                timer += Time.deltaTime;
                yield return null;
            }

            this.DestroyTarget(target);
        }

        /// <summary>
        /// Function that can be used to destroy game object or entity target.
        /// </summary>
        public void DestroyTarget(Target target)
        {

            var geoInfo = TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(target.geoInfoHashCode,
                this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos);
            Destroy(geoInfo.gameObject);
        
        }

        /// <summary>
        /// Function that can be used to destroy game object or entity target at given distance from targeting system
        /// </summary>
        /// <param name="target">Target to destroy</param>
        /// <param name="distanceToDestroyAt">Distance from target to targetProcessorSystem component, if closer then the target gets destroyed</param>
        /// <returns></returns>
        public IEnumerator DestroyTargetAtDistance(Target target, float distanceToDestroyAt, float timeOut)
        {
            var targetToBeDestroyed = target;
            if (targetToBeDestroyed.distanceFromTargetToCastOrigin == 0)
            {
                yield break;
            }

            yield return CheckDistanceAndDestroy(distanceToDestroyAt, timeOut, targetToBeDestroyed);

            this.DestroyTarget(targetToBeDestroyed);

            IEnumerator CheckDistanceAndDestroy(float distanceToDestroy, float timeOut1, Target targetToBeDestroyed1)
            {
                while (targetToBeDestroyed1.distanceFromTargetToCastOrigin > distanceToDestroy)
                {
                    targetToBeDestroyed1 =
                        TargetingSystemUtilities.GetTargetValuesFromAnotherSystem(targetToBeDestroyed1, this);

                    if (timeOut1 < 0.1f)
                    {
                        this.DestroyTarget(targetToBeDestroyed1);
                        break;
                    }

                    timeOut1 -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Function that can be used to destroy game object or entity target at given distance from targeting system
        /// </summary>
        /// <param name="target">Target to destroy</param>
        /// <param name="distanceToDestroyAt">Distance from target to targetProcessorSystem component, if closer then the target gets destroyed</param>
        /// <returns></returns>
        public IEnumerator DestroyTargetAtDistanceToObject(Target target, Transform transform,
            float distanceToDestroyAt, float timeOut)
        {
            var targetToBeDestroyed = target;

            float distanceToPosition = Vector3.Distance(transform.position, targetToBeDestroyed.position);

            while (distanceToPosition > distanceToDestroyAt)
            {
                distanceToPosition = Vector3.Distance(transform.position, targetToBeDestroyed.position);
                targetToBeDestroyed =
                    TargetingSystemUtilities.GetTargetValuesFromAnotherSystem(targetToBeDestroyed, this);

                if (timeOut < 0.1f)
                {
                    this.DestroyTarget(targetToBeDestroyed);
                    break;
                }

                timeOut -= Time.deltaTime;
                yield return null;
            }

            this.DestroyTarget(targetToBeDestroyed);
        }


        /// <summary>
        /// Spawn prefabs from actions template object.
        /// </summary>
        public void TriggerTargetingActions(Target targetIn)
        {
            if (this.LastTriggerActionsCount != this.collectedTriggerActionElements.Count)
            {
                this.collectedTriggerActionElements =
                    TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                        this.targetingInstructions, this.collectedTriggerActionElements, this);
            }

            if (targetIn.isEntity == TargetingSystemDataModels.Boolean.False)
            {
                TriggerGameObjectTargetingActionsFromTargetingInstructions(this.collectedTriggerActionElements);

                //
                //Locals
                //
                void TriggerGameObjectTargetingActionsFromTargetingInstructions(
                    List<ActionsTemplateTriggerActionElement> actionsTemplateTriggerActionElementsIn)
                {
                    foreach (var action in actionsTemplateTriggerActionElementsIn)
                    {
                        var filter = FilterUnwantedGameObjectTargetOut(action.TargetingInstruction);

                        if (filter == true)
                        {
                            continue;
                        }

                        this.TriggerActionAt(targetIn, action);
                    }
                }

                bool FilterUnwantedGameObjectTargetOut(TargetingInstruction targetingInstruction)
                {
                    var targetObject = TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(targetIn.geoInfoHashCode,
                        this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos);

                    if (targetObject.gameObject == null ||
                        targetObject.gameObject.CompareTag(targetingInstruction.TargetTag) == false)
                    {
                        return true;
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Triggers the targeting action at location using pooled entities
        /// </summary>  
        private void TriggerActionAt(Target targetIn, ActionsTemplateTriggerActionElement action)
        {
            if (action.SpawnAtSource)
            {
                this.StartCoroutine(
                    TimedSpawnDespawn.TimedSpawnDeSpawnQueueEntityAndGameObjectService(
                        action, this.cachedTransform, this.cachedTransform.position, targetIn,
                        this.targetingSystemsRunner));
            }

            if (action.SpawnAtTarget)
            {
                this.StartCoroutine(
                    TimedSpawnDespawn.TimedSpawnDeSpawnQueueEntityAndGameObjectService(
                        action, this.cachedTransform,
                        targetIn.position.xyz, targetIn, this.targetingSystemsRunner));
            }
        }

        /// <summary>
        /// Applies a game object tag to all targeting instructions.
        /// </summary>
        /// <param name="tagToAssign"></param>
        internal void ApplyTagToTargetingInstructions(string tagToAssign)
        {
            foreach (var targetingInstruction in this.TargetingInstructionsWithRefresh)
            {
                targetingInstruction.TargetTag = tagToAssign;
            }

            this.collectedTriggerActionElements =
                TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                    this.targetingInstructions, this.collectedTriggerActionElements, this);
        }

        /// <summary>
        /// Applies changes made to entity filter type by the user from outside
        /// 
        /// </summary>
        /// <remarks>It seems like the object type of script doesn't get saved during build, so it needs to be saved to serializable string to hold up the information
        /// about the script.</remarks>
        /// <param name="entityQueryFilter"></param>
        internal void ApplyEntityComponentFilterToTargetingInstructions(Object entityQueryFilter)
        {
            foreach (var targetingInstruction in this.targetingInstructions)
            {
                targetingInstruction.SetCurrentEntityFilterType(entityQueryFilter);
            }

            this.collectedTriggerActionElements =
                TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                    this.targetingInstructions, this.collectedTriggerActionElements, this);
        }

        /// <summary>
        /// Gets you a transform of a target object based on the hashCode inside it.
        /// </summary>
        /// <param name="geoInfoHashCode">The hashcode from target struct. Target.geoInfoHashCode</param>
        /// <returns>related gameObjects transform</returns>
        public Transform GetTransformBasedOnGeoHashCode(int4 geoInfoHashCode)
        {
            var geoInfo = this.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode().x == geoInfoHashCode.x);

            return geoInfo.transform;
        }

        /// <summary>
        /// When the camera is moved, rotated or both the frustum planes/view area that
        /// are used to filter out what objects are processed needs to be refreshes/regenerated
        /// </summary>
        /// <param name="fieldOfView"></param>
        /// <returns>void</returns>
        /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
        private void TryInitRegenerateVisionArea(float fieldOfViewIn)
        {
            if (this.HiddenUnityCamera == null)
            {
                this.HiddenUnityCamera = this.gameObject.AddComponent<Camera>();
            }

            this.fieldOfView = fieldOfViewIn;
            this.hiddenUnityCamera.fieldOfView = fieldOfViewIn;
            this.hiddenUnityCamera.nearClipPlane = this.nearDistanceOfView;
            this.hiddenUnityCamera.farClipPlane = this.TargetingDistance;
            if (this.VisDebug == null)
            {
                this.VisDebug = new TargetVisibilityDebugger();
            }

            this.VisDebug.RefreshFrustumCorners(this.hiddenUnityCamera);
            if (Application.isPlaying == false || this.targetingSystemMemory == null ||
                this.NativeMemoryInitialized == false)
            {
                return;
            }

            this.targetingSystemMemory.PlanesVertices =
                this.VisDebug.UpdatePlanesVertices(this.HiddenUnityCamera, this.targetingSystemMemory);
        }

        public TargetVisibilityDebugger VisDebug { get; private set; }
        
        /// <summary>
        /// When the camera is moved, rotated or both the frustum planes/view area that's
        /// are used to filter out what objects are processed needs to be refreshes/regenerated
        /// </summary>
        /// <param name="fieldOfView"></param>
        /// <returns>JobHandle</returns>
        /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
        internal JobHandle RegenerateVisionAreaWithJob()
        {
            if (this.VisDebug == null)
            {
                return new JobHandle();
            }
            
            if (Math.Abs(this.FieldOfView - this.targetingSystemMemory.LastFieldOfView) > 0.001 || 
                Math.Abs(this.TargetingDistance - this.targetingSystemMemory.LastTargetingDistance) > 0.001 )
            {
                this.targetingSystemMemory.LastTargetingDistance = this.TargetingDistance;
                this.targetingSystemMemory.LastFieldOfView = this.FieldOfView;
                this.VisDebug.RefreshFrustumCorners(this.hiddenUnityCamera);
                this.targetingSystemMemory.PlanesVertices = this.VisDebug.CreateVertices(this.VisDebug.frustumCornersNear, this.VisDebug.frustumCornersFar, this.targetingSystemMemory.PlanesVertices);

            }
            var localToWorldMatrix = this.transform.localToWorldMatrix;
            this.targetingSystemMemory.camera4X4m00_m01_m02_m03 = localToWorldMatrix.GetRow(0);
            this.targetingSystemMemory.camera4X4m10_m11_m12_m13 = localToWorldMatrix.GetRow(1);
            this.targetingSystemMemory.camera4X4m20_m21_m22_m23 = localToWorldMatrix.GetRow(2);

            
            var visionJobHandle = new JobHandle();

            visionJobHandle = new RegenerateVisionAreaJob()
            {
                planesIn = this.targetingSystemMemory.PlanesAsNative,
                vertices = this.targetingSystemMemory.PlanesVertices,

                m_01 = this.targetingSystemMemory.camera4X4m00_m01_m02_m03,
                m_10 = this.targetingSystemMemory.camera4X4m10_m11_m12_m13,
                m_20 = this.targetingSystemMemory.camera4X4m20_m21_m22_m23,
            }.Schedule();

            return visionJobHandle;
        }

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct RegenerateVisionAreaJob : IJob
        {
            [WriteOnly] public NativeArray<float4> planesIn;

            [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<float4> vertices;

            private float4 result;
            private float4 result1;
            private float4 result2;
            private float4 result3;

            [ReadOnly] public float4 m_01;
            [ReadOnly] public float4 m_10;
            [ReadOnly] public float4 m_20;

            public void Execute()
            {
                int i2 = 0;
                for (int i = 0; i < 6; i++, i2 += 6)
                {
                    this.result1 = MathUtilities.MultiplyPoint3x4(this.vertices[i2], this.m_01, this.m_10, this.m_20, this.result1);
                    this.result2 = MathUtilities.MultiplyPoint3x4(this.vertices[i2 + 1], this.m_01, this.m_10, this.m_20, this.result2);
                    this.result3 = MathUtilities.MultiplyPoint3x4(this.vertices[i2 + 2], this.m_01, this.m_10, this.m_20, this.result3);
                    this.result = MathUtilities.SetPlaneNormal(this.result1, this.result2, this.result3);
                    this.result.w = MathUtilities.SetPlaneDistance(this.result1, this.result);
                    this.planesIn[i] = this.result;
                }
            }
        }

        /// <summary>
        /// <para>Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.</para>
        /// </summary>
        /// <param name="a">First point in clockwise order.</param>
        /// <param name="b">Second point in clockwise order.</param>
        /// <param name="c">Third point in clockwise order.</param>
        public static Plane Set3Points(Vector3 a, Vector3 b, Vector3 c, Plane plane)
        {
            plane.normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            plane.distance = -Vector3.Dot(plane.normal, a);
            return plane;
        }

        /// <summary>
        /// Use this to remove eye game object or entity implementation.
        /// Also handles removing the MonoBehaviour component if the implementation is one 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal void RemoveVisibilityProcessor<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList<T>(ref this.targetVisibilityProcessors,
                new TargetingSystemComparers.VisibilityProcessorComparer());
        }

        /// <summary>
        /// Used to get the eye implementation for either game object or entities from hash set.
        /// </summary>
        /// <typeparam name="T">Implementation to get. If none exists return default T</typeparam>
        /// <returns></returns>
        public T GetVisibilityProcessor<T>()
        {
            return (T) InterfaceUtilities.GetInterfaceImplementationOfTypeFromList<T>(this.targetVisibilityProcessors);
        }


        /// <summary>
        /// Adds visibility processor component to the list and makes sure that the implementation to be added is unique.
        /// </summary>
        /// <typeparam name="T">Implementation ITargetVisibilityProcessor to add</typeparam>
        internal void AddGameObjectVisibilityProcessor<T>()
        {
            ITargetVisibilityProcessor implementation =
                (ITargetVisibilityProcessor) Activator.CreateInstance(typeof(T));

            if (this.TargetVisibilityProcessors == null)
            {
                this.TargetVisibilityProcessors =
                    new SortedSet<ITargetVisibilityProcessor>(
                        new TargetingSystemComparers.VisibilityProcessorComparer());
            }

            InterfaceUtilities.AddImplementation(InitVisibilityProcessor, implementation,
                this.TargetVisibilityProcessors);

            //Constructor function that gets called when adding the implementation
            ITargetVisibilityProcessor InitVisibilityProcessor(ITargetVisibilityProcessor visibilityProcessor)
            {
                visibilityProcessor.Id = this.GetHashCode();

                return visibilityProcessor;
            }
        }

        /// <summary>
        /// Gets the first targeting instruction matching the give type as GeometryType.
        /// </summary>
        /// <param name="geometryType">Targeting instruction search parameter. GeometryType to look for. Default use case is GeometryType.Objects</param>
        /// <returns></returns>
        internal TargetingInstruction GetTargetingInstructionOfType(GeometryType geometryType)
        {
            TargetingInstruction instructionToReturn = this.targetingInstructions[0];

            foreach (var instruction in this.TargetingInstructionsWithRefresh)
            {
                if ((int) instruction.GeometryType == (int) geometryType)
                {
                    instructionToReturn = instruction;
                    break;
                }
            }

            return instructionToReturn;
        }

        public int GetClosestTargetCount()
        {
            return this.closestTargets.Length;
        }

        /// <summary>
        /// Handles adding default targeting instruction with default settings to targeting instructions collection.
        /// Purpose: The targeting system needs targeting instruction to work.
        /// </summary>
        /// <param name="targetingInstructionsIn">Collection of targeting instructions to add the targeting instruction in.</param>
        /// <param name="entityBased">In case for pure entities, then put this to one true.</param>
        /// <param name="gameObjectBased">In case for pure game objects, then put this one to true.</param>
        /// <returns></returns>
        internal List<TargetingInstruction> AddDefaultTargetingInstructionIfNone(
            List<TargetingInstruction> targetingInstructionsIn, bool entityBased, bool gameObjectBased)
        {
            if (targetingInstructionsIn == null)
            {
                targetingInstructionsIn = new List<TargetingInstruction>();
            }

            if (targetingInstructionsIn.Count == 0)
            {
                GameObjectTargetProcessor objectTargetProcessor = null;

                if (gameObjectBased)
                {
                    objectTargetProcessor = new GameObjectTargetProcessor();
                }

                targetingInstructionsIn.Add(
                    new TargetingInstruction(GeometryType.Objects, TargetingSystemSettings.TriggerActionDefaultTag,
                        objectTargetProcessor, true));
            }

            return targetingInstructionsIn;
        }

        /// <summary>
        /// Updates the position to locate at closest target and if there are no visible targets hides the locator object 
        /// </summary>
        internal void UpdateTargetLocatorObjectPosition()
        {
            if (this.closestTargets.Length > 0)
            {
                this.targetingIndicatorObject.position = new Vector3(this.closestTargets[0].position.x,
                    this.closestTargets[0].position.y, this.closestTargets[0].position.z);
            }
        }

        /// <summary>
        /// Makes the target transform point at given transform
        /// </summary>
        /// <param name="transformToLookAt">Makes the locator objet to look at the given transform</param>
        internal void UpdateTargetLocatorObjectRotation(Transform transformToLookAt)
        {
            this.targetingIndicatorObject.LookAt(transformToLookAt);
        }

        /// <summary>
        /// Sets the default entity filter type for the first targeting instruction (TargetingInstructions[0])
        /// </summary>
        /// <param name="typeToSet">Type to be used in targeting search query</param>
        public void SetDefaultEntityFilter(Type typeToSet)
        {
            this.targetingInstructions[0].SetCurrentEntityFilterType(typeToSet);
        }

        /// <summary>
        /// Sets the default entity filter type for the first targeting instruction (TargetingInstructions[0])
        /// </summary>
        /// <param name="typeToSet">Type to be used in targeting search query</param>
        /// <param name="targetingInstructionToHaveNewFilterIndex"> selects targeting instruction with index</param>
        public void SettEntityFilterWithIndex(Type typeToSet, int targetingInstructionToHaveNewFilterIndex)
        {
            if (targetingInstructionToHaveNewFilterIndex >= targetingInstructions.Count)
            {
                Debug.Log("Targeting system with given index does not exist. Index = " + targetingInstructionToHaveNewFilterIndex);
                return;
            }
            this.targetingInstructions[targetingInstructionToHaveNewFilterIndex].SetCurrentEntityFilterType(typeToSet);
        }

        public int4 TargetingSystemIndex
        {
            get { return this.targetingSystemIndex; }
            set
            {
                if (this.indexIsSet == false)
                {
                    this.targetingSystemIndex = value;
                    this.indexIsSet = true;
                }
            }
        }

        internal void ForceSetTargetingSystemIndex(int4 newTargetingSystemIndex)
        {
            this.targetingSystemIndex = newTargetingSystemIndex;
        }

        public int GetCullingModeAsInt()
        {
            if (this.cullingBehaviour == TargetingSystemDataModels.CullingBehaviour.MiddleScreenSinglePointToTarget)
            {
                return 1;
            }

            return 0;
        }

        #region Drawing

#if UNITY_EDITOR

        /// <summary>
        /// Used for debugging geometry vision and is responsible for drawing debugging info from the data provided by
        /// GV_TargetingSystem plugin
        /// </summary>
        private void OnDrawGizmos()
        {
            this.InitUnityCamera();
            if (this.NativeMemoryInitialized == false)
            {
                return;
            }

            Material mat = new Material(Shader.Find("Specular"));
            int index2 = 0;

            if (this.refreshSystemState == true)
            {
                this.refreshSystemState = false;
                this.UpdateUnityCamera();

                this.ApplyEntityFilterChanges(this.targetingInstructions);

                this.targetingInstructions = this.AddDefaultTargetingInstructionIfNone(this.targetingInstructions,
                    false, this.gameObjectProcessing.Value);

                this.RefreshInstructions(false);
                if (Application.isPlaying && this.NativeMemoryInitialized && this.targetingSystemsRunner != null)
                {
                    this.SetGameObjectBasedProcessingOnOrOff(this.GameObjectBasedProcessing.Value);
                }
            }

            if (this.targetingDebugOptions.visualizationMode == TargetingSystemDataModels.VisualizationMode.None ||
                this.closestTargets.Length == 0)
            {
                return;
            }
        }
#endif
        /// <summary>
        /// Updates unity camera which provides some needed features for targeting system, like Gizmos, planes and matrices.
        /// </summary>
        private void UpdateUnityCamera()
        {
            if (this.HiddenUnityCamera)
            {
                this.HiddenUnityCamera.enabled = false;
                this.HiddenUnityCamera = this.gameObject.GetComponent<Camera>();
                this.HiddenUnityCamera.usePhysicalProperties = true;
                this.HiddenUnityCamera.aspect = 1f;

                this.HiddenUnityCamera.cameraType = CameraType.Game;
                this.HiddenUnityCamera.clearFlags = CameraClearFlags.Nothing;

                this.HiddenUnityCamera.fieldOfView = this.fieldOfView;
                this.HiddenUnityCamera.farClipPlane = this.TargetingDistance;
                this.HiddenUnityCamera.nearClipPlane = this.nearDistanceOfView;

                this.HiddenUnityCamera.enabled = false;
            }
        }

        #endregion

        private void FlushPersistentMemory()
        {
            if (this.ClosestTargetsContainer.IsCreated)
            {
                this.ClosestTargetsContainer.Dispose();
            }
            this.targetingSystemMemory.DisposePersistentNativeArrays();
            this.targetingSystemMemory = null;
        }
        
        public void SetGameObjectBasedProcessingOnOrOff(bool onOff)
        {
            if (this.factory == null)
            {
                this.InitializeTargetingSystem(new List<GeometryType>());
            }

            this.factory.ToggleGameObjectBasedSystem(onOff, this);
            this.TargetingSystemsRunner.UpdateTargetCreators();
        }


        public void ChangeTargetingInstructionTag(string newTagForTheInstruction, int targetingInstructionIndex)
        {
            this.TargetingInstructionsWithRefresh[targetingInstructionIndex].TargetTag = newTagForTheInstruction;
        }

        public void AddTargetingInstruction(TargetingInstruction targetingInstruction)
        {
            if (targetingInstruction.TargetProcessorEntities == null || targetingInstruction.TargetProcessorForGameObjects == null)
            {
                Debug.Log("You are trying to add targeting instruction without targeting processor. The targeting instruction will not provide any targets.");
            }
            this.TargetingInstructionsWithRefresh.Add(targetingInstruction);
            
            this.RefreshInstructions(true);
        }

        public void RemoveTargetingInstruction(TargetingInstruction targetingInstruction)
        {
            this.TargetingInstructionsWithRefresh.Remove(targetingInstruction);

            this.RefreshInstructions(true);
        }

        private void RefreshInstructions(bool forceRefresh)
        {
            TargetingSystemUtilities.ApplySystemToTargetingInstructions(this, this.targetingInstructions);
            if (this.collectedTriggerActionElements.Count != this.LastTriggerActionsCount || forceRefresh)
            {
                this.LastTriggerActionsCount = this.collectedTriggerActionElements.Count;
                this.collectedTriggerActionElements =
                    TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                        this.targetingInstructions, this.collectedTriggerActionElements, this);

            }

            if (this.targetingSystemsRunner != null)
            {
                foreach (var targetCreator in this.targetingSystemsRunner.TargetCreators)
                {
                    targetCreator.ScheduleUpdate();
                }
            }
            
            this.targetingInstructions = TargetingSystemUtilities.ValidateTargetingInstructions(
                this.targetingInstructions, this.gameObjectProcessing.Value, false);
            if (this.TargetingSystemsRunner != null)
            {
                this.TargetingSystemsRunner.UpdateTargetCreators();
            }
        }

        internal void QuickerRefreshInstructions()
        {
            TargetingSystemUtilities.ApplySystemToTargetingInstructions(this, this.targetingInstructions);

            this.collectedTriggerActionElements =
                TargetingSystemUtilities.CollectGameObjectTargetingActionsFromTargetingInstructions(
                    this.targetingInstructions, this.collectedTriggerActionElements, this);

            this.targetingInstructions = TargetingSystemUtilities.ValidateTargetingInstructions(
                this.targetingInstructions, this.gameObjectProcessing.Value, false);
        }

        public void ChangeTargetingInstructionTagWithGeometryType(GeometryType geometryType, string newTag,
            string oldTag)
        {
            try
            {
                this.targetingInstructions.First(instruction =>
                    instruction.GeometryType == geometryType && instruction.TargetTag == oldTag).TargetTag = newTag;
            }
            catch (Exception e)
            {
                Debug.Log("tag not found from targeting instructions, new tag: " + newTag + " old tag: " + oldTag);
            }
        }

        public List<GeometryType> GetGeometryTypesTargeted()
        {
            HashSet<GeometryType> geoTypes = new HashSet<GeometryType>();
            this.targetingInstructions.RemoveAll(item => item == null);
            foreach (var targetingInstruction in this.targetingInstructions)
            {
                geoTypes.Add(targetingInstruction.GeometryType);
            }

            return geoTypes.ToList();
        }
    }
}