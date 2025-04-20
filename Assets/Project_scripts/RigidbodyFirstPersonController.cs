using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        
        
        private CustomPlaneMesh oceanMesh; // Reference to the ocean mesh
        private GerstnerWaveManager waveManager; // Reference to the ocean mesh

        public OceanGridGenerator oceanGridGenerator;
        
        public PlayerState playerState;
            
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
            
            [Header("Swimming")]
            public float SwimSpeed = 5.0f; // Base swimming speed
            public float SwimSprintMultiplier = 1.5f; // Sprint multiplier when swimming
            public float SwimmingStaminaDrainRate = 15f; // How fast stamina drains while swimming sprinting
            public float VerticalSwimSpeed = 3.0f; // Speed for vertical swimming
            public float WaterDrag = 10f; // Drag when in water
            public float WaterAngularDrag = 1f; // Angular drag when in water
            public float BuoyancyForce = 12f; // Force pushing player up in water
            public float SwimAccelerationRate = 4f; // How quickly to reach target swim speed
            public float SwimDecelerationRate = 4f; // How quickly to slow down in water
            public float UnderwaterFogDensity = 0.1f; // Fog density when underwater
            public Color UnderwaterFogColor = new Color(0.1f, 0.2f, 0.4f, 0.6f); // Underwater fog color
            public AudioClip EnterWaterSound; // Sound when entering water
            public AudioClip ExitWaterSound; // Sound when exiting water
            public AudioClip UnderwaterAmbience; // Looping underwater sound
            public float BreathHoldTime = 20f; // How long player can hold breath underwater
            public float OxygenRegenRate = 10f; // How fast oxygen regenerates
            [Range(0, 100)] public float CurrentOxygen = 100f; // Current oxygen level

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
            public OceanGridGenerator oceanGrid;
            
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
            
            [Header("Swimming")]
            public float waterCheckDistance = 0.5f; // Distance to check for water surface
            public LayerMask waterLayer; // Layer mask for water detection
            public bool useWaterEffects = true; // Enable water visual effects
            public bool causeSplashes = true; // Enable splash effects
            public GameObject splashEffectPrefab; // Splash particle effect
            public float splashThresholdSpeed = 3f; // Speed needed to create splash
            public float surfaceOffset = 0.1f; // Offset from water surface for visuals
        }
        
        private bool m_IsInWater = false;
        private bool m_WasInWater = false;
        private bool m_IsUnderwater = false;
        private bool m_WasUnderwater = false;
        private float m_WaterSurfaceHeight = 0f;
        private float m_InitialDrag;
        private float m_InitialAngularDrag;
        private AudioSource m_UnderwaterAudioSource;
        private float m_DefaultFogDensity;
        private Color m_DefaultFogColor;
        private bool m_IsDrowning = false;
        private Vector3 m_LastSplashPosition;
        private float m_LastSplashTime;
        
        // Public properties for swimming state
        public bool IsInWater
        {
            get { return m_IsInWater; }
        }
        
        public bool IsUnderwater
        {
            get { return m_IsUnderwater; }
        }
        
        public float OxygenPercentage
        {
            get { return movementSettings.CurrentOxygen / movementSettings.BreathHoldTime; }
        }
        
        // Get the water height at a specific world position
        private float GetWaterHeightAtPosition(Vector3 position)
        {
            float waterHeight = 0f;
    
            // Try to use the currently cached wave manager first
            if (waveManager != null)
            {
                Vector3 wavePosition = waveManager.CalculateGerstnerWave(position.x, position.z, Time.time);
                waterHeight = wavePosition.y;
                return waterHeight;
            }
    
            // If no wave manager cached, try to find the active one
            waveManager = FindActiveWaveManager();
    
            if (waveManager != null)
            {
                // Use the found wave manager
                Vector3 wavePosition = waveManager.CalculateGerstnerWave(position.x, position.z, Time.time);
                waterHeight = wavePosition.y;
                return waterHeight;
            }
    
            // If all else fails, fall back to a default height
            // Get a reasonable guess at water height from OceanGridGenerator
            OceanGridGenerator oceanGrid = FindObjectOfType<OceanGridGenerator>();
            if (oceanGrid != null && oceanGrid.transform.childCount > 0)
            {
                // Use the Y position of the first active child as a fallback
                foreach (Transform child in oceanGrid.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        return child.position.y;
                    }
                }
            }
    
            // Last resort - use a hardcoded value (match your OceanGridGenerator's default height)
            return 25f;
        }
        
        // New method for water detection
        private void WaterCheck()
        {
            // Store previous state
            m_WasInWater = m_IsInWater;
            m_WasUnderwater = m_IsUnderwater;
    
            // Get water surface height from the active ocean plane at player's position
            float waterHeight = GetWaterHeightAtPosition(transform.position);
    
            // Store water height for other methods
            m_WaterSurfaceHeight = waterHeight;
    
            // Check if player is in water (head position is below water)
            float headHeight = transform.position.y + m_Capsule.height * 0.75f;
            m_IsUnderwater = headHeight < waterHeight;
    
            // Check if any part of the player is in water (feet position is below water)
            float feetHeight = transform.position.y - m_Capsule.height * 0.5f + m_Capsule.radius;
            m_IsInWater = feetHeight < waterHeight && transform.position.y - m_Capsule.height * 0.1f < waterHeight;
    
            // Handle state changes
            if (m_IsInWater != m_WasInWater)
            {
                OnWaterStateChanged();
            }
    
            if (m_IsUnderwater != m_WasUnderwater)
            {
                OnUnderwaterStateChanged();
            }
    
            // Handle being in water
            if (m_IsInWater)
            {
                HandleSwimming();
            }
        }
        
        // Handle when water state changes (entering/exiting water)
        private void OnWaterStateChanged()
        {
            // In OnWaterStateChanged method, when entering water:
            // In OnWaterStateChanged method, when entering water:
            if (m_IsInWater)
            {
                // Create splash if entering with sufficient velocity
                if (advancedSettings.causeSplashes && m_RigidBody.velocity.magnitude > advancedSettings.splashThresholdSpeed)
                {
                    CreateSplashEffect();
                }
    
                // Play enter water sound
                if (audioSource && movementSettings.EnterWaterSound)
                {
                    audioSource.clip = movementSettings.EnterWaterSound;
                    audioSource.Play();
                }
    
                // Store original physics values
                m_InitialDrag = m_RigidBody.drag;
                m_InitialAngularDrag = m_RigidBody.angularDrag;
    
                // Apply water physics - but maintain momentum initially
                // Use a lower initial drag to allow momentum to continue
                m_RigidBody.drag = movementSettings.WaterDrag * 0.3f;
                m_RigidBody.angularDrag = movementSettings.WaterAngularDrag;
    
                // Important - reduce gravity effect but don't eliminate it completely
                // This allows for initial sinking with momentum
                m_RigidBody.useGravity = true; // Keep gravity initially
    
                // Start coroutine to gradually transition to full swimming physics
                StartCoroutine(TransitionToSwimmingPhysics());
            }
            else
            {
                // Exited water - restore normal physics
                m_RigidBody.drag = m_InitialDrag;
                m_RigidBody.angularDrag = m_InitialAngularDrag;
                m_RigidBody.useGravity = true;
            
                // Exited water
                if (advancedSettings.causeSplashes)
                {
                    CreateSplashEffect();
                }
                
                // Play exit water sound
                if (audioSource && movementSettings.ExitWaterSound)
                {
                    audioSource.clip = movementSettings.ExitWaterSound;
                    audioSource.Play();
                }
                
                // Restore original physics
                m_RigidBody.drag = m_InitialDrag;
                m_RigidBody.angularDrag = m_InitialAngularDrag;
                m_RigidBody.useGravity = true;
    
                // Reset swimming physics flag
                m_SwimmingPhysicsActive = false;
            }
        }
        
        private IEnumerator TransitionToSwimmingPhysics()
        {
            // Track whether we're in the initial plunge or not
            bool initialPlunge = true;
            float entryTime = Time.time;
            float transitionDuration = 0.8f; // Time to transition to full swimming physics
    
            // Get initial velocity for reference
            float initialVerticalVelocity = m_RigidBody.velocity.y;
            bool wasMovingDown = initialVerticalVelocity < -2f; // Threshold for significant downward motion
    
            while (Time.time - entryTime < transitionDuration && m_IsInWater)
            {
                float progress = (Time.time - entryTime) / transitionDuration;
        
                // Gradually increase drag to full water drag
                m_RigidBody.drag = Mathf.Lerp(m_InitialDrag, movementSettings.WaterDrag, progress);
        
                // If we were moving down significantly, allow momentum to continue
                if (wasMovingDown && initialPlunge)
                {
                    // Allow the player to continue sinking before buoyancy takes over
                    if (m_RigidBody.velocity.y > -0.5f) // Almost stopped sinking
                    {
                        initialPlunge = false; // Done with initial plunge
                    }
                    else
                    {
                        // Reduce but don't eliminate downward velocity
                        float reducedGravity = Physics.gravity.y * 0.3f;
                        m_RigidBody.AddForce(new Vector3(0, reducedGravity, 0), ForceMode.Acceleration);
                    }
                }
                else
                {
                    // Start applying buoyancy once initial plunge is complete
                    float transitionedBuoyancy = Mathf.Lerp(0, movementSettings.BuoyancyForce, progress);
                    m_RigidBody.AddForce(Vector3.up * transitionedBuoyancy, ForceMode.Force);
                }
        
                yield return null;
            }
    
            // Finalize swimming physics state
            // At the end of the TransitionToSwimmingPhysics coroutine:
            if (m_IsInWater)
            {
                m_RigidBody.drag = movementSettings.WaterDrag;
                m_RigidBody.useGravity = false;
                m_SwimmingPhysicsActive = true; // Mark swimming physics as active
            }
        }
        
        // Handle when underwater state changes (going below/above surface)
        private void OnUnderwaterStateChanged()
        {
            if (m_IsUnderwater)
            {
                // Went underwater
                
                // Setup underwater audio
                if (movementSettings.UnderwaterAmbience)
                {
                    if (m_UnderwaterAudioSource == null)
                    {
                        // Create separate audio source for underwater ambience
                        m_UnderwaterAudioSource = gameObject.AddComponent<AudioSource>();
                        m_UnderwaterAudioSource.loop = true;
                        m_UnderwaterAudioSource.spatialBlend = 0f; // 2D sound
                        m_UnderwaterAudioSource.playOnAwake = false;
                    }
                    
                    m_UnderwaterAudioSource.clip = movementSettings.UnderwaterAmbience;
                    m_UnderwaterAudioSource.Play();
                }
                
                // Apply underwater visual effects
                if (advancedSettings.useWaterEffects)
                {
                    // Store original fog settings
                    m_DefaultFogDensity = RenderSettings.fogDensity;
                    m_DefaultFogColor = RenderSettings.fogColor;
                    
                    // Apply underwater fog
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = movementSettings.UnderwaterFogDensity;
                    RenderSettings.fogColor = movementSettings.UnderwaterFogColor;
                }
            }
            else
            {
                // Came up for air
                
                // Stop underwater audio
                if (m_UnderwaterAudioSource != null && m_UnderwaterAudioSource.isPlaying)
                {
                    m_UnderwaterAudioSource.Stop();
                }
                
                // Restore original visual settings
                if (advancedSettings.useWaterEffects)
                {
                    RenderSettings.fogDensity = m_DefaultFogDensity;
                    RenderSettings.fogColor = m_DefaultFogColor;
                }
            }
        }
        
        private bool m_SwimmingPhysicsActive = false;
        private void HandleSwimming()
        {
            if (!m_SwimmingPhysicsActive)
            {
                // Basic minimum buoyancy during transition
                m_RigidBody.AddForce(Vector3.up * (movementSettings.BuoyancyForce * 0.2f), ForceMode.Force);
                return;
            }
            
            float dynamicBuoyancy = movementSettings.BuoyancyForce;
            bool isDiving = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    
            if (isDiving)
            {
                // Significantly reduce buoyancy when trying to dive
                dynamicBuoyancy *= 0.2f;
        
                // Additional downward force when actively diving
                m_RigidBody.AddForce(Vector3.down * 4f, ForceMode.Force);
            }
            else if (m_RigidBody.velocity.y < 0 && !isDiving)
            {
                // Normal buoyancy when sinking but not actively diving
                dynamicBuoyancy *= 1.3f;
            }
    
            // Apply the buoyancy force
            m_RigidBody.AddForce(Vector3.up * dynamicBuoyancy, ForceMode.Force);
    
            // The rest of your swimming code...
        
    
    // Add slight water current/drift effect
    float time = Time.time * 0.5f;
    Vector3 currentForce = new Vector3(
        Mathf.Sin(time) * 0.3f,
        Mathf.Cos(time * 0.7f) * 0.1f,
        Mathf.Sin(time * 0.5f) * 0.3f
    );
    m_RigidBody.AddForce(currentForce, ForceMode.Force);
    
    // Depth-based movement speed adjustment
    float depthFactor = 1f;
    float depthFromSurface = m_WaterSurfaceHeight - transform.position.y;
    if (depthFromSurface > 5f)
    {
        // Movement gets slightly slower in deep water
        depthFactor = 0.8f;
    }
    else if (Mathf.Abs(transform.position.y - m_WaterSurfaceHeight) < 0.5f)
    {
        // Slightly faster movement near the surface
        depthFactor = 1.2f;
    }
    
    // Store depth factor for use in UpdateSwimMovement
    m_CurrentDepthFactor = depthFactor;
    
    // Smoother velocity capping - apply drag instead of hard capping
    if (m_RigidBody.velocity.y < -movementSettings.SwimSpeed)
    {
        float dragForce = (-movementSettings.SwimSpeed - m_RigidBody.velocity.y) * 2f;
        m_RigidBody.AddForce(Vector3.up * dragForce, ForceMode.Acceleration);
    }
    
    // Manage oxygen when underwater with improved mechanics
    if (m_IsUnderwater)
    {
        // Oxygen depletes faster based on movement and depth
        float activityFactor = 1.0f + (m_RigidBody.velocity.magnitude / movementSettings.SwimSpeed) * 0.5f;
        float depthPenalty = 1.0f + Mathf.Clamp01(depthFromSurface / 10f) * 0.5f;
        float oxygenDrainRate = Time.deltaTime * 10f * activityFactor * depthPenalty;
        
        // Apply oxygen drain
        movementSettings.CurrentOxygen = Mathf.Max(0, movementSettings.CurrentOxygen - oxygenDrainRate);
        
        // Visual feedback - screen pulse when low on oxygen
        if (movementSettings.CurrentOxygen < 30f)
        {
            // Pulse frequency increases as oxygen decreases
            float pulseFrequency = Mathf.Lerp(1f, 3f, 1f - (movementSettings.CurrentOxygen / 30f));
            m_OxygenPulseValue = Mathf.PingPong(Time.time * pulseFrequency, 1f);
            
            // Apply effects in UpdateCameraEffects
        }
        
        // Check for drowning
        if (movementSettings.CurrentOxygen <= 0 && !m_IsDrowning)
        {
            m_IsDrowning = true;
        }
        
        // Handle drowning state
        if (m_IsDrowning)
        {
            StartDrowning();
        }
    }
    else
    {
        // Regenerate oxygen when not underwater - faster when stationary
        float restFactor = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(m_RigidBody.velocity.magnitude / 2f));
        movementSettings.CurrentOxygen = Mathf.Min(100f, 
            movementSettings.CurrentOxygen + movementSettings.OxygenRegenRate * Time.deltaTime * restFactor);
        
        // Reset drowning state
        m_IsDrowning = false;
        m_OxygenPulseValue = 0f;
    }
    
    // Surface interactions - improved splash and bobbing
    float surfaceProximity = Mathf.Abs(transform.position.y - m_WaterSurfaceHeight);
    if (surfaceProximity < advancedSettings.surfaceOffset)
    {
        // Add subtle bobbing when near surface
        if (!m_IsApplyingSurfaceBob)
        {
            StartCoroutine(ApplySurfaceBobbing());
        }
        
        // Create splash effects when moving fast near surface
        if (advancedSettings.causeSplashes && 
            m_RigidBody.velocity.magnitude > advancedSettings.splashThresholdSpeed)
        {
            // More dynamic splash frequency based on speed and movement direction
            float splashThreshold = 0.5f * (m_RigidBody.velocity.magnitude / advancedSettings.splashThresholdSpeed);
            
            if (Time.time - m_LastSplashTime > splashThreshold && 
                Vector3.Distance(transform.position, m_LastSplashPosition) > 1f)
            {
                CreateSplashEffect();
            }
        }
    }
}   
        private float m_OxygenPulseValue = 0f; // Used for oxygen depletion visual feedback
        private float m_CurrentDepthFactor = 1f; // Store depth factor for movement adjustments
        private bool m_IsApplyingSurfaceBob = false; // Track bobbing coroutine
        
        private IEnumerator ApplySurfaceBobbing()
        {
            m_IsApplyingSurfaceBob = true;
    
            // Apply gentle bobbing at the surface for 2 seconds
            float startTime = Time.time;
            float duration = 2f;
    
            while (Time.time - startTime < duration && 
                   Mathf.Abs(transform.position.y - m_WaterSurfaceHeight) < advancedSettings.surfaceOffset)
            {
                // Calculate bob force
                float bobStrength = 0.2f * Mathf.Sin((Time.time - startTime) * 4f);
                m_RigidBody.AddForce(Vector3.up * bobStrength, ForceMode.Acceleration);
        
                yield return null;
            }
    
            m_IsApplyingSurfaceBob = false;
        }
        
        
        // Create water splash effect
        private void CreateSplashEffect()
        {
            if (advancedSettings.splashEffectPrefab != null)
            {
                Vector3 splashPosition = new Vector3(transform.position.x, m_WaterSurfaceHeight, transform.position.z);
                GameObject splash = Instantiate(advancedSettings.splashEffectPrefab, splashPosition, Quaternion.identity);
                
                // Scale splash based on velocity
                float splashScale = Mathf.Clamp(m_RigidBody.velocity.magnitude / 10f, 0.5f, 2f);
                splash.transform.localScale *= splashScale;
                
                // Store last splash data
                m_LastSplashPosition = transform.position;
                m_LastSplashTime = Time.time;
                
                // Destroy splash after 2 seconds
                Destroy(splash, 2f);
            }
        }
        
        
        private float m_LastDrowningDamageTime = 0f;
        private float m_DrowningDamageInterval = 5f; // Apply damage every second

        private void StartDrowning()
        {
            // Log drowning state
            Debug.Log("Player is drowning!");
    
            // Apply periodic damage
            if (Time.time - m_LastDrowningDamageTime >= m_DrowningDamageInterval)
            {
                // Apply drowning damage
                playerState.takeDamage(10);
                m_LastDrowningDamageTime = Time.time;
        
                // Apply screen effects, slow movement, etc.
                // Add your effects here
            }
    
            // Try to help player reach the surface
            Vector3 surfacePosition = transform.position;
            surfacePosition.y = m_WaterSurfaceHeight + m_Capsule.height;
            AddForce((surfacePosition - transform.position).normalized * 5f, ForceMode.Impulse);
        }
        
        private float m_SwimSprintFactor = 1f; // For smooth sprint transitions
        
        // Update swimming movement based on input
        private Vector3 UpdateSwimMovement(Vector2 input)
        {
            Vector3 swimDirection = Vector3.zero;
    
            // Calculate horizontal movement direction
            swimDirection += cam.transform.forward * input.y;
            swimDirection += cam.transform.right * input.x;
    
            // Add vertical movement with improved control
            if (Input.GetKey(KeyCode.Space))
            {
                // Swim up
                swimDirection += Vector3.up * 1.2f; // Slightly stronger upward movement
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            {
                // Swim down - stronger downward movement
                swimDirection += Vector3.down * 1.5f; // Significantly stronger to overcome buoyancy
            }
    
            // Normalize direction but preserve some of the extra vertical force
            if (swimDirection.magnitude > 1f)
            {
                // Less aggressive normalization for vertical movement
                if (swimDirection.y < 0) // When diving
                {
                    // Preserve more of the downward direction
                    float downMagnitude = -swimDirection.y;
                    swimDirection.Normalize();
                    swimDirection.y -= 0.3f * downMagnitude; // Add back some of the diving force
                    swimDirection.Normalize();
                }
                else
                {
                    swimDirection.Normalize();
                }
            }
    
    // Apply swimming speed with depth factor
    float swimSpeed = movementSettings.SwimSpeed * m_CurrentDepthFactor;
    
    // Check for sprint swimming with improved feel
    if (Input.GetKey(movementSettings.RunKey) && movementSettings.CurrentStamina > 0)
    {
        // Gradual sprint acceleration rather than immediate
        m_SwimSprintFactor = Mathf.MoveTowards(m_SwimSprintFactor, movementSettings.SwimSprintMultiplier, Time.deltaTime * 2f);
        
        // Drain stamina faster while sprint swimming
        movementSettings.CurrentStamina = Mathf.Max(0, 
            movementSettings.CurrentStamina - movementSettings.SwimmingStaminaDrainRate * Time.deltaTime);
            
        // If underwater, also drain oxygen slightly faster when sprinting
        if (m_IsUnderwater)
        {
            movementSettings.CurrentOxygen = Mathf.Max(0, 
                movementSettings.CurrentOxygen - Time.deltaTime * 2f);
        }
    }
    else
    {
        // Gradual sprint deceleration
        m_SwimSprintFactor = Mathf.MoveTowards(m_SwimSprintFactor, 1f, Time.deltaTime * 3f);
        
        // Regenerate stamina at a reduced rate while swimming
        movementSettings.CurrentStamina = Mathf.Min(movementSettings.StaminaMax, 
            movementSettings.CurrentStamina + (movementSettings.StaminaRegenRate * 0.5f) * Time.deltaTime);
    }
    
    // Apply sprint factor
    swimSpeed *= m_SwimSprintFactor;
    
    // Scale by swim speed
    swimDirection *= swimSpeed;
    
    return swimDirection;
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
    
            // Store initial physics values
            m_InitialDrag = m_RigidBody.drag;
            m_InitialAngularDrag = m_RigidBody.angularDrag;
    
            // Store initial fog settings
            m_DefaultFogDensity = RenderSettings.fogDensity;
            m_DefaultFogColor = RenderSettings.fogColor;
    
            // Find initial active wave manager
            waveManager = FindActiveWaveManager();
        }
        
        private float m_WaveManagerUpdateTime = 0f;
        private const float WAVE_MANAGER_UPDATE_INTERVAL = 2f; // Update every 2 seconds

        private void UpdateWaveManagerReference()
        {
            // Only update periodically to avoid constant searching
            if (Time.time - m_WaveManagerUpdateTime > WAVE_MANAGER_UPDATE_INTERVAL)
            {
                waveManager = oceanGridGenerator.getGerstnerWaveManager();
                m_WaveManagerUpdateTime = Time.time;
            }
        }

        private void Update()
        {
            // Update wave manager reference periodically
            UpdateWaveManagerReference();
            playerState.setCurrentBreath((int)movementSettings.CurrentOxygen);

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
            WaterCheck();
        }

        private void UpdateCameraEffects()
        {
            if (!cam) return;
            
            // Handle camera bob for walking/running
            if (advancedSettings.enableHeadBob && !m_IsInWater)
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
            // Add subtle water bob when swimming
            // In the UpdateCameraEffects method, add this section for underwater effects:
            if (m_IsUnderwater)
            {
                // Add underwater camera effects
    
                // Subtle sway based on movement
                float swayX = Mathf.Sin(Time.time * 0.8f) * 0.02f * m_RigidBody.velocity.magnitude;
                float swayY = Mathf.Cos(Time.time * 0.6f) * 0.015f * m_RigidBody.velocity.magnitude;
    
                Vector3 targetUnderwaterPos = m_CameraStartPosition + new Vector3(swayX, swayY, 0);
                cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetUnderwaterPos, Time.deltaTime * 2f);
    
                // Oxygen depletion effect - red pulsing vignette when low
                if (m_OxygenPulseValue > 0f)
                {
                    // Apply post-processing or UI effect here
                    // This would typically update a vignette intensity or red overlay
                    // You'll need to implement this based on your specific UI/post-processing setup
                }
    
                // Drowning camera shake
                if (m_IsDrowning)
                {
                    float shakeX = (UnityEngine.Random.value * 2f - 1f) * 0.03f;
                    float shakeY = (UnityEngine.Random.value * 2f - 1f) * 0.03f;
        
                    cam.transform.localPosition += new Vector3(shakeX, shakeY, 0);
                }
            }
            
            // Handle FOV effects
            if (advancedSettings.enableFOVKick)
            {
                float targetFOV;
                
                if (m_IsUnderwater)
                {
                    // Wider FOV underwater
                    targetFOV = m_OriginalCameraFOV + 5f;
                }
                else if (Running && !m_IsInWater)
                {
                    // Run FOV
                    targetFOV = m_OriginalCameraFOV + advancedSettings.FOVKickAmount;
                }
                else
                {
                    // Normal FOV
                    targetFOV = m_OriginalCameraFOV;
                }
                
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV,
                    Time.deltaTime * (1f / advancedSettings.FOVKickTime));
            }
        }
        
        private GerstnerWaveManager FindActiveWaveManager()
        {
            // Try to find the ocean grid first
            OceanGridGenerator oceanGrid = FindObjectOfType<OceanGridGenerator>();
            if (oceanGrid == null) return null;
    
            // Get all active ocean planes from the grid
            Transform gridTransform = oceanGrid.transform;
    
            // Find the closest active ocean plane to the player
            float closestDistance = float.MaxValue;
            GerstnerWaveManager closestWaveManager = null;
    
            // Check all child objects of the grid generator
            foreach (Transform child in gridTransform)
            {
                // Skip inactive planes
                if (!child.gameObject.activeSelf) continue;
        
                // Calculate horizontal distance only (ignore Y)
                Vector3 planePos = child.position;
                Vector3 playerPos = transform.position;
                float dist = Vector2.Distance(
                    new Vector2(planePos.x, planePos.z), 
                    new Vector2(playerPos.x, playerPos.z)
                );
        
                if (dist < closestDistance)
                {
                    // Try to get the GerstnerWaveManager component
                    GerstnerWaveManager waveManager = child.GetComponent<GerstnerWaveManager>();
                    if (waveManager != null)
                    {
                        closestDistance = dist;
                        closestWaveManager = waveManager;
                    }
                }
            }
    
            return closestWaveManager;
        }
        
        private void FixedUpdate()
        {
            // Skip physics when direct stepping
            

            bool wasGrounded = m_IsGrounded;
            GroundCheck();

            // Play landing sound when hitting the ground
            if (!wasGrounded && m_IsGrounded && m_PreviouslyGrounded)
            {
                movementSettings.PlayLandSound(audioSource);
            }

            Vector2 input = GetInput();


            if (m_IsInWater)
            {
                // Get swim movement
                Vector3 swimDirection = UpdateSwimMovement(input);

                // Get current velocity
                Vector3 currentVelocity = m_RigidBody.velocity;

                // Calculate force needed
                Vector3 targetVelocity = swimDirection;
                Vector3 velocityChange = targetVelocity - currentVelocity;

                // Apply acceleration limitation
                velocityChange = Vector3.ClampMagnitude(velocityChange, movementSettings.SwimAccelerationRate);

                // Apply force
                m_RigidBody.AddForce(velocityChange, ForceMode.VelocityChange);

                // If near surface and pressing jump, attempt to jump out of water
                m_Jump = false;
            }
            else
            {

                

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
                        m_RigidBody.AddForce(velocityChange * movementSettings.AccelerationRate,
                            ForceMode.Acceleration);
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
        
        private void OnDestroy()
        {
            // Reset any global settings we might have changed
            if (m_IsUnderwater && advancedSettings.useWaterEffects)
            {
                // Restore fog settings
                RenderSettings.fogDensity = m_DefaultFogDensity;
                RenderSettings.fogColor = m_DefaultFogColor;
            }
            
            // Clean up underwater audio source
            if (m_UnderwaterAudioSource != null)
            {
                m_UnderwaterAudioSource.Stop();
            }
        }
    }
}