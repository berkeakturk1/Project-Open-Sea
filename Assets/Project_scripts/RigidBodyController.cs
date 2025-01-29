using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GinjaGaming.FinalCharacterController
{
    [DefaultExecutionOrder(-1)]
    public class RigidbodyController : MonoBehaviour
    {
        #region Class Variables
        [Header("Components")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private CapsuleCollider _capsuleCollider;
        
        [Header("Environment Details")]
        [SerializeField] private LayerMask _groundLayers;
        
        private PlayerState _playerState;
        private bool InventoryMode = false;
        
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float sprintSpeedMultiplier = 1.5f;
        public float jumpForce = 5f;
        public float rotationSpeed = 10f;
        public float movingThreshold = 0.01f;
        
        [Header("Physics Settings")]
        public float gravityMultiplier = 2.0f; // Adjust this value for extra gravity

        [Header("Camera Settings")]
        public float lookSenseH = 2f;
        public float lookSenseV = 2f;
        public float lookLimitV = 60f;

        private PlayerLocomotionInput _playerLocomotionInput;
        
        private PlayerOnShipController _playerOnShipController;

        private Vector2 _cameraRotation = Vector2.zero;
        private bool _isGrounded = true;
        private bool _isSprinting = false;
        
        private bool _jumpedLastFrame = false;
        private bool _isRotatingClockwise = false;
        private float _rotatingToTargetTimer = 0f;
        private float _verticalVelocity = 0f;
        private float _antiBump;
        private float _stepOffset;

        private Vector3 _movementInput;
        
        private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;
        #endregion

        #region Startup
        private void Awake()
        {
            _playerOnShipController = GetComponent<PlayerOnShipController>();
            
            _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
            _playerState = GetComponent<PlayerState>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Start()
        {
            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        #endregion

        #region Update Logic
        private void Update()
        {
            UpdateMovementState();
            HandleInput();
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleMouseVisibility(InventoryMode);
            }
            UpdateCameraRotation();
        }
        
        public void ToggleMouseVisibility(bool visible)
        {
            // Enable or disable the cursor visibility
            Cursor.lockState = visible ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = visible ? false : true;;

            InventoryMode = visible ? false : true;
            // Disable camera movement by setting sensitivity to 0 if visible, otherwise restore default values

        }
        
        private void UpdateMovementState()
        {
           _lastMovementState = _playerState.CurrentPlayerMovementState;

            bool canRun = CanRun();
            bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;             //order
            bool isMovingLaterally = IsMovingLaterally();                                            //matters
            bool isSprinting = _playerLocomotionInput.SprintToggledOn && isMovingLaterally;          //order
            bool isWalking = isMovingLaterally && (!canRun || _playerLocomotionInput.WalkToggledOn); //matters
            bool isGrounded = true;

            PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                isSprinting ? PlayerMovementState.Sprinting :
                isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;

            _playerState.SetPlayerMovementState(lateralState);

            // Control Airborn State
            if ((!isGrounded || _jumpedLastFrame) && _rigidbody.velocity.y > 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                _jumpedLastFrame = false;
            }
            else if ((!isGrounded || _jumpedLastFrame) && _rigidbody.velocity.y <= 0f)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                _jumpedLastFrame = false;
            }
            
        }

        private void FixedUpdate()
        {
            HandleMovement();
            ApplyExtraGravity();
        }
        
        private void ApplyExtraGravity()
        {
            // Apply extra gravity only when the player is not grounded
            if (!_isGrounded)
            {
                Vector3 extraGravityForce = Physics.gravity * gravityMultiplier;
                _rigidbody.AddForce(extraGravityForce, ForceMode.Acceleration);
            }
        }
        
        private void HandleInput()
        {
            // Get movement input (normalized to avoid fast diagonal movement)
            Vector2 movementInput = _playerLocomotionInput.MovementInput;
            _movementInput = new Vector3(movementInput.x, 0f, movementInput.y).normalized;

            // Check sprint toggle
            _isSprinting = _playerLocomotionInput.SprintToggledOn;

            // Handle jump input
            if (_playerLocomotionInput.JumpPressed && _isGrounded)
            {
                Jump();
            }
        }

        private void HandleMovement()
        {
            // Calculate target velocity based on input
            float currentSpeed = _isSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;
            Vector3 targetDirection = _playerCamera.transform.TransformDirection(_movementInput);
            targetDirection.y = 0f; // Flatten direction to prevent vertical movement from camera
            targetDirection.Normalize();

            Vector3 targetVelocity = targetDirection * currentSpeed;
            Vector3 velocityChange = targetVelocity - new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            // Apply movement force
            _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);

            // Rotate toward movement direction if there is input
            if (_movementInput.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }

        private void Jump()
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _isGrounded = false; // Temporarily disable grounded check
            
        }
        #endregion

        #region Camera Logic
        private void UpdateCameraRotation()
        {
            if (InventoryMode)
                return;
            
            if (_playerOnShipController.isAtHelm)
                return;
            // Get look input and adjust camera rotation
            Vector2 lookInput = _playerLocomotionInput.LookInput;
            _cameraRotation.x += lookInput.x * lookSenseH;
            _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookInput.y * lookSenseV, -lookLimitV, lookLimitV);

            // Rotate player horizontally
            transform.rotation = Quaternion.Euler(0f, _cameraRotation.x, 0f);

            // Rotate camera vertically
            _playerCamera.transform.localRotation = Quaternion.Euler(_cameraRotation.y, 0f, 0f);
        }
        #endregion

        #region Collision Logic
        private void OnCollisionEnter(Collision collision)
        {
            // Check if grounded by detecting collisions with the ground layer
            if (collision.contacts[0].normal.y > 0.5f)
            {
                _isGrounded = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // If no longer touching ground, set grounded to false
            _isGrounded = false;
        }
        #endregion
        
        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

            return lateralVelocity.magnitude > movingThreshold;
        }
        private bool CanRun()
        {
            // This means player is moving diagonally at 45 degrees or forward, if so, we can run
            return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
        }
        
        private bool IsGrounded()
        {
            bool grounded = _playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();

            return grounded;
        }
        
        private bool IsGroundedWhileGrounded()
        {
            // Position the sphere slightly below the player's feet
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - (_capsuleCollider.height / 2f) + 0.1f, transform.position.z);
            float radius = _capsuleCollider.radius * 0.95f; // Slightly smaller than the collider radius to avoid edge detection issues

            // Use a small offset to ensure proper detection
            bool grounded = Physics.CheckSphere(spherePosition, radius, _groundLayers, QueryTriggerInteraction.Ignore);

            return true;
        }


        private bool IsGroundedWhileAirborne()
        {
            float rayLength = (_capsuleCollider.height / 2f) + 0.1f; // Add a small margin for ground detection
            Vector3 rayOrigin = transform.position + Vector3.up * (_capsuleCollider.height / 2f); // Start slightly above the player

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, _groundLayers))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up); // Calculate slope angle
                return angle <= 50f; // Replace 50f with your slope limit
            }

            return true;
        }

        
        
    }
}
