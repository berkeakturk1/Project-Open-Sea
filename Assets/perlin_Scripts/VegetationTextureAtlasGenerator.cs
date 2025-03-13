using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

// This class helps create texture atlases for vegetation elements
// It's primarily for use in the editor, not at runtime
public class VegetationTextureAtlasGenerator : MonoBehaviour {
    
    [System.Serializable]
    public class AtlasItem {
        public Texture2D sourceTexture;
        public string name;
        [Range(1, 4)]
        public int tilesX = 1;
        [Range(1, 4)]
        public int tilesY = 1;
    }
    
    public enum AtlasSize {
        Size_512x512 = 512,
        Size_1024x1024 = 1024,
        Size_2048x2048 = 2048,
        Size_4096x4096 = 4096
    }
    
    [Header("Source Textures")]
    public List<AtlasItem> sourceTextures = new List<AtlasItem>();
    
    [Header("Atlas Settings")]
    public AtlasSize atlasSize = AtlasSize.Size_1024x1024;
    public string atlasName = "VegetationAtlas";
    public bool generateMipmaps = true;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public FilterMode filterMode = FilterMode.Bilinear;
    
    [Header("Material Generation")]
    public Shader vegetationShader;
    public bool createMaterials = true;
    public bool assignToVegetationManager = true;
    
    [Header("Atlas Preview")]
    public Texture2D generatedAtlas;
    
    // Editor-only functionality for creating the atlas
#if UNITY_EDITOR
    public void GenerateAtlas() {
        if (sourceTextures.Count == 0) {
            Debug.LogError("No source textures specified!");
            return;
        }
        
        int atlasWidth = (int)atlasSize;
        int atlasHeight = (int)atlasSize;
        
        // Create a new texture for the atlas
        Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, generateMipmaps);
        atlas.filterMode = filterMode;
        atlas.wrapMode = wrapMode;
        
        // Fill with transparent pixels initially
        Color[] transparentPixels = new Color[atlasWidth * atlasHeight];
        for (int i = 0; i < transparentPixels.Length; i++) {
            transparentPixels[i] = Color.clear;
        }
        atlas.SetPixels(transparentPixels);
        
        // Calculate how many tiles we need in total
        int totalTiles = 0;
        foreach (var item in sourceTextures) {
            totalTiles += item.tilesX * item.tilesY;
        }
        
