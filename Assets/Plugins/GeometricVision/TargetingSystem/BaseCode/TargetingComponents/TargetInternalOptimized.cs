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
    
    public struct TargetInternalOptimized
    {
        //xyz position, w entityQueryIndex
        public float4 positionAndQueryIndex;
    }
    
    /// <summary>
    /// Multi threading friendly target object containing info how to find entity or gameObject.
    /// </summary>
    public struct TargetInternalOptimized2
    {
        //xyz position, w entityQueryIndex
        public float4 positionAndQueryIndex;

    }
}