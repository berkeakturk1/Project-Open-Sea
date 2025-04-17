using System;
using Unity.Mathematics;
using UnityEngine;
namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        
        public CustomPlaneMesh oceanMesh; // Reference to the ocean mesh
        
        [Serializable]
        public class MovementSettings
        {
            [Header("Basic Movement")] public float ForwardSpeed = 8.0f; // Speed when walking forward
            public float BackwardSpeed = 4.0f; // Speed when walking backwards
            public float StrafeSpeed = 4.0f; // Speed when walking sideways
 
            [Header("Running")] public float RunMultiplier = 2.0f; // Speed when sprinting
            public KeyCode RunKey = KeyCode.LeftShift;
            public float StaminaMax = 100f; // Maximum stamina
            public float StaminaDrainRate = 10f; // How fast stamina drains while running
            public float StaminaRegenRate = 5f; // How fast stamina regenerates
            [Range(0, 100)] public float CurrentStamina = 100f; // Current stamina level

            [Header("Gravity & Jumping")] public float JumpForce = 30f;
            public float JumpCooldown = 0.5f; // Time between jumps
            public bool EnableDoubleJump = true; // Allow double jumping
            public float DoubleJumpForce = 25f; // Force for second jump
            public float FallMultiplier = 2.5f; // Increased gravity while falling
            public float FastFallThreshold = -0.5f; // Velocity threshold to apply extra gravity
            public float MaxFallSpeed = 20f; // Maximum fall speed
            public AudioClip JumpSound; // Sound to play when jumping
            public AudioClip LandSound; // Sound to play when landing

            [Header("Advanced Movement")] public AnimationCurve SlopeCurveModifier =
                new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));

            public float AccelerationRate = 10f; // How quickly to reach target speed
            public float DecelerationRate = 10f; // How quickly to slow down
            [HideInInspector] public float CurrentTargetSpeed = 8f;

            [Header("Footsteps")] public AudioClip[] FootstepSounds; // Array of footstep sounds
            public float FootstepInterval = 0.5f; // Time between footsteps

            private bool m_Running;
            private float m_LastJumpTime;
            private float m_FootstepTimer;
            private bool m_CanDoubleJump;

            public void UpdateDesiredTargetSpeed(Vector2 input)
            {
                if (input == Vector2.zero) return;

                // Default to forward speed
                CurrentTargetSpeed = ForwardSpeed;

                // Override with appropriate speed based on input
                if (input.x > 0 || input.x < 0)
                {
                    // Strafe
                    CurrentTargetSpeed = StrafeSpeed;
                }

                if (input.y < 0)
                {
                    // Backwards
                    CurrentTargetSpeed = BackwardSpeed;
                }

                // Check for running if we have stamina
                if (Input.GetKey(RunKey) && CurrentStamina > 0)
                {
                    CurrentTargetSpeed *= RunMultiplier;
                    m_Running = true;

                    // Drain stamina while running
                    CurrentStamina = Mathf.Max(0, CurrentStamina - StaminaDrainRate * Time.deltaTime);
                }
                else
                {
                    m_Running = false;

                    // Regenerate stamina when not running
                    CurrentStamina = Mathf.Min(StaminaMax, CurrentStamina + StaminaRegenRate * Time.deltaTime);
                }
            }

            public bool CanJump(bool isGrounded)
            {
                // First jump from ground
                if (isGrounded && Time.time - m_LastJumpTime >= JumpCooldown)
                {
                    return true;
                }

                // Double jump in air
                if (!isGrounded && m_CanDoubleJump && EnableDoubleJump)
                {
                    return true;
                }

                return false;
            }

            public void Jump(Rigidbody rigidbody, bool isGrounded, AudioSource audioSource)
            {
                if (!CanJump(isGrounded)) return;

                // Reset velocity Y to ensure consistent jump height
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0f, rigidbody.velocity.z);

                if (isGrounded)
                {
                    // Regular jump
                    rigidbody.AddForce(new Vector3(0f, JumpForce, 0f), ForceMode.Impulse);
                    m_CanDoubleJump = true;

                    // Play jump sound
                    if (JumpSound && audioSource)
                    {
                        audioSource.clip = JumpSound;
                        audioSource.Play();
                    }
                }
                else if (m_CanDoubleJump && EnableDoubleJump)
                {
                    // Double jump
                    rigidbody.AddForce(new Vector3(0f, DoubleJumpForce, 0f), ForceMode.Impulse);
                    m_CanDoubleJump = false;

                    // Play jump sound at different pitch for double jump
                    if (JumpSound && audioSource)
                    {
                        audioSource.pitch = 1.2f;
                        audioSource.clip = JumpSound;
                        audioSource.Play();
                        audioSource.pitch = 1f;
                    }
                }

                m_LastJumpTime = Time.time;
            }

            public void PlayFootstepSound(float velocity, bool isGrounded, AudioSource audioSource)
            {
                if (!isGrounded || FootstepSounds == null || FootstepSounds.Length == 0 || audioSource == null)
                    return;

                // Calculate footstep interval based on velocity
                float speedPercent = Mathf.Clamp01(velocity / (ForwardSpeed * RunMultiplier));
                float adjustedInterval = FootstepInterval * (1f - speedPercent * 0.5f);

                m_FootstepTimer += Time.deltaTime;

                if (velocity > 0.1f && m_FootstepTimer >= adjustedInterval)
                {
                    m_FootstepTimer = 0f;
                    int n = UnityEngine.Random.Range(0, FootstepSounds.Length);
                    audioSource.clip = FootstepSounds[n];
                    audioSource.volume = UnityEngine.Random.Range(0.8f, 1.0f);
                    audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    audioSource.Play();
                }
            }

            public void PlayLandSound(AudioSource audioSource)
            {
                if (LandSound && audioSource)
                {
                    audioSource.clip = LandSound;
                    audioSource.volume = 0.8f;
                    audioSource.Play();
                }
            }

            public bool Running
            {
                get { return m_Running; }
            }

            public float StaminaPercentage
            {
                get { return CurrentStamina / StaminaMax; }
            }
        }

        [Serializable]
        public class AdvancedSettings
        {
            [Header("Ground Detection")]
            public float groundCheckDistance = 0.01f; // Distance for checking if the controller is grounded

            public float stickToGroundHelperDistance = 0.5f; // Stops the character from bouncing off uneven terrain

            [Header("Movement Control")]
            public float slowDownRate = 20f; // Rate at which the controller comes to a stop when there is no input

            public bool airControl = true; // Can the user control direction in the air

            [Header("Environment Interaction")] [Tooltip("Set to 0.1 or more if you get stuck in walls")]
            public float shellOffset = 0.1f; // Reduce the radius to avoid getting stuck in walls

            [Header("Camera Bob")] public bool enableHeadBob = true; // Enable camera bobbing while moving
            public float bobAmplitude = 0.15f; // How much the camera bobs
            public float bobFrequency = 10f; // How fast the camera bobs

            [Header("FOV Effects")] public bool enableFOVKick = true; // Change FOV when running
            public float FOVKickAmount = 5f; // How much FOV changes
            public float FOVKickTime = 0.3f; // How fast FOV transitions

            [Header("Step Climbing")] public bool enableStepClimbing = true; // Enable step climbing feature
            public float maxStepHeight = 0.5f; // Maximum height of steps that can be climbed
            public float stepCheckDistance = 0.8f; // Distance to check for steps
            public float rayOriginOffset = 0.1f; // Offset the ray origin from the ground
            public float stepUpForce = 1000f; // Force applied upwards when climbing steps
            public float stepForwardForce = 500f; // Force applied forward when climbing steps
            public float stepUpDuration = 0.1f; // Direct step up duration
            public bool directStepUp = true; // Use direct step up instead of forces
            public bool debugStepRays = false; // Show debug rays in scene view
        }

        [Header("References")] public Camera cam;
        public AudioSource audioSource;

        [Header("Settings")] public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();

        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;
        private float m_OriginalCameraFOV;
        private Vector3 m_CameraStartPosition;
        private float m_BobCycle;
        private bool m_WasRunning;
        private bool m_IsStepClimbing;
        private Vector3 m_TargetStepPosition;
        private float m_StepStartTime;
        private bool m_IsDirectStepping;

        public Vector3 Velocity
        {
            get { return m_RigidBody.velocity; }
        }

        public bool Grounded
        {
            get { return m_IsGrounded; }
        }

        public bool Jumping
        {
            get { return m_Jumping; }
        }

        public bool Running
        {
            get { return movementSettings.Running; }
        }

        public float StaminaPercentage
        {
            get { return movementSettings.StaminaPercentage; }
        }

        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init(transform, cam.transform);

            // Store original camera settings
            if (cam)
            {
                m_OriginalCameraFOV = cam.fieldOfView;
                m_CameraStartPosition = cam.transform.localPosition;
            }

            // Add audio source if needed
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // Make sound 2D
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            /*
            Vector3 newpos = new Vector3(transform.position.x, oceanMesh.transform.position.y, transform.position.z);
            oceanMesh.transform.SetPositionAndRotation(newpos, Quaternion.identity);
            */
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Break();
            }

            RotateView();

            if (Input.GetButtonDown("Jump") && !m_Jump)
            {
                m_Jump = true;
            }

            UpdateCameraEffects();
        }

        private void UpdateCameraEffects()
        {
            if (!cam) return;

            // Handle camera bob
            if (advancedSettings.enableHeadBob)
            {
                if (m_IsGrounded && m_RigidBody.velocity.magnitude > 0.1f)
                {
                    float bobSpeed = (Running ? 2f : 1f) * advancedSettings.bobFrequency;
                    m_BobCycle += m_RigidBody.velocity.magnitude * Time.deltaTime * bobSpeed;

                    float bobAmplitudeAdjusted = advancedSettings.bobAmplitude * (Running ? 1.2f : 1f);

                    // Calculate vertical and horizontal bob
                    float verticalBob = Mathf.Sin(m_BobCycle * Mathf.PI) * bobAmplitudeAdjusted;
                    float horizontalBob = Mathf.Cos(m_BobCycle * Mathf.PI * 0.5f) * bobAmplitudeAdjusted * 0.5f;

                    // Apply bob to camera position
                    Vector3 targetCamPos = m_CameraStartPosition + new Vector3(horizontalBob, verticalBob, 0f);
                    cam.transform.localPosition =
                        Vector3.Lerp(cam.transform.localPosition, targetCamPos, Time.deltaTime * 5f);
                }
                else
                {
                    // Return to starting position when not moving
                    cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, m_CameraStartPosition,
                        Time.deltaTime * 5f);
                    m_BobCycle = 0;
                }
            }

            // Handle FOV kick when running
            if (advancedSettings.enableFOVKick)
            {
                float targetFOV = Running ? m_OriginalCameraFOV + advancedSettings.FOVKickAmount : m_OriginalCameraFOV;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV,
                    Time.deltaTime * (1f / advancedSettings.FOVKickTime));
            }
        }

        private void FixedUpdate()
        {
            // Skip physics when direct stepping
            if (m_IsDirectStepping)
            {
                UpdateDirectStepUp();
                return;
            }

            bool wasGrounded = m_IsGrounded;
            GroundCheck();

            // Play landing sound when hitting the ground
            if (!wasGrounded && m_IsGrounded && m_PreviouslyGrounded)
            {
                movementSettings.PlayLandSound(audioSource);
            }

            Vector2 input = GetInput();

            // Check for step climbing if enabled and we're trying to move forward
            if (advancedSettings.enableStepClimbing && m_IsGrounded && input.y > 0.1f)
            {
                StepClimbingCheck();
            }

            // Calculate current velocity
            float currentSpeed = new Vector2(m_RigidBody.velocity.x, m_RigidBody.velocity.z).magnitude;

            // Apply movement forces if there's input or if we need to slow down
            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) &&
                (advancedSettings.airControl || m_IsGrounded))
            {
                // Calculate desired movement direction
                Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

                // Scale by target speed
                desiredMove *= movementSettings.CurrentTargetSpeed;

                // Get current horizontal velocity
                Vector3 currentVelocity = new Vector3(m_RigidBody.velocity.x, 0, m_RigidBody.velocity.z);

                // Calculate force needed to reach desired velocity
                Vector3 velocityChange = desiredMove - currentVelocity;

                // Apply different force based on ground state
                if (m_IsGrounded)
                {
                    m_RigidBody.AddForce(velocityChange * movementSettings.AccelerationRate, ForceMode.Acceleration);
                }
                else
                {
                    // Reduced control in air
                    m_RigidBody.AddForce(velocityChange * movementSettings.AccelerationRate * 0.5f,
                        ForceMode.Acceleration);
                }
            }
            else if (m_IsGrounded)
            {
                // Apply deceleration when no input
                Vector3 brakeForce = -m_RigidBody.velocity * movementSettings.DecelerationRate;
                brakeForce.y = 0; // Don't affect vertical movement
                m_RigidBody.AddForce(brakeForce, ForceMode.Acceleration);
            }

            if (m_IsGrounded)
            {
                m_RigidBody.drag = 5f;

                // Play footstep sounds
                movementSettings.PlayFootstepSound(currentSpeed, m_IsGrounded, audioSource);

                // Handle jump
                if (m_Jump)
                {
                    movementSettings.Jump(m_RigidBody, m_IsGrounded, audioSource);
                    m_Jumping = true;
                }

                // Put rigidbody to sleep if completely still
                if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon &&
                    m_RigidBody.velocity.magnitude < 0.5f)
                {
                    m_RigidBody.Sleep();
                }
            }
            else
            {
                m_RigidBody.drag = 0f;

                // Handle double jump
                if (m_Jump)
                {
                    movementSettings.Jump(m_RigidBody, m_IsGrounded, audioSource);
                }

                // Apply extra gravity when falling
                if (m_RigidBody.velocity.y < movementSettings.FastFallThreshold)
                {
                    // Calculate extra gravity force
                    float extraGravity = Physics.gravity.y * (movementSettings.FallMultiplier - 1);

                    // Apply the extra force
                    m_RigidBody.AddForce(new Vector3(0, extraGravity, 0), ForceMode.Acceleration);

                    // Clamp maximum fall speed
                    if (m_RigidBody.velocity.y < -movementSettings.MaxFallSpeed)
                    {
                        m_RigidBody.velocity = new Vector3(
                            m_RigidBody.velocity.x,
                            -movementSettings.MaxFallSpeed,
                            m_RigidBody.velocity.z
                        );
                    }
                }

                // Keep player grounded on slopes
                if (m_PreviouslyGrounded && !m_Jumping)
                {
                    StickToGroundHelper();
                }
            }

            m_Jump = false;
            m_WasRunning = Running;
        }

        private void StepClimbingCheck()
        {
            if (m_IsStepClimbing || m_IsDirectStepping)
                return;

            float radius = m_Capsule.radius;
            float rayHeight = advancedSettings.rayOriginOffset; // Start ray slightly above ground

            // Forward ray to detect obstacle
            Vector3 forwardRayStart = transform.position + new Vector3(0, rayHeight, 0);
            Vector3 forwardDir = transform.forward;

            if (advancedSettings.debugStepRays)
            {
                Debug.DrawRay(forwardRayStart, forwardDir * advancedSettings.stepCheckDistance, Color.red, 0.2f);
                Debug.Log("Step check from height: " + rayHeight);
            }

            RaycastHit forwardHit;
            if (Physics.Raycast(forwardRayStart, forwardDir, out forwardHit, advancedSettings.stepCheckDistance))
            {
                if (advancedSettings.debugStepRays)
                {
                    Debug.Log("Forward hit: " + forwardHit.collider.name + " at " + forwardHit.point);
                }

                // Found an obstacle, now cast ray from above to see if it's a step
                Vector3 aboveRayStart = transform.position + new Vector3(0, advancedSettings.maxStepHeight, 0)
                                                           + (forwardDir * forwardHit.distance);

                if (advancedSettings.debugStepRays)
                {
                    Debug.DrawRay(aboveRayStart, Vector3.down * advancedSettings.maxStepHeight, Color.green, 0.2f);
                }

                RaycastHit aboveHit;
                if (Physics.Raycast(aboveRayStart, Vector3.down, out aboveHit, advancedSettings.maxStepHeight))
                {
                    if (advancedSettings.debugStepRays)
                    {
                        Debug.Log("Above ray hit: " + aboveHit.collider.name + " at height " + aboveHit.point.y);
                        Debug.Log("Current position height: " + transform.position.y);
                        Debug.Log("Height difference: " + (aboveHit.point.y - transform.position.y));
                    }

                    // Check if the hit point is higher than our current position and within step height
                    float heightDifference = aboveHit.point.y - transform.position.y;

                    if (heightDifference > 0.05f && heightDifference <= advancedSettings.maxStepHeight)
                    {
                        if (advancedSettings.debugStepRays)
                        {
                            Debug.Log("Valid step detected! Height: " + heightDifference);
                        }

                        if (advancedSettings.directStepUp)
                        {
                            StartDirectStepUp(aboveHit.point);
                        }
                        else
                        {
                            ClimbStep();
                        }
                    }
                    else
                    {
                        if (advancedSettings.debugStepRays)
                        {
                            Debug.Log("Invalid step height: " + heightDifference);
                        }
                    }
                }
                else
                {
                    // No hit when casting down - might be a higher obstacle
                    if (advancedSettings.debugStepRays)
                    {
                        Debug.Log("No ground above the obstacle - too high to step");
                    }
                }
            }
        }

        private void StartDirectStepUp(Vector3 targetPoint)
        {
            m_IsDirectStepping = true;
            m_StepStartTime = Time.time;

            // Set the target position (maintain X/Z from the hit, use existing velocity)
            m_TargetStepPosition = new Vector3(
                targetPoint.x,
                targetPoint.y + 0.05f, // Small offset to ensure we're above the step
                targetPoint.z
            );

            // Preserve horizontal velocity but zero vertical
            Vector3 horizontalVelocity = m_RigidBody.velocity;
            horizontalVelocity.y = 0;

            // Temporarily disable physics
            m_RigidBody.isKinematic = true;

            if (advancedSettings.debugStepRays)
            {
                Debug.Log("Starting direct step up to: " + m_TargetStepPosition);
            }
        }

        private void UpdateDirectStepUp()
        {
            float elapsed = Time.time - m_StepStartTime;
            float percent = elapsed / advancedSettings.stepUpDuration;

            if (percent >= 1.0f)
            {
                // Step is complete
                FinishDirectStepUp();
                return;
            }

            // Smooth step for better motion
            float t = Mathf.SmoothStep(0, 1, percent);

            // Preserve our X/Z velocity direction but elevate the Y
            Vector3 currentPos = transform.position;
            Vector3 targetPos = new Vector3(
                Mathf.Lerp(currentPos.x, m_TargetStepPosition.x, t),
                Mathf.Lerp(currentPos.y, m_TargetStepPosition.y, t),
                Mathf.Lerp(currentPos.z, m_TargetStepPosition.z, t)
            );

            // Move toward the target
            transform.position = targetPos;

            if (advancedSettings.debugStepRays)
            {
                Debug.DrawLine(currentPos, targetPos, Color.yellow, 0.2f);
            }
        }

        private void FinishDirectStepUp()
        {
            if (advancedSettings.debugStepRays)
            {
                Debug.Log("Finished direct step up");
            }

            // Ensure we're at the right position
            transform.position = m_TargetStepPosition;

            // Re-enable physics
            m_RigidBody.isKinematic = false;

            // Add a bit of forward velocity to keep momentum
            m_RigidBody.velocity = transform.forward * movementSettings.CurrentTargetSpeed;

            m_IsDirectStepping = false;
        }

        private void ClimbStep()
        {
            // Apply stronger upward force to climb the step
            m_RigidBody.AddForce(Vector3.up * advancedSettings.stepUpForce, ForceMode.Acceleration);

            // Apply additional forward force to help clear the step
            m_RigidBody.AddForce(transform.forward * advancedSettings.stepForwardForce, ForceMode.Acceleration);

            // Temporarily reduce gravity influence
            m_RigidBody.useGravity = false;

            if (advancedSettings.debugStepRays)
            {
                Debug.Log("Applied step climbing forces: Up=" + advancedSettings.stepUpForce +
                          ", Forward=" + advancedSettings.stepForwardForce);
            }

            // Set flag to prevent multiple forces in a single frame
            m_IsStepClimbing = true;

            // Reset flag and gravity after a short delay
            Invoke("ResetStepClimbing", 0.2f);
        }

        private void ResetStepClimbing()
        {
            m_IsStepClimbing = false;
            m_RigidBody.useGravity = true;

            if (advancedSettings.debugStepRays)
            {
                Debug.Log("Step climbing reset, gravity restored");
            }
        }

        private float SlopeMultiplier()
        {
            float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
            return movementSettings.SlopeCurveModifier.Evaluate(angle);
        }

        private void StickToGroundHelper()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset),
                    Vector3.down, out hitInfo,
                    ((m_Capsule.height / 2f) - m_Capsule.radius) +
                    advancedSettings.stickToGroundHelperDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                {
                    m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
                }
            }
        }

        private Vector2 GetInput()
        {
            Vector2 input = new Vector2
            {
                x = Input.GetAxis("Horizontal"),
                y = Input.GetAxis("Vertical")
            };
            movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }

        private void RotateView()
        {
            // Avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // Get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation(transform, cam.transform);

            if (m_IsGrounded || advancedSettings.airControl)
            {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                m_RigidBody.velocity = velRotation * m_RigidBody.velocity;
            }
        }

        /// <summary>
        /// Sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        /// </summary>
        private void GroundCheck()
        {
            m_PreviouslyGrounded = m_IsGrounded;
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset),
                    Vector3.down, out hitInfo,
                    ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }

            if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
            {
                m_Jumping = false;
            }
        }

        // Optional: Add these methods to allow other scripts to access controller state

        /// <summary>
        /// Apply an external force to the character, such as explosion force
        /// </summary>
        public void AddForce(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
        {
            if (m_RigidBody != null)
            {
                m_RigidBody.AddForce(force, forceMode);
            }
        }

        /// <summary>
        /// Teleport the character to a new position
        /// </summary>
        public void Teleport(Vector3 position)
        {
            m_RigidBody.velocity = Vector3.zero;
            transform.position = position;
        }

        // Visual debugging
        private void OnDrawGizmosSelected()
        {
            // Draw step detection rays
            if (advancedSettings.enableStepClimbing)
            {
                Gizmos.color = Color.green;
                Vector3 stepRayStart = transform.position + new Vector3(0, advancedSettings.rayOriginOffset, 0);
                Gizmos.DrawRay(stepRayStart, transform.forward * advancedSettings.stepCheckDistance);

                Gizmos.color = Color.blue;
                Vector3 upperRayStart = transform.position + new Vector3(0, advancedSettings.maxStepHeight, 0);
                Gizmos.DrawRay(upperRayStart, transform.forward * advancedSettings.stepCheckDistance);
            }
        }
    }
}