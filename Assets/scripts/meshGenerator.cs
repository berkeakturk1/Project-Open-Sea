using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CustomPlaneMesh : MonoBehaviour
{
    [Header("Plane Settings")]
    public int widthVertices = 10; // Number of vertices along the width
    public int heightVertices = 10; // Number of vertices along the height
    public float width = 10f; // Physical width of the plane
    public float height = 10f; // Physical height of the plane

    private MeshFilter meshFilter;

    void Start()
    {
        GeneratePlane();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            GeneratePlane();
        }
    }

    void GeneratePlane()
    {
        meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        int numVertices = widthVertices * heightVertices;
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uv = new Vector2[numVertices];
        int[] triangles = new int[(widthVertices - 1) * (heightVertices - 1) * 6];

        float stepX = width / (widthVertices - 1);
        float stepZ = height / (heightVertices - 1);

        // Create vertices and UVs
        for (int z = 0; z < heightVertices; z++)
        {
            for (int x = 0; x < widthVertices; x++)
            {
                int index = z * widthVertices + x;
                vertices[index] = new Vector3(x * stepX, 0, z * stepZ);
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

        // Assign data to the mesh
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Assign mesh to the MeshFilter
        meshFilter.sharedMesh = mesh;
    }
}
