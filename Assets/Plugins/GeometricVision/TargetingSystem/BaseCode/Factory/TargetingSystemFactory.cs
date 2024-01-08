using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Factory
{
    public class TargetingSystemFactory
    {
        private TargetingSystemDataModels.FactorySettings settings;
        internal GV_TargetingSystem TargetingSystem { get; set; }

        public TargetingSystemFactory(TargetingSystemDataModels.FactorySettings settings)
        {
            this.settings = settings;
            if (settings.defaultTag != null && settings.defaultTag.Length > 0)
            {
                this.settings.defaultTag = settings.defaultTag;
            }
            else
            {
                this.settings.defaultTag = "Untagged";
            }

            this.settings.entityComponentQueryFilter = settings.entityComponentQueryFilter;
            this.settings.processGameObjects = settings.processGameObjects;
            this.settings.processEntities = settings.processEntities;
            this.settings.audioPoolSize = settings.audioPoolSize;
            this.settings.UseLocalToWorld = false;
        }

        public TargetingSystemFactory()
        {
        }

        public TargetingSystemDataModels.FactorySettings Settings
        {
            get { return this.settings; }
            set { this.settings = value; }
        }

        public void CreateTargetingSystemRunner(GV_TargetingSystem targetingSystem)
        {
            var targetingSystemGoRunnerGo = CreateTargetingSystemRunnerGameObject();
            this.CreateTargetingSystemRunnerFromFactory(targetingSystemGoRunnerGo, targetingSystem);
        }

        /// <summary>
        /// Factory method for building up Geometric vision plugin on a scene via code
        /// </summary>
        /// <remarks>By default GeometryType to use is objects since plugin needs that in order to work</remarks>
        /// <param name="startingPosition"></param>
        /// <param name="rotation"></param>
        /// <param name="geoTypes"></param>
        /// <param name="debugModeEnabled"></param>
        /// <returns></returns>
        public GameObject CreateTargetingSystemFromOutside(Vector3 startingPosition, Quaternion rotation,
            List<GeometryType> geoTypes, bool debugModeEnabled)
        {
            var targetingSystem = this.CreateTargetingSystemComponent(new GameObject(), debugModeEnabled, geoTypes);
            targetingSystem.InitPersistentNativeArrays();
            this.AddFactorySettingsToTargetingSystem(geoTypes, targetingSystem,
                this.settings); //Runs the init again but parametrized
            var geometryVision = targetingSystem.gameObject;
            var transform = geometryVision.transform;
            transform.position = startingPosition;
            transform.rotation = rotation;

            return geometryVision;
        }
        /// <summary>
        /// Factory method for building up targeting for different geometry types
        /// </summary>
        /// <param name="targetingSystem"></param>
        /// <param name="geoTypes"></param>
        internal void AddAdditionalGeometryTypeTargetProcessors(GV_TargetingSystem targetingSystem, List<GeometryType> geoTypes)
        {
            var targetingProcessors = this.MakeTargetingSystems();

            CreateTargetingInstructions();
            this.TryCreateVisibilityProcessors(geoTypes, targetingSystem);
            
            ///
            /// Locals
            ///
            
            void CreateTargetingInstructions()
            {
                foreach (var geoType in geoTypes)
                {
                    CreateInstructionForGeometryType(geoType);
                }
            }


            void CreateInstructionForGeometryType(GeometryType geoType)
            {
                if (geoType == GeometryType.Objects)
                {
                    var targetingInstruction = new TargetingInstruction(GeometryType.Objects, this.settings.defaultTag,
                        targetingProcessors.Item4, true);
                    targetingSystem.TargetingInstructions.Add(targetingInstruction);
                }

                if (geoType == GeometryType.Lines)
                {
                    var targetingInstruction = new TargetingInstruction(GeometryType.Lines, this.settings.defaultTag,
                        targetingProcessors.Item5, true);
                    targetingSystem.TargetingInstructions.Add(targetingInstruction);
                }

                /*if (geoType == GeometryType.Vertices)
                {
                    var targetingInstruction = new TargetingInstruction(GeometryType.Vertices, this.settings.defaultTag,
                        targetingProcessors.Item6, true);
                    targetingSystem.TargetingInstructions.Add(targetingInstruction);
                    targetingSystem.UseBounds = true;
                }*/
            }

        }
        void TryCreateVisibilityProcessors(List<GeometryType> geoTypes, GV_TargetingSystem targetingSystem)
        {
            foreach (var geoType in geoTypes)
            {
                CreateVisibilityProcessor(geoType);
            }

            void CreateVisibilityProcessor(GeometryType geoType)
            {
                var targetVisibilityProcessor =
                    targetingSystem.GetVisibilityProcessor<GameObjectTargetVisibilityProcessor>();
                if (targetVisibilityProcessor == null)
                {
                    targetingSystem.AddGameObjectVisibilityProcessor<GameObjectTargetVisibilityProcessor>();
                }

            }
        }
        
        //Local function that makes all the possible targeting systems
        (ITargetProcessor, ITargetProcessor, ITargetProcessor, ITargetProcessor, ITargetProcessor, ITargetProcessor )
            MakeTargetingSystems()
        {

            return (null, null,
                null, new GameObjectTargetProcessor(), null, null);
        }
        private static GameObject CreateTargetingSystemRunnerGameObject()
        {
            GameObject targetingSystemRunner = GameObject.Find(TargetingSystemSettings.RunnerName);

            if (targetingSystemRunner == null)
            {
                targetingSystemRunner = new GameObject(TargetingSystemSettings.RunnerName);
            }

            targetingSystemRunner.transform.position = TargetingSystemSettings.DefaultTransformLocation;
            return targetingSystemRunner;
        }

        private GV_TargetingSystem CreateTargetingSystemComponent(GameObject targetingSystem, bool debugModeEnabled,
            List<GeometryType> geoTypes)
        {
            var targetingSystemComponent = targetingSystem.AddComponent<GV_TargetingSystem>();
            //Init Unity camera is separated from InitializeTargetingSystem, since it cannot be called OnValidate or OnAwake
            targetingSystemComponent.InitUnityCamera();
            return targetingSystemComponent;
        }

        internal void AddFactorySettingsToTargetingSystem(List<GeometryType> geometryTypes,
            GV_TargetingSystem targetingSystem, TargetingSystemDataModels.FactorySettings settings)
        {
            StartFactory(targetingSystem, settings);

            //
            //Locals
            //

            //Runs all the required procedures for initializing targeting system with factory
            //Also add geometry types given as parameter to targeting system
            void StartFactory(GV_TargetingSystem targetingSystemIn,
                TargetingSystemDataModels.FactorySettings settingsIn)
            {
                this.Settings = settingsIn;

                if (geometryTypes != null)
                {
                    this.AddAdditionalGeometryTypeTargetProcessors(targetingSystemIn, geometryTypes);
                }
                
                targetingSystemIn.SetGameObjectBasedProcessingOnOrOff(this.Settings.processGameObjects);
                if (settings.fieldOfView != 0)
                {
                    targetingSystemIn.FieldOfView = settings.fieldOfView;
                    targetingSystemIn.TargetingDistance = settings.targetingDistance;
                    if (targetingSystemIn.TargetingDistance == 0)
                    {
                        targetingSystemIn.TargetingDistance = TargetingSystemSettings.DefaultTargetingDistance;
                    }
                }

                targetingSystemIn.ApplyTagToTargetingInstructions(settings.defaultTag);
                targetingSystemIn.ApplyEntityComponentFilterToTargetingInstructions(settings
                    .entityComponentQueryFilter);
                if (settings.TargetingActionsTemplateObject != null)
                {
                    targetingSystemIn.ApplyActionsTemplateObject(settings.TargetingActionsTemplateObject);
                }
                
                targetingSystemIn.CullingBehaviour = settings.cullingBehavior;
                targetingSystemIn.TargetingDebugOptions.visualizationMode = settingsIn.VisualizationMode;
                targetingSystemIn.TargetingSystemsRunner.AudioPoolSize = settingsIn.audioPoolSize;
            }
        }
        
        // Handles target initialization. Adds needed components and subscribes changing variables to logic that updates the targeting system.
        internal List<TargetingInstruction> InitializeTargeting(List<TargetingInstruction> targetingInstructionsIn, GV_TargetingSystem targetingSystem)
        {
            targetingInstructionsIn = targetingSystem.AddDefaultTargetingInstructionIfNone(targetingInstructionsIn,
                false, targetingSystem.GameObjectBasedProcessing.Value);

            foreach (var targetingInstruction in targetingInstructionsIn)
            {
                AssignActionsTemplate(targetingInstruction, targetingInstructionsIn.IndexOf(targetingInstruction), targetingSystem);
                targetingInstruction.IsTargetingEnabled = true;
            }

            return targetingInstructionsIn;

            //
            //Local functions
            //

            //Creates default template scriptable object that can hold actions on what to do when targeting
            void AssignActionsTemplate(TargetingInstruction targetingInstruction, int indexOf, GV_TargetingSystem targetingSystemIn)
            {
                if (targetingInstruction.TargetingActions == null)
                {
                    var newActions = ScriptableObject.CreateInstance<TargetingActionsTemplateObject>();
                    newActions.name += "_" + indexOf;
                    targetingInstruction.TargetingActions = newActions;
                }

                targetingInstruction.TargetingActions.TargetingSystem = targetingSystemIn;
            }
        }

        public void ToggleGameObjectBasedSystem(bool gameObjectBasedProcessing, GV_TargetingSystem targetingSystem)
        {

            if (gameObjectBasedProcessing == true)
            {
                EnableGameObjects();
            }

            if (gameObjectBasedProcessing == false)
            {
                DisableGameObjects();
            }

            //
            //Local functions
            //
            void EnableGameObjects()
            {
                this.TryCreateVisibilityProcessors(targetingSystem.GetGeometryTypesTargeted(), targetingSystem);
                targetingSystem.GameObjectBasedProcessing.Value = true;

                var targetCreator = targetingSystem.TargetingSystemsRunner.GetTargetCreator<GameObjectTargetCreator>();
                if (targetCreator == null)
                {
                    targetingSystem.TargetingSystemsRunner.AddGameObjectTargetCreator<GameObjectTargetCreator>();
                    targetCreator = targetingSystem.TargetingSystemsRunner.GetTargetCreator<GameObjectTargetCreator>();
                    ((ITargetCreator) targetCreator).SetRunner(targetingSystem.TargetingSystemsRunner);
                }

                this.IfNoDefaultGameObjectTargetingAddOne(targetingSystem);
            }

            void DisableGameObjects()
            {
                if (targetingSystem.TargetingSystemsRunner.TargetingSystems.Count <= 1)
                {
                    targetingSystem.TargetingSystemsRunner.RemoveGameObjectTargetCreator<GameObjectTargetCreator>();
                }

                targetingSystem.GameObjectBasedProcessing.Value = false;
                targetingSystem.RemoveVisibilityProcessor<GameObjectTargetVisibilityProcessor>();
                targetingSystem.UpdateClosestTargets(true, false).Complete();
            }
        
    }

        /// <summary>
        /// Provides Needed default object targeting for the system in case there is none. Otherwise replaces one from the current users
        /// targeting instructions. 
        /// </summary>
        /// <param name="targetingSystem"></param>
        /// <param name="targetsProcessor"></param>
        internal void IfNoDefaultGameObjectTargetingAddOne(GV_TargetingSystem targetingSystem)
        {
            var targetingInstruction = targetingSystem.GetTargetingInstructionOfType(GeometryType.Objects);
            string defaultTag = TargetingSystemSettings.TriggerActionDefaultTag;

            if (targetingInstruction == null)
            {
                targetingInstruction = new TargetingInstruction(GeometryType.Objects, defaultTag,
                    new GameObjectTargetProcessor(), true);

                targetingSystem.TargetingInstructionsWithRefresh.Add(targetingInstruction);
            }
            else
            {
                targetingInstruction.TargetProcessorForGameObjects = new GameObjectTargetProcessor();
            }
        }
        
        internal int CreateIndexForTargetingSystem(TargetingSystemsRunner targetingSystemsRunnerIn,
            GV_TargetingSystem targetingSystemIn)
        {
            targetingSystemIn.targetingSystemIndex.x = -1;
            int upperLimitForIndex = int.MaxValue;
            for (int i = 0; i < upperLimitForIndex; i++)
            {
                if (targetingSystemsRunnerIn.TargetingSystems.All(targetingSystemLambda =>
                    targetingSystemLambda.TargetingSystemIndex.x != i))
                {
                    targetingSystemIn.targetingSystemIndex.x = i;
                    targetingSystemIn.indexIsSet = true;
                    break;
                }
            }

            return targetingSystemIn.targetingSystemIndex.x;
        }

        private void CreateTargetingSystemRunnerFromFactory(GameObject targetingSystemRunner,
            GV_TargetingSystem targetingSystem)
        {
            if (targetingSystemRunner.GetComponent<TargetingSystemsRunner>() == null)
            {
                targetingSystemRunner.AddComponent<TargetingSystemsRunner>();
                var createdRunner = targetingSystemRunner.GetComponent<TargetingSystemsRunner>();
                createdRunner.TargetingSystems = new HashSet<GV_TargetingSystem>();
            }

            var runner = targetingSystemRunner.GetComponent<TargetingSystemsRunner>();

            if (runner.TargetingSystems == null)
            {
                runner.TargetingSystems = new HashSet<GV_TargetingSystem>();
            }


            targetingSystem.TargetingSystemsRunner = runner;
            runner.TargetingSystems.Add(targetingSystem);
        }
    }
}