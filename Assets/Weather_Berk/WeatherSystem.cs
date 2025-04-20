using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    [Header("Seasonal Rain Chances")]
    [Range(0f, 1f)] public float chanceToRainSpring = 0.3f;
    [Range(0f, 1f)] public float chanceToRainSummer = 0.0f;
    [Range(0f, 1f)] public float chanceToRainFall   = 0.4f;
    [Range(0f, 1f)] public float chanceToRainWinter = 0.7f;

    [Header("Rain Visuals")]
    public GameObject rainEffect;     // Particle system or VFX object
    public Material  rainSkybox;      // Stormy skybox

    [Header("References")]
    public DayNightSystem dayNightSystem;

    [HideInInspector] public bool isSpecialWeather;

    private Material originalSkybox;
    private enum WeatherCondition { Sunny, Rainy }
    private WeatherCondition currentWeather = WeatherCondition.Sunny;

    private void Start()
    {
        // Cache the original skybox
        originalSkybox = RenderSettings.skybox;

        // Listen for each new day
        TimeManager.Instance.OnDayPass.AddListener(GenerateRandomWeather);

        // Ensure rain effect is off
        if (rainEffect != null) rainEffect.SetActive(false);
    }

    private void GenerateRandomWeather()
    {
        // Determine chance based on season
        float chanceToRain = 0f;
        switch (TimeManager.Instance.currentSeason)
        {
            case TimeManager.Season.Spring: chanceToRain = chanceToRainSpring; break;
            case TimeManager.Season.Summer: chanceToRain = chanceToRainSummer; break;
            case TimeManager.Season.Fall:   chanceToRain = chanceToRainFall;   break;
            case TimeManager.Season.Winter: chanceToRain = chanceToRainWinter; break;
        }

        // Roll for weather
        if (Random.value <= chanceToRain)
        {
            currentWeather   = WeatherCondition.Rainy;
            isSpecialWeather = true;
            StartRain();
        }
        else
        {
            currentWeather   = WeatherCondition.Sunny;
            isSpecialWeather = false;
            StopRain();
        }
    }

    public void StartRain()
    {
        if (rainEffect != null) rainEffect.SetActive(true);
        if (rainSkybox  != null) RenderSettings.skybox = rainSkybox;
    }

    public void StopRain()
    {
        if (rainEffect != null) rainEffect.SetActive(false);
        if (originalSkybox != null) RenderSettings.skybox = originalSkybox;
    }
}
