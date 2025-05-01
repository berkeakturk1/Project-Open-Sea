using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This script helps initialize and manage the vegetation system in the context of your island generator
public class VegetationAdaptor : MonoBehaviour {
    
    [Header("References")]
    public MapGenerator mapGenerator;
    public VegetationManager vegetationManager;
    
    [Header("Default Vegetation")]
    [Tooltip("Create default vegetation types if none exist")]
    public bool createDefaultVegetation = true;
    
    [Header("Default Grass Textures")]
    public Texture2D[] grassTextures;
    [Header("Default Flower Textures")]
    public Texture2D[] flowerTextures;
    
    private void Awake() {
        // Find references if not set
        if (mapGenerator == null) {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator == null) {
                Debug.LogError("No MapGenerator found in the scene!");
            }
        }
        
        if (vegetationManager == null) {
            vegetationManager = FindObjectOfType<VegetationManager>();
            if (vegetationManager == null) {
                Debug.Log("Creating VegetationManager...");
                GameObject managerObj = new GameObject("VegetationManager");
                vegetationManager = managerObj.AddComponent<VegetationManager>();
            }
        }
        
        // Ensure the map generator knows about the vegetation manager
        if (mapGenerator != null && vegetationManager != null) {
            mapGenerator.vegetationManager = vegetationManager;
            mapGenerator.spawnVegetation = true;
        }
        
