using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using perlin_Scripts.Data;

public class OceanFloorGenerator : MonoBehaviour {
    public OceanData oceanData;
    public Transform viewer;
    public Material oceanFloorMaterial;
    
    private GameObject oceanFloorObject;
    private MapGenerator mapGenerator;
    private EndlessTerrain endlessTerrain;
    private Vector2 lastViewerPosition;
    private bool hasInitialized = false;
    
    void Awake() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        endlessTerrain = FindObjectOfType<EndlessTerrain>();
        
        // Get viewer from EndlessTerrain if not assigned
        if (viewer == null && endlessTerrain != null) {
            viewer = endlessTerrain.viewer;
        }
    }
    
    void Start() {
        if (oceanData == null) {
            Debug.LogError("OceanData not assigned to OceanFloorGenerator!");
            return;
        }
        
        if (oceanFloorMaterial != null && oceanData != null) {
            oceanData.ApplyToMaterial(oceanFloorMaterial);
        }
        
        if (oceanData.generateOceanFloor) {
            StartCoroutine(DelayedGeneration());
        }
        
        // Subscribe to update events from OceanData
        if (oceanData != null) {
            oceanData.OnValuesUpdated += OnOceanDataValuesUpdated;
        }
    }
    
    IEnumerator DelayedGeneration() {
        // Wait for other systems to initialize
        yield return new WaitForSeconds(0.2f);
        GenerateOceanFloor();
        hasInitialized = true;
    }
    
    void OnOceanDataValuesUpdated() {
        if (hasInitialized && oceanData.generateOceanFloor) {
            if (oceanFloorMaterial != null) {
                oceanData.ApplyToMaterial(oceanFloorMaterial);
            }
            GenerateOceanFloor();
        }
    }
    
    void Update() {
        if (!hasInitialized || !oceanData.generateOceanFloor || !oceanData.followPlayer || viewer == null) return;
        
        Vector2 viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        
        // Only update ocean position when viewer moves significantly
        if ((viewerPosition - lastViewerPosition).sqrMagnitude > 100f) {
            UpdateOceanPosition();
            lastViewerPosition = viewerPosition;
        }
    }
    
    void GenerateOceanFloor() {
        if (oceanFloorObject != null) {
            Destroy(oceanFloorObject);
        }
        
        oceanFloorObject = new GameObject("Ocean Floor");
        oceanFloorObject.transform.parent = transform;
        
        // Create ocean floor mesh
        MeshFilter meshFilter = oceanFloorObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = oceanFloorObject.AddComponent<MeshRenderer>();
        
        if (oceanFloorMaterial != null) {
            meshRenderer.material = oceanFloorMaterial;
        } else {
            Debug.LogWarning("Ocean floor material not assigned. Using default material.");
            meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        
        meshFilter.mesh = CreateOceanFloorMesh();
        
        // Add collider for the ocean floor
        MeshCollider meshCollider = oceanFloorObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        
        // Set initial position
        UpdateOceanPosition();
    }
    
    Mesh CreateOceanFloorMesh() {
    Mesh mesh = new Mesh();
    
    int resolution = oceanData.oceanFloorResolution;
    Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
    int[] triangles = new int[resolution * resolution * 6];
    Vector2[] uvs = new Vector2[vertices.Length];
    Vector3[] normals = new Vector3[vertices.Length];
    
    float halfSize = oceanData.oceanFloorSize / 2f;
    float cellSize = oceanData.oceanFloorSize / resolution;
    
    // Generate noise map for ocean floor variation
    float[,] noiseMap = Noise.GenerateNoiseMap(
        resolution + 1, 
        resolution + 1,
        oceanData.oceanNoiseSeed,
        oceanData.oceanNoiseScale,
        oceanData.oceanNoiseOctaves,
        oceanData.oceanNoisePersistance,
        oceanData.oceanNoiseLacunarity,
        Vector2.zero,
        Noise.NormalizeMode.Local
    );
    
    // Create vertices
    for (int z = 0; z <= resolution; z++) {
        for (int x = 0; x <= resolution; x++) {
            int i = z * (resolution + 1) + x;
            
            float worldX = x * cellSize - halfSize;
            float worldZ = z * cellSize - halfSize;
            
            // Calculate height based on noise
            float height = oceanData.oceanDepth + (noiseMap[x, z] * oceanData.oceanFloorVariation);
            
            vertices[i] = new Vector3(worldX, height, worldZ);
            
            // Improved UV mapping
            uvs[i] = new Vector2(
                (worldX + halfSize) / oceanData.oceanFloorSize, 
                (worldZ + halfSize) / oceanData.oceanFloorSize
            );
            
            // Default normal pointing up
            normals[i] = Vector3.up;
        }
    }
    
    // Create triangles (unchanged)
    int triangleIndex = 0;
    for (int z = 0; z < resolution; z++) {
        for (int x = 0; x < resolution; x++) {
            int topLeft = z * (resolution + 1) + x;
            int topRight = topLeft + 1;
            int bottomLeft = (z + 1) * (resolution + 1) + x;
            int bottomRight = bottomLeft + 1;
            
            // First triangle
            triangles[triangleIndex++] = topLeft;
            triangles[triangleIndex++] = bottomLeft;
            triangles[triangleIndex++] = topRight;
            
            // Second triangle
            triangles[triangleIndex++] = topRight;
            triangles[triangleIndex++] = bottomLeft;
            triangles[triangleIndex++] = bottomRight;
        }
    }
    
    // Enhanced normal calculation
    for (int z = 1; z < resolution; z++) {
        for (int x = 1; x < resolution; x++) {
            int i = z * (resolution + 1) + x;
            
            // Get surrounding vertices
            Vector3 up = vertices[i - (resolution + 1)];
            Vector3 down = vertices[i + (resolution + 1)];
            Vector3 left = vertices[i - 1];
            Vector3 right = vertices[i + 1];
            Vector3 center = vertices[i];
            
            // Calculate normal using cross products
            Vector3 normal1 = Vector3.Cross(up - center, right - center).normalized;
            Vector3 normal2 = Vector3.Cross(right - center, down - center).normalized;
            Vector3 normal3 = Vector3.Cross(down - center, left - center).normalized;
            Vector3 normal4 = Vector3.Cross(left - center, up - center).normalized;
            
            // Average the normals
            normals[i] = (normal1 + normal2 + normal3 + normal4).normalized;
        }
    }
    
    // Debug logging for mesh information
    Debug.Log($"Ocean Floor Mesh Debug:");
    Debug.Log($"Vertices: {vertices.Length}");
    Debug.Log($"Triangles: {triangles.Length}");
    Debug.Log($"UV Range X: [{uvs.Min(uv => uv.x)}, {uvs.Max(uv => uv.x)}]");
    Debug.Log($"UV Range Y: [{uvs.Min(uv => uv.y)}, {uvs.Max(uv => uv.y)}]");
    
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.normals = normals;
    
    // Comprehensive mesh optimization
    mesh.RecalculateBounds();
    mesh.RecalculateNormals();
    mesh.RecalculateTangents();
    mesh.Optimize();
    
    return mesh;
}
    
    void UpdateOceanPosition() {
        // Move ocean with the viewer for infinite ocean effect
        if (oceanFloorObject != null && oceanData.followPlayer && viewer != null) {
            Vector3 viewerPos = viewer.position;
            viewerPos.y = 0; // Keep y-position constant
            
            // Round to grid to avoid floating point precision issues
            oceanFloorObject.transform.position = new Vector3(
                Mathf.Round(viewerPos.x / oceanData.oceanFloorSize) * oceanData.oceanFloorSize,
                0,
                Mathf.Round(viewerPos.z / oceanData.oceanFloorSize) * oceanData.oceanFloorSize
            );
        }
    }
    
    // Utility method to get heightmap value at a specific world position
    public float GetHeightAtPosition(Vector3 worldPos) {
        if (oceanFloorObject == null) return oceanData.oceanDepth;
        
        // Convert world position to local grid coordinates
        Vector3 localPos = worldPos - oceanFloorObject.transform.position;
        float halfSize = oceanData.oceanFloorSize / 2f;
        
        // Check if position is within ocean bounds
        if (Mathf.Abs(localPos.x) > halfSize || Mathf.Abs(localPos.z) > halfSize) {
            return oceanData.oceanDepth;
        }
        
        // Convert to grid coordinates
        float gridX = (localPos.x + halfSize) / oceanData.oceanFloorSize * oceanData.oceanFloorResolution;
        float gridZ = (localPos.z + halfSize) / oceanData.oceanFloorSize * oceanData.oceanFloorResolution;
        
        // Get the four surrounding grid points
        int x0 = Mathf.FloorToInt(gridX);
        int x1 = Mathf.CeilToInt(gridX);
        int z0 = Mathf.FloorToInt(gridZ);
        int z1 = Mathf.CeilToInt(gridZ);
        
        // Clamp to valid range
        x0 = Mathf.Clamp(x0, 0, oceanData.oceanFloorResolution);
        x1 = Mathf.Clamp(x1, 0, oceanData.oceanFloorResolution);
        z0 = Mathf.Clamp(z0, 0, oceanData.oceanFloorResolution);
        z1 = Mathf.Clamp(z1, 0, oceanData.oceanFloorResolution);
        
        // Get vertex indices
        int i00 = z0 * (oceanData.oceanFloorResolution + 1) + x0;
        int i01 = z0 * (oceanData.oceanFloorResolution + 1) + x1;
        int i10 = z1 * (oceanData.oceanFloorResolution + 1) + x0;
        int i11 = z1 * (oceanData.oceanFloorResolution + 1) + x1;
        
        // Get vertex heights
        MeshFilter meshFilter = oceanFloorObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return oceanData.oceanDepth;
        
        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        if (i00 >= vertices.Length || i01 >= vertices.Length || 
            i10 >= vertices.Length || i11 >= vertices.Length) {
            return oceanData.oceanDepth;
        }
        
        float h00 = vertices[i00].y;
        float h01 = vertices[i01].y;
        float h10 = vertices[i10].y;
        float h11 = vertices[i11].y;
        
        // Bilinear interpolation
        float tx = gridX - x0;
        float tz = gridZ - z0;
        
        float h0 = Mathf.Lerp(h00, h01, tx);
        float h1 = Mathf.Lerp(h10, h11, tx);
        
        return Mathf.Lerp(h0, h1, tz);
    }
    
    // Calculate distance from a world position to the nearest island
    public float GetDistanceToNearestIsland(Vector3 worldPos) {
        if (endlessTerrain == null) return oceanData.flatOceanDistance;
        
        // Get the method that's been added to EndlessTerrain
        // This is a helpful way to access the data without using reflection
        System.Type terrainType = endlessTerrain.GetType();
        System.Reflection.MethodInfo methodInfo = terrainType.GetMethod("GetDistanceToClosestIsland");
        
        if (methodInfo != null) {
            return (float)methodInfo.Invoke(endlessTerrain, new object[] { new Vector2(worldPos.x, worldPos.z) });
        }
        
        // Fallback if method doesn't exist - islands are too far
        return oceanData.flatOceanDistance;
    }
    
    void OnDrawGizmosSelected() {
        // Visualize the ocean boundaries in the editor
        if (oceanData != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, new Vector3(oceanData.oceanFloorSize, 10, oceanData.oceanFloorSize));
        }
    }
    
    void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks
        if (oceanData != null) {
            oceanData.OnValuesUpdated -= OnOceanDataValuesUpdated;
        }
    }
}