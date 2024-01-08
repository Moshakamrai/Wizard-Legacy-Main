using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED

#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.ThirdPersonControllerFromUnityStarterAssets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")] [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")] [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)] [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Tooltip("For locking the camera position on all axis")]
        private bool snapTurns = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        [SerializeField] private GV_TargetingSystem targetingSystem = null;
        [SerializeField] private GV_TargetingSystem secondaryCameraTargetingSystem = null;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        public float VerticalVelocity { get; internal set; }
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        public CharacterController Controller { get; private set; }
        private StarterAssetsInputs _input;
        [SerializeField] private GameObject _mainCamera = null;
        [SerializeField] private float snapSpeed = 8;
        [SerializeField] private float revertBackToRestRotationSpeed = 3f;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private Quaternion initialLocalRotation = new Quaternion();
        private Quaternion lookRotation = new Quaternion();
        private Target previousTarget = new Target();
        private Target newTarget = new Target();
        public bool RopeActive { get; set; } = false;
        public float AdditionalVelocity { get; set; }
        public bool MovementActive { get; set; } = true;

        public bool SnapTurns
        {
            get { return this.snapTurns; }
            set { this.snapTurns = value; }
        }

        private void Start()
        {
            this._hasAnimator = this.TryGetComponent(out this._animator);
            this.Controller = this.GetComponent<CharacterController>();
            this._input = this.GetComponent<StarterAssetsInputs>();

            this.AssignAnimationIDs();

            // reset our timeouts on start
            this._jumpTimeoutDelta = this.JumpTimeout;
            this._fallTimeoutDelta = this.FallTimeout;
            this.initialLocalRotation = this._mainCamera.transform.localRotation;
            this._input.UiIsVisible(false);
        }

        private void Update()
        {
            //Get new target every frame
            //This has no performance impact, since internally targeting system uses precalculate results cached in a local variable
            this.newTarget = this.targetingSystem.GetClosestTarget(false);
            this._hasAnimator = this.TryGetComponent(out this._animator);

            if (this._input || this.MovementActive == false)
            {
                return;
            }

            this.JumpAndGravity();
            this.GroundedCheck();
            if (this._input == false)
            {
                this.Move();
            }
        }

        private void LateUpdate()
        {
            if (this._input || this.MovementActive == false)
            {
                return;
            }

            ReleaseSnapWhenCameraIsPointingCloseEnoughToTarget();

            this.CameraRotation();

            void ReleaseSnapWhenCameraIsPointingCloseEnoughToTarget()
            {
                if (this.snapTurns && this.newTarget.Exists() == TargetingSystemDataModels.Boolean.True)
                {
                    float releaseThreshold = 0.075f;
                    if (this.secondaryCameraTargetingSystem.GetTargetDataForTarget(this.newTarget, true)
                        .distanceFromTargetToProjectedPoint < releaseThreshold)
                    {
                        this.previousTarget = this.newTarget;
                    }
                }
            }
        }

        private void AssignAnimationIDs()
        {
            this._animIDSpeed = Animator.StringToHash("Speed");
            this._animIDGrounded = Animator.StringToHash("Grounded");
            this._animIDJump = Animator.StringToHash("Jump");
            this._animIDFreeFall = Animator.StringToHash("FreeFall");
            this._animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(this.transform.position.x,
                this.transform.position.y - this.GroundedOffset, this.transform.position.z);
            this.Grounded = Physics.CheckSphere(spherePosition, this.GroundedRadius, this.GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (this._hasAnimator)
            {
                this._animator.SetBool(this._animIDGrounded, this.Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (this._input.look.sqrMagnitude >= _threshold && !this.LockCameraPosition)
            {
                var timeDelta = Mathf.Clamp(Time.deltaTime, 0f, 0.11f);
                this._cinemachineTargetYaw += this._input.look.x * timeDelta;
                this._cinemachineTargetPitch += this._input.look.y * timeDelta;
            }

            // clamp our rotations so our values are limited 360 degrees
            this._cinemachineTargetYaw = ClampAngle(this._cinemachineTargetYaw, float.MinValue, float.MaxValue);
            this._cinemachineTargetPitch = ClampAngle(this._cinemachineTargetPitch, this.BottomClamp, this.TopClamp);

            // Cinemachine will follow this target
            this.CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                this._cinemachineTargetPitch + this.CameraAngleOverride, this._cinemachineTargetYaw, 0.0f);

            //Code made for targeting system 2 snap camera targeting mode.
            //If no snap turning, then use the main cameras initial rotation.
            if (this.snapTurns == false)
            {
                this._mainCamera.transform.localRotation = this.initialLocalRotation;
            }
            else
            {
                SnapToTarget();
                RotateCameraBackToInitialRotation();
            }


            void SnapToTarget()
            {
                if (this.newTarget.Exists() == TargetingSystemDataModels.Boolean.True)
                {
                    this.newTarget = this.secondaryCameraTargetingSystem.GetTargetDataForTarget(this.newTarget, true);

                    var lookDirectionalVector =
                        (Vector3) this.secondaryCameraTargetingSystem.GetTargetDataForTarget(this.newTarget, true)
                            .position - this._mainCamera.transform.position;

                    this.lookRotation = Quaternion.LookRotation(lookDirectionalVector.normalized, Vector3.up);
                    RotateCameraToNewTarget();

                    void RotateCameraToNewTarget()
                    {
                        if (this.previousTarget.geoInfoHashCode != this.newTarget.geoInfoHashCode)
                        {
                            this._mainCamera.transform.rotation = Quaternion.Slerp(this._mainCamera.transform.rotation,
                                this.lookRotation,
                                Time.deltaTime * this.snapSpeed);
                        }
                    }
                }
            }

            void RotateCameraBackToInitialRotation()
            {
                if (this.newTarget.Exists() == TargetingSystemDataModels.Boolean.False)
                {
                    var localRotation = this._mainCamera.transform.localRotation;

                    localRotation = Quaternion.Slerp(localRotation, this.initialLocalRotation,
                        Time.deltaTime * this.revertBackToRestRotationSpeed);
                    this._mainCamera.transform.localRotation = localRotation;

                    this.previousTarget = this.newTarget;
                }
            }
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = this._input.sprint ? this.SprintSpeed : this.MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (this._input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed =
                new Vector3(this.Controller.velocity.x, 0.0f, this.Controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = this._input.analogMovement ? this._input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                this._speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * this.SpeedChangeRate);

                // round speed to 3 decimal places
                this._speed = Mathf.Round(this._speed * 1000f) / 1000f;
            }
            else
            {
                this._speed = targetSpeed;
            }

            this._animationBlend = Mathf.Lerp(this._animationBlend, targetSpeed, Time.deltaTime * this.SpeedChangeRate);

            // normalise input direction
            Vector3 inputDirection = new Vector3(this._input.move.x, 0.0f, this._input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (this._input.move != Vector2.zero)
            {
                this._targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                       this._mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(this.transform.eulerAngles.y, this._targetRotation,
                    ref this._rotationVelocity, this.RotationSmoothTime);

                // rotate to face input direction relative to camera position
                this.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, this._targetRotation, 0.0f) * Vector3.forward;

            // move the player
            this.Controller.Move(targetDirection.normalized * (this._speed * Time.deltaTime) +
                                 new Vector3(0.0f, this.VerticalVelocity + this.AdditionalVelocity, 0.0f) *
                                 Time.deltaTime);

            // update animator if using character
            if (this._hasAnimator)
            {
                this._animator.SetFloat(this._animIDSpeed, this._animationBlend);
                this._animator.SetFloat(this._animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (this.Grounded)
            {
                // reset the fall timeout timer
                this._fallTimeoutDelta = this.FallTimeout;

                // update animator if using character
                if (this._hasAnimator)
                {
                    this._animator.SetBool(this._animIDJump, false);
                    this._animator.SetBool(this._animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (this.VerticalVelocity < 0.0f)
                {
                    this.VerticalVelocity = -2f;
                }

                // Jump
                if (this._input.jump && this._jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    this.VerticalVelocity = Mathf.Sqrt(this.JumpHeight * -2f * this.Gravity);

                    // update animator if using character
                    if (this._hasAnimator)
                    {
                        this._animator.SetBool(this._animIDJump, true);
                    }
                }

                // jump timeout
                if (this._jumpTimeoutDelta >= 0.0f)
                {
                    this._jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                this._jumpTimeoutDelta = this.JumpTimeout;

                // fall timeout
                if (this._fallTimeoutDelta >= 0.0f)
                {
                    this._fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (this._hasAnimator)
                    {
                        this._animator.SetBool(this._animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                this._input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (this.VerticalVelocity < this._terminalVelocity)
            {
                this.VerticalVelocity += this.Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (this.Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(this.transform.position.x, this.transform.position.y - this.GroundedOffset,
                    this.transform.position.z), this.GroundedRadius);
        }
    }
}