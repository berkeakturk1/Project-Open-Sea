using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using perlin_Scripts.Data;

#if UNITY_EDITOR
public class TerrainChunkCreator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public MapGenerator mapGenerator;
    public Material terrainMaterial;
    
    [Header("Prefab Settings")]
    public string prefabName = "TerrainChunk";
    public string prefabPath = "Assets/Prefabs/Terrain";
    
    [Header("Generation Settings")]
    public Vector2 chunkPosition = Vector2.zero;
    public int previewLOD = 0;
    
    [Header("Collider Settings")]
    public bool generateCollider = true;
    
    [Header("Tree Settings")]
    public bool includeTreesInPrefab = true;
    
    [Header("Vegetation Settings")]
    public bool includeVegetationInPrefab = true;
    
    [ContextMenu("Generate Terrain Chunk")]
    public void GenerateTerrainChunk()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator reference is missing!");
            return;
        }
        
        // Ensure the MapGenerator is properly initialized
        if (mapGenerator.terrainData == null || mapGenerator.noiseData == null)
        {
            Debug.LogError("MapGenerator is missing required data (TerrainData or NoiseData)!");
            return;
        }
        
        // Force update textures to ensure proper material properties
        if (mapGenerator.textureData != null && mapGenerator.terrainMaterial != null)
        {
            mapGenerator.textureData.ApplyToMaterial(mapGenerator.terrainMaterial);
            mapGenerator.textureData.UpdateMeshHeights(
                mapGenerator.terrainMaterial, 
                mapGenerator.terrainData.minHeight, 
                mapGenerator.terrainData.maxHeight
            );
        }
        
        // Ensure we have a material
        if (terrainMaterial == null)
        {
            terrainMaterial = mapGenerator.terrainMaterial;
            if (terrainMaterial == null)
            {
                Debug.LogError("Terrain material is missing!");
                return;
            }
        }
        
        // Generate the terrain data
        MapData mapData = mapGenerator.GenerateMapData(chunkPosition);
        
        // Create the parent game object
        GameObject terrainChunkObject = new GameObject(prefabName);
        terrainChunkObject.transform.position = new Vector3(chunkPosition.x, 0, chunkPosition.y) * mapGenerator.terrainData.uniformScale;
        terrainChunkObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
        terrainChunkObject.layer = LayerMask.NameToLayer("Terrain");
        
        // Add the necessary components
        MeshFilter meshFilter = terrainChunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainChunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;
        
        // Generate the mesh
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(
            mapData.heightMap, 
            mapGenerator.terrainData.meshHeightMultiplier, 
            mapGenerator.terrainData.meshHeightCurve, 
            previewLOD, 
            mapGenerator.terrainData.useFlatShading);
        
        // Create the actual mesh and make sure it gets properly assigned
        Mesh mesh = meshData.CreateMesh();
        if (mesh == null)
        {
            Debug.LogError("Failed to create mesh!");
            return;
        }
        
        // Ensure mesh has proper bounds
        mesh.RecalculateBounds();
        
        // Assign the mesh to the mesh filter
        meshFilter.sharedMesh = mesh;
        
        // Verify the mesh assignment
        if (meshFilter.sharedMesh == null)
        {
            Debug.LogError("Mesh assignment failed!");
            return;
        }
        
        Debug.Log($"Mesh created successfully with {mesh.vertexCount} vertices and {mesh.triangles.Length/3} triangles");
        
        // Add collider if needed
        if (generateCollider)
        {
            MeshCollider meshCollider = terrainChunkObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
        
        // Add a component to identify this as a terrain chunk
        terrainChunkObject.AddComponent<TerrainChunkIdentifier>();
        
        // Generate trees if requested
        if (includeTreesInPrefab && mapGenerator.spawnTrees)
        {
            GameObject treeContainer = new GameObject("Trees");
            treeContainer.transform.parent = terrainChunkObject.transform;
            
            // Use a similar method to what MapGenerator uses for editor preview
            SpawnTreesOnChunk(mapData, chunkPosition, previewLOD, treeContainer.transform);
        }
        
        // Generate vegetation if requested
        if (includeVegetationInPrefab && mapGenerator.spawnVegetation && mapGenerator.vegetationManager != null)
        {
            GameObject vegetationContainer = new GameObject("Vegetation");
            vegetationContainer.transform.parent = terrainChunkObject.transform;
            
            // Use a similar method to what MapGenerator uses for vegetation
            GenerateVegetationOnChunk(mapData, chunkPosition, previewLOD, vegetationContainer.transform);
        }
        
        // Create the prefab
        SaveAsPrefab(terrainChunkObject);
    }
    
    private void SpawnTreesOnChunk(MapData mapData, Vector2 position, int lod, Transform parent)
    {
        if (mapGenerator.treePrefabs == null || mapGenerator.treePrefabs.Length == 0) return;
        
        float[,] heightMap = mapData.heightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Use similar settings to MapGenerator
        float adjustedDensity = mapGenerator.treeDensity;
        int step = Mathf.Max(1, Mathf.FloorToInt(1f / adjustedDensity / 10f));
        
        int treeCount = 0;
        List<Vector3> treePositions = new List<Vector3>();
        
        // Use deterministic seed for consistent tree placement
        System.Random prng = new System.Random(
            (int)(position.x * 1000 + position.y * 10000 + mapGenerator.noiseData.seed)
        );
        
        float scale = mapGenerator.terrainData.uniformScale;
        
        for (int y = 1; y < height - 1; y += step)
        {
            for (int x = 1; x < width - 1; x += step)
            {
                if (treeCount >= mapGenerator.maxTreesPerChunk) break;
                
                if (prng.NextDouble() > adjustedDensity) continue;
                
                float heightValue = heightMap[x, y];
                
                if (heightValue < mapGenerator.minHeightThreshold || heightValue > mapGenerator.maxHeightThreshold) continue;
                
                // Calculate position from heightmap 
                float worldX = (x - width/2f);
                float worldZ = (y - height/2f);
                float worldY = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightValue) * mapGenerator.terrainData.meshHeightMultiplier;
                
                // Check slope
                Vector3 normal = CalculateNormal(x, y, heightMap, mapGenerator.terrainData);
                float slope = Vector3.Angle(normal, Vector3.up);
                
                if (slope > mapGenerator.maxSlopeAngle) continue;
                
                // Calculate position
                Vector3 calculatedPosition = new Vector3(
                    (worldX + position.x) * scale, 
                    worldY * scale, 
                    (worldZ + position.y) * scale
                );
                
                // Final position and rotation 
                Vector3 finalPosition = calculatedPosition;
                
                // Small offset to prevent z-fighting
                finalPosition += Vector3.up * 0.05f;
                
                // Check minimum distance to other trees
                bool tooClose = false;
                foreach (Vector3 existingTree in treePositions)
                {
                    Vector2 pos1 = new Vector2(finalPosition.x, finalPosition.z);
                    Vector2 pos2 = new Vector2(existingTree.x, existingTree.z);
                    if (Vector2.Distance(pos1, pos2) < 2.0f * scale)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (tooClose) continue;
                
                treePositions.Add(finalPosition);
                
                // Create a blended normal
                Vector3 blendedNormal = Vector3.Lerp(Vector3.up, normal, (float)prng.NextDouble() * mapGenerator.treeNormalAlignment);
                float rotation = (float)(prng.NextDouble() * 360f);
                Quaternion normalAlignment = Quaternion.FromToRotation(Vector3.up, blendedNormal);
                Quaternion finalRotation = normalAlignment * Quaternion.Euler(0, rotation, 0);
                
                // Scale and create the tree
                float treeScale = (float)(mapGenerator.minTreeScale + prng.NextDouble() * (mapGenerator.maxTreeScale - mapGenerator.minTreeScale));
                Vector3 treeSizeVector = new Vector3(treeScale, treeScale, treeScale);
                
                // Select a random tree prefab
                int prefabIndex = prng.Next(0, mapGenerator.treePrefabs.Length);
                GameObject selectedTreePrefab = mapGenerator.treePrefabs[prefabIndex];
                
                if (selectedTreePrefab != null)
                {
                    // Instantiate the tree
                    GameObject tree = PrefabUtility.InstantiatePrefab(selectedTreePrefab, parent) as GameObject;
                    if (tree != null)
                    {
                        tree.transform.position = finalPosition;
                        tree.transform.rotation = finalRotation;
                        tree.transform.localScale = treeSizeVector;
                        treeCount++;
                    }
                }
            }
        }
        
        Debug.Log($"Added {treeCount} trees to the terrain chunk");
    }
    
    private void GenerateVegetationOnChunk(MapData mapData, Vector2 position, int lod, Transform parent)
    {
        if (mapGenerator.vegetationManager == null) return;
        
        float[,] heightMap = mapData.heightMap;
        int mapSize = heightMap.GetLength(0);
        float uniformScale = mapGenerator.terrainData.uniformScale;
        
        foreach (var vegType in mapGenerator.vegetationManager.vegetationTypes)
        {
            if (vegType.mesh == null || vegType.material == null) continue;
            
            // Create a container for this vegetation type
            GameObject typeContainer = new GameObject(vegType.name);
            typeContainer.transform.SetParent(parent);
            
            // Create a material instance to avoid modifying the original
            Material instanceMaterial = new Material(vegType.material);
            
            // Use density to determine step size
            float density = vegType.density;
            int step = Mathf.Max(1, Mathf.FloorToInt(5f / density));
            
            // Seed for consistent vegetation
            System.Random prng = new System.Random(
                (int)(position.x * 2000 + position.y * 20000 + mapGenerator.noiseData.seed)
            );
            
            int instanceCount = 0;
            int maxInstances = vegType.maxInstancesPerChunk;
            
            for (int y = 1; y < mapSize - 1; y += step)
            {
                for (int x = 1; x < mapSize - 1; x += step)
                {
                    if (instanceCount >= maxInstances) break;
                    
                    // Use probability to control density
                    if (prng.NextDouble() > density) continue;
                    
                    float heightValue = heightMap[x, y];
                    
                    // Skip if outside height thresholds
                    if (heightValue < vegType.minHeightThreshold || heightValue > vegType.maxHeightThreshold) continue;
                    
                    // Calculate position
                    float worldX = (x - mapSize/2f);
                    float worldZ = (y - mapSize/2f);
                    float worldY = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightValue) * mapGenerator.terrainData.meshHeightMultiplier;
                    
                    // Check slope
                    Vector3 normal = CalculateNormal(x, y, heightMap, mapGenerator.terrainData);
                    float slope = Vector3.Angle(normal, Vector3.up);
                    if (slope > vegType.maxSlopeAngle) continue;
                    
                    // Random offset
                    float offsetX = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset;
                    float offsetZ = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset;
                    
                    // Create instance
                    GameObject instance = new GameObject($"{vegType.name}_{instanceCount}");
                    instance.transform.SetParent(typeContainer.transform);
                    
                    Vector3 finalPosition = new Vector3(
                        (worldX + offsetX + position.x) * uniformScale, 
                        worldY * uniformScale, 
                        (worldZ + offsetZ + position.y) * uniformScale
                    );
                    
                    instance.transform.position = finalPosition;
                    
                    // Random rotation
                    float rotY = (float)prng.NextDouble() * 360f;
                    instance.transform.rotation = Quaternion.Euler(0, rotY, 0) * Quaternion.FromToRotation(Vector3.up, normal);
                    
                    // Random scale
                    float scaleValue = vegType.minScale + (float)prng.NextDouble() * (vegType.maxScale - vegType.minScale);
                    instance.transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
                    
                    // Add mesh renderer and filter
                    MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = instance.AddComponent<MeshRenderer>();
                    
                    meshFilter.sharedMesh = vegType.mesh;
                    meshRenderer.sharedMaterial = instanceMaterial;
                    
                    instanceCount++;
                }
                
                if (instanceCount >= maxInstances) break;
            }
            
            Debug.Log($"Added {instanceCount} instances of {vegType.name} vegetation");
        }
    }
    
    private Vector3 CalculateNormal(int x, int y, float[,] heightMap, TerrainData terrainData)
    {
        float heightL = terrainData.meshHeightCurve.Evaluate(heightMap[x-1, y]) * terrainData.meshHeightMultiplier;
        float heightR = terrainData.meshHeightCurve.Evaluate(heightMap[x+1, y]) * terrainData.meshHeightMultiplier;
        float heightD = terrainData.meshHeightCurve.Evaluate(heightMap[x, y-1]) * terrainData.meshHeightMultiplier;
        float heightU = terrainData.meshHeightCurve.Evaluate(heightMap[x, y+1]) * terrainData.meshHeightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    private void VerifyTerrainChunk(GameObject terrainChunkObject)
    {
        // Make sure the mesh is properly assigned
        MeshFilter meshFilter = terrainChunkObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("Terrain chunk prefab missing mesh!");
            
            // Try to diagnose the issue
            if (meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing!");
            }
            else if (meshFilter.sharedMesh == null)
            {
                Debug.LogError("Mesh is null! This could be due to mesh creation failure.");
                
                // Try to manually generate a simple mesh as a fallback
                Mesh fallbackMesh = new Mesh();
                fallbackMesh.vertices = new Vector3[] 
                {
                    new Vector3(-50, 0, -50),
                    new Vector3(50, 0, -50),
                    new Vector3(50, 0, 50),
                    new Vector3(-50, 0, 50)
                };
                fallbackMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                fallbackMesh.uv = new Vector2[] 
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1)
                };
                fallbackMesh.RecalculateNormals();
                fallbackMesh.RecalculateBounds();
                
                meshFilter.sharedMesh = fallbackMesh;
                Debug.Log("Applied fallback mesh as emergency measure");
            }
        }
    }
    
    private void SaveAsPrefab(GameObject terrainChunkObject)
    {
        // Verify the terrain chunk first
        VerifyTerrainChunk(terrainChunkObject);
        
        // Make sure the directory exists
        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            string[] folders = prefabPath.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string folderPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = folderPath;
            }
        }
        
        // Create a unique name for the prefab
        string prefabFullPath = $"{prefabPath}/{prefabName}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.prefab";
        
        // Create the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(terrainChunkObject, prefabFullPath);
        if (prefab != null)
        {
            Debug.Log($"Terrain chunk prefab created at: {prefabFullPath}");
            
            // Select the prefab in the Project window
            Selection.activeObject = prefab;
        }
        else
        {
            Debug.LogError("Failed to create prefab!");
        }
        
        // Destroy the temporary object
        DestroyImmediate(terrainChunkObject);
    }
}

// Simple component to identify a terrain chunk
public class TerrainChunkIdentifier : MonoBehaviour
{
    // This is just a marker component
    // You can add properties here if needed
}
#endif