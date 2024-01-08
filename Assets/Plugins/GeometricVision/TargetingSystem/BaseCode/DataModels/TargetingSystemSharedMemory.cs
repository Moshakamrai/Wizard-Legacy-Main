using System.Collections.Generic;
using UnityEngine;

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
using Plugins.GeometricVision.TargetingSystem.Entities.Components.Core;
using Unity.Entities;
using Unity.Mathematics;

#endif

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    public class TargetingSystemSharedMemory
    {
        private List<TargetingSystemDataModels.GeoInfo> geoInfos = new List<TargetingSystemDataModels.GeoInfo>();
        public TargetingSystemSharedMemory()
        {
            this.GeoInfos = new List<TargetingSystemDataModels.GeoInfo>();
        }

        public List<TargetingSystemDataModels.GeoInfo> GeoInfos
        {
            get { return this.geoInfos; }
            set { this.geoInfos = value; }
        }
        

        public void DisposeNatives()
        {
#if TARGETING_SYSTEM_GEOMETRY_BASED_TARGETING
            for (var index = 0; index < this.GeoInfos.Count; index++)
            {
                var geoInfo = this.GeoInfos[index];
                if (geoInfo.edgesCreated == TargetingSystemDataModels.Boolean.True)
                {
                    geoInfo.edges.Dispose();
                    geoInfo.edgesCreated = TargetingSystemDataModels.Boolean.False;
                }
            }
#endif
        }
    }
}