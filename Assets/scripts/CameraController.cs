using UnityEngine;

public class OrbitCameraTopDown : MonoBehaviour
{
    public Transform target; // The object to orbit around
    public float distance = 10.0f; // Distance from the target
    public float orbitSpeed = 50.0f; // Speed of orbiting
    public float downwardAngle = 20.0f; // Fixed downward angle
    public bool autoOrbit = false; // Toggle for automatic orbit

    private float currentAngle = 0.0f; // Current angle of orbit

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("No target assigned for OrbitCameraTopDown!");
            return;
        }

        // Initialize camera position
        UpdateCameraPosition();
    }

    void Update()
    {
        if (target == null) return;

        if (autoOrbit)
        {
            currentAngle += orbitSpeed * Time.deltaTime;
        }
        else
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            currentAngle += horizontalInput * orbitSpeed * Time.deltaTime;
        }

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        // Convert angle to radians for calculation
        float angleInRadians = currentAngle * Mathf.Deg2Rad;

        // Calculate new position around the target
        float x = target.position.x + distance * Mathf.Cos(angleInRadians);
        float z = target.position.z + distance * Mathf.Sin(angleInRadians);
        float y = target.position.y + distance * Mathf.Sin(downwardAngle * Mathf.Deg2Rad); // Set height based on angle

        // Set camera position
        transform.position = new Vector3(x, y, z);

        // Look at the target
        transform.LookAt(target.position);
    }
}