        // Setup default vegetation if needed
        SetupDefaultVegetation();
    }
    
    private void SetupDefaultVegetation() {
        if (!createDefaultVegetation || vegetationManager == null) return;
        
        // Only create default vegetation if none exists
        if (vegetationManager.vegetationTypes == null || vegetationManager.vegetationTypes.Length == 0) {
            Debug.Log("Creating default vegetation types...");
            
            List<VegetationType> types = new List<VegetationType>();
            
            // Create grass types
            VegetationType grassType = CreateVegetationType(
                "Grass",
                CreateBasicMaterial("Grass", Color.green),
                2.0f,
                0.1f, 0.85f,
                35f,
                0.7f, 1.3f,
                8000,
                new Color(0.6f, 0.8f, 0.6f, 1f),
                new Color(0.7f, 0.9f, 0.7f, 1f)
            );
            types.Add(grassType);
            
            // Create tall grass type
            VegetationType tallGrassType = CreateVegetationType(
                "TallGrass",
                CreateBasicMaterial("TallGrass", Color.green),
                1.0f,
                0.2f, 0.8f,
                25f,
                1.0f, 1.5f,
                2000,
                new Color(0.5f, 0.7f, 0.5f, 1f),
                new Color(0.7f, 0.8f, 0.6f, 1f)
            );
            types.Add(tallGrassType);
            
            // Create flower type
            VegetationType flowerType = CreateVegetationType(
                "Flowers",
                CreateBasicMaterial("Flowers", Color.white),
                0.4f,
                0.3f, 0.7f,
                15f,
                0.8f, 1.2f,
                500,
                new Color(1.0f, 0.8f, 0.8f, 1f),
                new Color(0.8f, 0.8f, 1.0f, 1f)
            );
            types.Add(flowerType);
            
            // Apply created types to vegetation manager
            vegetationManager.vegetationTypes = types.ToArray();
            
            // Assign textures if available
            AssignDefaultTextures();
        }
    }
    
    private VegetationType CreateVegetationType(
        string name,
        Material material,
        float density,
        float minHeight, float maxHeight,
        float maxSlope,
        float minScale, float maxScale,
        int maxInstances,
        Color colorA, Color colorB
    ) {
        // Create basic mesh for the vegetation type
        Mesh mesh = null;
        
        if (name.Contains("Grass")) {
            mesh = CreateCrossMesh();
        } else {
            mesh = CreateQuadMesh();
        }
        
        return new VegetationType {
            name = name,
            mesh = mesh,
            material = material,
            density = density,
            minHeightThreshold = minHeight,
            maxHeightThreshold = maxHeight,
            maxSlopeAngle = maxSlope,
            minScale = minScale,
            maxScale = maxScale,
            randomOffset = 0.1f,
            maxInstancesPerChunk = maxInstances,
            useColorVariation = true,
            colorA = colorA,
            colorB = colorB,
            colorVariationStrength = 0.5f,
            useHeightBasedVariation = true,
            heightVariationStrength = 0.3f
        };
    }
    
    private Material CreateBasicMaterial(string name, Color color) {
        // Create a new material using the appropriate shader
        Shader vegetationShader = null;
        
        if (name.Contains("Grass")) {
            vegetationShader = Shader.Find("Custom/Vegetation");
        } else {
            vegetationShader = Shader.Find("Custom/BillboardVegetation");
        }
        
        // Fallback to standard cutout shader if custom shaders not found
        if (vegetationShader == null) {
            vegetationShader = Shader.Find("Standard");
            Debug.LogWarning("Custom vegetation shaders not found, using Standard shader.");
        }
        
        Material material = new Material(vegetationShader);
        material.name = name;
        material.color = color;
        
        // Enable instancing for better performance
        material.enableInstancing = true;
        
        // Set alpha cutoff for transparency
        if (material.HasProperty("_Cutoff")) {
            material.SetFloat("_Cutoff", 0.5f);
        }
        
        // Set wind parameters if available
        if (material.HasProperty("_WindStrength")) {
            material.SetFloat("_WindStrength", 0.3f);
            material.SetFloat("_WindSpeed", 1.0f);
            material.SetFloat("_WindScale", 2.0f);
        }
        
        return material;
    }
    
    private Mesh CreateQuadMesh() {
        Mesh mesh = new Mesh();
        
        // Create a basic quad for billboard vegetation
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];
        
        vertices[0] = new Vector3(-0.5f, 0, 0);
        vertices[1] = new Vector3(0.5f, 0, 0);
        vertices[2] = new Vector3(-0.5f, 1, 0);
        vertices[3] = new Vector3(0.5f, 1, 0);
        
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private Mesh CreateCrossMesh() {
        Mesh mesh = new Mesh();
        
        // Create a cross-quad mesh for grass
        Vector3[] vertices = new Vector3[8];
        Vector2[] uv = new Vector2[8];
        int[] triangles = new int[12];
        
        // First quad (front-facing)
        vertices[0] = new Vector3(-0.5f, 0, 0);
        vertices[1] = new Vector3(0.5f, 0, 0);
        vertices[2] = new Vector3(-0.5f, 1, 0);
        vertices[3] = new Vector3(0.5f, 1, 0);
        
        // Second quad (side-facing, crosses the first)
        vertices[4] = new Vector3(0, 0, -0.5f);
        vertices[5] = new Vector3(0, 0, 0.5f);
        vertices[6] = new Vector3(0, 1, -0.5f);
        vertices[7] = new Vector3(0, 1, 0.5f);
        
        // UVs for both quads
        for (int i = 0; i < 8; i += 4) {
            uv[i + 0] = new Vector2(0, 0);
            uv[i + 1] = new Vector2(1, 0);
            uv[i + 2] = new Vector2(0, 1);
            uv[i + 3] = new Vector2(1, 1);
        }
        
        // First quad triangles
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;
        
        // Second quad triangles
        triangles[6] = 4;
        triangles[7] = 6;
        triangles[8] = 5;
        triangles[9] = 5;
        triangles[10] = 6;
        triangles[11] = 7;
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private void AssignDefaultTextures() {
        if (vegetationManager.vegetationTypes == null) return;
        
        int grassIndex = 0;
        int flowerIndex = 0;
        
        // Go through each vegetation type
        for (int i = 0; i < vegetationManager.vegetationTypes.Length; i++) {
            VegetationType type = vegetationManager.vegetationTypes[i];
            
            // Skip if no material
            if (type.material == null) continue;
            
            // Assign texture based on type name
            if (type.name.Contains("Grass") && grassTextures != null && grassTextures.Length > 0) {
                Texture2D texture = grassTextures[grassIndex % grassTextures.Length];
                if (texture != null) {
                    type.material.mainTexture = texture;
                    grassIndex++;
                }
            }
            else if (type.name.Contains("Flower") && flowerTextures != null && flowerTextures.Length > 0) {
                Texture2D texture = flowerTextures[flowerIndex % flowerTextures.Length];
                if (texture != null) {
                    type.material.mainTexture = texture;
                    flowerIndex++;
                }
            }
        }
    }
    
    // This method can be called at runtime to replace textures
    public void AssignTextures(Texture2D[] grassTextures, Texture2D[] flowerTextures) {
        this.grassTextures = grassTextures;
        this.flowerTextures = flowerTextures;
        AssignDefaultTextures();
    }
}