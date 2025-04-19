using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages grass for each terrain chunk in the procedural terrain system
/// </summary>
public class ChunkGrassManager : MonoBehaviour 
{
    [Header("Grass Settings")]
    public bool enableGrass = true;
    
    [Tooltip("Material used for rendering grass. Must have the ModelGrass URP shader.")]
    public Material grassMaterial;
    
    [Tooltip("Mesh for the grass blades. Required.")]
    public Mesh grassMesh;
    
    [Tooltip("Simplified mesh for distant grass. Will use main mesh if not set.")]
    public Mesh grassLODMesh;
    
    [Tooltip("Density of grass instances per terrain unit")]
    public int grassDensity = 3;
    
    [Tooltip("Height displacement strength")]
    public float displacementStrength = 10f;
    
    [Tooltip("Distance at which grass changes to LOD model")]
    public float lodCutoff = 250.0f;

    [Tooltip("Maximum distance at which grass is rendered")]
    public float distanceCutoff = 500.0f;

    [Header("Grass Styling")]
    public Color albedo1 = new Color(0.26f, 0.35f, 0.1f);
    public Color albedo2 = new Color(0.4f, 0.5f, 0.2f);
    public Color tipColor = new Color(0.7f, 0.9f, 0.3f);
    public Color aoColor = new Color(0.3f, 0.3f, 0.3f);
    
    [Tooltip("Vertical scaling of grass blades")]
    public float scale = 0.3f;
    
    [Tooltip("How much the grass blades bend")]
    public float droop = 0.8f;

    [Header("Wind Settings")]
    [Tooltip("Speed of wind animation")]
    public float windSpeed = 1.0f;
    
    [ Tooltip("Frequency of wind waves")]
    public float frequency = 1.0f;
    
    [Tooltip("Strength of wind effect on grass")]
    public float windStrength = 1.0f;
    
    [Header("Fog Settings")]
    public Color fogColor = new Color(0.8f, 0.9f, 1.0f);
    
    [Tooltip("Density of distance fog")]
    public float fogDensity = 0.01f;
    
    [Tooltip("Distance at which fog starts")]
    public float fogOffset = 3f;
    
    [Header("Height and Slope Constraints")]
    [Tooltip("Minimum terrain height for grass placement (0-1)")]
    public float minHeightThreshold = 0.2f;
    
    [Tooltip("Maximum terrain height for grass placement (0-1)")]
    public float maxHeightThreshold = 0.8f;
    
    [Tooltip("Maximum slope angle for grass placement (degrees)")]
    public float maxSlopeAngle = 30f;
    
    // Support fields
    private Dictionary<Vector2, ModelGrass> activeGrassChunks = new Dictionary<Vector2, ModelGrass>();
    private MapGenerator mapGenerator;
    private ComputeShader windNoiseShader;
    private RenderTexture windTexture;
    
    // Wind texture settings
    private const int WIND_TEXTURE_SIZE = 1024;
    
    // Template grass mesh for fallback
    private Mesh defaultGrassMesh;
    
    void Awake() 
    {
        mapGenerator = GetComponent<MapGenerator>();
        if (mapGenerator == null) {
            Debug.LogError("ChunkGrassManager requires a MapGenerator component on the same GameObject");
            enabled = false;
            return;
        }
        
        // Create default grass mesh if none is assigned
        if (grassMesh == null) {
            defaultGrassMesh = CreateSimpleGrassMesh();
            grassMesh = defaultGrassMesh;
            Debug.LogWarning("No grass mesh assigned, using a simple default mesh");
        }
        
        // If no LOD mesh is assigned, use the main mesh
        if (grassLODMesh == null) {
            grassLODMesh = grassMesh;
        }
        
        // Create default material if none is assigned
        if (grassMaterial == null) {
            // Try to find a suitable grass material
            grassMaterial = Resources.Load<Material>("ModelGrass");
            
            if (grassMaterial == null) {
                // Create a basic material if none is found
                grassMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                //grassMaterial.color = Color.green;
                Debug.LogWarning("No grass material assigned or found, using a basic green material");
            }
        }
        
        // Initialize wind texture
        InitializeWindTexture();
    }
    
