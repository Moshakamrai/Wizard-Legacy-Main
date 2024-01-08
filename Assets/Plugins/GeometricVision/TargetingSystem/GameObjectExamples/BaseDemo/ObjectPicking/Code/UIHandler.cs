using System.Collections;
using System.Linq;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.Scripts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public class UIHandler : MonoBehaviour
    {
        [SerializeField] private GV_TargetingSystem targetingSystem = null;
        [FormerlySerializedAs("CharacterController")] [SerializeField] private ThirdPersonController characterController = null;
        [SerializeField] private UIDocument ui = null;

        [SerializeField]
        private TargetingActionsTemplateObject targetingActions = null;

        [SerializeField]
        private float defaultSnapValueForSlider = 1.5f;
        private float previousSnapValueForSlider = 1.5f;

        private ListView listView = null;
        private Toggle targetCullingToggle = null;
        private Toggle snapCameraToTargetMode = null;
        private Slider targetingRadiusSlider = null;
        private string[] tags = new string [4];
        [SerializeField]
        private LineRenderer lineRenderer = null;

        private Vector3[] circleLinePositions = null;
        private int lineCount = 55;

        private void Awake()
        {
            this.tags[0] = "Weapon";
            this.tags[1] = "Apple";
            this.tags[2] = "EnergyKit";
            this.tags[3] = "MedPack";

            this.previousSnapValueForSlider = this.defaultSnapValueForSlider;
            var root = this.ui.rootVisualElement;
            this.listView = root.Q<ListView>();
            this.targetingRadiusSlider = root.Q<Slider>("TargetingRadius");

            this.targetCullingToggle = root.Q<Toggle>("BlockTargetingByObstacles");
            this.snapCameraToTargetMode = root.Q<Toggle>("snapTurns");
            this.targetCullingToggle.RegisterValueChangedCallback(toggleEvent => this.TurnCullingOnOff(toggleEvent.newValue));
            this.snapCameraToTargetMode.RegisterValueChangedCallback(toggleEvent => this.EnableSnapTurns(toggleEvent.newValue));
            this.targetingRadiusSlider.RegisterValueChangedCallback(sliderChanged => this.ChangeSliderValue(sliderChanged.newValue));
        }

        private void Start()
        {
            this.circleLinePositions = new Vector3[this.lineCount];
            this.lineRenderer.loop = true;
        }

        private void EnableSnapTurns(bool toggleEventNewValue)
        {
            this.targetingSystem.IndicatorVisibilityDistance = this.defaultSnapValueForSlider;
            this.characterController.SnapTurns = toggleEventNewValue;
        }

        private void ChangeSliderValue(float newValue)
        {
            this.targetingSystem.IndicatorVisibilityDistance = newValue;
            float depthAddition = 1;
            if (this.targetingSystem.GetClosestTarget(false).distanceFromProjectedPointToCastOrigin > 0.1f)
            {
                depthAddition = this.targetingSystem.GetClosestTarget(false).distanceFromProjectedPointToCastOrigin;
            }
            //TODO:Low priority calculate from cameras field of view
            this.StartCoroutine(this.DrawTargetRadiusCircle(newValue/40f * depthAddition/1.5f));
        }

        /// <summary>
        /// Draws circle to visualize targeting radius.
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        IEnumerator DrawTargetRadiusCircle(float radius)
        {
            this.lineRenderer.enabled = true;
            this.lineRenderer.positionCount = this.lineCount;
            float theta = (2f * Mathf.PI) / this.lineCount;

            BuildAndDrawCircle(theta, radius, this.circleLinePositions);
  

            yield return null;
            
            
            void BuildAndDrawCircle(float thetaIn, float radiusIn, Vector3[] linePositionsIn)
            {
                float angle = 0;
                for (int index = 0; index < this.lineCount; index++)
                {
                    float x = radiusIn * Mathf.Cos(angle);
                    float y = radiusIn * Mathf.Sin(angle);
                    linePositionsIn[index] = new Vector3(x, y, 0);
                    
                    angle += thetaIn;
                }

                this.lineRenderer.SetPositions(linePositionsIn);
            }
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            AddTagsToListItem();
            
            void AddTagsToListItem()
            {
            
                for (int i = 0; i < this.tags.Length; i++)
                {
                    var toggleItem = new Toggle(this.tags[i]);
                    var targetingInstructionsWithWantedTag = this.targetingSystem.TargetingInstructionsWithRefresh.Where(instruction => instruction.TargetTag == this.tags[i]).ToList();
                    if (targetingInstructionsWithWantedTag.Count() != 0)
                    {
                        toggleItem.value = true;
                    }
                    toggleItem.RegisterValueChangedCallback(toggleEvent => this.AddOrRemoveTargetingInstructionWithTag(toggleEvent.newValue, toggleItem.label));
                    this.listView.hierarchy.Add(toggleItem);
                }
            }
        }

       

        private void AddOrRemoveTargetingInstructionWithTag(bool enabledTag, string tag)
        {
            var targetingInstructionsWithWantedTag = this.targetingSystem.TargetingInstructionsWithRefresh.Where(instruction => instruction.TargetTag == tag).ToList();

            if (enabledTag && targetingInstructionsWithWantedTag.Any() == false)
            {
                var newTargetingInstruction =
                    new TargetingInstruction(GeometryType.Objects, tag, new GameObjectTargetProcessor(), true)
                    {
                        TargetingActions = this.targetingActions
                    };
                this.targetingSystem.AddTargetingInstruction(newTargetingInstruction);
            }
            else
            {
                this.targetingSystem.RemoveTargetingInstruction(targetingInstructionsWithWantedTag[0]);
            }
        }
        private void TurnCullingOnOff(bool on)
        {

            if (on)
            {
                this.targetingSystem.CullingBehaviour =
                    TargetingSystemDataModels.CullingBehaviour.MiddleScreenSinglePointToTarget;
            }
            else
            {
                this.targetingSystem.CullingBehaviour =
                    TargetingSystemDataModels.CullingBehaviour.None;
            }
        }
    }
}