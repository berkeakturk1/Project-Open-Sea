using System.Collections.Generic;
using perlin_Scripts.Data;
using UnityEngine;

namespace perlin_Scripts
{
    public class OceanFloorLODGenerator : MonoBehaviour 
    {
        [System.Serializable]
        public class OceanFloorLODSettings 
        {
            [Tooltip("Resolution of the mesh for this LOD level")]
            public int resolution = 20;
        
            [Tooltip("Size of the mesh for this LOD level")]
            public float size = 500f;
        
            [Tooltip("Distance from player where this LOD becomes active")]
            public float activeDistance = 500f;
        
            [Tooltip("How much detail/variation is applied at this LOD")]
            [Range(0f, 1f)]
            public float detailIntensity = 1f;
        }

        [Header("Ocean Floor LOD Settings")]
        public OceanData oceanData;
        public Transform viewer;
        public Material oceanFloorMaterial;

        [Header("LOD Configuration")]
        public OceanFloorLODSettings[] lodLevels = new OceanFloorLODSettings[3];

        [Header("Performance")]
        public int maxActiveChunks = 16;
        public float chunkUpdateInterval = 0.5f;

        // Internal tracking
        private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
        private float lastUpdateTime;
        private Vector2 lastViewerPosition;

        void Awake() 
        {
            // Initialize LOD levels if not set
            if (lodLevels[0] == null) lodLevels[0] = new OceanFloorLODSettings 
            {
                resolution = 20,
                size = 500f,
                activeDistance = 500f,
                detailIntensity = 1f
            };

            if (lodLevels[1] == null) lodLevels[1] = new OceanFloorLODSettings 
            {
                resolution = 10,
                size = 1000f,
                activeDistance = 1500f,
                detailIntensity = 0.5f
            };

            if (lodLevels[2] == null) lodLevels[2] = new OceanFloorLODSettings 
            {
                resolution = 5,
                size = 2000f,
                activeDistance = 3000f,
                detailIntensity = 0.2f
            };

            // Find viewer if not assigned
            if (viewer == null) 
            {
                viewer = FindObjectOfType<EndlessTerrain>()?.viewer;
            }
        }

        void Start() 
        {
            if (oceanData == null) 
            {
                Debug.LogError("OceanData not assigned to OceanFloorLODGenerator!");
                return;
            }

            if (oceanFloorMaterial != null) 
            {
                oceanData.ApplyToMaterial(oceanFloorMaterial);
            }
        }

        void Update() 
        {
            if (viewer == null) return;

            // Throttle updates to prevent constant regeneration
            if (Time.time - lastUpdateTime < chunkUpdateInterval) return;
            lastUpdateTime = Time.time;

            Vector2 currentViewerPos = new Vector2(viewer.position.x, viewer.position.z);

            // Only update if player has moved significantly
            if (Vector2.Distance(currentViewerPos, lastViewerPosition) < 10f) return;
            lastViewerPosition = currentViewerPos;

            UpdateOceanFloorChunks(currentViewerPos);
        }

        void UpdateOceanFloorChunks(Vector2 viewerPos) 
        {
            // Clear out old chunks
            ClearDistantChunks(viewerPos);

            // Generate new chunks based on LOD levels
            for (int lodLevel = 0; lodLevel < lodLevels.Length; lodLevel++) 
            {
                OceanFloorLODSettings currentLOD = lodLevels[lodLevel];
            
                // Calculate grid of chunks to generate
                int chunksPerSide = Mathf.CeilToInt(currentLOD.activeDistance * 2 / currentLOD.size);
            
                for (int x = -chunksPerSide / 2; x < chunksPerSide / 2; x++) 
                {
                    for (int z = -chunksPerSide / 2; z < chunksPerSide / 2; z++) 
                    {
                        Vector2 chunkCenter = new Vector2(
                            Mathf.Floor(viewerPos.x / currentLOD.size) * currentLOD.size + x * currentLOD.size,
                            Mathf.Floor(viewerPos.y / currentLOD.size) * currentLOD.size + z * currentLOD.size
                        );

                        // Calculate distance from viewer
                        float distanceToChunk = Vector2.Distance(viewerPos, chunkCenter);

                        // Skip if outside this LOD's range
                        if (distanceToChunk > currentLOD.activeDistance) continue;

                        Vector2Int chunkKey = new Vector2Int(
                            Mathf.RoundToInt(chunkCenter.x / currentLOD.size),
                            Mathf.RoundToInt(chunkCenter.y / currentLOD.size)
                        );

                        // Skip if chunk already exists
                        if (activeChunks.ContainsKey(chunkKey)) continue;

                        // Generate chunk
                        GameObject chunk = GenerateOceanFloorChunk(chunkCenter, currentLOD, lodLevel);
                    
                        // Track the chunk
                        activeChunks[chunkKey] = chunk;

                        // Limit total active chunks
                        if (activeChunks.Count >= maxActiveChunks) 
                        {
                            return;
                        }
                    }
                }
            }
        }