    private Mesh CreateSimpleGrassMesh()
    {
        Mesh mesh = new Mesh();
        
        // Simple grass blade as a quad
        Vector3[] vertices = new Vector3[4] {
            new Vector3(-0.05f, 0, 0),
            new Vector3(0.05f, 0, 0),
            new Vector3(-0.05f, 1, 0),
            new Vector3(0.05f, 1, 0)
        };
        
        Vector2[] uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6] {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void OnDestroy()
    {
        if (windTexture != null) {
            windTexture.Release();
            windTexture = null;
        }
        
        // Clean up all active grass instances
        foreach (var grass in activeGrassChunks.Values) {
            if (grass != null) {
                Destroy(grass.gameObject);
            }
        }
        activeGrassChunks.Clear();
        
        // Clean up default resources
        if (defaultGrassMesh != null) {
            Destroy(defaultGrassMesh);
        }
    }
    
    private void Update()
    {
        if (windNoiseShader != null && windTexture != null) {
            UpdateWindTexture();
        }
    }
    
    private void InitializeWindTexture()
    {
        try {
            // Create wind texture
            windTexture = new RenderTexture(WIND_TEXTURE_SIZE, WIND_TEXTURE_SIZE, 0, RenderTextureFormat.ARGBFloat);
            windTexture.enableRandomWrite = true;
            windTexture.Create();
            
            // Load wind shader
            windNoiseShader = Resources.Load<ComputeShader>("WindNoise");
            if (windNoiseShader == null) {
                Debug.LogWarning("Could not load WindNoise shader. Make sure it's in the Resources folder.");
                // Create a fallback static texture
                CreateFallbackWindTexture();
                return;
            }
            
            // Initial update
            UpdateWindTexture();
        }
        catch (Exception e) {
            Debug.LogError($"Error initializing wind texture: {e.Message}");
            CreateFallbackWindTexture();
        }
    }
    
    private void CreateFallbackWindTexture()
    {
        // Create a static texture for wind if compute shader fails
        Texture2D staticWindTex = new Texture2D(256, 256, TextureFormat.RGBAFloat, false);
        Color[] pixels = new Color[256 * 256];
        
        for (int y = 0; y < 256; y++) {
            for (int x = 0; x < 256; x++) {
                float u = (float)x / 256;
                float v = (float)y / 256;
                
                // Simple static wind pattern
                float windX = Mathf.Sin(u * 5) * Mathf.Cos(v * 3);
                float windY = Mathf.Sin(v * 7) * Mathf.Cos(u * 4);
                
                pixels[y * 256 + x] = new Color(windX * 0.5f, windY * 0.5f, 0, 1);
            }
        }
        
        staticWindTex.SetPixels(pixels);
        staticWindTex.Apply();
        
        // Copy to render texture
        if (windTexture == null || !windTexture.IsCreated()) {
            windTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGBFloat);
            windTexture.enableRandomWrite = true;
            windTexture.Create();
        }
        
        RenderTexture prevRT = RenderTexture.active;
        RenderTexture.active = windTexture;
        Graphics.Blit(staticWindTex, windTexture);
        RenderTexture.active = prevRT;
        
        // Clean up temporary texture
        Destroy(staticWindTex);
    }
    
