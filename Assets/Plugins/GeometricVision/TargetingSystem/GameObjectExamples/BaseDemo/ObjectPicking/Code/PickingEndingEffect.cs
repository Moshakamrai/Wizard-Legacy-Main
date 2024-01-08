using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using UnityEngine;
using UnityEngine.Serialization;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public class PickingEndingEffect : MonoBehaviour
    {
        private GV_TargetingSystem targetingSystem;
        private Target closestTarget;

        [SerializeField, Tooltip("Locks the effect to be spawned to the GV_TargetingSystem components transforms position")]
        private bool lockPositionToTarget = false;
        private Transform cachedTransform;
        private ParticleSystem particleSystemEffect = new ParticleSystem();
        [FormerlySerializedAs("rotateTowardsCamera")] [SerializeField]private bool lookAtCamera = false;
        private void Awake()
        {
            this.particleSystemEffect = this.GetComponent<ParticleSystem>();
            this.particleSystemEffect.Stop();
            
        }

        // Start is called before the first frame update
        void Start()
        {
            this.cachedTransform = this.transform;
            if (this.cachedTransform.parent != null)
            {
                this.targetingSystem = this.cachedTransform.parent.GetComponent<GV_TargetingSystem>();
            }

            this.cachedTransform.parent = null;
            this.closestTarget = this.targetingSystem.GetClosestTarget(false);
            this.cachedTransform.position = new Vector3(this.closestTarget.position.x, this.closestTarget.position.y, this.closestTarget.position.z );
            this.particleSystemEffect.Play();
        }

        // Update is called once per frame
        void Update()
        {
            //Use the initial target and update its values, thus preventing target from swapping and still getting new values
            this.closestTarget = TargetingSystemUtilities.GetTargetValuesFromAnotherSystem(this.closestTarget, this.targetingSystem);  
                

            if (this.closestTarget.distanceFromTargetToCastOrigin > 0)
            {
                if (this.lockPositionToTarget)
                {
                    this.cachedTransform.position =  new Vector3(this.closestTarget.position.x, this.closestTarget.position.y, this.closestTarget.position.z );
                }

                if (this.lookAtCamera)
                {
                    this.cachedTransform.LookAt(this.targetingSystem.transform.position);
                }
            }
        }
    }
}