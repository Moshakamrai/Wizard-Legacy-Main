using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Debugging
{
    /// <summary>
    /// Contains user defined targeting instructions for the GV_TargetingSystem object
    /// For example enable editor drawing from inspector
    /// </summary>
    [System.Serializable]
    public class TargetingDebugInstruction
    {
        [SerializeField, Tooltip("Can be used to limit how much targets are shown")]
        public TargetingSystemDataModels.VisualizationMode visualizationMode;
    }
}