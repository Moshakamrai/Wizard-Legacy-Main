using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT

#endif

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingJobs
{
    public static class TargetingVisibilityJobs 
    {
        
        /// <summary>
        /// Copies targets from 1 array to another.
        /// </summary>
        [BurstCompile]
        internal struct CombineEntityAndGameObjectTargetsOptimized : IJobParallelFor
        {
            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<Target> targetingContainer;

            [ReadOnly] public NativeSlice<Target> targetsToInsert;

            //x=targetingContainerLength, y= targetsToInsertLength, z= Offset, w = index
            public int4 targetingContainerLengthTargetsToInsertLengthOffset;


            public void Execute(int targetsToInsertIndex)
            {
                this.targetingContainerLengthTargetsToInsertLengthOffset.w = targetsToInsertIndex + this.targetingContainerLengthTargetsToInsertLengthOffset.z;
                if (this.targetingContainerLengthTargetsToInsertLengthOffset.w >= this.targetingContainerLengthTargetsToInsertLengthOffset.x)
                {
                    return;
                }
                if (targetsToInsertIndex < this.targetingContainerLengthTargetsToInsertLengthOffset.y)
                {
                    this.targetingContainer[this.targetingContainerLengthTargetsToInsertLengthOffset.w] = this.targetsToInsert[targetsToInsertIndex];
                }
            }
        }
        
        [BurstCompile]
        public struct SortTargetsSliceJob : IJob
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeSlice<Target> newClosestTargets;

            public bool favorDistanceToCameraInsteadDistanceToPointer;
            public TargetingSystemComparers.DistanceComparerToViewDirection comparerToViewDirection;
            public TargetingSystemComparers.DistanceComparerToCamera favorDistanceToCameraComparerToViewDirection;

            public void Execute()
            {
                if (this.newClosestTargets.Length > 0)
                {
                    if (this.favorDistanceToCameraInsteadDistanceToPointer == false)
                    {
                        this.newClosestTargets.Sort(this.comparerToViewDirection);
                    }
                    else
                    {
                        this.newClosestTargets.Sort(this.favorDistanceToCameraComparerToViewDirection);
                    }
                }
            }
        }
    }
}
