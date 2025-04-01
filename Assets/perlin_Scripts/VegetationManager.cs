using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class VegetationType {
    public string name;
    public Mesh mesh;
    public Material material;
    [Range(0.01f, 10f)]
    public float density = 1f;
    [Range(0f, 1f)]
    public float minHeightThreshold = 0.2f;
    [Range(0f, 1f)]
    public float maxHeightThreshold = 0.8f;
    [Range(0f, 85f)]
    public float maxSlopeAngle = 35f;
    [Range(0.01f, 3f)]
    public float minScale = 0.7f;
    [Range(0.01f, 3f)]
    public float maxScale = 1.3f;
    [Range(0f, 1f)]
    public float randomOffset = 0.1f;
    public int maxInstancesPerChunk = 5000;
    
    // New fields for color variation
    public bool useColorVariation = true;
    public Color colorA = Color.white;
    public Color colorB = Color.white;
    [Range(0f, 1f)]
    public float colorVariationStrength = 0.5f;
    
    // For mapping vegetation to biomes
    [Range(0f, 1f)]
    public float moistureThreshold = 0f;
    public bool useHeightBasedVariation = false;
    [Range(0f, 1f)]
    public float heightVariationStrength = 0.3f;
}

// Helper class to track vegetation batches - moved to public
public class VegetationBatch {
    public Mesh mesh;
    public Material material;
    public Matrix4x4[] matrices;
    public MaterialPropertyBlock propertyBlock;
}

public class VegetationManager : MonoBehaviour {
    [Header("Vegetation Settings")]
    public VegetationType[] vegetationTypes;
    
    [Header("Performance Settings")]
    [Tooltip("Automatically adjust vegetation density based on distance")]
    public bool useLodDensityReduction = true;
    [Range(0.1f, 1f)]
    public float farDistanceDensityMultiplier = 0.3f;
    [Tooltip("Maximum number of instances to render per draw call")]
    public int maxInstancesPerBatch = 1023;
    [Tooltip("Maximum distance to render vegetation")]
    public float maxViewDistance = 500f;
    
    [Header("Wind Animation")]
    public bool enableWindAnimation = true;
    [Range(0f, 2f)]
    public float windStrength = 0.5f;
    [Range(0.1f, 5f)]
    public float windSpeed = 1f;
    [Range(0.1f, 10f)]
    public float windScale = 2f;
    
    public bool showDebugVisualization = true;
    
    // Dictionary to track vegetation batches for each chunk position
    private Dictionary<Vector2, List<VegetationBatch>> chunkVegetation = new Dictionary<Vector2, List<VegetationBatch>>();
    private Transform vegetationContainer;
    
    // Shared wind properties
    private Vector4 windParams;
    private float windTime;
    
    // Material property IDs for better performance
    private int colorVariationId;
    private int windParamsId;
    private int windTimeId;
    
    private void Awake() {
        foreach (var vegType in vegetationTypes) {
            if (vegType.material != null) {
                // Verify shader supports instancing
                if (!vegType.material.enableInstancing) {
                    vegType.material.enableInstancing = true;
                    Debug.Log($"Enabled instancing on material {vegType.material.name}");
                }
        
                // Check if shader has instancing variant
                if (!vegType.material.shader.name.Contains("Custom/")) {
                    Debug.LogWarning($"Material {vegType.material.name} uses shader {vegType.material.shader.name} which may not support instancing");
                }
            }
        }
        
        GameObject container = new GameObject("VegetationContainer");
        vegetationContainer = container.transform;
        vegetationContainer.SetParent(transform);
        
        // Cache property IDs
        colorVariationId = Shader.PropertyToID("_ColorVariation");
        windParamsId = Shader.PropertyToID("_WindParams");
        windTimeId = Shader.PropertyToID("_WindTime");
        
        SetupDefaultMeshes();
    }
    
    private void Update() {
        if (enableWindAnimation) {
            // Update wind time
            windTime += Time.deltaTime * windSpeed;
            windParams = new Vector4(windStrength, windScale, 0, 0);
            
            // Update wind parameters for all vegetation batches
            foreach (var chunkBatches in chunkVegetation.Values) {
                foreach (var batch in chunkBatches) {
                    if (batch.propertyBlock != null) {
                        batch.propertyBlock.SetFloat(windTimeId, windTime);
                        batch.propertyBlock.SetVector(windParamsId, windParams);
                    }
                }
            }
        }
    }
    
