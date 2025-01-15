using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GinjaGaming.FinalCharacterController
{
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyShipController : MonoBehaviour
    {
        #region Class Variables

        [Header("Components")]
        [SerializeField] private Camera _playerCamera;

        // For reference: We'll mimic the same stats as the CharacterController version.
        public float RotationMismatch { get; private set; } = 0f;
        public bool IsRotatingToTarget { get; private set; } = false;

        [Header("Base Movement")]
        public float walkAcceleration = 25f;
        public float walkSpeed = 2f;
        public float runAcceleration = 35f;
        public float runSpeed = 4f;
        public float sprintAcceleration = 50f;
        public float sprintSpeed = 7f;
        public float inAirAcceleration = 25f;
        public float drag = 20f;
        public float inAirDrag = 5f;
        public float gravity = 25f;
        public float terminalVelocity = 50f;
        public float jumpSpeed = 0.8f;
        public float movingThreshold = 0.01f;

        [Header("Animation / Rotation")]
        public float playerModelRotationSpeed = 10f;
        public float rotateToTargetTime = 0.67f;

        [Header("Camera Settings")]
        public float lookSenseH = 0.1f;
        public float lookSenseV = 0.1f;
        public float lookLimitV = 89f;

        [Header("Environment Details")]
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckOffset = 0.5f;
        [SerializeField] private float slopeLimit = 45f; // used for steep walls

        private PlayerLocomotionInput _playerLocomotionInput;
        private PlayerState _playerState;

        // Internals
        private Rigidbody _rb;
        private Vector2 _cameraRotation = Vector2.zero;
        private Vector2 _playerTargetRotation = Vector2.zero;

        private bool _jumpedLastFrame = false;
        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        private float _antiBump;
        private float _stepOffset; // Not really used in a rigidbody approach

        private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();

            // Instead of CharacterController, we have a Rigidbody
            _rb = GetComponent<Rigidbody>();

            // Lock rotation on X/Z so we don't topple over
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // We'll mimic the old step offset logic by storing something,
            // though we can't apply it the same way with a rigidbody
            _stepOffset = 0.3f;

            // Using sprintSpeed for anti-bump (like before)
            _antiBump = sprintSpeed;
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            // Update state machine
            UpdateMovementState();

            // Handle vertical & jump logic (just sets _verticalVelocity, then applied in FixedUpdate)
            HandleVerticalMovement();
        }

        private void FixedUpdate()
        {
            // Actual movement is applied in FixedUpdate for Rigidbody
            HandleLateralMovement();

            // Apply final velocity changes
            ApplyVelocity();
           
            Debug.Log($"MovementInput = {_playerLocomotionInput.MovementInput}, finalVel = {_rb.velocity}");
            Debug.Log($"Position = {transform.position}, Velocity = {_rb.velocity}");


            // Then do camera rotation in LateUpdate-like approach
        }
        #endregion

        #region Movement State
        private void UpdateMovementState()
        {
            _lastMovementState = _playerState.CurrentPlayerMovementState;

            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;
            bool isMovingLaterally = IsMovingLaterally();
            bool isSprinting = _playerLocomotionInput.SprintToggledOn && isMovingLaterally;
            bool isWalking = isMovingLaterally && (!canRun || _playerLocomotionInput.WalkToggledOn);
            bool isGrounded = IsGrounded();

            // Determine lateral movement state
            PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                                                  isSprinting ? PlayerMovementState.Sprinting :
                                                  (isMovingLaterally || isMovementInput) ? PlayerMovementState.Running
                                                                                        : PlayerMovementState.Idling;

            _playerState.SetPlayerMovementState(lateralState);

            // Now handle jump/fall states
            if ((!isGrounded || _jumpedLastFrame) && _rb.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _rb.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
            }
        }
        #endregion

        #region Vertical Movement
        private void HandleVerticalMovement()
        {
            bool isGrounded = _playerState.InGroundedState();

            // Gravity accumulates in _verticalVelocity
            _verticalVelocity -= gravity * Time.deltaTime;

            // If grounded and falling, set a small negative velocity to keep us grounded
            if (isGrounded && _verticalVelocity < 0)
                _verticalVelocity = -_antiBump;

            // Jump
            if (_playerLocomotionInput.JumpPressed && isGrounded)
            {
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
                _jumpedLastFrame = true;
            }

            // If we just left the ground but were in a grounded state, add a bit of antiBump
            if (_playerState.IsStateGroundedState(_lastMovementState) && !isGrounded)
            {
                _verticalVelocity += _antiBump;
            }

            // Clamp at terminal velocity
            if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
            {
                _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
            }
        }
        #endregion

        #region Lateral Movement
        private void HandleLateralMovement()
        {
            // We'll figure out lateral velocity in the XZ plane
            bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = _playerState.InGroundedState();
            bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;

            // State-based acceleration and speed
            float lateralAcceleration = !isGrounded ? inAirAcceleration :
                                        isWalking ? walkAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;

            float clampLateralMagnitude = !isGrounded ? sprintSpeed :
                                          isWalking ? walkSpeed :
                                          isSprinting ? sprintSpeed : runSpeed;

            // Build movement direction from camera
            Vector3 camForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 camRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized;
            Vector3 movementDirection = camRightXZ * _playerLocomotionInput.MovementInput.x 
                                      + camForwardXZ * _playerLocomotionInput.MovementInput.y;

            // We'll store the current velocity
            Vector3 currentVel = _rb.velocity;

            // Apply lateral acceleration
            Vector3 desiredLateral = new Vector3(currentVel.x, 0f, currentVel.z);
            Vector3 moveDelta = movementDirection * (lateralAcceleration * Time.fixedDeltaTime);
            desiredLateral += moveDelta;

            // Apply drag
            float dragMagnitude = isGrounded ? drag : inAirDrag;
            Vector3 dragVector = desiredLateral.normalized * (dragMagnitude * Time.fixedDeltaTime);
            desiredLateral = (desiredLateral.magnitude > dragVector.magnitude) ? desiredLateral - dragVector : Vector3.zero;

            // Clamp to top speed
            desiredLateral = Vector3.ClampMagnitude(desiredLateral, clampLateralMagnitude);

            // Reassign to x/z
            currentVel.x = desiredLateral.x;
            currentVel.z = desiredLateral.z;

            // We'll handle steep walls
            if (!isGrounded)
            {
                currentVel = HandleSteepWalls(currentVel);
            }

            // We'll store back into the rigidbody velocity, 
            // but do NOT apply it yet (we do that in ApplyVelocity()).
            _rb.velocity = currentVel;
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        {
            // A simple approach is to do a spherecast downward to get normal
            // then see if it's too steep
            RaycastHit hit;
            Vector3 origin = transform.position + (Vector3.up * 0.3f);
            float radius = 0.3f;
            if (Physics.SphereCast(origin, radius, Vector3.down, out hit, 0.5f, _groundLayers))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                bool validAngle = angle <= slopeLimit;
                if (!validAngle && velocity.y < 0f)
                {
                    // Project away from the steep slope
                    velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                }
            }

            return velocity;
        }

        private void ApplyVelocity()
        {
            // Finally apply the stored vertical velocity to the rigidbody
            Vector3 finalVel = _rb.velocity;
            finalVel.y = _verticalVelocity;
            _rb.velocity = finalVel;
        }
        #endregion

        #region Late Update Logic
        private void LateUpdate()
        {
            UpdateCameraRotation();
        }

        private void UpdateCameraRotation()
        {
            // Same approach for camera rotation
            _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

            _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;

            float rotationTolerance = 90f;
            bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
            IsRotatingToTarget = (_rotatingToTargetTimer > 0);

            // Rotate if not idling
            if (!isIdling)
            {
                RotatePlayerToTarget();
            }
            else if (Mathf.Abs(RotationMismatch) > rotationTolerance || IsRotatingToTarget)
            {
                UpdateIdleRotation(rotationTolerance);
            }

            // Update the camera's pitch/yaw
            _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

            // Compute RotationMismatch
            Vector3 camForwardProjectedXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            RotationMismatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            // If we need to rotate
            if (Mathf.Abs(RotationMismatch) > rotationTolerance)
            {
                _rotatingToTargetTimer = rotateToTargetTime;
                _isRotatingClockwise = RotationMismatch > rotationTolerance;
            }
            _rotatingToTargetTimer -= Time.deltaTime;

            if ((_isRotatingClockwise && RotationMismatch > 0f) ||
                (!_isRotatingClockwise && RotationMismatch < 0f))
            {
                RotatePlayerToTarget();
            }
        }

        private void RotatePlayerToTarget()
        {
            // Actually rotate the rigidbody (or transform)
            Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
            Quaternion newRot = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);

            // Apply rotation to rigidbody
            _rb.MoveRotation(newRot);
        }
        #endregion

        #region State Checks
        private bool IsMovingLaterally()
        {
            Vector3 velXZ = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            return velXZ.magnitude > movingThreshold;
        }

        private bool IsGrounded()
        {
            // Sphere check below the player's feet
            // We can do it at (transform.position + Vector3.up * smallOffset)
            Vector3 spherePos = transform.position + (Vector3.up * groundCheckOffset);
            bool grounded = Physics.CheckSphere(spherePos, groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);
            return grounded;
        }

        private bool CanRun()
        {
            // Same diagonal check
            return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
        }
        #endregion
    }
}
