using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;
    const TextureFormat heightmapFormat = TextureFormat.R8;
    const TextureFormat normalMapFormat = TextureFormat.RGB24;
    const TextureFormat roughnessMapFormat = TextureFormat.R8;

    public Layer[] layers;

    // Advanced texture settings
    [Header("Advanced Texture Settings")]
    public bool enableHeightmaps = false;
    public bool enableNormalMaps = false;
    public bool enableRoughnessMaps = false;
    [Range(0f, 1f)] public float heightmapScale = 0.1f;
    [Range(0f, 1f)] public float globalRoughness = 0.5f;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material) {
        // Basic texture layer data
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());

        // Main texture array
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        // Optional heightmap support
        if (enableHeightmaps && layers.Any(l => l.heightmap != null)) {
            Texture2DArray heightmapsArray = GenerateTextureArray(layers.Select(x => x.heightmap).ToArray(), heightmapFormat);
            material.SetTexture("baseHeightmaps", heightmapsArray);
            material.SetFloat("_HeightmapEnabled", 1);
            material.SetFloat("_HeightScale", heightmapScale);
        } else {
            material.SetFloat("_HeightmapEnabled", 0);
        }

        // Optional normal map support
        if (enableNormalMaps && layers.Any(l => l.normalMap != null)) {
            Texture2DArray normalMapsArray = GenerateTextureArray(layers.Select(x => x.normalMap).ToArray(), normalMapFormat);
            material.SetTexture("baseNormalMaps", normalMapsArray);
            material.SetFloat("_NormalMapEnabled", 1);
        } else {
            material.SetFloat("_NormalMapEnabled", 0);
        }

        // Optional roughness map support
        if (enableRoughnessMaps && layers.Any(l => l.roughnessMap != null)) {
            Texture2DArray roughnessMapsArray = GenerateTextureArray(layers.Select(x => x.roughnessMap).ToArray(), roughnessMapFormat);
            material.SetTexture("baseRoughnessMaps", roughnessMapsArray);
            material.SetFloat("_RoughnessMapEnabled", 1);
            material.SetFloatArray("baseRoughnessValues", layers.Select(x => x.roughnessIntensity).ToArray());
        } else {
            material.SetFloat("_RoughnessMapEnabled", 0);
            material.SetFloat("_GlobalRoughness", globalRoughness);
        }

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures, TextureFormat format = TextureFormat.RGB565) {
        // Handle case where textures might be null or empty
        if (textures == null || textures.Length == 0) {
            return null;
        }

        // Ensure all textures are valid
        var validTextures = textures.Where(t => t != null).ToArray();
        if (validTextures.Length == 0) {
            Debug.LogWarning("No valid textures provided for texture array.");
            return null;
        }

        // Create texture array
        Texture2DArray textureArray = new Texture2DArray(
            textureSize, 
            textureSize, 
            validTextures.Length, 
            format, 
            true
        );

        // Populate texture array
        for (int i = 0; i < validTextures.Length; i++) {
            // Resize and convert texture if needed
            Texture2D processedTexture = ResizeTexture(validTextures[i], textureSize, format);
            textureArray.SetPixels(processedTexture.GetPixels(), i);
        }

        textureArray.Apply();
        return textureArray;
    }

    Texture2D ResizeTexture(Texture2D originalTexture, int targetSize, TextureFormat targetFormat) {
        // Create a new texture with the target size and format
        RenderTexture tmp = RenderTexture.GetTemporary(targetSize, targetSize);
        Graphics.Blit(originalTexture, tmp);

        Texture2D resizedTexture = new Texture2D(targetSize, targetSize, targetFormat, true);
        RenderTexture.active = tmp;
        resizedTexture.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
        resizedTexture.Apply();

        RenderTexture.ReleaseTemporary(tmp);
        return resizedTexture;
    }

    [System.Serializable]
    public class Layer {
        public Texture2D texture;
        public Texture2D heightmap;
        public Texture2D normalMap;
        public Texture2D roughnessMap;

        public Color tint = Color.white;
        [Range(0,1)]
        public float tintStrength = 0.5f;
        [Range(0,1)]
        public float startHeight = 0f;
        [Range(0,1)]
        public float blendStrength = 0.5f;
        public float textureScale = 1f;

        [Range(0,1)]
        public float roughnessIntensity = 0.5f;
    }
}