    private void SetupDefaultMeshes() {
        // Create default quad mesh if not assigned
        for (int i = 0; i < vegetationTypes.Length; i++) {
            if (vegetationTypes[i].mesh == null) {
                // Use a cross-quad mesh for grass/flowers by default
                vegetationTypes[i].mesh = CreateVegetationMesh(vegetationTypes[i].name == "Grass");
                
                // If we have a material but it doesn't support instancing, fix it
                if (vegetationTypes[i].material != null && !vegetationTypes[i].material.enableInstancing) {
                    Debug.LogWarning($"Material for {vegetationTypes[i].name} doesn't have instancing enabled. Enabling it now.");
                    vegetationTypes[i].material.enableInstancing = true;
                }
            }
        }
    }
    
    private Mesh CreateVegetationMesh(bool isGrass) {
        Mesh mesh = new Mesh();
        
        if (isGrass) {
            // Create a basic cross-quad for grass
            Vector3[] verticesArray = new Vector3[8];
            Vector2[] uvArray = new Vector2[8];
            int[] trianglesArray = new int[12];
            
            // First quad (front-facing)
            verticesArray[0] = new Vector3(-0.5f, 0, 0);
            verticesArray[1] = new Vector3(0.5f, 0, 0);
            verticesArray[2] = new Vector3(-0.5f, 1, 0);
            verticesArray[3] = new Vector3(0.5f, 1, 0);
            
            // Second quad (side-facing)
            verticesArray[4] = new Vector3(0, 0, -0.5f);
            verticesArray[5] = new Vector3(0, 0, 0.5f);
            verticesArray[6] = new Vector3(0, 1, -0.5f);
            verticesArray[7] = new Vector3(0, 1, 0.5f);
            
            // UVs for the first quad
            uvArray[0] = new Vector2(0, 0);
            uvArray[1] = new Vector2(1, 0);
            uvArray[2] = new Vector2(0, 1);
            uvArray[3] = new Vector2(1, 1);
            
            // UVs for the second quad
            uvArray[4] = new Vector2(0, 0);
            uvArray[5] = new Vector2(1, 0);
            uvArray[6] = new Vector2(0, 1);
            uvArray[7] = new Vector2(1, 1);
            
            // Triangles
            // First quad
            trianglesArray[0] = 0;
            trianglesArray[1] = 2;
            trianglesArray[2] = 1;
            trianglesArray[3] = 1;
            trianglesArray[4] = 2;
            trianglesArray[5] = 3;
            
            // Second quad
            trianglesArray[6] = 4;
            trianglesArray[7] = 6;
            trianglesArray[8] = 5;
            trianglesArray[9] = 5;
            trianglesArray[10] = 6;
            trianglesArray[11] = 7;
            
            mesh.vertices = verticesArray;
            mesh.uv = uvArray;
            mesh.triangles = trianglesArray;
        } else {
            // Create a basic quad for flowers/small vegetation that always faces camera
            Vector3[] verticesArray = new Vector3[4];
            Vector2[] uvArray = new Vector2[4];
            int[] trianglesArray = new int[6];
            
            verticesArray[0] = new Vector3(-0.5f, 0, 0);
            verticesArray[1] = new Vector3(0.5f, 0, 0);
            verticesArray[2] = new Vector3(-0.5f, 1, 0);
            verticesArray[3] = new Vector3(0.5f, 1, 0);
            
            uvArray[0] = new Vector2(0, 0);
            uvArray[1] = new Vector2(1, 0);
            uvArray[2] = new Vector2(0, 1);
            uvArray[3] = new Vector2(1, 1);
            
            trianglesArray[0] = 0;
            trianglesArray[1] = 2;
            trianglesArray[2] = 1;
            trianglesArray[3] = 1;
            trianglesArray[4] = 2;
            trianglesArray[5] = 3;
            
            mesh.vertices = verticesArray;
            mesh.uv = uvArray;
            mesh.triangles = trianglesArray;
        }
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    public void GenerateVegetation(MapData mapData, Vector2 chunkPosition, int lod, AnimationCurve heightCurve, float heightMultiplier) {
    if (vegetationTypes == null || vegetationTypes.Length == 0)
        return;
        
    // First clear any existing vegetation for this chunk
    ClearVegetation(chunkPosition);
    
    // Get scale once to avoid repeated lookups
    MapGenerator mapGen = FindObjectOfType<MapGenerator>();
    float uniformScale = mapGen.terrainData.uniformScale;
    
    // Create a new container for this chunk's vegetation
    GameObject chunkVegetationObj = new GameObject($"Vegetation_{chunkPosition.x}_{chunkPosition.y}");
    chunkVegetationObj.transform.SetParent(vegetationContainer);
    
    // Position the container at the chunk's world position
    chunkVegetationObj.transform.position = new Vector3(chunkPosition.x, 0, chunkPosition.y) * uniformScale;
    
    float[,] heightMap = mapData.heightMap;
    
    // Create a list to hold all the batches for this chunk
    List<VegetationBatch> batches = new List<VegetationBatch>();
    
    int mapSize = heightMap.GetLength(0);
    
    // Calculate each vegetation type
    for (int typeIndex = 0; typeIndex < vegetationTypes.Length; typeIndex++) {
        var vegType = vegetationTypes[typeIndex];
        
        if (vegType.mesh == null || vegType.material == null) {
            Debug.LogWarning($"Vegetation type {vegType.name} is missing mesh or material. Skipping.");
            continue;
        }
        
        // Special treatment for grass - much denser coverage
        bool isGrass = vegType.name.ToLower().Contains("grass");
        
        // Apply LOD-based density reduction
        float lodDensityFactor = useLodDensityReduction ? Mathf.Lerp(1f, farDistanceDensityMultiplier, lod / 4f) : 1f;
        
        // For grass, we use a much higher base density
        float adjustedDensity = isGrass ? 
            Mathf.Max(3.0f, vegType.density) * lodDensityFactor : 
            vegType.density * lodDensityFactor;
        
        // Calculate grid size for more even distribution
        int maxPossibleInstances = (mapSize - 2) * (mapSize - 2);
        int targetInstanceCount = Mathf.Min(vegType.maxInstancesPerChunk, 
                                          Mathf.FloorToInt(maxPossibleInstances * adjustedDensity));
        
        // For grass, use very small step size for dense coverage
        int step = isGrass ? 1 : Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(maxPossibleInstances / (float)targetInstanceCount)));
        
        // Create our instance matrices and colors arrays
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        List<Vector4> colorVariations = new List<Vector4>();
        
        // Set up deterministic random generator for consistent vegetation placement
        System.Random prng = new System.Random((int)(chunkPosition.x * 1000 + chunkPosition.y * 10000 + typeIndex * 100 + 12345));
        
        // Modified strategy for grass - ensure dense coverage across the entire surface
        if (isGrass) {
            // Use a tighter grid with small random offsets for natural look
            for (int y = 1; y < mapSize - 1; y += step) {
                for (int x = 1; x < mapSize - 1; x += step) {
                    // Skip if our instance count exceeds the limit
                    if (matrices.Count >= vegType.maxInstancesPerChunk) break;
                    
                    // Get height at this point
                    float heightValue = heightMap[x, y];
                    
                    // Skip if outside height thresholds
                    if (heightValue < vegType.minHeightThreshold || heightValue > vegType.maxHeightThreshold) continue;
                    
                    // Check slope constraint
                    Vector3 normal = CalculateNormal(x, y, heightMap, heightCurve, heightMultiplier);
                    float slope = Vector3.Angle(normal, Vector3.up);
                    if (slope > vegType.maxSlopeAngle) continue;
                    
                    // For grass, we want multiple instances in nearby positions to create thickness
                    int grassDensity = 6; // Number of grass blades per grid cell
                    for (int i = 0; i < grassDensity; i++) {
                        // Calculate multiple positions within the cell
                        float cellOffsetX = (float)prng.NextDouble() * step;
                        float cellOffsetY = (float)prng.NextDouble() * step;
                        
                        int finalX = Mathf.Clamp(x + Mathf.FloorToInt(cellOffsetX), 1, mapSize - 2);
                        int finalY = Mathf.Clamp(y + Mathf.FloorToInt(cellOffsetY), 1, mapSize - 2);
                        
                        // Calculate position in heightmap space
                        float worldX = (finalX - mapSize/2f);
                        float worldZ = (finalY - mapSize/2f);
                        
                        // Calculate height using same method as terrain
                        float worldY = heightCurve.Evaluate(heightMap[finalX, finalY]) * heightMultiplier;
                        
                        // Small random offset for natural appearance within the cell
                        float offsetX = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset * 0.5f;
                        float offsetZ = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset * 0.5f;
                        
                        // Final position with offset
                        Vector3 position = new Vector3(
                            worldX + offsetX, 
                            worldY, 
                            worldZ + offsetZ);
                        
                        // Random rotation around Y-axis for grass
                        Quaternion rotation = Quaternion.Euler(
                            0, 
                            (float)(prng.NextDouble() * 360f), 
                            0);
                        
                        // Align with terrain normal
                        rotation = Quaternion.FromToRotation(Vector3.up, normal) * rotation;
                        
                        // Random scale - Using the same scale as the terrain but with more variation for grass
                        float scaleVariation = (float)prng.NextDouble() * 0.3f + 0.7f; // 0.7 to 1.0 range
                        float scaleValue = vegType.minScale * scaleVariation;
                        scaleValue *= uniformScale * 0.15f; // Scale proportionally to terrain
                        
                        Vector3 scale = new Vector3(scaleValue, scaleValue, scaleValue);
                        
                        // Create transformation matrix
                        Matrix4x4 matrix = Matrix4x4.TRS(position * uniformScale, rotation, scale);
                        matrices.Add(matrix);
                        
                        // Color variation - more subtle for grass
                        if (vegType.useColorVariation) {
                            float colorLerp = (float)prng.NextDouble();
                            Vector4 colorVariation = Vector4.Lerp(
                                (Vector4)vegType.colorA, 
                                (Vector4)vegType.colorB, 
                                colorLerp) * vegType.colorVariationStrength * 0.7f; // Subtler variation
                            colorVariations.Add(colorVariation);
                        } else {
                            colorVariations.Add(Vector4.zero);
                        }
                        
                        // Safety check to avoid exceeding instance limit
                        if (matrices.Count >= vegType.maxInstancesPerChunk) break;
                    }
                }
                
                // Break early if we've hit our instance limit
                if (matrices.Count >= vegType.maxInstancesPerChunk) break;
            }
        } 
        // For non-grass vegetation, use the original method with some improvements
        else {
            // Track positions to avoid duplicate placements
            Dictionary<Vector2Int, bool> occupiedPositions = new Dictionary<Vector2Int, bool>();
            
            // Generate vegetation instances with more even distribution
            for (int y = 1; y < mapSize - 1; y += step) {
                for (int x = 1; x < mapSize - 1; x += step) {
                    // Skip if our instance count exceeds the limit
                    if (matrices.Count >= vegType.maxInstancesPerChunk) break;
                    
                    // Add some jitter within each grid cell for natural appearance
                    int jitterX = Mathf.FloorToInt((float)prng.NextDouble() * step * 0.8f);
                    int jitterY = Mathf.FloorToInt((float)prng.NextDouble() * step * 0.8f);
                    
                    int finalX = Mathf.Clamp(x + jitterX, 1, mapSize - 2);
                    int finalY = Mathf.Clamp(y + jitterY, 1, mapSize - 2);
                    
                    // Ensure we don't place multiple instances at the same position
                    Vector2Int gridPos = new Vector2Int(finalX, finalY);
                    if (occupiedPositions.ContainsKey(gridPos)) continue;
                    
                    // Still use some probability for variation in density
                    if (prng.NextDouble() > adjustedDensity * 1.5f) continue;
                    
                    float heightValue = heightMap[finalX, finalY];
                    
                    // Skip if outside height thresholds
                    if (heightValue < vegType.minHeightThreshold || heightValue > vegType.maxHeightThreshold) continue;
                    
                    // Calculate position in heightmap space
                    float worldX = (finalX - mapSize/2f);
                    float worldZ = (finalY - mapSize/2f);
                    
                    // Calculate height using same method as terrain
                    float worldY = heightCurve.Evaluate(heightValue) * heightMultiplier;
                    
                    // Check slope
                    Vector3 normal = CalculateNormal(finalX, finalY, heightMap, heightCurve, heightMultiplier);
                    float slope = Vector3.Angle(normal, Vector3.up);
                    
                    if (slope > vegType.maxSlopeAngle) continue;
                    
                    // Small random offset for natural appearance
                    float offsetX = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset;
                    float offsetZ = (float)(prng.NextDouble() * 2 - 1) * vegType.randomOffset;
                    
                    // Final position
                    Vector3 position = new Vector3(
                        worldX + offsetX, 
                        worldY, 
                        worldZ + offsetZ);
                    
                    // Random rotation around Y-axis
                    Quaternion rotation = Quaternion.Euler(
                        0, 
                        (float)(prng.NextDouble() * 360f), 
                        0);
                    
                    // Align with terrain normal
                    rotation = Quaternion.FromToRotation(Vector3.up, normal) * rotation;
                    
                    // Random scale - Using the same scale as the terrain
                    float scaleValue = vegType.minScale + (float)prng.NextDouble() * (vegType.maxScale - vegType.minScale);
                    scaleValue *= uniformScale * 0.2f; // Scale proportionally to terrain
                    
                    Vector3 scale = new Vector3(scaleValue, scaleValue, scaleValue);
                    
                    // Create transformation matrix
                    Matrix4x4 matrix = Matrix4x4.TRS(position * uniformScale, rotation, scale);
                    matrices.Add(matrix);
                    
                    // Mark position as occupied
                    occupiedPositions[gridPos] = true;
                    
                    // Color variation
                    if (vegType.useColorVariation) {
                        float colorLerp = (float)prng.NextDouble();
                        Vector4 colorVariation = Vector4.Lerp(
                            (Vector4)vegType.colorA, 
                            (Vector4)vegType.colorB, 
                            colorLerp) * vegType.colorVariationStrength;
                        colorVariations.Add(colorVariation);
                    } else {
                        colorVariations.Add(Vector4.zero);
                    }
                }
            }
        }
        
        // Create batches
        for (int i = 0; i < matrices.Count; i += maxInstancesPerBatch) {
            int count = Mathf.Min(maxInstancesPerBatch, matrices.Count - i);
            
            Matrix4x4[] batchMatrices = new Matrix4x4[count];
            Vector4[] batchColors = new Vector4[count];
            
            for (int j = 0; j < count; j++) {
                batchMatrices[j] = matrices[i + j];
                batchColors[j] = colorVariations[i + j];
            }
            
            // Create batch
            VegetationBatch batch = new VegetationBatch {
                mesh = vegType.mesh,
                material = vegType.material,
                matrices = batchMatrices,
                propertyBlock = new MaterialPropertyBlock()
            };
            
            batch.propertyBlock.SetVectorArray(colorVariationId, batchColors);
            
            if (enableWindAnimation) {
                batch.propertyBlock.SetVector(windParamsId, windParams);
                batch.propertyBlock.SetFloat(windTimeId, windTime);
            }
            
            batches.Add(batch);
            
            GameObject batchObj = new GameObject($"{vegType.name}_Batch_{i/maxInstancesPerBatch}");
            batchObj.transform.SetParent(chunkVegetationObj.transform);
            batchObj.transform.localPosition = Vector3.zero;
            
            VegetationRenderer renderer = batchObj.AddComponent<VegetationRenderer>();
            renderer.Initialize(batch);
        }
    }
    
