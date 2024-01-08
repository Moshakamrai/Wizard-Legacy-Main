using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.UsingSystemForSpellCasting
{
    public class LightningSpell : MonoBehaviour
    {
        [SerializeField] private float maxDistance;
        [SerializeField] private float radius;

        private GV_TargetingSystem targetingSystem;

        private void Start()
        {
            this.targetingSystem = this.GetComponent<GV_TargetingSystem>();
        }

        public void OnPick(InputValue value)
        {
            this.Cast();
        }


        void OnValidate()
        {
            this.maxDistance = Mathf.Clamp(this.maxDistance, 0, float.MaxValue);
            this.radius = Mathf.Clamp(this.radius, 0, float.MaxValue);
        }

        void Cast()
        {
            var target = this.targetingSystem.GetClosestTarget(false);
            // if distances are zeroed it means there was no targets inside vision area and the system return default
            // target, because target struct cannot be null for it to work with entities

            if ((target.distanceFromTargetToCastOrigin > float4.zero).w)
            {
                if (MathUtilities.Float4Distance(new float4 (this.transform.position, 1), target.projectedTargetPosition).x < this.maxDistance
                    && MathUtilities.Float3Distance(target.projectedTargetPosition, target.position).x < this.radius)
                {
                    this.targetingSystem.TriggerTargetingActions(target);
                }
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
                    this.targetingSystem = this.GetComponent<GV_TargetingSystem>();
                    return;
                }
                var targetingSystemTransform = this.targetingSystem.transform;

                DrawTargetingVisualIndicators(targetingSystemTransform.position, this.targetingSystem.CalculateTargetingSystemDirectionInWorldSpace(),
                    Color.green);

                var position = this.targetingSystem.transform.position;
                var forward = position + this.targetingSystem.transform.forward * this.maxDistance;
                var up = targetingSystemTransform.up;
                var borderUp = Vector3.Scale(up, new Vector3(this.radius, this.radius, this.radius));
                var right = targetingSystemTransform.right;
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
                Handles.DrawWireArc(forward, targetingSystemTransform.forward, right, 360, this.radius);
                Handles.DrawWireArc(position, targetingSystemTransform.forward, right, 360, this.radius);

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

