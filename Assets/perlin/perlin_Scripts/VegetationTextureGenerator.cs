using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This class is OPTIONAL and can be completely omitted if you prefer to use your own textures
// It's provided as a convenience for quickly testing the vegetation system
public class VegetationTextureGenerator : MonoBehaviour
{
    [Header("Grass Texture Settings")]
    public int grassTextureSize = 128;
    public Color grassBaseColor = new Color(0.2f, 0.8f, 0.1f, 1f); 
    public Color grassTipColor = new Color(0.8f, 1f, 0.3f, 1f);
    public float noiseScale = 20f;
    
    [Header("Flower Texture Settings")]
    public int flowerTextureSize = 128;
    public Color flowerPetalColor = new Color(1f, 0.5f, 0.5f, 1f);
    public Color flowerCenterColor = new Color(1f, 1f, 0.2f, 1f);
    public float petalNoiseScale = 5f;
    
    [Header("Output")]
    public bool generateOnStart = false;
    public string outputFolder = "Assets/Textures/Generated";
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateGrassTexture();
            GenerateFlowerTexture("daisy");
            GenerateFlowerTexture("rose");
        }
    }
    
    public Texture2D GenerateGrassTexture()
    {
        Texture2D texture = new Texture2D(grassTextureSize, grassTextureSize, TextureFormat.RGBA32, false);
        
        // Create gradient from base to tip
        for (int y = 0; y < grassTextureSize; y++)
        {
            float t = y / (float)grassTextureSize;
            Color blendedColor = Color.Lerp(grassBaseColor, grassTipColor, t);
            
            for (int x = 0; x < grassTextureSize; x++)
            {
                // Add some noise variation
                float noise = Mathf.PerlinNoise(x / noiseScale, y / noiseScale);
                Color finalColor = Color.Lerp(blendedColor, Color.white, noise * 0.1f);
                
                // Add transparency near edges for better blending
                float edgeX = Mathf.Abs(x - grassTextureSize/2) / (float)(grassTextureSize/2);
                float edgeAlpha = 1f - Mathf.Clamp01(edgeX * 1.5f - 0.2f);
                
                // Fade out alpha at the base
                float baseAlpha = t < 0.1f ? t * 10f : 1f;
                
                finalColor.a = edgeAlpha * baseAlpha;
                
                texture.SetPixel(x, y, finalColor);
            }
        }
        
        texture.Apply();
        
        // Save the texture in editor
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            string path = $"{outputFolder}/GrassTexture.png";
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();
            
            // Set import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
        #endif
        
        return texture;
    }
    
    public Texture2D GenerateFlowerTexture(string flowerType = "daisy")
    {
        Texture2D texture = new Texture2D(flowerTextureSize, flowerTextureSize, TextureFormat.RGBA32, false);
        
        // Customize based on flower type
        Color petalColor = flowerPetalColor;
        Color centerColor = flowerCenterColor;
        float centerSize = 0.2f;
        int petalCount = 8;
        
        if (flowerType.ToLower() == "rose")
        {
            petalColor = new Color(0.9f, 0.1f, 0.2f, 1f);
            centerColor = new Color(0.7f, 0.1f, 0.1f, 1f);
            centerSize = 0.3f;
            petalCount = 12;
        }
        else if (flowerType.ToLower() == "sunflower")
        {
            petalColor = new Color(1f, 0.8f, 0.1f, 1f);
            centerColor = new Color(0.6f, 0.4f, 0.1f, 1f);
            centerSize = 0.4f;
            petalCount = 16;
        }
        else if (flowerType.ToLower() == "bluebell")
        {
            petalColor = new Color(0.3f, 0.5f, 1f, 1f);
            centerColor = new Color(0.8f, 0.8f, 1f, 1f);
            centerSize = 0.15f;
            petalCount = 6;
        }
        
        Vector2 center = new Vector2(flowerTextureSize / 2, flowerTextureSize / 2);
        float maxDist = flowerTextureSize / 2f;
        
        for (int y = 0; y < flowerTextureSize; y++)
        {
            for (int x = 0; x < flowerTextureSize; x++)
            {
                // Distance from center
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center) / maxDist;
                
                // Default to transparent
                Color color = new Color(0, 0, 0, 0);
                
                // Draw center
                if (dist < centerSize)
                {
                    color = centerColor;
                    
                    // Add noise to center
                    float noise = Mathf.PerlinNoise(x / petalNoiseScale, y / petalNoiseScale);
                    color = Color.Lerp(color, Color.black, noise * 0.5f);
                    color.a = 1f;
                }
                // Draw petals
                else if (dist < 0.9f)
                {
                    // Calculate angle to determine petal
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    angle = (angle + Mathf.PI) / (2 * Mathf.PI); // Normalize to 0-1
                    
                    // Determine if in petal 
                    float petalWidth = 1f / petalCount / 2.5f;
                    float petalAngle = Mathf.Repeat(angle, 1f / petalCount);
                    
                    if (petalAngle < petalWidth || petalAngle > (1f / petalCount - petalWidth))
                    {
                        // We're in a petal
                        color = petalColor;
                        
                        // Add variation to petals
                        float noise = Mathf.PerlinNoise(x / (petalNoiseScale * 2), y / (petalNoiseScale * 2));
                        color = Color.Lerp(color, Color.white, noise * 0.3f);
                        
                        // Fade alpha at edges
                        float alpha = 1f - Mathf.Clamp01((dist - 0.4f) * 2f);
                        color.a = alpha;
                    }
                }
                
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        
        // Save the texture in editor
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            string path = $"{outputFolder}/FlowerTexture_{flowerType}.png";
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();
            
            // Set import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
        #endif
        
        return texture;
    }
}