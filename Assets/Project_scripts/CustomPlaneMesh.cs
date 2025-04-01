using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomPlaneMesh : MonoBehaviour
{
    [Header("Plane Settings")]
    public int highDetailWidthVertices = 100;
    public int highDetailHeightVertices = 100;
    public int lowDetailWidthVertices = 20;
    public int lowDetailHeightVertices = 20;

    public float width = 10f;
    public float height = 10f;

    
    private Transform target; // Reference to the target (e.g., camera or player)

    private MeshFilter meshFilter;
    private bool isHighDetail = false;

    void Start()
    {
        target = GameObject.Find("Main Camera").transform;
        meshFilter = GetComponent<MeshFilter>();

        // Generate a default low-detail mesh at the start
        GeneratePlane(lowDetailWidthVertices, lowDetailHeightVertices);
        isHighDetail = false;

        // Immediately update the LOD based on the target's position
        UpdateLOD();
    }

    void Update()
    {
        if (target != null)
        {
            UpdateLOD();
        }
    }

    void UpdateLOD()
    {
        if (target == null) return;

        // Get the bounds of the plane in world space
        Vector3 planePosition = transform.position;
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        float minX = planePosition.x - halfWidth;
        float maxX = planePosition.x + halfWidth;
        float minZ = planePosition.z - halfHeight;
        float maxZ = planePosition.z + halfHeight;

        // Check if the target's position is within the bounds of the plane
        if (target.position.x >= minX && target.position.x <= maxX &&
            target.position.z >= minZ && target.position.z <= maxZ)
        {
            // Target is within the bounds: Use high detail
            if (!isHighDetail)
            {
                GeneratePlane(highDetailWidthVertices, highDetailHeightVertices);
                isHighDetail = true;
            }
        }
        else
        {
            // Target is outside the bounds: Use low detail
            if (isHighDetail)
            {
                GeneratePlane(lowDetailWidthVertices, lowDetailHeightVertices);
                isHighDetail = false;
            }
        }
    }

    void GeneratePlane(int widthVertices, int heightVertices)
    {
        Mesh mesh = new Mesh();

        int numVertices = widthVertices * heightVertices;
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uv = new Vector2[numVertices];
        int[] triangles = new int[(widthVertices - 1) * (heightVertices - 1) * 6];

        float stepX = width / (widthVertices - 1);
        float stepZ = height / (heightVertices - 1);

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Create vertices and UVs starting from -halfWidth and -halfHeight
        for (int z = 0; z < heightVertices; z++)
        {
            for (int x = 0; x < widthVertices; x++)
            {
                int index = z * widthVertices + x;
                vertices[index] = new Vector3(x * stepX - halfWidth, 0, z * stepZ - halfHeight);
                uv[index] = new Vector2((float)x / (widthVertices - 1), (float)z / (heightVertices - 1));
            }
        }

        // Create triangles
        int triIndex = 0;
        for (int z = 0; z < heightVertices - 1; z++)
        {
            for (int x = 0; x < widthVertices - 1; x++)
            {
                int topLeft = z * widthVertices + x;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + widthVertices;
                int bottomRight = bottomLeft + 1;

                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;

                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
    }

    // Draw Gizmos to visualize the bounds
    /*void OnDrawGizmos()
    {
        if (target == null) return;

        // Plane bounds (Green)
        Gizmos.color = Color.green;
        Vector3 planePosition = transform.position;
        Gizmos.DrawWireCube(planePosition, new Vector3(width, 0, height));

        // Calculated bounds (Red)
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        float minX = planePosition.x - halfWidth;
        float maxX = planePosition.x + halfWidth;
        float minZ = planePosition.z - halfHeight;
        float maxZ = planePosition.z + halfHeight;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(minX, planePosition.y, minZ), new Vector3(maxX, planePosition.y, minZ));
        Gizmos.DrawLine(new Vector3(minX, planePosition.y, maxZ), new Vector3(maxX, planePosition.y, maxZ));
        Gizmos.DrawLine(new Vector3(minX, planePosition.y, minZ), new Vector3(minX, planePosition.y, maxZ));
        Gizmos.DrawLine(new Vector3(maxX, planePosition.y, minZ), new Vector3(maxX, planePosition.y, maxZ));
    }*/
    
}
