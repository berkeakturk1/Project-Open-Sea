using UnityEngine;

public class SailController : MonoBehaviour
{
    [Tooltip("Reference to the sail Material with the SailWindAnimation shader")]
    public Material sailMaterial;

    [Tooltip("Enable/disable wind animation")]
    public bool windEnabled = true;

    [Tooltip("Wind animation speed")]
    [Range(0, 5)]
    public float windSpeed = 1.0f;

    [Tooltip("Wind animation intensity")]
    [Range(0, 1)]
    public float windIntensity = 0.5f;

    [Tooltip("Wind frequency (ripples)")]
    [Range(0, 10)]
    public float windFrequency = 1.0f;

    [Tooltip("Wind turbulence")]
    [Range(0, 1)]
    public float windTurbulence = 0.3f;

    [Tooltip("Billow amount")]
    [Range(0, 2)]
    public float billowAmount = 0.5f;

    [Tooltip("Billow speed")]
    [Range(0, 2)]
    public float billowSpeed = 0.2f;

    // Shader property IDs (cached for better performance)
    private int windEnabledProperty;
    private int windSpeedProperty;
    private int windIntensityProperty;
    private int windFrequencyProperty;
    private int windTurbulenceProperty;
    private int billowAmountProperty;
    private int billowSpeedProperty;

    private void Awake()
    {
        // Cache shader property IDs for better performance
        windEnabledProperty = Shader.PropertyToID("_WindEnabled");
        windSpeedProperty = Shader.PropertyToID("_WindSpeed");
        windIntensityProperty = Shader.PropertyToID("_WindIntensity");
        windFrequencyProperty = Shader.PropertyToID("_WindFrequency");
        windTurbulenceProperty = Shader.PropertyToID("_WindTurbulence");
        billowAmountProperty = Shader.PropertyToID("_BillowAmount");
        billowSpeedProperty = Shader.PropertyToID("_BillowSpeed");
    }

    private void Start()
    {
        if (sailMaterial == null)
        {
            Debug.LogError("Sail material reference is missing! Please assign the material in the inspector.");
            return;
        }

        // Apply initial settings
        UpdateShaderProperties();
    }

    private void Update()
    {
        // Update shader properties if values change at runtime
        UpdateShaderProperties();
    }

    /// <summary>
    /// Updates all shader properties with current values
    /// </summary>
    private void UpdateShaderProperties()
    {
        if (sailMaterial == null) return;

        // Update shader keyword
        if (windEnabled)
        {
            sailMaterial.EnableKeyword("_WIND_ENABLED");
        }
        else
        {
            sailMaterial.DisableKeyword("_WIND_ENABLED");
        }

        // Update shader properties
        sailMaterial.SetFloat(windEnabledProperty, windEnabled ? 1.0f : 0.0f);
        sailMaterial.SetFloat(windSpeedProperty, windSpeed);
        sailMaterial.SetFloat(windIntensityProperty, windIntensity);
        sailMaterial.SetFloat(windFrequencyProperty, windFrequency);
        sailMaterial.SetFloat(windTurbulenceProperty, windTurbulence);
        sailMaterial.SetFloat(billowAmountProperty, billowAmount);
        sailMaterial.SetFloat(billowSpeedProperty, billowSpeed);
    }

    /// <summary>
    /// Enables or disables the wind animation
    /// </summary>
    /// <param name="enable">True to enable wind, false to disable</param>
    public void SetWindEnabled(bool enable)
    {
        windEnabled = enable;
        UpdateShaderProperties();
    }

    /// <summary>
    /// Sets the wind speed
    /// </summary>
    /// <param name="speed">Wind speed value (0-5)</param>
    public void SetWindSpeed(float speed)
    {
        windSpeed = Mathf.Clamp(speed, 0f, 5f);
        UpdateShaderProperties();
    }

    /// <summary>
    /// Sets the wind intensity
    /// </summary>
    /// <param name="intensity">Wind intensity value (0-1)</param>
    public void SetWindIntensity(float intensity)
    {
        windIntensity = Mathf.Clamp(intensity, 0f, 1f);
        UpdateShaderProperties();
    }

    /// <summary>
    /// Creates a gust of wind by temporarily increasing wind intensity
    /// </summary>
    /// <param name="gustIntensity">Gust intensity multiplier</param>
    /// <param name="duration">Duration of the gust in seconds</param>
    public void TriggerWindGust(float gustIntensity = 1.5f, float duration = 2.0f)
    {
        StartCoroutine(WindGustCoroutine(gustIntensity, duration));
    }

    private System.Collections.IEnumerator WindGustCoroutine(float gustIntensity, float duration)
    {
        // Store original values
        float originalIntensity = windIntensity;
        float originalTurbulence = windTurbulence;
        
        // Apply gust
        windIntensity = Mathf.Clamp(originalIntensity * gustIntensity, 0f, 1f);
        windTurbulence = Mathf.Clamp(windTurbulence * 1.2f, 0f, 1f);
        UpdateShaderProperties();
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Restore original values
        windIntensity = originalIntensity;
        windTurbulence = originalTurbulence;
        UpdateShaderProperties();
    }
}