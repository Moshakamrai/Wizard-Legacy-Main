using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    /// <summary>
    /// Simple example script made to demo what can be done with the system.
    /// Draws an electrified line between target and player/hand
    /// 
    /// </summary>
    public class PickingMiddleEffect : MonoBehaviour
    {
        private GV_TargetingSystem targetingSystem;
        private LineRenderer lineRenderer;
        private Target currentTarget;

        private Vector3[] positions = new Vector3[10];
        private float time = 0;

        [SerializeField, Tooltip("Frequency of the lightning effect")]
        private float frequency = 0;

        [SerializeField, Tooltip("how wide and strong is the effect")]
        private float strengthModifier = 0;
        [SerializeField] private float strengthModifier1 = 0.05f;
        [Range(-10.0f, 10.0f)]
        [SerializeField]private float strengthModifier2 = 0;

        private float sinTime = 0;
        
        private HandRef handRef = null;

        // Start is called before the first frame update
        void Start()
        {
            GetTargetingSystemFromParentAndUnParent();
            this.currentTarget = this.targetingSystem.GetClosestTarget(false);
            this.lineRenderer = this.GetComponent<LineRenderer>();

            void GetTargetingSystemFromParentAndUnParent()
            {
                if (this.transform.parent != null)
                {
                    var parent = this.transform.parent;
                    this.targetingSystem = parent.GetComponent<GV_TargetingSystem>();
                    this.handRef = parent.GetComponent<HandRef>();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            this.currentTarget = TargetingSystemUtilities.GetTargetValuesFromAnotherSystem(this.currentTarget, this.targetingSystem);
            
            if (this.currentTarget.Exists() == TargetingSystemDataModels.Boolean.False || this.currentTarget.distanceFromTargetToProjectedPoint > this.targetingSystem.IndicatorVisibilityDistance )
            {
                Destroy(this.gameObject);
            }
            else
            {
                var position = this.handRef.hand.position;
                float distance = Vector3.Distance(position, this.currentTarget.position);
                this.positions = LineEffect.ElectrifyPoints(position, this.frequency, new Vector3(this.currentTarget.position.x, this.currentTarget.position.y, this.currentTarget.position.z),
                    distance, this.time, this.sinTime, this.positions, this.strengthModifier, this.strengthModifier1, this.strengthModifier2);

                this.lineRenderer.positionCount = this.positions.Length;
                this.lineRenderer.SetPositions(this.positions);
            }
        }
    }
}