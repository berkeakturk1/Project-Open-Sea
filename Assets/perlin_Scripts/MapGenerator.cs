using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using perlin_Scripts.Data;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode {NoiseMap, Mesh, FalloffMap, OceanFloorMap};
    public DrawMode drawMode;
    
    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;
    public Material terrainMaterial;
    
    [Header("Ocean Settings")]
    public OceanData oceanData;
    public bool linkOceanToTerrain = true;
    private OceanFloorGenerator oceanFloorGenerator;
    
    [Header("Dynamic Tree Placement")]
    public bool useDynamicTreePlacement = true;
    public float raycastThreshold = 200f; // Distance threshold for using raycasts instead of heightmap
    private Dictionary<Vector2, bool> chunksUsingRaycast = new Dictionary<Vector2, bool>();

    [Header("Vegetation Settings")]
    public bool spawnVegetation = true;
    [Tooltip("Reference to the VegetationManager component")]
    public VegetationManager vegetationManager;
    
    [Header("Tree Settings")]
    public bool spawnTrees = true;
    [Tooltip("List of tree prefabs to randomly select from")]
    public GameObject[] treePrefabs;
    [Range(0.01f, 1f)]
    public float treeDensity = 0.1f;
    [Range(0f, 1f)]
    public float minHeightThreshold = 0.2f;
    [Range(0f, 1f)]
    public float maxHeightThreshold = 0.8f;
    [Range(0f, 85f)]
    public float maxSlopeAngle = 30f;
    [Range(1.0f, 10.0f)]
    public float minTreeScale = 0.8f;
    [Range(1.0f, 15.0f)]
    public float maxTreeScale = 1.2f;
    public int maxTreesPerChunk = 500;

    [Range(0,MeshGenerator.numSupportedChunkSizes-1)]
    public int chunkSizeIndex;

    [Range(0,MeshGenerator.numSupportedFlatshadedChunkSizes-1)]
    public int flatshadedChunkSizeIndex;

    [Range(0,MeshGenerator.numSupportedLODs-1)]
    public int editorPreviewLOD;

    public bool autoUpdate;
    
    private Transform editorPreviewTreeContainer;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    
    // Changed approach: Instead of doing tree generation in a separate thread, 
    // we'll just queue up the requests and do the actual generation in the main thread
    Queue<TreeSpawnRequest> treeSpawnRequests = new Queue<TreeSpawnRequest>();
    
    Dictionary<Vector2, Transform> treeContainers = new Dictionary<Vector2, Transform>();
    
    private Transform editorPreviewVegetationContainer;
    
    void Awake() {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    
        if (oceanData != null && oceanData.generateOceanFloor) {
            // First check for an existing OceanFloorGenerator in the scene
            oceanFloorGenerator = FindObjectOfType<OceanFloorGenerator>();
        
            if (oceanFloorGenerator == null) {
                // Only create a new one if none exists
                oceanFloorGenerator = gameObject.AddComponent<OceanFloorGenerator>();
                oceanFloorGenerator.oceanData = oceanData;
                oceanFloorGenerator.viewer = FindObjectOfType<EndlessTerrain>().viewer;
            } else {
                // If one exists, just make sure it has the correct data
                if (oceanFloorGenerator.oceanData == null) {
                    oceanFloorGenerator.oceanData = oceanData;
                }
            
                if (oceanFloorGenerator.viewer == null) {
                    oceanFloorGenerator.viewer = FindObjectOfType<EndlessTerrain>().viewer;
                }
            }
        }
    
        // Initialize tree pool with a delayed call
        StartCoroutine(SetupTreePoolDelayed());
    }
    
    void Start() {
        if (TerrainSceneFlag.IsComingFromAnotherScene) {
            // Reapply texture and shader properties after scene load.
            textureData.ApplyToMaterial(terrainMaterial);
            textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        
            // Reset the flag so that subsequent loads don't repeat this unnecessarily
            TerrainSceneFlag.IsComingFromAnotherScene = false;
        }
    }


    private IEnumerator SetupTreePoolDelayed() {
        Debug.Log("MapGenerator: Starting delayed tree pool setup");
    
        // Wait a couple of frames to ensure other components initialize
        yield return new WaitForSeconds(0.1f);
    
        // Find the TreePoolManager if it exists
        TreePoolManager poolManager = FindObjectOfType<TreePoolManager>();
    
        if (poolManager == null) {
            Debug.LogWarning("No TreePoolManager found in scene. Creating one.");
            GameObject poolObj = new GameObject("TreePoolManager");
            poolManager = poolObj.AddComponent<TreePoolManager>();
        }
    
        // Now that we have a reference, initialize it with our prefabs
        if (treePrefabs != null && treePrefabs.Length > 0) {
            poolManager.SetupTreePrefabs(treePrefabs);
            Debug.Log("MapGenerator: Tree pool setup complete");
        } else {
            Debug.LogError("MapGenerator: No tree prefabs assigned, cannot initialize pool");
        }
    }
    
    // Get a random tree prefab using the provided random number generator
    public GameObject GetRandomTreePrefab(System.Random prng) {
        if (treePrefabs == null || treePrefabs.Length == 0) return null;
        
        int index = prng.Next(0, treePrefabs.Length);
        return treePrefabs[index];
    }

    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            // Clear existing preview objects before redrawing
            ClearEditorPreviewObjects();
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }
    
    public void RequestVegetationData(MapData mapData, Vector2 chunkPosition, int lod) {
        if (!spawnVegetation || vegetationManager == null) return;
    
        // Queue vegetation request
        treeSpawnRequests.Enqueue(new TreeSpawnRequest(mapData, chunkPosition, lod));
    }
    
    
    
    public int mapChunkSize {
        get {
            if (terrainData.useFlatShading) {
                return MeshGenerator.supportedFlatshadedChunkSizes[flatshadedChunkSizeIndex] - 1;
            } else {
                return MeshGenerator.supportedChunkSizes[chunkSizeIndex] - 1;
            }
        }
    }

    public void DrawMapInEditor() {
    textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    MapData mapData = GenerateMapData(Vector2.zero);
    MapDisplay display = FindObjectOfType<MapDisplay>();
    
    if (drawMode == DrawMode.NoiseMap) {
        display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
    } else if (drawMode == DrawMode.Mesh) {
        display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
        
        // Spawn trees in editor preview
        if (spawnTrees && treePrefabs != null && treePrefabs.Length > 0) {
            // Clear existing preview trees
            if (editorPreviewTreeContainer != null) {
                DestroyImmediate(editorPreviewTreeContainer.gameObject);
            }
            
            // Create a container specifically for preview trees
            GameObject treeContainer = new GameObject("PreviewTrees");
            editorPreviewTreeContainer = treeContainer.transform;
            editorPreviewTreeContainer.SetParent(transform);
            
            // Create preview trees
            SpawnTreesForPreview(mapData, Vector2.zero, editorPreviewLOD, editorPreviewTreeContainer);
            
            // Add HideOnPlay component to make sure they disappear when entering play mode
            if (!editorPreviewTreeContainer.GetComponent<HideOnPlay>()) {
                editorPreviewTreeContainer.gameObject.AddComponent<HideOnPlay>();
            }
        }
        
        // NEW CODE: Add vegetation preview
        if (spawnVegetation && vegetationManager != null) {
            // Clear existing preview vegetation
            if (editorPreviewVegetationContainer != null) {
                DestroyImmediate(editorPreviewVegetationContainer.gameObject);
            }
            
            // Create a container for preview vegetation
            GameObject vegetationContainer = new GameObject("PreviewVegetation");
            editorPreviewVegetationContainer = vegetationContainer.transform;
            editorPreviewVegetationContainer.SetParent(transform);
            
            // Generate preview vegetation
            GeneratePreviewVegetation(mapData, Vector2.zero, editorPreviewLOD);
            
            // Add HideOnPlay component
            if (!editorPreviewVegetationContainer.GetComponent<HideOnPlay>()) {
                editorPreviewVegetationContainer.gameObject.AddComponent<HideOnPlay>();
            }
        }
    } else if (drawMode == DrawMode.FalloffMap) {
        display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
    } else if (drawMode == DrawMode.OceanFloorMap) {
        // Preview ocean floor noise
        float[,] oceanNoiseMap = Noise.GenerateNoiseMap(
            mapChunkSize, 
            mapChunkSize,
            oceanData.oceanNoiseSeed,
            oceanData.oceanNoiseScale,
            oceanData.oceanNoiseOctaves,
            oceanData.oceanNoisePersistance,
            oceanData.oceanNoiseLacunarity,
            Vector2.zero,
            Noise.NormalizeMode.Local
        );
        display.DrawTexture(TextureGenerator.TextureFromHeightMap(oceanNoiseMap));
    }
}
    
    private void ClearEditorPreviewObjects() {
        // Clear trees
        if (editorPreviewTreeContainer != null) {
            DestroyImmediate(editorPreviewTreeContainer.gameObject);
            editorPreviewTreeContainer = null;
        }
    
        // Clear vegetation
        if (editorPreviewVegetationContainer != null) {
            DestroyImmediate(editorPreviewVegetationContainer.gameObject);
            editorPreviewVegetationContainer = null;
        }
    
        Debug.Log("Editor: Cleared preview objects");
    }
    public void RequestMapData(Vector2 centre, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(centre, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback) {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    // New approach: queue a request for tree spawning instead of doing it in a thread
    public void RequestTreeData(MapData mapData, Vector2 chunkPosition, int lod, bool useRaycast = false) {
        if (!spawnTrees || treePrefabs == null || treePrefabs.Length == 0) return;
    
        // Store which method this chunk is using
        chunksUsingRaycast[chunkPosition] = useRaycast;
    
        // Queue the request
        lock (treeSpawnRequests) {
            treeSpawnRequests.Enqueue(new TreeSpawnRequest(mapData, chunkPosition, lod));
        }
    }

    void Update() {
        // Process map data thread info queue
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        // Process mesh data thread info queue
        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        // Process tree spawn requests in the main thread
        if (treeSpawnRequests.Count > 0) {
            int maxToProcess = 2; // Limit how many we process per frame to prevent lag
            int processed = 0;
            
            while (treeSpawnRequests.Count > 0 && processed < maxToProcess) {
                TreeSpawnRequest request;
                lock (treeSpawnRequests) {
                    request = treeSpawnRequests.Dequeue();
                }
                
                // Generate and spawn trees directly in the main thread
                SpawnTreesOnChunk(request.mapData, request.chunkPosition, request.lod);
                
                if (spawnVegetation && vegetationManager != null) {
                    vegetationManager.GenerateVegetation(
                        request.mapData, 
                        request.chunkPosition, 
                        request.lod, 
                        terrainData.meshHeightCurve, 
                        terrainData.meshHeightMultiplier
                    );
                }
                
                processed++;
            }
        }
    }

    MapData GenerateMapData(Vector2 centre) {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff) {
            if (falloffMap == null) {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

            for (int y = 0; y < mapChunkSize + 2; y++) {
                for (int x = 0; x < mapChunkSize + 2; x++) {
                    if (terrainData.useFalloff) {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    // This function runs in the main thread, so it's safe to use Random
    void SpawnTreesOnChunk(MapData mapData, Vector2 chunkPosition, int lod) {
        // Check if we've already spawned trees for this chunk
        bool chunkHasTrees = treeContainers.TryGetValue(chunkPosition, out Transform treeParent);
        
        // Check which placement method we should use
        bool useRaycast = false;
        if (chunksUsingRaycast.TryGetValue(chunkPosition, out bool savedMethod)) {
            useRaycast = savedMethod;
        }
        
        // If the chunk already has trees but we're switching placement methods, clear existing trees
        if (chunkHasTrees && chunkHasTrees != useRaycast) {
            ClearTrees(chunkPosition);
            chunkHasTrees = false;
        }
        
        // Create or get the container for this chunk's trees
        if (!chunkHasTrees) {
            GameObject treeContainer = new GameObject("Trees_" + chunkPosition.x + "_" + chunkPosition.y);
            treeParent = treeContainer.transform;
            treeParent.SetParent(transform);
            treeParent.position = new Vector3(chunkPosition.x, 0, chunkPosition.y);
            treeContainers[chunkPosition] = treeParent;
        }

        float[,] heightMap = mapData.heightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Adjust density based on LOD
        float adjustedDensity = treeDensity / (lod + 1);
        int step = Mathf.Max(1, Mathf.FloorToInt(1f / adjustedDensity / 10f));
        step = Mathf.Max(1, step - lod);

        int treeCount = 0;
        List<Vector3> treePositions = new List<Vector3>();

        // Use deterministic seed based on chunk position for consistent tree placement
        System.Random prng = new System.Random(
            (int)(chunkPosition.x * 1000 + chunkPosition.y * 10000 + noiseData.seed)
        );
        
        float scale = terrainData.uniformScale;
        int terrainLayerMask = LayerMask.GetMask("Default");
        
        for (int y = 1; y < height - 1; y += step) {
            for (int x = 1; x < width - 1; x += step) {
                if (treeCount >= maxTreesPerChunk) break;
                
                if (prng.NextDouble() > adjustedDensity) continue;
                
                float heightValue = heightMap[x, y];
                
                if (heightValue < minHeightThreshold || heightValue > maxHeightThreshold) continue;
                
                // Calculate position from heightmap 
                float worldX = (x - width/2f);
                float worldZ = (y - height/2f);
                float worldY = terrainData.meshHeightCurve.Evaluate(heightValue) * terrainData.meshHeightMultiplier;
                
                // Check slope
                Vector3 normal = CalculateNormal(x, y, heightMap);
                float slope = Vector3.Angle(normal, Vector3.up);
                
                if (slope > maxSlopeAngle) continue;
                
                // Calculate heightmap-based position (always needed as fallback)
                Vector3 calculatedPosition = new Vector3(
                    (worldX + chunkPosition.x) * scale, 
                    worldY * scale, 
                    (worldZ + chunkPosition.y) * scale
                );
                
                // Final position and rotation 
                Vector3 finalPosition = calculatedPosition;
                Quaternion finalRotation;
                
                // If close enough to player, use raycasting for precision
                if (useRaycast) {
                    // Try raycast from above the calculated position
                    Vector3 rayStart = new Vector3(
                        calculatedPosition.x,
                        calculatedPosition.y + 10f,
                        calculatedPosition.z
                    );
                    
                    RaycastHit hit;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, terrainLayerMask)) {
                        // Use the more precise raycast position
                        finalPosition = hit.point;
                        
                        // Align with ground normal from raycast
                        Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, hit.normal);
                        float rotation = (float)(prng.NextDouble() * 360f);
                        Quaternion randomRotation = Quaternion.Euler(0, rotation, 0);
                        finalRotation = normalAlignment * randomRotation;
                    } else {
                        // Fallback to heightmap normal if raycast fails
                        float rotation = (float)(prng.NextDouble() * 360f);
                        Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, normal);
                        finalRotation = normalAlignment * Quaternion.Euler(0, rotation, 0);
                    }
                } else {
                    // For distant chunks, just use the heightmap data
                    float rotation = (float)(prng.NextDouble() * 360f);
                    Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, normal);
                    finalRotation = normalAlignment * Quaternion.Euler(0, rotation, 0);
                }
                
                // Check minimum distance
                bool tooClose = false;
                foreach (Vector3 existingTree in treePositions) {
                    Vector2 pos1 = new Vector2(finalPosition.x, finalPosition.z);
                    Vector2 pos2 = new Vector2(existingTree.x, existingTree.z);
                    if (Vector2.Distance(pos1, pos2) < 2.0f * scale) {
                        tooClose = true;
                        break;
                    }
                }
                
                if (tooClose) continue;
                
                treePositions.Add(finalPosition);
                
                // Small offset to prevent z-fighting
                finalPosition += Vector3.up * 0.05f;
                
                // Scale and create the tree
                float treeScale = (float)(minTreeScale + prng.NextDouble() * (maxTreeScale - minTreeScale));
                Vector3 treeSizeVector = new Vector3(treeScale, treeScale, treeScale);
                
                // Select a random tree prefab
                GameObject selectedTreePrefab = GetRandomTreePrefab(prng);
                if (selectedTreePrefab != null) {
                    if (TreePoolManager.Instance != null) {
                        GameObject tree = TreePoolManager.Instance.GetTree(finalPosition, finalRotation, treeSizeVector, treeParent, selectedTreePrefab);
                        
                        // Only add the placer component if we're in play mode and the tree doesn't already have one
                        if (Application.isPlaying && tree != null && tree.GetComponent<TreePlacer>() == null) {
                            tree.AddComponent<TreePlacer>();
                        }
                    } else {
                        GameObject tree = Instantiate(selectedTreePrefab, finalPosition, finalRotation, treeParent);
                        tree.transform.localScale = treeSizeVector;
                        
                        if (Application.isPlaying && tree != null && tree.GetComponent<TreePlacer>() == null) {
                            tree.AddComponent<TreePlacer>();
                        }
                    }
                }
                
                treeCount++;
            }
        }
        
        Debug.Log($"Spawned {treeCount} trees in chunk at {chunkPosition} using {(useRaycast ? "raycast" : "heightmap")}");
    }
    
    
    private void GeneratePreviewVegetation(MapData mapData, Vector2 position, int lod) {
    if (vegetationManager == null) {
        Debug.LogWarning("No VegetationManager found. Cannot generate preview vegetation.");
        return;
    }
    
    // Create a simplified version of vegetation generation for preview
    // This avoids modifying the VegetationManager's state during editor preview
    GameObject previewObj = new GameObject("VegetationPreview");
    previewObj.transform.SetParent(editorPreviewVegetationContainer);
    previewObj.transform.position = new Vector3(position.x, 0, position.y) * terrainData.uniformScale;
    
    float[,] heightMap = mapData.heightMap;
    int mapSize = heightMap.GetLength(0);
    float uniformScale = terrainData.uniformScale;
    
    // For each vegetation type
    foreach (var vegType in vegetationManager.vegetationTypes) {
        if (vegType.mesh == null || vegType.material == null) continue;
        
        // Create a container for this vegetation type
        GameObject typeContainer = new GameObject(vegType.name);
        typeContainer.transform.SetParent(previewObj.transform);
        
        // Create a material instance to avoid modifying the original
        Material previewMaterial = new Material(vegType.material);
        
        // Use density to determine step size
        float density = vegType.density * 2f; // Increase for preview
        int step = Mathf.Max(1, Mathf.FloorToInt(5f / density));
        
        // Create mesh instances based on density
        System.Random prng = new System.Random(12345); // Fixed seed for preview
        
        // Limit preview instances to a reasonable number
        int maxPreviewInstances = Mathf.Min(vegType.maxInstancesPerChunk, 1000);
        int instanceCount = 0;
        
        for (int y = 1; y < mapSize - 1; y += step) {
            for (int x = 1; x < mapSize - 1; x += step) {
                if (instanceCount >= maxPreviewInstances) break;
                
                // Use probability to control density
                if (prng.NextDouble() > 0.8f) continue;
                
                float heightValue = heightMap[x, y];
                
                // Skip if outside height thresholds
                if (heightValue < vegType.minHeightThreshold || heightValue > vegType.maxHeightThreshold) continue;
                
                // Calculate position
                float worldX = (x - mapSize/2f);
                float worldZ = (y - mapSize/2f);
                float worldY = terrainData.meshHeightCurve.Evaluate(heightValue) * terrainData.meshHeightMultiplier;
                
                // Check slope
                Vector3 normal = CalculateNormal(x, y, heightMap);
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope > vegType.maxSlopeAngle) continue;
                
                // Random offset
                float offsetX = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset * Random.Range(-5f, 5f);
                float offsetZ = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset * Random.Range(-5f, 5f);
                
                // Create instance
                GameObject instance = new GameObject($"Instance_{instanceCount}");
                instance.transform.SetParent(typeContainer.transform);
                
                // Position should match how you place vegetation at runtime
                Vector3 _position = new Vector3(
                    (worldX + offsetX) * uniformScale, 
                    worldY * uniformScale, 
                    (worldZ + offsetZ) * uniformScale
                );
                
                instance.transform.position = _position;
                
                // Random rotation
                float rotY = (float)prng.NextDouble() * 360f;
                instance.transform.rotation = Quaternion.Euler(0, rotY, 0) * Quaternion.FromToRotation(Vector3.up, normal);
                
                // Random scale
                float scaleValue = vegType.minScale + (float)prng.NextDouble() * (vegType.maxScale - vegType.minScale);
                scaleValue *= uniformScale * 0.15f;
                instance.transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
                
                // Add mesh renderer and filter
                MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = instance.AddComponent<MeshRenderer>();
                
                meshFilter.sharedMesh = vegType.mesh;
                meshRenderer.sharedMaterial = previewMaterial;
                
                instanceCount++;
            }
            
            if (instanceCount >= maxPreviewInstances) break;
        }
        
        Debug.Log($"Generated {instanceCount} preview instances of {vegType.name}");
    }
}
    
    void SpawnTreesForPreview(MapData mapData, Vector2 position, int lod, Transform parent) {
        float[,] heightMap = mapData.heightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Adjust density based on LOD
        float adjustedDensity = treeDensity / (lod + 1);
        int step = Mathf.Max(1, Mathf.FloorToInt(1f / adjustedDensity / 10f));
        step = Mathf.Max(1, step - lod);

        int treeCount = 0;
        List<Vector3> treePositions = new List<Vector3>();

        // Use deterministic seed based on chunk position for consistent tree placement
        System.Random prng = new System.Random(
            (int)(position.x * 1000 + position.y * 10000 + noiseData.seed)
        );
        
        float scale = terrainData.uniformScale;
        int terrainLayerMask = LayerMask.GetMask("Default");
        
        for (int y = 1; y < height - 1; y += step) {
            for (int x = 1; x < width - 1; x += step) {
                if (treeCount >= maxTreesPerChunk) break;
                
                if (prng.NextDouble() > adjustedDensity) continue;
                
                float heightValue = heightMap[x, y];
                
                if (heightValue < minHeightThreshold || heightValue > maxHeightThreshold) continue;
                
                // Calculate position from heightmap 
                float worldX = (x - width/2f);
                float worldZ = (y - height/2f);
                float worldY = terrainData.meshHeightCurve.Evaluate(heightValue) * terrainData.meshHeightMultiplier;
                
                // Check slope
                Vector3 normal = CalculateNormal(x, y, heightMap);
                float slope = Vector3.Angle(normal, Vector3.up);
                
                if (slope > maxSlopeAngle) continue;
                
                // Calculate heightmap-based position (always needed as fallback)
                Vector3 calculatedPosition = new Vector3(
                    (worldX + position.x) * scale, 
                    worldY * scale, 
                    (worldZ + position.y) * scale
                );
                
                // Final position and rotation 
                Vector3 finalPosition = calculatedPosition;
                Quaternion finalRotation;
                
                // Try raycast for preview (just like in runtime)
                Vector3 rayStart = new Vector3(
                    calculatedPosition.x,
                    calculatedPosition.y + 10f,
                    calculatedPosition.z
                );
                
                RaycastHit hit;
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, terrainLayerMask)) {
                    // Use the more precise raycast position
                    finalPosition = hit.point;
                    
                    // Align with ground normal from raycast
                    Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    float rotation = (float)(prng.NextDouble() * 360f);
                    Quaternion randomRotation = Quaternion.Euler(0, rotation, 0);
                    finalRotation = normalAlignment * randomRotation;
                } else {
                    // Fallback to heightmap normal if raycast fails
                    float rotation = (float)(prng.NextDouble() * 360f);
                    Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, normal);
                    finalRotation = normalAlignment * Quaternion.Euler(0, rotation, 0);
                }
                
                // Check minimum distance
                bool tooClose = false;
                foreach (Vector3 existingTree in treePositions) {
                    Vector2 pos1 = new Vector2(finalPosition.x, finalPosition.z);
                    Vector2 pos2 = new Vector2(existingTree.x, existingTree.z);
                    if (Vector2.Distance(pos1, pos2) < 2.0f * scale) {
                        tooClose = true;
                        break;
                    }
                }
                
                if (tooClose) continue;
                
                treePositions.Add(finalPosition);
                
                // Small offset to prevent z-fighting
                finalPosition += Vector3.up * 0.05f;
                
                // Scale and create the tree
                float treeScale = (float)(minTreeScale + prng.NextDouble() * (maxTreeScale - minTreeScale));
                Vector3 treeSizeVector = new Vector3(treeScale, treeScale, treeScale);
                
                // Select a random tree prefab for preview
                GameObject selectedTreePrefab = GetRandomTreePrefab(prng);
                if (selectedTreePrefab != null) {
                    // Editor preview doesn't use pooling, but uses direct instantiation
                    GameObject tree = Instantiate(selectedTreePrefab, finalPosition, finalRotation, parent);
                    tree.transform.localScale = treeSizeVector;
                }
                
                treeCount++;
            }
        }
        
        Debug.Log($"Spawned {treeCount} preview trees");
    }
    
    public void ClearTrees(Vector2 chunkPosition) {
        if (treeContainers.TryGetValue(chunkPosition, out Transform treeParent)) {
            if (spawnVegetation && vegetationManager != null) {
                vegetationManager.ClearVegetation(chunkPosition);
            }
            if (TreePoolManager.Instance != null) {
                TreePoolManager.Instance.ReturnTrees(treeParent);
                Debug.Log($"Trees from {chunkPosition} returned to pool");
            } else {
                // Fallback destruction if pool isn't available
                for (int i = treeParent.childCount - 1; i >= 0; i--) {
                    Destroy(treeParent.GetChild(i).gameObject);
                }
                Debug.LogWarning("Pool not available, trees destroyed directly");
            }
    
            // Destroy the container
            Destroy(treeParent.gameObject);
            treeContainers.Remove(chunkPosition);
        }
    }

    public void ClearAllTrees() {
        foreach (var container in treeContainers.Values) {
            if (container != null) {
                TreePoolManager.Instance.ReturnTrees(container);
                Destroy(container.gameObject);
            }
        }
        treeContainers.Clear();
    }
    
    public bool ShouldRespawnTrees(Vector2 chunkPosition, bool shouldUseRaycast) {
        // Check if we've recorded what method was used for this chunk
        if (chunksUsingRaycast.TryGetValue(chunkPosition, out bool currentMethod)) {
            // If the method changed, we should respawn
            return currentMethod != shouldUseRaycast;
        }
    
        // If we have no record, assume we need to spawn
        return false;
    }
    
    void OnDrawGizmosSelected() {
        if (!Application.isPlaying) return;
    
        // Visualize tree positions and raycasts
        Gizmos.color = Color.green;
    
        // Draw a sphere at each tree container position
        foreach (var container in treeContainers.Values) {
            if (container != null) {
                Gizmos.DrawSphere(container.position, 1f);
            
                // Draw a line for each tree in the container
                for (int i = 0; i < container.childCount; i++) {
                    Transform child = container.GetChild(i);
                    Gizmos.DrawLine(child.position, child.position + Vector3.up * 2f);
                }
            }
        }
    }

    Vector3 CalculateNormal(int x, int y, float[,] heightMap) {
        float heightL = terrainData.meshHeightCurve.Evaluate(heightMap[x-1, y]) * terrainData.meshHeightMultiplier;
        float heightR = terrainData.meshHeightCurve.Evaluate(heightMap[x+1, y]) * terrainData.meshHeightMultiplier;
        float heightD = terrainData.meshHeightCurve.Evaluate(heightMap[x, y-1]) * terrainData.meshHeightMultiplier;
        float heightU = terrainData.meshHeightCurve.Evaluate(heightMap[x, y+1]) * terrainData.meshHeightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    

    void OnValidate() {
        if (terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null) {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    struct TreeSpawnRequest {
        public readonly MapData mapData;
        public readonly Vector2 chunkPosition;
        public readonly int lod;

        public TreeSpawnRequest(MapData mapData, Vector2 chunkPosition, int lod) {
            this.mapData = mapData;
            this.chunkPosition = chunkPosition;
            this.lod = lod;
        }
    }
}

public struct MapData {
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap) {
        this.heightMap = heightMap;
    }
}