    private void UpdateWindTexture()
    {
        if (windNoiseShader == null || windTexture == null || !windTexture.IsCreated()) return;
        
        try {
            int kernelIndex = windNoiseShader.FindKernel("WindNoise");
            
            windNoiseShader.SetTexture(kernelIndex, "_WindMap", windTexture);
            windNoiseShader.SetFloat("_Time", Time.time * windSpeed);
            windNoiseShader.SetFloat("_Frequency", frequency);
            windNoiseShader.SetFloat("_Amplitude", windStrength);
            
            int threadGroupsX = Mathf.CeilToInt(windTexture.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(windTexture.height / 8.0f);
            windNoiseShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
        }
        catch (Exception e) {
            Debug.LogWarning($"Error updating wind texture: {e.Message}");
        }
    }
    
    /// <summary>
    /// Sets up grass for a terrain chunk
    /// </summary>
    public void SetupGrassForChunk(Vector2 position, MapData mapData, GameObject chunkObject) 
    {
        if (!enableGrass) return;
        
        // Validate required resources
        if (grassMesh == null) {
            Debug.LogError("Cannot create grass: grassMesh is null");
            return;
        }
        
        // Skip if we already have grass for this chunk
        if (activeGrassChunks.ContainsKey(position)) return;
        
        try {
            // Create a new GameObject for grass
            GameObject grassObject = new GameObject($"GrassChunk_{position.x}_{position.y}");
            grassObject.transform.SetParent(chunkObject.transform);
            grassObject.transform.localPosition = Vector3.zero;
            
            // Add ModelGrass component
            ModelGrass grassComponent = grassObject.AddComponent<ModelGrass>();
            
            // Configure grass settings based on chunk data
            ConfigureGrassForChunk(grassComponent, position, mapData);
            
            // Store the reference
            activeGrassChunks[position] = grassComponent;
        }
        catch (Exception e) {
            Debug.LogError($"Error setting up grass for chunk at {position}: {e.Message}");
        }
    }
    
    private void ConfigureGrassForChunk(ModelGrass grass, Vector2 position, MapData mapData) 
    {
        // Set the standard parameters
        grass.grassMaterial = new Material(grassMaterial);
        grass.grassMesh = grassMesh;
        grass.grassLODMesh = grassLODMesh;
        
        // Graphics settings
        grass.displacementStrength = displacementStrength;
        grass.lodCutoff = lodCutoff;
        grass.distanceCutoff = distanceCutoff;
        grass.windSpeed = windSpeed;
        grass.frequency = frequency;
        grass.windStrength = windStrength;
        
        // Configure material properties
        if (grass.grassMaterial.HasProperty("_Albedo1")) grass.grassMaterial.SetColor("_Albedo1", albedo1);
        if (grass.grassMaterial.HasProperty("_Albedo2")) grass.grassMaterial.SetColor("_Albedo2", albedo2);
        if (grass.grassMaterial.HasProperty("_TipColor")) grass.grassMaterial.SetColor("_TipColor", tipColor);
        if (grass.grassMaterial.HasProperty("_AOColor")) grass.grassMaterial.SetColor("_AOColor", aoColor);
        if (grass.grassMaterial.HasProperty("_FogColor")) grass.grassMaterial.SetColor("_FogColor", fogColor);
        if (grass.grassMaterial.HasProperty("_FogDensity")) grass.grassMaterial.SetFloat("_FogDensity", fogDensity);
        if (grass.grassMaterial.HasProperty("_FogOffset")) grass.grassMaterial.SetFloat("_FogOffset", fogOffset);
        if (grass.grassMaterial.HasProperty("_Scale")) grass.grassMaterial.SetFloat("_Scale", scale);
        if (grass.grassMaterial.HasProperty("_Droop")) grass.grassMaterial.SetFloat("_Droop", droop);
        
        // Set the shared wind texture
        if (windTexture != null && grass.grassMaterial.HasProperty("_WindTex")) {
            grass.grassMaterial.SetTexture("_WindTex", windTexture);
        }
        
        // Calculate the field size based on map chunk size
        grass.fieldSize = mapGenerator.mapChunkSize;
        grass.chunkDensity = grassDensity;
        grass.numChunks = 1; // Since we're using one ModelGrass per actual terrain chunk
        
        // Create a height map texture from the MapData that respects our constraints
        int mapSize = mapData.heightMap.GetLength(0);
        Texture2D heightMapTexture = CreateFilteredHeightMap(mapData, mapSize);
        
        // Set the height map
        grass.heightMap = heightMapTexture;
        
        // Set the position offset in the grass material
        // Convert position to world units
        Vector3 worldPosition = new Vector3(
            position.x * mapGenerator.terrainData.uniformScale,
            0,
            position.y * mapGenerator.terrainData.uniformScale
        );
        
        // Set chunk offset if the material supports it
        if (grass.grassMaterial.HasProperty("_ChunkOffset")) {
            grass.grassMaterial.SetVector("_ChunkOffset", new Vector4(worldPosition.x, 0, worldPosition.z, 0));
        }
        
        // Set chunk number for variation
        if (grass.grassMaterial.HasProperty("_ChunkNum")) {
            grass.grassMaterial.SetInt("_ChunkNum", Mathf.FloorToInt(position.x * 1000 + position.y));
        }
        
        // Apply terrain uniform scale
        float uniformScale = mapGenerator.terrainData.uniformScale;
        if (grass.grassMaterial.HasProperty("_TerrainScale")) {
            grass.grassMaterial.SetFloat("_TerrainScale", uniformScale);
        }
    }
    
    
    private Texture2D CreateFilteredHeightMap(MapData mapData, int mapSize)
    {
        Texture2D heightMapTexture = new Texture2D(mapSize, mapSize, TextureFormat.RFloat, false);
        Color[] heightColors = new Color[mapSize * mapSize];
        float[,] heightMap = mapData.heightMap;
        
        // Fill the height texture based on the map data
        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                float height = heightMap[x, y];
                
                // Skip areas outside height thresholds
                if (height < minHeightThreshold || height > maxHeightThreshold) {
                    heightColors[y * mapSize + x] = new Color(0, 0, 0, 0);
                    continue;
                }
                
                // Check slope (using the CalculateNormal method from MapGenerator)
                if (x > 0 && x < mapSize - 1 && y > 0 && y < mapSize - 1) {
                    Vector3 normal = CalculateNormal(x, y, heightMap);
                    float slope = Vector3.Angle(normal, Vector3.up);
                    
                    if (slope > maxSlopeAngle) {
                        heightColors[y * mapSize + x] = new Color(0, 0, 0, 0);
                        continue;
                    }
                }
                
                // If we passed all constraints, use the height value
                heightColors[y * mapSize + x] = new Color(height, 0, 0, 1);
            }
        }
        