        // Calculate grid dimensions
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalTiles));
        int tileSize = atlasWidth / gridSize;
        
        // Track generated materials for assignment
        List<Material> generatedMaterials = new List<Material>();
        
        // Place each texture into the atlas
        int currentTile = 0;
        Dictionary<string, Rect> uvRects = new Dictionary<string, Rect>();
        
        foreach (var item in sourceTextures) {
            if (item.sourceTexture == null) continue;
            
            // Calculate tile size within the source texture
            int srcTileWidth = item.sourceTexture.width / item.tilesX;
            int srcTileHeight = item.sourceTexture.height / item.tilesY;
            
            for (int y = 0; y < item.tilesY; y++) {
                for (int x = 0; x < item.tilesX; x++) {
                    // Calculate position in the atlas
                    int row = currentTile / gridSize;
                    int col = currentTile % gridSize;
                    
                    int atlasX = col * tileSize;
                    int atlasY = atlasHeight - (row + 1) * tileSize;
                    
                    // Calculate source rectangle
                    int srcX = x * srcTileWidth;
                    int srcY = item.sourceTexture.height - (y + 1) * srcTileHeight;
                    
                    // Get pixels from source texture
                    Color[] pixels = item.sourceTexture.GetPixels(srcX, srcY, srcTileWidth, srcTileHeight);
                    
                    // Scale pixels to fit tile size if necessary
                    if (srcTileWidth != tileSize || srcTileHeight != tileSize) {
                        pixels = ScalePixels(pixels, srcTileWidth, srcTileHeight, tileSize, tileSize);
                    }
                    
                    // Set pixels in the atlas
                    atlas.SetPixels(atlasX, atlasY, tileSize, tileSize, pixels);
                    
                    // Store UV coordinates
                    string tileName = $"{item.name}_{x}_{y}";
                    Rect uvRect = new Rect(
                        (float)atlasX / atlasWidth,
                        (float)atlasY / atlasHeight,
                        (float)tileSize / atlasWidth,
                        (float)tileSize / atlasHeight
                    );
                    uvRects[tileName] = uvRect;
                    
                    // Generate material
                    if (createMaterials && vegetationShader != null) {
                        Material mat = new Material(vegetationShader);
                        mat.name = tileName;
                        mat.SetTexture("_MainTex", atlas);
                        mat.SetTextureOffset("_MainTex", new Vector2(uvRect.x, uvRect.y));
                        mat.SetTextureScale("_MainTex", new Vector2(uvRect.width, uvRect.height));
                        mat.enableInstancing = true;
                        
                        // Save the material as an asset
                        string materialPath = $"Assets/Materials/Vegetation/{tileName}.mat";
                        
                        // Create directory if it doesn't exist
                        string directory = System.IO.Path.GetDirectoryName(materialPath);
                        if (!System.IO.Directory.Exists(directory)) {
                            System.IO.Directory.CreateDirectory(directory);
                        }
                        
                        AssetDatabase.CreateAsset(mat, materialPath);
                        generatedMaterials.Add(mat);
                    }
                    
                    currentTile++;
                }
            }
        }
        
        // Apply all pixel changes to the atlas
        atlas.Apply();
        
        // Save the atlas as an asset
        string atlasPath = $"Assets/Textures/Vegetation/{atlasName}.png";
        
        // Create directory if it doesn't exist
        string atlasDirectory = System.IO.Path.GetDirectoryName(atlasPath);
        if (!System.IO.Directory.Exists(atlasDirectory)) {
            System.IO.Directory.CreateDirectory(atlasDirectory);
        }
        
        // Save texture to PNG
        byte[] pngData = atlas.EncodeToPNG();
        System.IO.File.WriteAllBytes(atlasPath, pngData);
        AssetDatabase.ImportAsset(atlasPath);
        
        // Update atlas import settings
        TextureImporter importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
        if (importer != null) {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = generateMipmaps;
            importer.wrapMode = wrapMode;
            importer.filterMode = filterMode;
            importer.SaveAndReimport();
        }
        
        // Update reference to the generated atlas
        generatedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        
        // Assign materials to the VegetationManager if requested
        if (assignToVegetationManager && generatedMaterials.Count > 0) {
            VegetationManager vegetationManager = FindObjectOfType<VegetationManager>();
            if (vegetationManager != null) {
                // Create or update vegetation types based on materials
                List<VegetationType> types = new List<VegetationType>();
                
                foreach (Material mat in generatedMaterials) {
                    // Try to find existing type with the same name
                    VegetationType existingType = null;
                    
                    if (vegetationManager.vegetationTypes != null) {
                        foreach (var type in vegetationManager.vegetationTypes) {
                            if (type.name == mat.name) {
                                existingType = type;
                                break;
                            }
                        }
                    }
                    
                    if (existingType != null) {
                        // Update existing type
                        existingType.material = mat;
                        types.Add(existingType);
                    } else {
                        // Create new type with default values
                        VegetationType newType = new VegetationType {
                            name = mat.name,
                            material = mat,
                            density = 1f,
                            minHeightThreshold = 0.2f,
                            maxHeightThreshold = 0.8f,
                            maxSlopeAngle = 30f,
                            minScale = 0.7f,
                            maxScale = 1.2f,
                            randomOffset = 0.1f,
                            maxInstancesPerChunk = 1000,
                            colorA = new Color(0.8f, 1.0f, 0.8f, 1.0f),
                            colorB = new Color(1.0f, 1.0f, 0.8f, 1.0f)
                        };
                        
                        // Set different defaults based on name
                        if (mat.name.ToLower().Contains("grass")) {
                            newType.density = 2.0f;
                            newType.maxInstancesPerChunk = 5000;
                        } else if (mat.name.ToLower().Contains("flower")) {
                            newType.density = 0.5f;
                            newType.maxInstancesPerChunk = 500;
                            newType.colorA = new Color(1.0f, 0.8f, 0.8f, 1.0f);
                            newType.colorB = new Color(0.8f, 0.8f, 1.0f, 1.0f);
                        }
                        
                        types.Add(newType);
                    }
                }
                
                // Update vegetation manager
                vegetationManager.vegetationTypes = types.ToArray();
                EditorUtility.SetDirty(vegetationManager);
            }
        }
        
        Debug.Log($"Atlas generated with {currentTile} tiles at {atlasPath}");
        AssetDatabase.Refresh();
    }
    
    private Color[] ScalePixels(Color[] sourcePixels, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight) {
        Color[] result = new Color[targetWidth * targetHeight];
        
        for (int y = 0; y < targetHeight; y++) {
            for (int x = 0; x < targetWidth; x++) {
                float u = x / (float)targetWidth;
                float v = y / (float)targetHeight;
                
                int sourceX = Mathf.FloorToInt(u * sourceWidth);
                int sourceY = Mathf.FloorToInt(v * sourceHeight);
                
                result[y * targetWidth + x] = sourcePixels[sourceY * sourceWidth + sourceX];
            }
        }
        
        return result;
    }
#endif
}