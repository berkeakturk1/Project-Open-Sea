using UnityEngine;

public class OceanGridGenerator : MonoBehaviour
{
    [Header("Ocean Plane Settings")]
    public GameObject oceanPlanePrefab; // Prefab of the ocean plane
    [Tooltip("Number of ocean planes per side (e.g., 7x7 grid). Best to use an odd number.")]
    public int gridSize = 7;            // Number of ocean planes per side
    [Tooltip("Size of each ocean plane (assumes square planes)")]
    public float planeSize = 10f;       // Size of each ocean plane

    [Header("Position Settings")]
    [Tooltip("The target to follow, typically the player or camera")]
    public Transform target;            // Reference to the target
    [Tooltip("The vertical position of the ocean surface")]
    public float oceanHeight = 0f;      // Fixed ocean height (Y position)

    private GameObject[,] oceanPlanes;  // 2D array to store the ocean planes
    private Vector3 lastGridCenter;     // The center of the grid in world space

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned for OceanGridGenerator.");
            this.enabled = false;
            return;
        }

        GenerateOceanGrid();
        PositionGrid();
    }

    void Update()
    {
        // Check if the target has moved to a new grid cell
        if (Mathf.Abs(target.position.x - lastGridCenter.x) >= planeSize ||
            Mathf.Abs(target.position.z - lastGridCenter.z) >= planeSize)
        {
            PositionGrid();
        }
    }

    // Instantiates the ocean planes in a grid formation
    void GenerateOceanGrid()
    {
        oceanPlanes = new GameObject[gridSize, gridSize];
        if (oceanPlanePrefab == null)
        {
            Debug.LogError("oceanPlanePrefab is not assigned!");
            return;
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                GameObject plane = Instantiate(oceanPlanePrefab, Vector3.zero, Quaternion.identity, transform);
                plane.name = $"OceanPlane_{x}_{z}";
                oceanPlanes[x, z] = plane;
            }
        }
    }

    // Positions the grid around the target
    void PositionGrid()
    {
        // Calculate the grid cell coordinates of the target
        int targetX = Mathf.RoundToInt(target.position.x / planeSize);
        int targetZ = Mathf.RoundToInt(target.position.z / planeSize);

        // Calculate the new center of the grid
        lastGridCenter = new Vector3(targetX * planeSize, oceanHeight, targetZ * planeSize);

        // Position each plane in the grid relative to the new center
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                int offsetX = x - gridSize / 2;
                int offsetZ = z - gridSize / 2;

                Vector3 position = new Vector3(
                    lastGridCenter.x + offsetX * planeSize,
                    oceanHeight,
                    lastGridCenter.z + offsetZ * planeSize
                );

                oceanPlanes[x, z].transform.position = position;
            }
        }
    }
    
    // This is the exact implementation as requested.
    // Note: This gets the component from the prefab asset, not an instance in the scene.
    public GerstnerWaveManager getGerstnerWaveManager()
    {
        return oceanPlanePrefab.GetComponent<GerstnerWaveManager>();
    }

    // Draw Gizmos to visualize the grid in the Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || oceanPlanes == null) return;

        Gizmos.color = Color.cyan;
        foreach (var plane in oceanPlanes)
        {
            if (plane != null)
            {
                Gizmos.DrawWireCube(plane.transform.position, new Vector3(planeSize, 0, planeSize));
            }
        }

        // Visualize the grid center
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastGridCenter, 1f);
    }
}