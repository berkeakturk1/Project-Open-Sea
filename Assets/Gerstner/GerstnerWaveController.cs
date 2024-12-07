using UnityEngine;

[ExecuteInEditMode]
public class GerstnerWaveController : MonoBehaviour
{
    // Reference to the material using the Gerstner Wave shader
    public Material gerstnerMaterial;

    // Adjustable properties for the shader
    [Range(0.0f, 1.0f)] public float steepness = 1.0f;
    public float speed = 1.0f;
    public float amplitude = 1.0f;
    public float wavelength = 1.0f;
    public Vector2 waveDirection = new Vector2(1, 0);  // X and Z direction of the wave

    void Start()
    {
        if (gerstnerMaterial == null)
        {
            Debug.LogError("Please assign the material with the Gerstner wave shader.");
        }
    }

    void Update()
    {
        // Ensure material is assigned
        if (gerstnerMaterial != null)
        {
            // Update the shader properties
            gerstnerMaterial.SetFloat("_Steepness", steepness);
            gerstnerMaterial.SetFloat("_Speed", speed);
            gerstnerMaterial.SetFloat("_Amplitude", amplitude);
            gerstnerMaterial.SetFloat("_Wavelength", wavelength);
            gerstnerMaterial.SetVector("_TestDirection", waveDirection.normalized);
        }
    }
}