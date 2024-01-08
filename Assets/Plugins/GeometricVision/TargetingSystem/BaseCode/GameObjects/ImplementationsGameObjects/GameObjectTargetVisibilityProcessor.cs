using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using RaycastHit = UnityEngine.RaycastHit;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects
{
    /// <summary>
    /// Class that is responsible for seeing objects and geometry.
    /// It checks, if object is inside visibility area and filters out unwanted objects and geometry.
    ///
    /// Usage: Add GV_TargetingSystem component to object you want it to. The component will handle the rest. Component has list of geometry types.
    /// These are used to see certain type of objects and clicking the targeting option from the inspector UI the user can
    /// add option to find the closest element of this type.
    /// </summary>
    [Preserve]
    public class GameObjectTargetVisibilityProcessor : ITargetVisibilityProcessor
    {
        private float4 planeNormalAndDistance0 = float4.zero;
        private float4 planeNormalAndDistance1 = float4.zero;
        private float4 planeNormalAndDistance2 = float4.zero;
        private float4 planeNormalAndDistance3 = float4.zero;
        private float4 planeNormalAndDistance4 = float4.zero;
        private float4 planeNormalAndDistance5 = float4.zero;
        private float4 targetPosition = float4.zero;

        public GameObjectTargetVisibilityProcessor()
        {
            this.Initialize();
        }

        public int Id { get; set; }

        private GV_TargetingSystem TargetingSystem { get; set; }

        public TargetingSystemsRunner Runner { get; set; }

        public bool IsEntityBased()
        {
            return false;
        }

        private void Initialize()
        {
        }

        /// <summary>
        /// Updates visibility of the objects in the eye and processor/manager
        /// </summary>
        public void UpdateVisibility(GV_TargetingSystem targetingSystemIn, bool forceUpdateIn)
        {
            if (targetingSystemIn.GameObjectBasedProcessing.Value == false)
            {
                return;
            }
            this.TargetingSystem = targetingSystemIn;
            InitPrepare(out var targetingSystemPosition, out var singlePointCulling, out var hit);

            this.TargetingSystem.SeenGeometryInfos = this.UpdateGeometryInfoVisibilities(targetingSystemPosition,  singlePointCulling,  hit, targetingSystemIn.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos, targetingSystemIn.UseBounds);

            void InitPrepare(out float4 geometryVisionPosition,

                out int4 singlePointCulling, out RaycastHit hit)
            {
                this.planeNormalAndDistance0.xyz = targetingSystemIn.targetingSystemMemory.PlanesAsNative[0].xyz;
                this.planeNormalAndDistance0.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[0].w;
                this.planeNormalAndDistance1.xyz = targetingSystemIn.targetingSystemMemory.PlanesAsNative[1].xyz;
                this.planeNormalAndDistance1.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[1].w;
                this.planeNormalAndDistance2.xyz =targetingSystemIn.targetingSystemMemory.PlanesAsNative[2].xyz;
                this.planeNormalAndDistance2.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[2].w;
                this.planeNormalAndDistance3.xyz = targetingSystemIn.targetingSystemMemory.PlanesAsNative[3].xyz;
                this.planeNormalAndDistance3.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[3].w;
                this.planeNormalAndDistance4.xyz =targetingSystemIn.targetingSystemMemory.PlanesAsNative[4].xyz;
                this.planeNormalAndDistance4.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[4].w;
                this.planeNormalAndDistance5.xyz = targetingSystemIn.targetingSystemMemory.PlanesAsNative[5].xyz;
                this.planeNormalAndDistance5.w = targetingSystemIn.targetingSystemMemory.PlanesAsNative[5].w;
                geometryVisionPosition = new float4(this.TargetingSystem.transform.position, 1);

                singlePointCulling = this.TargetingSystem.GetCullingModeAsInt();
                hit = new RaycastHit();
            }
        }

        /// <summary>
        /// Updates Data structures made for Game Objects visibility.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="allTargetGeoObjects"></param>
        /// <param name="useBounds"></param>
        private List<TargetingSystemDataModels.GeoInfo> UpdateGeometryInfoVisibilities(float4 targetingSystemPosition, int4 singlePointCulling, RaycastHit hit, List<TargetingSystemDataModels.GeoInfo> allTargetGeoObjects, bool useBounds)
        {
            // Updates object collection containing geometry and data related to seen object. Usage is to internally update seen geometry objects by checking objects renderer bounds
            // against eyes/cameras frustum
            foreach (var geoInfo in allTargetGeoObjects)
            {
                var geoInfoTransform = geoInfo.transform;

                if (!geoInfoTransform)
                {
                    continue;
                }

                var isSeen = ObjectIsSeen(useBounds, geoInfo, geoInfoTransform.position);
                AddSeenObjectToSeenObjects(useBounds, isSeen, geoInfo);
            }

            return this.TargetingSystem.SeenGeometryInfos;

            bool ObjectIsSeen(bool useBoundsIn, TargetingSystemDataModels.GeoInfo geoInfoIn, Vector3 targetPositionIn)
            {
                this.targetPosition.x = targetPositionIn.x;
                this.targetPosition.y = targetPositionIn.y;
                this.targetPosition.z = targetPositionIn.z;
                bool isSeen = false;
                
                if (useBoundsIn && geoInfoIn.renderer)
                {
                    isSeen = TargetingSystemUtilities.TestPlanesAABBInternalFastest(this.planeNormalAndDistance0, this.planeNormalAndDistance1, this.planeNormalAndDistance2, this.planeNormalAndDistance3, this.planeNormalAndDistance4, this.planeNormalAndDistance5,
                                 geoInfoIn.boundsMin, geoInfoIn.boundsMax, float3.zero) == TargetingSystemDataModels.Boolean.True;
                }
                else
                {
                    isSeen = TargetingSystemUtilities.IsInsideFrustum2(this.targetPosition, this.planeNormalAndDistance0, this.planeNormalAndDistance1, this.planeNormalAndDistance2, this.planeNormalAndDistance3, this.planeNormalAndDistance4, this.planeNormalAndDistance5, float4.zero) ==
                             TargetingSystemDataModels.Boolean.True;
                }

                bool isCulled = false;
                if (isSeen)
                {
                    isCulled = TargetingSystemUtilities.IsGameObjectTargetCulledByGameObjectOrEntity(
                    singlePointCulling,
                    targetingSystemPosition, geoInfoIn, hit);
                }

                if (isSeen == true && isCulled == false)
                {
                    return true;
                }

                return false;
            }


            void AddSeenObjectToSeenObjects(bool useBounds, bool isSeen, TargetingSystemDataModels.GeoInfo geoInfo1)
            {
                if (isSeen)
                {
                    AddToSeenListIfNotAnEffect(geoInfo1);
                }
                
                void AddToSeenListIfNotAnEffect(TargetingSystemDataModels.GeoInfo geoInfo)
                {
                    bool isEffect = false;
                    isEffect = IsToBeIgnored(geoInfo, isEffect);

                    if (isEffect == false)
                    {
                        this.TargetingSystem.SeenGeometryInfos.Add(geoInfo);
                    }
                    //
                    //Local functions
                    //
                    bool IsToBeIgnored(TargetingSystemDataModels.GeoInfo geoInfoIn, bool isEffectIn)
                    {
                        foreach (var actionElement in this.TargetingSystem.CollectedTriggerActionElements)
                        {
                            isEffectIn =
                                TargetingSystemUtilities.TransformIsEffect(geoInfoIn.gameObject,
                                    actionElement.GameObjectTag);
                        }

                        return isEffectIn;
                    }
                }
            }
        }
        public int CompareTo(ITargetVisibilityProcessor other)
        {
            return 1;
        }
    }
}