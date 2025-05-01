using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using perlin_Scripts.Data;

public class OceanFloorGenerator : MonoBehaviour {
    public OceanData oceanData;
    public Transform viewer;
    public Material oceanFloorMaterial;

    private Dictionary<Vector2Int, GameObject> oceanFloorTiles = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int currentCenterTile;
    private MapGenerator mapGenerator;
    private EndlessTerrain endlessTerrain;
    private bool hasInitialized = false;

    void Awake() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        endlessTerrain = FindObjectOfType<EndlessTerrain>();
        if (viewer == null && endlessTerrain != null) {
            viewer = endlessTerrain.viewer;
        }
    }

    void Start() {
        if (oceanData == null) {
            Debug.LogError("OceanData not assigned to OceanFloorGenerator!");
            return;
        }

        if (oceanFloorMaterial != null) {
            oceanData.ApplyToMaterial(oceanFloorMaterial);
        }

        if (oceanData.generateOceanFloor) {
            StartCoroutine(DelayedGeneration());
        }

        if (oceanData != null) {
            oceanData.OnValuesUpdated += OnOceanDataValuesUpdated;
        }
    }

    IEnumerator DelayedGeneration() {
        yield return new WaitForSeconds(0.2f);
        UpdateOceanPosition();
        hasInitialized = true;
    }

    void OnOceanDataValuesUpdated() {
        if (hasInitialized && oceanData.generateOceanFloor) {
            if (oceanFloorMaterial != null) {
                oceanData.ApplyToMaterial(oceanFloorMaterial);
            }
            UpdateOceanPosition();
        }
    }

    void Update() {
        if (!hasInitialized || !oceanData.generateOceanFloor || !oceanData.followPlayer || viewer == null) return;

        Vector2 viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        Vector2Int centerTile = new Vector2Int(
            Mathf.RoundToInt(viewer.position.x / oceanData.oceanFloorSize),
            Mathf.RoundToInt(viewer.position.z / oceanData.oceanFloorSize)
        );

        if (centerTile != currentCenterTile) {
            currentCenterTile = centerTile;
            UpdateOceanFloorTiles(centerTile);
        }
    }

    void UpdateOceanPosition() {
        if (viewer == null || !oceanData.followPlayer) return;

        Vector2Int centerTile = new Vector2Int(
            Mathf.RoundToInt(viewer.position.x / oceanData.oceanFloorSize),
            Mathf.RoundToInt(viewer.position.z / oceanData.oceanFloorSize)
        );

        currentCenterTile = centerTile;
        UpdateOceanFloorTiles(centerTile);
    }

    void UpdateOceanFloorTiles(Vector2Int centerTile) {
        HashSet<Vector2Int> neededTiles = new HashSet<Vector2Int>();

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                Vector2Int tileCoord = new Vector2Int(centerTile.x + x, centerTile.y + y);
                neededTiles.Add(tileCoord);

                if (!oceanFloorTiles.ContainsKey(tileCoord)) {
                    Vector3 tileWorldPos = new Vector3(
                        tileCoord.x * oceanData.oceanFloorSize,
                        0,
                        tileCoord.y * oceanData.oceanFloorSize
                    );
                    GameObject tile = CreateOceanFloorTile(tileWorldPos);
                    oceanFloorTiles.Add(tileCoord, tile);
                }
            }
        }

        // Destroy unneeded tiles
        var keysToRemove = oceanFloorTiles.Keys.Where(key => !neededTiles.Contains(key)).ToList();
        foreach (var key in keysToRemove) {
            Destroy(oceanFloorTiles[key]);
            oceanFloorTiles.Remove(key);
        }
    }

    GameObject CreateOceanFloorTile(Vector3 position) {
        GameObject tile = new GameObject($"OceanFloorTile_{position.x}_{position.z}");
        tile.transform.parent = transform;
        tile.transform.position = position;

        MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = tile.AddComponent<MeshCollider>();

        meshFilter.mesh = CreateOceanFloorMesh();
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        if (oceanFloorMaterial != null) {
            meshRenderer.material = oceanFloorMaterial;
        } else {
            meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        return tile;
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

        for (int z = 0; z <= resolution; z++) {
            for (int x = 0; x <= resolution; x++) {
                int i = z * (resolution + 1) + x;
                float worldX = x * cellSize - halfSize;
                float worldZ = z * cellSize - halfSize;
                float height = oceanData.oceanDepth + (noiseMap[x, z] * oceanData.oceanFloorVariation);
                vertices[i] = new Vector3(worldX, height, worldZ);
                uvs[i] = new Vector2((worldX + halfSize) / oceanData.oceanFloorSize, (worldZ + halfSize) / oceanData.oceanFloorSize);
                normals[i] = Vector3.up;
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < resolution; z++) {
            for (int x = 0; x < resolution; x++) {
                int topLeft = z * (resolution + 1) + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * (resolution + 1) + x;
                int bottomRight = bottomLeft + 1;

                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;

                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = bottomRight;
            }
        }

        for (int z = 1; z < resolution; z++) {
            for (int x = 1; x < resolution; x++) {
                int i = z * (resolution + 1) + x;
                Vector3 up = vertices[i - (resolution + 1)];
                Vector3 down = vertices[i + (resolution + 1)];
                Vector3 left = vertices[i - 1];
                Vector3 right = vertices[i + 1];
                Vector3 center = vertices[i];

                Vector3 normal1 = Vector3.Cross(up - center, right - center).normalized;
                Vector3 normal2 = Vector3.Cross(right - center, down - center).normalized;
                Vector3 normal3 = Vector3.Cross(down - center, left - center).normalized;
                Vector3 normal4 = Vector3.Cross(left - center, up - center).normalized;

                normals[i] = (normal1 + normal2 + normal3 + normal4).normalized;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.Optimize();

        return mesh;
    }

    void OnDrawGizmosSelected() {
        if (oceanData != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, new Vector3(oceanData.oceanFloorSize, 10, oceanData.oceanFloorSize));
        }
    }

    void OnDestroy() {
        if (oceanData != null) {
            oceanData.OnValuesUpdated -= OnOceanDataValuesUpdated;
        }
    }
}