    // Store the batches for this chunk
    chunkVegetation[chunkPosition] = batches;
}
    public void ClearVegetation(Vector2 chunkPosition) {
        if (chunkVegetation.ContainsKey(chunkPosition)) {
            // Find and destroy the container GameObject
            Transform container = vegetationContainer.Find($"Vegetation_{chunkPosition.x}_{chunkPosition.y}");
            if (container != null) {
                Destroy(container.gameObject);
            }
            
            // Remove from our tracking dictionary
            chunkVegetation.Remove(chunkPosition);
        }
    }
    
    public void ClearAllVegetation() {
        // Clear all vegetation containers
        foreach (Transform child in vegetationContainer) {
            Destroy(child.gameObject);
        }
        
        // Clear our tracking dictionary
        chunkVegetation.Clear();
    }
    
    private Vector3 CalculateNormal(int x, int y, float[,] heightMap, AnimationCurve heightCurve, float heightMultiplier) {
        float heightL = heightCurve.Evaluate(heightMap[x-1, y]) * heightMultiplier;
        float heightR = heightCurve.Evaluate(heightMap[x+1, y]) * heightMultiplier;
        float heightD = heightCurve.Evaluate(heightMap[x, y-1]) * heightMultiplier;
        float heightU = heightCurve.Evaluate(heightMap[x, y+1]) * heightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    void OnDrawGizmos() {
        if (!Application.isPlaying || !showDebugVisualization) return;

        Gizmos.color = Color.green;
        foreach (var entry in chunkVegetation) {
            Vector2 pos = entry.Key;
            Vector3 chunkCenter = new Vector3(pos.x, 0, pos.y);
            Gizmos.DrawWireCube(chunkCenter, new Vector3(10, 1, 10));
        
            // Draw debug spheres at each vegetation batch container position
            Transform container = vegetationContainer.Find($"Vegetation_{pos.x}_{pos.y}");
            if (container != null) {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(container.position, 1f);
            
                // Draw a few vegetation positions from each batch
                Gizmos.color = Color.yellow;
                foreach (Transform batchTransform in container) {
                    VegetationRenderer renderer = batchTransform.GetComponent<VegetationRenderer>();
                    if (renderer != null && renderer.batch != null && renderer.batch.matrices != null) {
                        // Draw a few sample positions
                        for (int i = 0; i < Mathf.Min(5, renderer.batch.matrices.Length); i++) {
                            Vector3 localPos = renderer.batch.matrices[i].GetColumn(3);
                            Vector3 worldPos = container.TransformPoint(localPos);
                            Gizmos.DrawSphere(worldPos, 0.5f);
                        }
                    }
                }
            }
        }
    }
    
    
}

