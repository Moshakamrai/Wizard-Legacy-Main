using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects
{
    public class GameObjectTargetProcessor : ITargetProcessor
    {

        public NativeArray<Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection,
            GV_TargetingSystem gvTargetingSystem, TargetingInstruction targetingInstruction)
        {
            throw new NotImplementedException();
        }

        public void GetTargetsAsNativeSlice(Vector3 rayLocation, Vector3 rayDirection,
            GV_TargetingSystem gvTargetingSystem)
        {
        }


        public JobHandle GetTargetsJobHandle(GV_TargetingSystem gvTargetingSystem)
        {
            throw new NotImplementedException();
        }

        NativeList<Target> ITargetProcessor.GetTargets(Vector3 rayLocation, Vector3 rayDirectionWS,
            GV_TargetingSystem targetingSystem, TargetingInstruction targetingInstruction)
        {
            Target target = new Target();

            if (targetingInstruction.TargetTag.Length > 0)
            {
                targetingSystem.targetingSystemMemory.GameObjectProcessorTargets = GetTargetingDataForTargets(rayLocation, rayDirectionWS, targetingSystem,
                    targetingInstruction, targetingSystem.targetingSystemMemory.GameObjectProcessorTargets, target);
            }

            return targetingSystem.targetingSystemMemory.GameObjectProcessorTargets;

            //
            //Local functions 
            //
            NativeList<Target> GetTargetingDataForTargets(Vector3 rayLocation1, Vector3 rayDirectionWs,
                GV_TargetingSystem targetingSystemIn, TargetingInstruction targetingInstruction1,
                NativeList<Target> targets,
                Target targetIn)
            {
                var visibilityProcessor =
                    targetingSystemIn.GetVisibilityProcessor<GameObjectTargetVisibilityProcessor>();
                if (visibilityProcessor != null)
                {
                    foreach (TargetingSystemDataModels.GeoInfo geoInfo in targetingSystemIn.SeenGeometryInfos)
                    {
                        var geoInfo1 = geoInfo;
                        if (geoInfo1.gameObject.layer == TargetingSystemSettings.IgnoreRayCastLayer ||
                            geoInfo1.gameObject.CompareTag(targetingInstruction1.TargetTag) == false)
                        {
                            continue;
                        }

                        targetIn = PrepareTarget(geoInfo1, targetIn);
                        targetIn = TargetingSystemUtilities.GetTargetingDataForTargetNoSqrt(targetIn,
                            new float4(rayLocation1, 1), new float4(rayDirectionWs, 1));
                        targets.Add(targetIn);
                    }
                }

                return targets;
            }

            Target PrepareTarget(TargetingSystemDataModels.GeoInfo geoInfo, Target targetIn)
            {
                targetIn.position = geoInfo.transform.position;
                targetIn.geoInfoHashCode = geoInfo.GetHashCode().x;
                return targetIn;
            }
        }

        public GeometryType TargetedType
        {
            get { return GeometryType.Objects; }
        }

        public bool IsForEntities()
        {
            return false;
        }


        public int CompareTo(ITargetProcessor other)
        {
            return -1;
        }
    }
}