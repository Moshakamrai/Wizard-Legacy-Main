using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    public enum GeometryType
    {
        Objects = 0 ,
        Lines = 1,

    }

    /// <summary>
    /// Contains most of the public data blueprints for the GV_TargetingSystem plugin
    /// </summary>
    public static class TargetingSystemDataModels
    {
        /// <summary>
        /// Cache object for GameObject related data
        /// </summary>
        public struct GeoInfo
        {
            public GameObject gameObject;
            public Renderer renderer;
            public Transform transform;
            public Boolean edgesCreated;
            public Boolean isHitByRay;
            public Bounds bounds;
            public float4 boundsMin;
            public float4 boundsMax;
            public Mesh mesh;
            public Mesh colliderMesh;
#pragma warning disable 108,114
            internal int4 GetHashCode()
#pragma warning restore 108,114
            {
                return this.gameObject.GetHashCode();
            }
        }

        public enum PlaneOrdering : ushort
        {
            left = 0,
            right = 1,
            down = 2,
            up = 3,
            near = 4,
            far = 5,
        }

        public enum CullingBehaviour
        {
            None,
            MiddleScreenSinglePointToTarget
        }

        public enum VisualizationMode
        {
            None,
            Single,
            All
        }

        public struct FactorySettings
        {
            public bool processGameObjects;
            public bool processEntities;
            public float fieldOfView;
            public float targetingDistance;
            public bool edgesTargeted;
            public bool defaultTargeting;
            public string defaultTag;
            public Object entityComponentQueryFilter;
            public TargetingActionsTemplateObject TargetingActionsTemplateObject;
            public CullingBehaviour cullingBehavior;
            public int audioPoolSize;
            public VisualizationMode VisualizationMode;
            public bool filterDuplicateEdges;
            public bool prioritizeColliders;
            public bool UseLocalToWorld { get; set; }
        }

        /// <summary>
        /// Great and easy idea for Blittable type boolean from playerone-studio.com
        /// </summary>
        public enum Boolean : byte
        {
            False = 0,
            True = 1
        }
    }
}