        heightMapTexture.SetPixels(heightColors);
        heightMapTexture.Apply();
        return heightMapTexture;
    }
    
    // Copy of the CalculateNormal method from MapGenerator to ensure consistency
    private Vector3 CalculateNormal(int x, int y, float[,] heightMap) {
        float heightL = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x-1, y]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightR = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x+1, y]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightD = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x, y-1]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightU = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x, y+1]) * mapGenerator.terrainData.meshHeightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    /// <summary>
    /// Removes grass from a terrain chunk
    /// </summary>
    public void RemoveGrassFromChunk(Vector2 position) 
    {
        if (activeGrassChunks.TryGetValue(position, out ModelGrass grass)) {
            if (grass != null) {
                Destroy(grass.gameObject);
            }
            activeGrassChunks.Remove(position);
        }
    }
    
   
    public void UpdateGrassVisibility(Vector2 viewerPosition, float maxViewDistance) 
    {
        if (!enableGrass) return;
        
        // Convert viewer position to world units
        Vector2 worldViewerPosition = viewerPosition * mapGenerator.terrainData.uniformScale;
        
        // Disable grass chunks that are too far away
        foreach (var kvp in activeGrassChunks) {
            Vector2 chunkPosition = kvp.Key;
            ModelGrass grassChunk = kvp.Value;
            
            if (grassChunk == null) continue;
            
            // Convert chunk position to world units
            Vector2 worldChunkPosition = new Vector2(
                chunkPosition.x * mapGenerator.terrainData.uniformScale,
                chunkPosition.y * mapGenerator.terrainData.uniformScale
            );
            
            float viewerDistanceFromNearestEdge = Vector2.Distance(worldViewerPosition, worldChunkPosition);
            
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
            
            if (grassChunk.gameObject.activeSelf != visible) {
                grassChunk.gameObject.SetActive(visible);
            }
        }
    }
}