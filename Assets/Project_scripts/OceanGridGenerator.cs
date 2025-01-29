using UnityEngine;

public class OceanGridGenerator : MonoBehaviour
{
    [Header("Ocean Plane Settings")]
    public GameObject oceanPlanePrefab; // Prefab of the ocean plane
    public int gridSize = 7;            // Number of ocean planes per side (gridSize x gridSize)
    public float planeSize = 10f;       // Size of each ocean plane (assumes square planes)
    public float overlapMargin = 0.5f;  // Overlap margin to avoid visible gaps between planes

    [Header("Position Settings")]
    public Transform target;            // Reference to the target (e.g., player or camera)
    public float updateThreshold = 20f; // Distance threshold to trigger grid repositioning

    private GameObject[,] oceanPlanes;  // 2D array to store references to the ocean planes
    private Vector3 lastTargetPosition; // Track the last target position to determine when to update the grid

    private Camera mainCamera;          // Reference to the main camera for visibility checks

    void Start()
    {
        mainCamera = Camera.main;
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

        UpdatePlaneVisibility();
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

    // Update visibility of ocean planes based on the camera's frustum
    void UpdatePlaneVisibility()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        foreach (var plane in oceanPlanes)
        {
            if (plane != null)
            {
                // Get the renderer bounds of the plane
                Renderer renderer = plane.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
                    plane.SetActive(isVisible);
                }
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
            if (plane != null && plane.activeSelf)
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
