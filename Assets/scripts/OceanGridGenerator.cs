using UnityEngine;

public class OceanGridGenerator : MonoBehaviour
{
    [Header("Ocean Plane Settings")]
    public GameObject oceanPlanePrefab; // Prefab of the ocean plane
    public int gridSize = 3;            // Number of ocean planes per side (gridSize x gridSize)
    public float planeSize = 10f;       // Size of each ocean plane (assumes square planes)
    public float overlapMargin = 0.5f;  // Overlap margin to avoid visible gaps between planes

    [Header("Position Settings")]
    public Transform target;            // Reference to the target (e.g., player or camera)
    public float updateThreshold = 20f; // Distance threshold to trigger grid repositioning

    private GameObject[,] oceanPlanes;  // 2D array to store references to the ocean planes
    private Vector3 lastTargetPosition; // Track the last target position to determine when to update the grid

    void Start()
    {
        GenerateOceanGrid();
        lastTargetPosition = target.position;
    }

    void Update()
    {
        if (Vector3.Distance(target.position, lastTargetPosition) > updateThreshold)
        {
            RepositionOceanGrid();
            lastTargetPosition = target.position;
        }
    }

    // Generate the initial ocean grid
    void GenerateOceanGrid()
    {
        oceanPlanes = new GameObject[gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Instantiate the ocean plane prefab with overlap margin
                Vector3 position = new Vector3(x * (planeSize - overlapMargin), 0, z * (planeSize - overlapMargin));
                GameObject plane = Instantiate(oceanPlanePrefab, position, Quaternion.identity, transform);
                plane.name = $"OceanPlane_{x}_{z}";

                oceanPlanes[x, z] = plane;
            }
        }
    }

    // Reposition the ocean grid based on the target's position
    void RepositionOceanGrid()
    {
        Vector3 targetPosition = target.position;

        // Calculate the center of the grid based on the target's position
        int centerX = Mathf.RoundToInt(targetPosition.x / planeSize);
        int centerZ = Mathf.RoundToInt(targetPosition.z / planeSize);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Calculate the new position for each ocean plane with overlap margin
                int offsetX = x - gridSize / 2;
                int offsetZ = z - gridSize / 2;

                Vector3 newPosition = new Vector3((centerX + offsetX) * (planeSize - overlapMargin), 0, (centerZ + offsetZ) * (planeSize - overlapMargin));
                oceanPlanes[x, z].transform.position = newPosition;
            }
        }
    }

    // Draw Gizmos to visualize the grid in the Scene view
    void OnDrawGizmos()
    {
        if (oceanPlanes == null) return;

        Gizmos.color = Color.cyan;
        foreach (var plane in oceanPlanes)
        {
            if (plane != null)
            {
                Gizmos.DrawWireCube(plane.transform.position, new Vector3(planeSize, 0, planeSize));
            }
        }
    }
    
    public GerstnerWaveManager getGerstnerWaveManager()
    {
        return oceanPlanePrefab.GetComponent<GerstnerWaveManager>();
    }
}
