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
    public int startHour = 8; // Default to 8:00 AM
    
    [Tooltip("What minute (0-59) should the day start at?")]
    [Range(0, 59)]
    public int startMinute = 0;

    [Header("Skybox & Lighting Mappings by Hour")]
    public List<SkyboxTimeMapping> timeMappings;

    [Header("External Systems")]
    public WeatherSystem weatherSystem;

    [Range(0f, 1f)]
    private float currentTimeOfDay;
    public int currentHour;

    private bool lockNextDayTrigger = false;

    void Start()
    {
        // Calculate starting time based on startHour and startMinute
        float startTimeInHours = startHour + (startMinute / 60f);
        currentTimeOfDay = startTimeInHours / 24f;
        
        // Calculate current hour immediately
        float hours = currentTimeOfDay * 24f;
        currentHour = Mathf.FloorToInt(hours);
        
        Debug.Log($"Day/Night cycle starting at {startHour:00}:{startMinute:00} (hour: {currentHour}, normalized time: {currentTimeOfDay:F3})");
        
        // CRITICAL: Initialize the lighting immediately on start
        InitializeLighting(hours);
    }
    
    void InitializeLighting(float hours)
    {
        // Set initial sun rotation
        directionalLight.transform.rotation =
            Quaternion.Euler((currentTimeOfDay * 360f) - 90f, 170f, 0f);
        
        // Set initial skybox, fog & light colors
        UpdateSkybox(hours);
        
        Debug.Log($"[DayNight] Lighting initialized for hour {currentHour}");
    }

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
        if (weatherSystem != null && weatherSystem.isSpecialWeather && weatherSystem.rainSkybox != null)
        {
            RenderSettings.skybox = weatherSystem.rainSkybox;
            return;
        }

        // 1) Find the mapping for this exact hour
        SkyboxTimeMapping curr = null;
        foreach (var m in timeMappings)
        {
            if (m.hour == currentHour)
            {
                curr = m;
                break;
            }
        }
        
        // Handle weather triggers separately (don't skip mappings)
        if (weatherSystem != null)
        {
            if (currentHour == 12 && weatherSystem.isSpecialWeather) 
            {
                weatherSystem.StartRain();
            }
            if (currentHour == 17 && weatherSystem.isSpecialWeather) 
            {
                weatherSystem.StopRain();
            }
        }
        
        if (curr == null) 
        {
            Debug.LogWarning($"[DayNight] No mapping found for hour {currentHour}! Add a SkyboxTimeMapping for this hour.");
            return;
        }

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
        if (staticLight != null)
            staticLight.color = Color.Lerp(curr.lightColor, nxt.lightColor, blend);
        RenderSettings.fogColor = Color.Lerp(curr.fogColor, nxt.fogColor, blend);
    }
    
    // Debug methods
    [ContextMenu("Set to Dawn (7 AM)")]
    public void SetToDawn()
    {
        SetTime(7, 0);
    }
    
    [ContextMenu("Set to Morning (9 AM)")]
    public void SetToMorning()
    {
        SetTime(9, 0);
    }
    
    [ContextMenu("Set to Noon (12 PM)")]
    public void SetToNoon()
    {
        SetTime(12, 0);
    }
    
    [ContextMenu("Force Refresh Lighting")]
    public void ForceRefreshLighting()
    {
        float hours = currentTimeOfDay * 24f;
        currentHour = Mathf.FloorToInt(hours);
        InitializeLighting(hours);
    }
    
    public void SetTime(int hour, int minute)
    {
        float timeInHours = hour + (minute / 60f);
        currentTimeOfDay = timeInHours / 24f;
        
        float hours = currentTimeOfDay * 24f;
        currentHour = Mathf.FloorToInt(hours);
        
        Debug.Log($"Time manually set to {hour:00}:{minute:00}");
        
        // Immediately update lighting for the new time
        InitializeLighting(hours);
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
