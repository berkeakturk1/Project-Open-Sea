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

    private Vector3 smoothVelocity = Vector3.zero;
    private float previousWaveHeight = 0f;

    private void Start()
    {
        if (rigidBody == null)
        {
            rigidBody = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        // Apply gravity evenly across the floaters
        rigidBody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);

        // Get the current wave height at the floater's position
        float targetWaveHeight = waveManager.GetWaveHeight(new Vector3(transform.position.x, transform.position.y, transform.position.z));

        // Smooth the wave height using Mathf.Lerp
        float smoothedWaveHeight = Mathf.Lerp(previousWaveHeight, targetWaveHeight, 0.1f);
        previousWaveHeight = smoothedWaveHeight;

        // Check if the floater is below the smoothed water surface
        if (transform.position.y < smoothedWaveHeight)
        {
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
            // Reset drag when not in water
            rigidBody.drag = 0f;
            rigidBody.angularDrag = 0f;
        }
    }
}
