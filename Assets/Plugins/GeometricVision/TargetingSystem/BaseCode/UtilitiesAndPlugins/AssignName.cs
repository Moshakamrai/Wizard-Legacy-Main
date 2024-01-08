using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;

using UnityEngine;

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
using Unity.Entities;
using Plugins.GeometricVision.TargetingSystem.Entities.Components;
#endif

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public class AssignName : MonoBehaviour
    {
        [SerializeField]private GV_TargetingSystem targetingSystem;
        [SerializeField] private GameObject targetingIndicator = null;
        private GameObject spawnedTargetingIndicator;
        [SerializeField] private TextMesh text;
        [SerializeField] private bool detailedText;
        public GameObject Indicator
        {
            get { return this.targetingIndicator; }
            set { this.targetingIndicator = value; }
        }

        public GV_TargetingSystem TargetingSystem
        {
            get { return this.targetingSystem; }
            set { this.targetingSystem = value; }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (this.targetingSystem == null)
            {
                this.TargetingSystem = this.GetComponent<GV_TargetingSystem>();
            }

            this.spawnedTargetingIndicator = this.Indicator;

            if (this.text == null)
            {
                this.text = this.spawnedTargetingIndicator.GetComponentInChildren<TextMesh>();

                if (this.text == null)
                {
                    Debug.Log("No text has been found. Make sure there is text component available either on the children or assigned through inspector");
                }
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            var target = this.TargetingSystem.GetClosestTarget(false);

            if (target.isEntity == TargetingSystemDataModels.Boolean.True)
            {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
                AssignEntityName();
                
                void AssignEntityName()
                {
                    if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<Name>(target.entity))
                    {
                        this.text.text = World.DefaultGameObjectInjectionWorld.EntityManager
                            .GetComponentData<Name>(target.entity).Value.ToString();
                    }
                    else
                    {
                        this.text.text = "Unknown";
                    }
                }
#endif
            }
            else
            {
                AssignGameObjectsName();
                
                void AssignGameObjectsName()
                {
                    var go = TargetingSystemUtilities.GetGeoInfoBasedOnHashCode(target.geoInfoHashCode, this.targetingSystem.TargetingSystemsRunner.SharedTargetingMemory.GeoInfos);
                    if (this.detailedText)
                    {

                        if (go.gameObject)
                        {
                            this.text.text = "Target name: " + go.gameObject.name + "\n" 
                                             +" distance between direction and target: " + target.distanceFromTargetToProjectedPoint + "\n" 
                                             +" distance between target and camera: " + target.distanceFromTargetToCastOrigin;
                        }
                    }
                    else
                    {
                        if (go.gameObject)
                        {
                            this.text.text = "" + go.gameObject.name;
                        }
                    }
                }
            }
        }
    }
}
