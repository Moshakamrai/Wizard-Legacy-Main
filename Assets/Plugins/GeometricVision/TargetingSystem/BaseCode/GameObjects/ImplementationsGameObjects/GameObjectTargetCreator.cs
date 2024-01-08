using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects
{
    /// <inheritdoc cref="ITargetCreator" />
    public class GameObjectTargetCreator : ITargetCreator
    {
        [Preserve]
        public GameObjectTargetCreator()
        {
        }

        private int lastCount = 0;

        private Dictionary<string, HashSet<Transform>> allTransformsGroupedByTags =
            new Dictionary<string, HashSet<Transform>>();

        private List<GameObject> RootObjects { get; set; } = new List<GameObject>();
        private bool forceUpdate = false;
        private TargetingSystemsRunner runner;
        private HashSet<Transform> allTransforms = new HashSet<Transform>();

        public Dictionary<string, HashSet<Transform>> AllTransformsGroupedByTags
        {
            get { return this.allTransformsGroupedByTags; }
            set { this.allTransformsGroupedByTags = value; }
        }


        void ITargetCreator.SetRunner(TargetingSystemsRunner runner)
        {
            this.runner = runner;
        }

        public int CountSceneObjects()
        {
            SceneManager.GetActiveScene().GetRootGameObjects(this.RootObjects);
            return this.CountObjectsInHierarchy(this.RootObjects);
        }

        /// <summary>
        /// Checks changes for all systems.
        /// </summary>
        public void CheckSceneChanges()
        {
            SceneManager.GetActiveScene().GetRootGameObjects(this.RootObjects);
            var currentObjectCount = this.CountObjectsInHierarchy(this.RootObjects);

            var needsToCheckForIfUpdateIsNeeded = currentObjectCount == this.lastCount && this.forceUpdate == false;
            //Only should be ran if nothing is marked for update.
            if (needsToCheckForIfUpdateIsNeeded)
            {
                this.forceUpdate =
                    TargetingSystemUtilities.CheckIfUpdateIsNeededFromInstructions(this.runner.TargetingSystems,
                        GeometryType.Objects);
                if (this.forceUpdate == false)
                {
                    return;
                }
            }

            TargetingSystemUtilities.UpdatedInstructions(this.runner.TargetingSystems, GeometryType.Objects);
            this.AllTransformsGroupedByTags.Clear();
            this.allTransforms.Clear();
            HashSet<string> tags = new HashSet<string>();
            TargetingSystemUtilities.CollectTagsWithGeometryTypeFromSystems(this.runner.TargetingSystems, tags, true,
                GeometryType.Objects);

            //Fetch all geometry from all systems from fetched tags
            foreach (var targetTag in tags)
            {
                this.AllTransformsGroupedByTags =
                    this.GetAllTransformGroupedByTags(targetTag, this.AllTransformsGroupedByTags, this.RootObjects);
            }

            this.runner.SharedTargetingMemory.GeoInfos.Clear();
            this.runner.SharedTargetingMemory.GeoInfos =
                this.CreateGeoInfoObjects(this.runner.SharedTargetingMemory.GeoInfos);
            this.lastCount = currentObjectCount;
            this.forceUpdate = false;

            foreach (var runnerTargetCreator in this.runner.TargetCreators)
            {
                if (runnerTargetCreator as GameObjectTargetCreator != null &&
                    (GameObjectTargetCreator) runnerTargetCreator == this)
                {
                    continue;
                }

                runnerTargetCreator.ScheduleUpdate();
            }
        }

        public void ScheduleUpdate()
        {
            this.forceUpdate = true;
        }

        private Dictionary<string, HashSet<Transform>> GetAllTransformGroupedByTags(
            string targetTag, Dictionary<string, HashSet<Transform>> groupedByTags,
            List<GameObject> rootObjects)
        {
            //clear transforms. They will be added to dictionary later
            this.allTransforms.Clear();
            this.allTransforms =
                TargetingSystemUtilities.UpdateTransformSceneChanges(targetTag, this.allTransforms, rootObjects);

            return TargetingSystemUtilities.AddTransformsToTagDictionary(targetTag, groupedByTags, this.allTransforms);
        }

        /// <summary>
        /// Goes through all the root objects and counts their children.
        /// </summary>
        /// <param name="rootGameObjects"></param>
        /// <returns></returns>
        public int CountObjectsInHierarchy(List<GameObject> rootGameObjects)
        {
            int numberOfObjects = 0;
            int rootGOCount = rootGameObjects.Count;
            for (var index = 0; index < rootGOCount; index++)
            {
                var root = rootGameObjects[index];
                if (root.layer == TargetingSystemSettings.IgnoreRayCastLayer)
                {
                    continue;
                }

                numberOfObjects =
                    TargetingSystemUtilities.CountObjectsInTransformHierarchy(root.transform, numberOfObjects + 1);
            }

            return numberOfObjects;
        }


        /// <summary>
        /// Gets all the transforms from list of objects
        /// </summary>
        /// <param name="gameObjects"></param>
        /// <param name="targetTransforms"></param>
        /// <returns></returns>
        public void ExtractTransformFromGameObjects(List<GameObject> gameObjects,
            ref HashSet<Transform> targetTransforms)
        {
            for (var index = 0; index < gameObjects.Count; index++)
            {
                var root = gameObjects[index];
                targetTransforms.Add(root.transform);
            }
        }
        
        /// <summary>
        /// Creates GeoInfo objects and optionally handles copying geometry from Unity Mesh to geoInfo object.
        /// </summary>
        /// <param name="geoInfos"></param>
        /// <param name="targetedGeometries"></param>
        /// <param name="collidersTargeted"></param>
        /// <param name="useBounds"></param>
        private List<TargetingSystemDataModels.GeoInfo> CreateGeoInfoObjects(
            List<TargetingSystemDataModels.GeoInfo> geoInfos)
        {
            TargetingSystemDataModels.GeoInfo geoInfo;
            float3 position = new float3();

            foreach (KeyValuePair<string, HashSet<Transform>> transforms in this.AllTransformsGroupedByTags)
            {
                foreach (var transformInTransforms in transforms.Value)
                {
                    if (transformInTransforms == null)
                    {
                        continue;
                    }

                    geoInfo = CreateGeoInfoObject(transformInTransforms);
                    geoInfos.Add(geoInfo);
                }
            }

            return geoInfos;

            //
            //Locals
            //
            TargetingSystemDataModels.GeoInfo CreateGeoInfoObject(Transform transformIn)
            {
                geoInfo = new TargetingSystemDataModels.GeoInfo
                {
                    gameObject = transformIn.gameObject,
                    transform = transformIn,
                };
                return geoInfo;
            }
        }

        public int CompareTo(ITargetCreator other)
        {
            return -1;
        }
    }
}