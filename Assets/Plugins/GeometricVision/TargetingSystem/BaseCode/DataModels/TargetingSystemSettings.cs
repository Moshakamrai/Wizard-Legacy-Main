using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
        /*
         * Global settings for all the components
         * Put only permanent stuff here
         */
        public struct TargetingSystemSettings
        {
            public const string RunnerName  = "TargetingSystemsRunner";
            public const string HeaderImagePath  = "/Plugins/GeometricVision/TargetingSystem/Editor/UI/Images/GeoVisionTargeting.png";
            public const string NewActionsAssetForTargetingPath = "Assets/Plugins/GeometricVision/TargetingSystem/NewActionsAssetForTargeting.asset";
            
            //These settings can be overridden from targeting systems runner game object.
            //Initial amount of estimated targets the runner has available. Bigger numbers mean more targets can be stored, but will cost more memory.
            public static int MaxTargets = 15000;
            public static int MaxChunks = 600;
            public static int MaxTargetsPerSystem { get; set; } = 15000;
            public static int DebugLoggingTargetingSystemIndex = -1;
            public static int DefaultAudioPoolSize = 100;
            
            public const string AudioPoolBaseName = "audioPool";
            public const string AudioPoolParentName = AudioPoolBaseName +"Parent";
            public const string AudioPoolItemName = AudioPoolBaseName +"PItem";
            public static readonly Vector3 DefaultTransformLocation = new Vector3(0,-100,0);
            public const float DefaultTargetingDistance = 500;
            public const string TriggerActionDefaultTag = "Untagged";
            public const int IgnoreRayCastLayer = 2;

            public static string PathToStaticTaggedObject { get; } =
                "Assets/Plugins/GeometricVision/TargetingSystem/Entities/Components/StaticTaggedObject.cs";
            public static string PathToRotationSpeedSpawnAndRemove { get;  } = "Assets/Plugins/GeometricVision/TargetingSystem/Entities/Components/FromUnity/RotationSpeed_SpawnAndRemove.cs";

        }
}