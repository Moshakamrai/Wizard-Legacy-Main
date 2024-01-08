using Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED

#endif

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.InputSystem
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		private bool inMenu;
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;
		
		[SerializeField]
		private UIDocument ui = null;
		[SerializeField]
		private LineRenderer circleRenderer = null;

#if !UNITY_IOS || !UNITY_ANDROID
		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
#endif
		
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		[SerializeField] private PickObjectScript pickingScript = null;

		public bool InMenu
		{
			get { return this.inMenu; }
			set { this.inMenu = value; }
		}

		public UIDocument UI
		{
			get { return this.ui; }
			set { this.ui = value; }
		}

		public void OnMove(InputValue value)
		{
			this.MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(this.cursorInputForLook)
			{
				this.LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			this.JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			this.SprintInput(value.isPressed);
		}
		
		public void OnPick(InputValue value)
		{
			if (this.InMenu == false)
			{
				this.pickingScript.Pick();
			}
			
		}
		
				
		public void OnToggleMenu()
		{
			var closedMenu = CloseMenu();
			if (closedMenu) {return;}
			
			OpenMenu();

			bool CloseMenu()
			{
				if (this.inMenu)
				{
					this.UiIsVisible(false);
					this.inMenu = false;
					this.cursorLocked = true;
					Cursor.visible = false;
					this.LockCursor(true);
					this.circleRenderer.GetComponent<LineRenderer>().enabled = false;
					return true;
				}

				return false;
			}

			void OpenMenu()
			{
				if (this.inMenu == false)
				{
					this.inMenu = true;
					this.UiIsVisible(true);
					this.cursorLocked = false;
					Cursor.visible = true;
					this.LockCursor(false);
				}
			}
		}
#else
	// old input sys if we do decide to have it (most likely wont)...
#endif
		public void UiIsVisible(bool visible)
		{
			this.ui.rootVisualElement.Q<Slider>("TargetingRadius").visible = visible;
			this.ui.rootVisualElement.Q<ListView>().visible = visible;
			this.ui.rootVisualElement.Q<Toggle>("BlockTargetingByObstacles").visible = visible;
			this.ui.rootVisualElement.Q<Toggle>("snapTurns").visible = visible;
			this.LockCursor(!visible);
		}

		public void MoveInput(Vector2 newMoveDirection)
		{
			this.move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			this.look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			this.jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			this.sprint = newSprintState;
		}

#if !UNITY_IOS || !UNITY_ANDROID

		private void OnApplicationFocus(bool hasFocus)
		{
			
		}

		private void LockCursor(bool newState)
		{
			if (newState)
				Cursor.lockState = CursorLockMode.Locked;
			else
				Cursor.lockState = CursorLockMode.None;
		}

#endif

	}
	
}