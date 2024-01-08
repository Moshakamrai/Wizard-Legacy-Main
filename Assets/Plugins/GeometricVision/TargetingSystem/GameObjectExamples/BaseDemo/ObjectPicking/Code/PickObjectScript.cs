using System.Collections;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public class PickObjectScript : MonoBehaviour
    {
        [SerializeField] private float maxDistance;
        private float radius;
        [SerializeField] private float pickingSpeed;
        [SerializeField] private float distanceToStop = 1f;
        [SerializeField] private Transform player;
        [SerializeField] private float grabHookLength = 5;
        private GV_TargetingSystem targetingSystem;

        [SerializeField] 
        private Transform handTransform = null;
        [SerializeField] 
        private ThirdPersonController playerController = null;

        private void Start()
        {
            this.targetingSystem = this.GetComponent<GV_TargetingSystem>();
        }


        private void Update()
        {
            this.radius = this.targetingSystem.IndicatorVisibilityDistance;
#if ENABLE_INPUT_SYSTEM == false && STARTER_ASSETS_PACKAGES_CHECKED == false
            if (Input.GetMouseButtonUp(0))
            {
                Pick();
            }
#endif

        }

        void OnValidate()
        {
            this.maxDistance = Mathf.Clamp(this.maxDistance, 0, float.MaxValue);

            this.pickingSpeed = Mathf.Clamp(this.pickingSpeed, 0, float.MaxValue);
        }

        internal void Pick()
        {
            var target = this.targetingSystem.GetClosestTarget(false);
            
            if (target.Exists() == TargetingSystemDataModels.Boolean.True )
            {
                if (MathUtilities.Float4Distance(new float4(this.handTransform.position,1), target.projectedTargetPosition).x < this.maxDistance
                    && MathUtilities.Float3Distance(target.projectedTargetPosition, target.position).x < this.radius)
                {
                    this.targetingSystem.TargetingIndicatorObject.gameObject.SetActive(true);
                    this.targetingSystem.TriggerTargetingActions(target);
                    var closestTargetGameObject = this.targetingSystem.GetClosestTargetAsGameObject(false);
                    if (closestTargetGameObject.CompareTag("Rope") == false && this.targetingSystem.GetClosestTargetAsGameObject(false).CompareTag("RopeSwing") == false)
                    {
                        this.targetingSystem.MoveClosestTargetToPosition(this.handTransform.position, this.pickingSpeed, this.distanceToStop);
                        var safeZone = 1.4f;
                        this.StartCoroutine(this.targetingSystem.DestroyTargetAtDistanceToObject(target, this.handTransform, this.distanceToStop +safeZone, 4f));
                    }
                    else if (closestTargetGameObject.CompareTag("RopeSwing") == true)
                    {
                        this.HookToObstacle(target, closestTargetGameObject);
                    }
                }
            }
        }

        private void HookToObstacle(Target target, GameObject closestTargetGameObject)
        {
            this.StartCoroutine(this.giveForceUntil(target, closestTargetGameObject));
          //  this.StartCoroutine(this.ropeForceUntil(target, closestTargetGameObject));
            this.playerController.RopeActive = true;
        }


        IEnumerator giveForceUntil(Target target, GameObject closestTargetGameObject)
        {
            float counter = 0;
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            float  currentVerticalVelocity = Mathf.Sqrt(2f * -2f * this.playerController.Gravity);
            this.playerController.AdditionalVelocity +=  currentVerticalVelocity;
            while (counter < 4f && this.playerController.Grounded == false )//&& MathUtilities.Float3Distance(target.position, this.player.transform.position).x < this.grabHookLength
            {
                currentVerticalVelocity += 0.025f;
                counter += Time.deltaTime;
                yield return null;
            }

            while (this.playerController.Grounded == true && currentVerticalVelocity >0) //&& MathUtilities.Float3Distance(target.position, this.player.transform.position).x < this.grabHookLength
            {
                currentVerticalVelocity = 0;
                this.playerController.AdditionalVelocity = 0;
            }
        }
        IEnumerator ropeForceUntil(Target target, GameObject closestTargetGameObject)
        {
            float counter = 0;
            // the square root of Height * -2 * G = how much velocity needed to reach desired height
            float  currentVerticalVelocity = Mathf.Sqrt(2f * -2f * this.playerController.Gravity);
            
            while (counter < 4f && this.playerController.Grounded == false )//&& MathUtilities.Float3Distance(target.position, this.player.transform.position).x < this.grabHookLength
            {
                var distance =Vector3.Distance(this.playerController.transform.position, target.position);
                this.playerController.AdditionalVelocity =  currentVerticalVelocity +distance * 0.05f;
               // currentVerticalVelocity += 0.025f ;
                counter += Time.deltaTime;
                yield return null;
            }

            while (this.playerController.Grounded == true && currentVerticalVelocity >0) //&& MathUtilities.Float3Distance(target.position, this.player.transform.position).x < this.grabHookLength
            {
                currentVerticalVelocity = 0;
                this.playerController.AdditionalVelocity = 0;
            }
        }
#if UNITY_EDITOR

        /// <summary>
        /// Used for debugging geometry vision and is responsible for drawing debugging info from the data providid by
        /// GV_TargetingSystem plugin
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if (this.targetingSystem == null)
            {
                this.targetingSystem = this.GetComponent<GV_TargetingSystem>();
            }

            if (Selection.activeTransform == this.transform)
            {
                DrawHelper();
            }


            void DrawHelper()
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = Color.green;
                Vector3 resetToVector = Vector3.zero;

                if (this.targetingSystem == null)
                {
                   return; 
                }
                var  targetingSystemGoTransform = this.targetingSystem.transform;
                
                DrawTargetingVisualIndicators(targetingSystemGoTransform.position, this.targetingSystem.CalculateTargetingSystemDirectionInWorldSpace(),
                    Color.green);

                var position = this.targetingSystem.transform.position;
                var forward = position + this.targetingSystem.transform.forward * this.maxDistance;
                var up = targetingSystemGoTransform.up;
                var borderUp = Vector3.Scale(up, new Vector3(this.radius, this.radius, this.radius));
                var right = targetingSystemGoTransform.right;
                var borderRight = Vector3.Scale(right, new Vector3(this.radius, this.radius, this.radius));
                var borderLeft = Vector3.Scale(-right, new Vector3(this.radius, this.radius, this.radius));
                var borderDown = Vector3.Scale(-up, new Vector3(this.radius, this.radius, this.radius));

                DrawTargetingVisualIndicators(forward + borderRight, position + borderRight,
                    Color.green);
                DrawTargetingVisualIndicators(forward + borderLeft, position + borderLeft,
                    Color.green);
                DrawTargetingVisualIndicators(forward + borderUp, position + borderUp, Color.green);
                DrawTargetingVisualIndicators(forward + borderDown, position + borderDown,
                    Color.green);
                Handles.DrawWireArc(forward, targetingSystemGoTransform.forward, right, 360, this.radius);
                Handles.DrawWireArc(position, targetingSystemGoTransform.forward, right, 360, this.radius);

                void DrawTargetingVisualIndicators(Vector3 spherePosition, Vector3 lineStartPosition, Color color)
                {
                    Gizmos.color = color;
                    Gizmos.DrawLine(lineStartPosition, spherePosition);
                }
            }
        }
#endif
    }
}