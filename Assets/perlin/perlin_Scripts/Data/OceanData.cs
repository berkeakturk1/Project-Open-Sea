using UnityEngine;

namespace perlin_Scripts.Data
{
    [CreateAssetMenu()]
    public class OceanData : UpdatableData 
    {
        [Header("Ocean Floor Settings")]
        public bool generateOceanFloor = true;
        public float oceanDepth = -50f;
    
        [Header("Noise Settings")]
        public float oceanFloorVariation = 10f;
        public float oceanNoiseScale = 40f;
        public int oceanNoiseOctaves = 4;
        [Range(0, 1)]
        public float oceanNoisePersistance = 0.5f;
        public float oceanNoiseLacunarity = 2f;
        public int oceanNoiseSeed = 42;
    
        [Header("Material Settings")]
        [Tooltip("Base color of the sand")]
        public Color sandColor = new Color(0.8f, 0.7f, 0.5f, 1f);
        [Tooltip("Scale of the sand textures")]
        [Range(1, 100)]
        public float textureScale = 20f;
        [Tooltip("Strength of the normal map")]
        [Range(0, 2)]
        public float normalStrength = 1.0f;
        [Tooltip("Strength of the displacement map")]
        [Range(0, 2)]
        public float displacementStrength = 0.3f;
        [Tooltip("Roughness multiplier")]
        [Range(0, 1)]
        public float roughness = 0.8f;
        
        [Header("Wave Animation")]
        [Tooltip("Speed of the waves")]
        [Range(0, 5)]
        public float waveSpeed = 0.5f;
        [Tooltip("Strength of the wave displacement")]
        [Range(0, 1)]
        public float waveStrength = 0.1f;
        [Tooltip("Scale of the waves")]
        [Range(1, 50)]
        public float waveScale = 10f;
    
        [Header("Performance Settings")]
        [Tooltip("Resolution of the ocean floor mesh (lower for better performance)")]
        [Range(10, 100)]
        public int oceanFloorResolution = 20;
        [Tooltip("Size of the ocean floor mesh in world units")]
        public float oceanFloorSize = 1000f;
        [Tooltip("Whether the ocean floor should follow the player")]
        public bool followPlayer = true;
        
        // Add this field back to the OceanData class:
        [Header("Transition Settings")]
        [Tooltip("How far from islands the ocean floor becomes flat")]
        public float flatOceanDistance = 200f;
    
#if UNITY_EDITOR
        protected override void OnValidate() {
            // Clamp values to reasonable ranges
            oceanNoiseOctaves = Mathf.Max(1, oceanNoiseOctaves);
            oceanFloorVariation = Mathf.Max(0, oceanFloorVariation);
            oceanFloorSize = Mathf.Max(100, oceanFloorSize);
        
            base.OnValidate();
        }
#endif
    
        public void ApplyToMaterial(Material material) {
            if (material == null) return;
            
            // Apply the new simplified shader properties
            material.SetColor("_SandColor", sandColor);
            material.SetFloat("_TextureScale", textureScale);
            material.SetFloat("_NormalStrength", normalStrength);
            material.SetFloat("_DisplacementStrength", displacementStrength);
            material.SetFloat("_Roughness", roughness);
            material.SetFloat("_WaveSpeed", waveSpeed);
            material.SetFloat("_WaveStrength", waveStrength);
            material.SetFloat("_WaveScale", waveScale);
        }
    }
}