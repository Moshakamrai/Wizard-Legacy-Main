using Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.InputSystem;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            this.starterAssetsInputs.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            this.starterAssetsInputs.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            this.starterAssetsInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            this.starterAssetsInputs.SprintInput(virtualSprintState);
        }
        
    }

}
