using UnityEngine;

public class Floater : MonoBehaviour
{
    [Header("Physics Settings")]
    public Rigidbody rigidBody;
    public float depthBeforeSubmerged = 1f; // Depth before fully submerged
    public float buoyancyForce = 3f;        // Force multiplier for buoyancy
    public int floaterCount = 4;            // Number of floaters on the object

    [Header("Damping Settings")]
    public float waterDrag = 1f;            // Drag when in water
    public float waterAngularDrag = 0.5f;   // Angular drag when in water
    public float verticalDamping = 0.5f;    // Damping for vertical motion
    public float horizontalDamping = 0.3f;  // Damping for horizontal motion

    [Header("Water Settings")]
    public GerstnerWaveManager waveManager; // Reference to the wave manager

    private Vector3 temp_wave = new Vector3();
    private Vector3 smoothVelocity = Vector3.zero;
    private float previousWaveHeight = 0f;
    private bool inWater = false;

    private void Start()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        float time = Time.time;

        // Get the current wave height at the floater's position
        temp_wave = waveManager.CalculateGerstnerWave(transform.position.x, transform.position.z, time);
        float targetWaveHeight = temp_wave.y;

        // Smooth the wave height using Mathf.Lerp
        float smoothedWaveHeight = Mathf.Lerp(previousWaveHeight, targetWaveHeight, 0.1f);
        previousWaveHeight = smoothedWaveHeight;

        // Check if the floater is below the smoothed water surface
        if (transform.position.y < smoothedWaveHeight)
        {
            if (!inWater)
            {
                // Switch to manual gravity when entering the water
                rigidBody.useGravity = false;
                inWater = true;
            }

            // Calculate the displacement multiplier based on how far the object is submerged
            float displacementMultiplier = Mathf.Clamp01((smoothedWaveHeight - transform.position.y) / depthBeforeSubmerged) * buoyancyForce;

            // Apply the buoyancy force
            Vector3 upwardForce = new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f);
            rigidBody.AddForceAtPosition(upwardForce, transform.position, ForceMode.Acceleration);

            // Apply damping to vertical and horizontal motion
            rigidBody.AddForce(-rigidBody.velocity * verticalDamping * displacementMultiplier, ForceMode.Acceleration);
            rigidBody.AddTorque(-rigidBody.angularVelocity * horizontalDamping * displacementMultiplier, ForceMode.Acceleration);

            // Apply water drag to simulate resistance
            rigidBody.drag = waterDrag;
            rigidBody.angularDrag = waterAngularDrag;
        }
        else
        {
            if (inWater)
            {
                // Switch back to normal gravity when leaving the water
                rigidBody.useGravity = true;
                inWater = false;
            }
            
        }
    }
}
