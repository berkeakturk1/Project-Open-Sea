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

    [Header("Start Time")]
    [Tooltip("What hour (0-23) should the day start at?")]
    [Range(0, 23)]
    public int startHour = 10;

    [Tooltip("What minute (0-59) should the day start at?")]
    [Range(0, 59)]
    public int startMinute = 0;

    [Header("Skybox & Lighting Mappings by Hour")]
    [Tooltip("Define lighting settings for different hours. The system will blend between them. You don't need to define all 24 hours.")]
    public List<SkyboxTimeMapping> timeMappings;

    [Header("External Systems")]
    public WeatherSystem weatherSystem;

    // Public properties for other scripts to access
    public float CurrentTimeOfDay { get; private set; } // 0-1
    public int CurrentHour { get; private set; } // 0-23

    private bool lockNextDayTrigger = false;
    private List<SkyboxTimeMapping> _sortedTimeMappings;

    void Awake()
    {
        // --- PERFORMANCE FIX ---
        // The original script sorted this list every frame, which is very inefficient.
        // We now sort it only once when the script awakens.
        if (timeMappings == null || timeMappings.Count == 0)
        {
            Debug.LogError("[DayNight] Time Mappings list is not set up. The system will not work.");
            this.enabled = false; // Disable the script to prevent errors
            return;
        }

        // Sort mappings by hour to enable efficient lookups
        _sortedTimeMappings = new List<SkyboxTimeMapping>(timeMappings);
        _sortedTimeMappings.Sort((a, b) => a.hour.CompareTo(b.hour));
    }

    void Start()
    {
        // Calculate starting time based on startHour and startMinute
        float startTimeInHours = startHour + (startMinute / 60f);
        CurrentTimeOfDay = startTimeInHours / 24f;

        // Immediately update lighting to the correct starting state
        ForceRefreshLighting();
        Debug.Log($"Day/Night cycle starting at {startHour:00}:{startMinute:00}");
    }

    void Update()
    {
        // Advance normalized time (0–1) and wrap around
        CurrentTimeOfDay += Time.deltaTime / dayDurationInSeconds;
        CurrentTimeOfDay %= 1f;

        // Update the lighting and sun position based on the new time
        UpdateLighting();

        // Trigger next‐day once at midnight
        if (CurrentHour == 0 && !lockNextDayTrigger)
        {
            // Assuming you have a TimeManager singleton
            // TimeManager.Instance.TriggerNextDay();
            Debug.Log("A new day has begun!");
            lockNextDayTrigger = true;
        }
        if (CurrentHour != 0)
        {
            lockNextDayTrigger = false;
        }
    }

    private void UpdateLighting()
    {
        // --- LOGIC FIX ---
        // This function now contains the core logic that was split between Update and UpdateSkybox.

        float hours = CurrentTimeOfDay * 24f;
        CurrentHour = Mathf.FloorToInt(hours);

        // Rotate sun
        directionalLight.transform.rotation = Quaternion.Euler((CurrentTimeOfDay * 360f) - 90f, 170f, 0f);

        // Update skybox, fog & light colors
        UpdateColorsAndSkybox(hours);
        
        // Handle weather triggers
        HandleWeatherEvents();
    }
    
    private void UpdateColorsAndSkybox(float hours)
    {
        // 0) If raining, show the rain skybox and skip normal cycle
        if (weatherSystem != null && weatherSystem.isSpecialWeather && weatherSystem.rainSkybox != null)
        {
            RenderSettings.skybox = weatherSystem.rainSkybox;
            // You might want to set specific light/fog colors for rain here too
            return;
        }
        
        // --- LOGIC & BLENDING FIX ---
        // The original script could only find mappings for the *exact* current hour and could not
        // blend over gaps (e.g., from hour 18 to hour 6 the next day). This new logic fixes that.

        // 1) Find the two mappings we are currently between
        SkyboxTimeMapping currentMapping = GetCurrentMapping(hours);
        SkyboxTimeMapping nextMapping = GetNextMapping(hours);

        // 2) Calculate the blend factor between these two mappings
        float blend = 0f;
        float currentMappingHour = currentMapping.hour;
        float nextMappingHour = nextMapping.hour;

        // Handle the wrap-around case for blending (e.g., from 10 PM to 4 AM)
        if (nextMappingHour < currentMappingHour)
        {
            nextMappingHour += 24f; 
            // If current time is also past midnight, adjust it too
            if (hours < currentMappingHour)
            {
                hours += 24f;
            }
        }

        // Calculate blend using InverseLerp for correct interpolation over time
        blend = Mathf.InverseLerp(currentMappingHour, nextMappingHour, hours);

        // 3) Drive the skybox shader transition if applicable
        var mat = currentMapping.skyboxMaterial;
        if (mat != null && mat.shader.name == "Custom/SkyboxTransition")
        {
            mat.SetFloat("_TransitionFactor", blend);
        }
        RenderSettings.skybox = mat; // Note: This will "pop" the skybox material at the hour change.

        // 4) Smoothly interpolate fog & light color in lockstep
        directionalLight.color = Color.Lerp(currentMapping.lightColor, nextMapping.lightColor, blend);
        if (staticLight != null)
            staticLight.color = Color.Lerp(currentMapping.lightColor, nextMapping.lightColor, blend);
        RenderSettings.fogColor = Color.Lerp(currentMapping.fogColor, nextMapping.fogColor, blend);
    }
    
    private SkyboxTimeMapping GetCurrentMapping(float hours)
    {
        // Find the most recent mapping that has passed
        for (int i = _sortedTimeMappings.Count - 1; i >= 0; i--)
        {
            if (hours >= _sortedTimeMappings[i].hour)
            {
                return _sortedTimeMappings[i];
            }
        }
        // If we're before the first mapping (e.g., time is 4:00, first map is 6:00),
        // then the "current" one is the last one from the previous day.
        return _sortedTimeMappings[_sortedTimeMappings.Count - 1];
    }
    
    private SkyboxTimeMapping GetNextMapping(float hours)
    {
        // Find the upcoming mapping
        for (int i = 0; i < _sortedTimeMappings.Count; i++)
        {
            if (hours < _sortedTimeMappings[i].hour)
            {
                return _sortedTimeMappings[i];
            }
        }
        // If we're past the last mapping (e.g., time is 22:00, last map is 20:00),
        // then the "next" one is the first one of the next day.
        return _sortedTimeMappings[0];
    }
    
    private void HandleWeatherEvents()
    {
        if (weatherSystem == null) return;
        
        // This logic remains the same
        if (CurrentHour == 12 && weatherSystem.isSpecialWeather)
        {
            weatherSystem.StartRain();
        }
        if (CurrentHour == 17 && weatherSystem.isSpecialWeather)
        {
            weatherSystem.StopRain();
        }
    }
    
    // --- DEBUG & UTILITY METHODS ---

    [ContextMenu("Force Refresh Lighting")]
    public void ForceRefreshLighting()
    {
        UpdateLighting();
        Debug.Log($"[DayNight] Lighting manually refreshed for time {CurrentTimeOfDay * 24f:F2} (Hour: {CurrentHour})");
    }
    
    public void SetTime(int hour, int minute)
    {
        float timeInHours = hour + (minute / 60f);
        CurrentTimeOfDay = timeInHours / 24f;
        
        Debug.Log($"Time manually set to {hour:00}:{minute:00}");
        
        // Immediately update lighting for the new time
        ForceRefreshLighting();
    }
    
    // Unchanged ContextMenu shortcuts
    [ContextMenu("Set to Dawn (7 AM)")] public void SetToDawn() => SetTime(7, 0);
    [ContextMenu("Set to Morning (9 AM)")] public void SetToMorning() => SetTime(9, 0);
    [ContextMenu("Set to Noon (12 PM)")] public void SetToNoon() => SetTime(12, 0);
}


// --- This class does not need any changes ---
[Serializable]
public class SkyboxTimeMapping
{
    public string phaseName;
    [Tooltip("Hour of day (0–23) when this mapping becomes active")]
    public int hour;
    public Material skyboxMaterial;

    [Header("Lighting & Fog Colors")]
    public Color lightColor = Color.white;
    public Color fogColor = Color.gray;
}