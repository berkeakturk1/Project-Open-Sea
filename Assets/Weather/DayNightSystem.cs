using System;
using System.Collections.Generic;
using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    [Header("Sun & Cycle Settings")]
    public Light directionalLight;

    public Light staticLight;
    
    [Tooltip("In seconds, how long a full 24h cycle takes.")]
    public float dayDurationInSeconds = 24f;

    [Header("Skybox & Lighting Mappings by Hour")]
    public List<SkyboxTimeMapping> timeMappings;

    [Header("External Systems")]
    public WeatherSystem weatherSystem;

    [Range(0f, 1f)]
    private float currentTimeOfDay = 0.35f;
    public int currentHour;

    private bool lockNextDayTrigger = false;

    void Update()
    {
        // Advance normalized time (0–1)
        currentTimeOfDay += Time.deltaTime / dayDurationInSeconds;
        currentTimeOfDay %= 1f;

        // Compute exact hour
        float hours = currentTimeOfDay * 24f;
        currentHour = Mathf.FloorToInt(hours);

        // Rotate sun
        directionalLight.transform.rotation =
            Quaternion.Euler((currentTimeOfDay * 360f) - 90f, 170f, 0f);

        // Update skybox, fog & light
        UpdateSkybox(hours);

        // Trigger next‐day once at midnight
        if (currentHour == 0 && !lockNextDayTrigger)
        {
            TimeManager.Instance.TriggerNextDay();
            lockNextDayTrigger = true;
        }
        if (currentHour != 0)
        {
            lockNextDayTrigger = false;
        }
    }

    private void UpdateSkybox(float hours)
    {
        // 0) If raining, show the rain skybox and skip normal cycle
        if (weatherSystem.isSpecialWeather && weatherSystem.rainSkybox != null)
        {
            RenderSettings.skybox = weatherSystem.rainSkybox;
            return;
        }

        // 1) Find the mapping for this exact hour (weather logic preserved)
        SkyboxTimeMapping curr = null;
        foreach (var m in timeMappings)
        {
            if (m.hour != currentHour) continue;
            if (currentHour == 12 && !weatherSystem.isSpecialWeather) continue;
            if (currentHour == 12 &&  weatherSystem.isSpecialWeather) weatherSystem.StartRain();
            if (currentHour == 15 && !weatherSystem.isSpecialWeather) continue;
            if (currentHour == 16 &&  weatherSystem.isSpecialWeather) continue;
            if (currentHour == 17 &&  weatherSystem.isSpecialWeather) weatherSystem.StopRain();
            curr = m;
            break;
        }
        if (curr == null) return;

        // 2) Determine the next mapping in chronological order
        var sorted = new List<SkyboxTimeMapping>(timeMappings);
        sorted.Sort((a, b) => a.hour.CompareTo(b.hour));
        int idx = sorted.IndexOf(curr);
        SkyboxTimeMapping nxt = sorted[(idx + 1) % sorted.Count];

        // 3) Fractional blend within the hour (0 at :00 → 1 at :59)
        float blend = Mathf.Clamp01(hours - currentHour);

        // 4) Drive the skybox shader transition if applicable
        var mat = curr.skyboxMaterial;
        if (mat != null && mat.shader != null && mat.shader.name == "Custom/SkyboxTransition")
            mat.SetFloat("_TransitionFactor", blend);

        RenderSettings.skybox = mat;

        // 5) Smoothly interpolate fog & light color in lockstep
        directionalLight.color = Color.Lerp(curr.lightColor, nxt.lightColor, blend);
        staticLight.color = Color.Lerp(curr.lightColor, nxt.lightColor, blend);
        RenderSettings.fogColor  = Color.Lerp(curr.fogColor,   nxt.fogColor,   blend);
    }
}

[Serializable]
public class SkyboxTimeMapping
{
    public string phaseName;
    [Tooltip("Hour of day (0–23) when this mapping becomes active")]
    public int hour;
    public Material skyboxMaterial;

    [Header("Lighting & Fog Colors")]
    public Color lightColor = Color.white;
    public Color fogColor   = Color.gray;
}