        GameObject GenerateOceanFloorChunk(Vector2 chunkCenter, OceanFloorLODSettings lodSettings, int lodLevel) 
        {
            GameObject chunkObject = new GameObject($"OceanFloor_LOD{lodLevel}_{chunkCenter.x}_{chunkCenter.y}");
            chunkObject.transform.SetParent(transform);
        
            // Position the chunk
            chunkObject.transform.position = new Vector3(chunkCenter.x, 0, chunkCenter.y);

            // Add mesh filter and renderer
            MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

            // Create the mesh
            Mesh mesh = CreateOceanFloorMesh(lodSettings, chunkCenter);
            meshFilter.mesh = mesh;

            // Apply material
            if (oceanFloorMaterial != null) 
            {
                meshRenderer.material = oceanFloorMaterial;
            }

            // Optional: Add collider
            MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            return chunkObject;
        }

        Mesh CreateOceanFloorMesh(OceanFloorLODSettings lodSettings, Vector2 chunkCenter) 
        {
            Mesh mesh = new Mesh();
        
            int resolution = lodSettings.resolution;
            float size = lodSettings.size;
        
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            int[] triangles = new int[resolution * resolution * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
        
            float halfSize = size / 2f;
            float cellSize = size / resolution;
        
            // Generate noise map with reduced detail for lower LODs
            float[,] noiseMap = Noise.GenerateNoiseMap(
                resolution + 1, 
                resolution + 1,
                oceanData.oceanNoiseSeed,
                oceanData.oceanNoiseScale * (1f / (lodSettings.detailIntensity + 0.1f)),
                oceanData.oceanNoiseOctaves,
                oceanData.oceanNoisePersistance,
                oceanData.oceanNoiseLacunarity,
                chunkCenter,
                Noise.NormalizeMode.Local
            );
        
            // Create vertices
            for (int z = 0; z <= resolution; z++) {
                for (int x = 0; x <= resolution; x++) {
                    int i = z * (resolution + 1) + x;
                
                    float worldX = x * cellSize - halfSize;
                    float worldZ = z * cellSize - halfSize;
                
                    // Calculate height based on noise and LOD detail
                    float height = oceanData.oceanDepth + 
                                   (noiseMap[x, z] * oceanData.oceanFloorVariation * lodSettings.detailIntensity);
                
                    vertices[i] = new Vector3(worldX, height, worldZ);
                
                    // UV mapping
                    uvs[i] = new Vector2(
                        (worldX + halfSize) / size, 
                        (worldZ + halfSize) / size
                    );
                }
            }
        
            // Create triangles
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
        
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
        
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        
            return mesh;
        }

        void ClearDistantChunks(Vector2 viewerPos) 
        {
            // Create a list of chunks to remove to avoid modifying collection during iteration
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();

            foreach (var chunk in activeChunks) 
            {
                Vector2 chunkPos = new Vector2(chunk.Value.transform.position.x, chunk.Value.transform.position.z);
            
                // Check against all LOD levels
                bool chunkStillRelevant = false;
                foreach (var lodSettings in lodLevels) 
                {
                    if (Vector2.Distance(viewerPos, chunkPos) <= lodSettings.activeDistance) 
                    {
                        chunkStillRelevant = true;
                        break;
                    }
                }

                // Remove distant chunks
                if (!chunkStillRelevant) 
                {
                    Destroy(chunk.Value);
                    chunksToRemove.Add(chunk.Key);
                }
            }

            // Remove tracked chunks
            foreach (var chunkToRemove in chunksToRemove) 
            {
                activeChunks.Remove(chunkToRemove);
            }
        }

        void OnDrawGizmosSelected() 
        {
            if (viewer == null) return;

            // Visualize LOD ranges
            Gizmos.color = Color.blue;
            Vector3 viewerPos = viewer.position;

            foreach (var lodSettings in lodLevels) 
            {
                // Draw wire circle for each LOD range
                Gizmos.DrawWireSphere(viewerPos, lodSettings.activeDistance);
            }
        }
    }
}