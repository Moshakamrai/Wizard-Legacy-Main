using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Unity.Mathematics;

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
using Unity.Entities;
#endif

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents
{

    /// <summary>
    /// Multi threading friendly target object containing info how to find entity or gameObject.
    /// </summary>
    public struct Target : IComparable<Target>
    {
        public float3 position;
        public float4 projectedTargetPosition;
        public float distanceFromTargetToProjectedPoint;
        public float distanceFromTargetToCastOrigin;
        public float distanceFromProjectedPointToCastOrigin;
        public TargetingSystemDataModels.Boolean isEntity;
        public int geoInfoHashCode;

        public int CompareTo(Target other)
        {
            var distanceToRayComparison = this.distanceFromTargetToProjectedPoint.CompareTo(other.distanceFromTargetToProjectedPoint);
            if (distanceToRayComparison != 0)
            {
                return distanceToRayComparison;
            }

            var distanceToCastOriginComparison = this.distanceFromTargetToCastOrigin.CompareTo(other.distanceFromTargetToCastOrigin);
                
            if (distanceToCastOriginComparison != 0)
            {
                return distanceToCastOriginComparison;
            }

            return 0;
        }

        public TargetingSystemDataModels.Boolean Exists()
        {
            if (this.distanceFromTargetToCastOrigin != 0)
            {
                return TargetingSystemDataModels.Boolean.True;
            }
            else
            {
                return TargetingSystemDataModels.Boolean.False;
            }
        }
    }
}
