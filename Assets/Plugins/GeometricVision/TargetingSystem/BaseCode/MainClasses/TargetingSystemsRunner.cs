// Copyright © 2020-2022 Mikael Korpinen (Finland). All Rights Reserved.
using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Audio;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses
{
    /// <summary>
    /// Contains all the added GV_TargetingSystem components and TargetCreators.
    /// Code is run every frame updating all the required sub systems that makes the getting targets possible.
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetingSystemsRunner : MonoBehaviour
    {
        #region FieldDeclarations

        [SerializeField,
         Tooltip(
             "How many frames to skip until a new targeting data update. Higher numbers will result in less updates, but also more gives a speed boost.")]
        private int skipFrames = 0;

        [SerializeField, Range(0,10000), Tooltip("How many audio sources are supported when effect are triggered. 0 will disable audio pool creation.")]
        private int audioPoolSize = TargetingSystemSettings.DefaultAudioPoolSize;

        private AudioPoolManager audioPoolManager = null;

        [SerializeField, Tooltip("Do not log editor only console warnings(I know what I'm doing).")]
        internal bool suppressEditorWarnings = false;

        private HashSet<GV_TargetingSystem> targetingSystems;

        private SortedSet<ITargetCreator> targetCreators =
            new SortedSet<ITargetCreator>(new TargetingSystemComparers.TargetCreatorComparer());

#if TARGETING_SYSTEM_DEBUG
        [SerializeField]
        private int targetingSystemIndexToDebug;
#endif

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
        [Header("Global entity processing settings (When editing from editor configure before play mode).")]
        [SerializeField, Tooltip("Size of the buffer affects memory usage and speed.")]
        private int maximumAmountOfTargetsInBuffer = 15000;

        [SerializeField, Tooltip("Size of the buffer affects memory usage and speed.")]
        private int maximumAmountOfChunks = 600;

        [SerializeField, Tooltip("Size of the groups/area where targets are divided. System checks, if it can see that chunk and access the targets if it does. Configuring the chunk size according to the scene size can improve performance.")]
        private int chunkSize = 300;
        [SerializeField, Tooltip("Each targeting system saves targets inside their managed component. In scenarios where targeting systems will not see the entire scene. Just hand full of entities. It might be good idea to limit the size of target containers to save memory")]
        private int targetingSystemTargetContainerSize = 15000;

        public SharedEntityMemory SharedEntityMemory { get; set; } = null;

        [SerializeField, Tooltip("In case the entity being targeted doesn't use Translation component for positioning, then this can be used to switch targeting to use local to world component instead. This setting needs to be changed before new targets are spawned.")]
        public bool useLocalToWorld = true;


        public EntityTargetVisibilityProcessor EntityVisibilityProcessor { get; internal set; } = null;
#endif
        private int framesCounter = 0;
        private TargetingSystemSharedMemory sharedTargetingMemory = new TargetingSystemSharedMemory();
#if TARGETING_SYSTEM_GEOMETRY_BASED_TARGETING
        [Header("Line/Edge based targeting settings")] [SerializeField, Tooltip("Normally geometry is cached, but in case shape is changed during runtime the mesh needs to be updated in order to get accurate results.")]
        internal bool updateGeometryEveryFrame = false;

        [SerializeField,
         Tooltip(
             "Can in some cases improve speed and memory at the cost of larger update times. Normally triangles/quads share some edges. This will remove those extra edges, since they are not needed for targeting. " +
             "Higher values will reduce amount of edges considered for targeting. Best to be used with update geometryEveryFrame set to false.")]
        internal bool filterDuplicateEdgesOut = false;
        
        [SerializeField, Range(0f, 1f),
         Tooltip(
             "Requires filter duplicate edges on. Raising the value will reduce the amount of edges to be processed at the cost of accuracy, lower values will filter duplicate edges more precisely.")]
        internal float minVertexOverlapDistanceThreshold = 0.0000001f;
        
        [SerializeField, Tooltip(
             "This option can be used to target user given low poly collider from the collider component. Improves performance and memory.")]
        internal bool prioritizeColliders = true;
#endif
        private NativeList<JobHandle> cameraJobHandles;
        private JobHandle sortHandle = new JobHandle();
        #endregion

        #region Initialize

        private void Awake()
        {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            this.SharedEntityMemory = new SharedEntityMemory();
#endif
            this.cameraJobHandles = new NativeList<JobHandle>(1024, Allocator.Persistent);
            this.gameObject.layer = TargetingSystemSettings.IgnoreRayCastLayer;
            this.targetCreators = new SortedSet<ITargetCreator>(new TargetingSystemComparers.TargetCreatorComparer());
            this.targetingSystems = new HashSet<GV_TargetingSystem>();
#if TARGETING_SYSTEM_DEBUG
            targetingSystemIndexToDebug = TargetingSystemSettings.DebugLoggingTargetingSystemIndex;
#endif
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            TargetingSystemSettings.MaxTargets = this.maximumAmountOfTargetsInBuffer;
            TargetingSystemSettings.MaxChunks = this.maximumAmountOfChunks;
            TargetingSystemSettings.TargetChunkSize = this.chunkSize;
            TargetingSystemSettings.MaxTargetsPerSystem = this.targetingSystemTargetContainerSize;
#endif
        }

        /// <summary>
        /// Builds up the targeting system audio pool from scratch
        /// </summary>
        /// <param name="audioPoolSizeIn"></param>
        public void InitAudioPoolingSystem(int audioPoolSizeIn)
        {
            this.AudioPoolSize = audioPoolSizeIn;
            if (this.audioPoolManager == null)
            {
                this.audioPoolManager = new AudioPoolManager();
            }

            this.audioPoolManager.ResizeAudioPool(audioPoolSizeIn, this);
            this.audioPoolManager.runner = this;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            this.AddEntityTargetCreator<EntityTargetCreator>(World.DefaultGameObjectInjectionWorld);
#endif
        }

        private void Start()
        {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT

            this.SharedEntityMemory.Init();
#endif
            this.InitAudioPoolingSystem(this.audioPoolSize);
        }


        private void OnValidate()
        {
#if TARGETING_SYSTEM_DEBUG
            TargetingSystemSettings.DebugLoggingTargetingSystemIndex = this.targetingSystemIndexToDebug;
#endif
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            TargetingSystemSettings.MaxTargets = this.maximumAmountOfTargetsInBuffer;
            TargetingSystemSettings.MaxChunks = this.maximumAmountOfChunks;
            TargetingSystemSettings.TargetChunkSize = this.chunkSize;
            TargetingSystemSettings.MaxTargetsPerSystem = this.targetingSystemTargetContainerSize;
#endif
        }

        void Reset()
        {
            this.targetCreators = new SortedSet<ITargetCreator>(new TargetingSystemComparers.TargetCreatorComparer());
        }

        #endregion

        #region Destroy

        private void OnDestroy()
        {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            this.RemoveEntityTargetCreator<EntityTargetCreator>();
            this.SharedEntityMemory.Flush();
            this.SharedEntityMemory = new SharedEntityMemory();
            this.cameraJobHandles.Dispose();
#endif
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
            this.audioPoolManager.FlushAudioPool(this, true);
            this.sharedTargetingMemory.DisposeNatives();
        }

        #endregion

        #region Update Targeting Data

        public void Update()
        {
            this.framesCounter++;
            if (this.framesCounter < this.skipFrames || this.targetingSystems == null)
            {
                return;
            }
            Profiler.BeginSample("Camera Frustum Update");
            if (this.cameraJobHandles.IsCreated)
            {
                JobHandle.CompleteAll(this.cameraJobHandles);
            }
            Profiler.EndSample();
            this.framesCounter = 0;
            this.InitVariables();
            this.RunTargetingSystems();
            this.UpdateAndSortTargetSlices(this.targetingSystems, new JobHandle()).Complete();
            Profiler.BeginSample("Sort targets");
            this.sortHandle.Complete();
            Profiler.EndSample();
            this.UpdateTargetingIndicators();
        }

        private void LateUpdate()
        {
            this.framesCounter++;
            if (this.framesCounter < this.skipFrames || this.targetingSystems == null)
            {
                return;
            }

            //Wait for sort targets job at the end of the frame
            this.cameraJobHandles.Clear();
            this.cameraJobHandles = this.UpdateFrustumViews(this.cameraJobHandles);
            JobHandle.CompleteAll(this.cameraJobHandles);
            this.cameraJobHandles.Clear();
        }

        void InitVariables()
        {
            foreach (var targetingSystem in this.targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    //We still need to clear some variables so when asking for results from the system it returns empty
                    ResetTargetCountAndBooleans(targetingSystem);
                    continue;
                }

                this.InitVariablesForSystem(targetingSystem);
            }
        }

        void InitVariablesForSystem(GV_TargetingSystem targetingSystem)
        {
            ResetTargetCountAndBooleans(targetingSystem);
        }
        
        private static void ResetTargetCountAndBooleans(GV_TargetingSystem targetingSystem)
        {
            targetingSystem.MostDistantTargetFetchedForCurrentFrame = false;
            targetingSystem.ClosestTargetFetchedForCurrentFrame = false;
            targetingSystem.AmountOfTargets = 0;
        }

        /// <summary>
        /// Updates all targeting systems and their targets
        /// </summary>
        private void RunTargetingSystems()
        {
            Profiler.BeginSample("Check scene changes");
            this.CheckSceneChangesAndInitializeTargetingData();
            Profiler.EndSample();
            this.SetVisibilityOfTargetsForSystems();
            this.UpdateTargetingData(this.targetingSystems);


        }

        /// <summary>
        /// Runs some code for all the systems and some code for the given system.
        /// The result is fetched instantly unlike the the one running on the Update.
        /// If you only need to update one system, then this might be faster.
        /// Accessing already existing target by calling GetClosestTarget(forceUpdate: false) is still fastest.
        /// </summary>
        /// <param name="targetingSystemIn"></param>
        internal void RunTargetingSystems(GV_TargetingSystem targetingSystemIn)
        {
            if (this.targetingSystems == null)
            {
                //TODO:Reinitialize all systems on script reload
                return;
            }

            this.cameraJobHandles = this.UpdateFrustumViews(this.cameraJobHandles);
            JobHandle.CompleteAll(this.cameraJobHandles);
            this.InitVariablesForSystem(targetingSystemIn);
            this.CheckSceneChangesAndInitializeTargetingData();
            this.SetVisibilityOfTargetsForSystem(targetingSystemIn);
            this.UpdateTargetingDataForSystem(targetingSystemIn, new JobHandle()).Complete();
            targetingSystemIn.UpdateTargetsSlice(new JobHandle()).Complete();
            this.RunActionOnSystems(this.UpdateTargetingIndicator);
        }

        private void RunActionOnSystems(Func<GV_TargetingSystem, JobHandle> actionToInvoke)
        {
            foreach (var targetingSystem in this.targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                actionToInvoke(targetingSystem);
            }
        }

        private void UpdateTargetingIndicators()
        {
            foreach (var targetingSystem in this.targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                this.UpdateTargetingIndicator(targetingSystem);
            }
        }


        void SetVisibilityOfTargetsForSystems()
        {
            foreach (var targetingSystem in this.targetingSystems)
            {
                this.SetVisibilityOfTargetsForSystem(targetingSystem);
            }
        }

        private NativeList<JobHandle> UpdateFrustumViews(NativeList<JobHandle> nativeArray)
        {
            foreach (var targetingSystem in this.targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                var handle = targetingSystem.RegenerateVisionAreaWithJob();
                nativeArray.Add(handle);
            }

            return nativeArray;
        }

        private void SetVisibilityOfTargetsForSystem(GV_TargetingSystem targetingSystem)
        {
            if (targetingSystem.GameObjectBasedProcessing.Value == false)
            {
                return;
            }

            this.UpdateGameObjectVisibilities(targetingSystem, false);
        }

        private void UpdateTargetingData(HashSet<GV_TargetingSystem> targetingSystemsIn)
        {

            foreach (var targetingSystem in targetingSystemsIn)
            {
                this.UpdateTargetingDataForSystem(targetingSystem, new JobHandle());
            }
        }

        private JobHandle UpdateTargetingDataForSystem(GV_TargetingSystem gvTargetingSystem, JobHandle newJobHandle)
        {
            newJobHandle = this.GetTargetingDataForTargets(gvTargetingSystem);

            return newJobHandle;
        }

        /// <summary>
        /// Creates a slice of the main targets container and sorts the content inside the slice
        /// </summary>
        /// <param name="targetingSystems"></param>
        /// <param name="newJobHandle"></param>
        /// <returns></returns>
        private JobHandle UpdateAndSortTargetSlices(HashSet<GV_TargetingSystem> targetingSystems, JobHandle newJobHandle)
        {
            var newHandle = new JobHandle();
            foreach (var targetingSystem in targetingSystems)
            {
                newJobHandle = targetingSystem.UpdateTargetsSlice(newHandle);
            }

            return newJobHandle;
        }

        /// <summary>
        /// Creates required data for systems visibility processors.
        /// </summary>
        internal void CheckSceneChangesAndInitializeTargetingData()
        {
            foreach (var targetCreator in this.TargetCreators)
            {
                targetCreator.CheckSceneChanges();
            }
        }

        /// <summary>
        /// Creates required data for systems visibility processors.
        /// </summary>
        internal void CheckSceneChangesAndInitializeTargetingData(bool forceUpdate)
        {
            foreach (var targetCreator in this.TargetCreators)
            {
                targetCreator.CheckSceneChanges();
                if (forceUpdate)
                {
                    targetCreator.ScheduleUpdate();
                }
            }
        }

        void UpdateGameObjectVisibilities(GV_TargetingSystem targetingSystem, bool forceUpdate)
        {
            if (targetingSystem.GameObjectBasedProcessing.Value == false)
            {
                return;
            }

            targetingSystem.SeenGeometryInfos.Clear();


            foreach (ITargetVisibilityProcessor visibilityProcessor in targetingSystem.TargetVisibilityProcessors)
            {
                if (visibilityProcessor.IsEntityBased() == true)
                {
                    continue;
                }

                visibilityProcessor.UpdateVisibility(targetingSystem, forceUpdate);

                if (targetingSystem.TargetingDebugOptions.visualizationMode !=
                    TargetingSystemDataModels.VisualizationMode.None && Application.isPlaying)
                {
                    targetingSystem.VisDebug.Debug(visibilityProcessor, targetingSystem);
                }
            }
        }

        internal JobHandle UpdateTargetingIndicator(GV_TargetingSystem targetingSystem)
        {
            if (targetingSystem.UpdateTargetingIndicator)
            {
                targetingSystem.UpdateTargetLocatorObjectPosition();
                targetingSystem.UpdateTargetLocatorObjectRotation(targetingSystem.transform);
                var target = targetingSystem.GetClosestTarget(false);

                if (target.Exists() == TargetingSystemDataModels.Boolean.False)
                {
                    targetingSystem.TargetingIndicatorObject.gameObject.SetActive(false);
                }
                else if (MathUtilities.Float3Distance(target.projectedTargetPosition, target.position).x <
                         targetingSystem.IndicatorVisibilityDistance)
                {
                    targetingSystem.TargetingIndicatorObject.gameObject.SetActive(true);
                }
                else
                {
                    targetingSystem.TargetingIndicatorObject.gameObject.SetActive(false);
                }
            }

            return new JobHandle();
        }

        private JobHandle GetTargetingDataForTargets(GV_TargetingSystem gvTargetingSystem)
        {
            return gvTargetingSystem.UpdateClosestTargets(gvTargetingSystem.GameObjectBasedProcessing.Value,
                false);
        }

        /// <summary>
        /// Gets the processor of given type from the runner.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetTargetCreator<T>() where T : ITargetCreator
        {
            return InterfaceUtilities.GetInterfaceImplementationOfTypeFromList<T>(this.TargetCreators);
        }

        public void RemoveGameObjectTargetCreator<T>()
        {
            InterfaceUtilities.RemoveInterfaceImplementationsOfTypeFromList<T>(ref this.targetCreators,
                new TargetingSystemComparers.TargetCreatorComparer());
        }

        #endregion

        #region Getters and setters

        public TargetingSystemSharedMemory SharedTargetingMemory
        {
            get { return this.sharedTargetingMemory; }
            set { this.sharedTargetingMemory = value; }
        }

        public HashSet<GV_TargetingSystem> TargetingSystems
        {
            get { return this.targetingSystems; }

            set { this.targetingSystems = value; }
        }

        public SortedSet<ITargetCreator> TargetCreators
        {
            get { return this.targetCreators; }
        }


        public AudioPoolManager AudioPoolManager
        {
            get { return this.audioPoolManager; }
            set { this.audioPoolManager = value; }
        }

        public int AudioPoolSize
        {
            get { return this.audioPoolSize; }
            set { this.audioPoolSize = value; }
        }

#if TARGETING_SYSTEM_GEOMETRY_BASED_TARGETING

        public bool UpdateGeometryEveryFrame
        {
            get { return this.updateGeometryEveryFrame; }
            set { this.updateGeometryEveryFrame = value; }
        }
#endif
        public void AddGameObjectTargetCreator<T>() where T : ITargetCreator
        {
            ITargetCreator targetCreator = Activator.CreateInstance<T>();
            targetCreator.SetRunner(this);
            InterfaceUtilities.AddImplementation(targetCreator, this.TargetCreators);
        }

        #endregion

        public void UpdateTargetCreators()
        {
            foreach (var targetCreator in this.TargetCreators)
            {
                targetCreator.ScheduleUpdate();
            }
        }
    }
}