// Component to render vegetation using Graphics.DrawMeshInstanced
public class VegetationRenderer : MonoBehaviour {
    public VegetationBatch batch;
    private bool isInitialized = false;
    private bool hasLoggedError = false;
    
    
    public void Initialize(VegetationBatch batch) {
        this.batch = batch;
        isInitialized = true;
    }
    
    void LateUpdate() {
        if (!isInitialized || batch == null) {
            Debug.LogWarning("Vegetation batch not initialized");
            return;
        }

        if (batch.mesh == null || batch.material == null || batch.matrices == null || batch.matrices.Length == 0) {
            if (!hasLoggedError) {
                Debug.LogError($"Vegetation batch is invalid: Mesh={batch.mesh != null}, Material={batch.material != null}, Matrices={(batch.matrices != null ? batch.matrices.Length : 0)}");
                hasLoggedError = true;
            }
            return;
        }

        // Get the parent transform (chunk container)
        Transform parent = transform.parent;
        if (parent == null) return;

        // Use parent's transformation to calculate world matrices
        Matrix4x4[] worldMatrices = new Matrix4x4[batch.matrices.Length];
    
        for (int i = 0; i < batch.matrices.Length; i++) {
            // Get local position, rotation, and scale from the matrix
            Vector3 position = batch.matrices[i].GetColumn(3);
            Quaternion rotation = Quaternion.LookRotation(
                batch.matrices[i].GetColumn(2),
                batch.matrices[i].GetColumn(1)
            );
            Vector3 scale = new Vector3(
                batch.matrices[i].GetColumn(0).magnitude,
                batch.matrices[i].GetColumn(1).magnitude,
                batch.matrices[i].GetColumn(2).magnitude
            );
        
            // Transform to world space using the parent's transform
            Vector3 worldPos = parent.TransformPoint(position);
            Quaternion worldRot = parent.rotation * rotation;
        
            // Create world matrix
            worldMatrices[i] = Matrix4x4.TRS(worldPos, worldRot, scale);
        }
    
        // Draw the mesh instances
        Graphics.DrawMeshInstanced(
            batch.mesh,
            0,
            batch.material,
            worldMatrices,
            worldMatrices.Length,
            batch.propertyBlock);
    }
    
    
}


