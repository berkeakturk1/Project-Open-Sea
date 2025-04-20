using System;
using System.Collections.Generic;
using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    [Header("Primary Directional Light")]
    public Light directionalLight;

    [Header("Day Duration Settings")]
    [Tooltip("In seconds, how long a full 24h cycle takes.")]
    public float dayDurationInSeconds = 24f;

    public int currentHour;
    [Range(0f, 1f)] public float currentTimeOfDay = 0.35f;

    private float blendedValue = 0f;
    private bool lockNextDayTrigger = false;

    [Header("Skybox & Lighting Mappings by Hour")]
    public List<SkyboxTimeMapping> timeMappings;

    [Header("External Systems")]
    public WeatherSystem weatherSystem;

    void Update()
    {
        // Advance time
        currentTimeOfDay += Time.deltaTime / dayDurationInSeconds;
        currentTimeOfDay %= 1f;

        // Calculate current hour
        currentHour = Mathf.FloorToInt(currentTimeOfDay * 24f);

        // Rotate sun
        float sunAngle = (currentTimeOfDay * 360f) - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Update environment
        UpdateEnvironment();

        // Trigger next-day event
        if (currentHour == 0 && !lockNextDayTrigger)
        {
            TimeManager.Instance.TriggerNextDay();
            lockNextDayTrigger = true;
        }
        else if (currentHour != 0)
        {
            lockNextDayTrigger = false;
        }
    }

    private void UpdateEnvironment()
    {
        if (timeMappings == null || timeMappings.Count == 0)
            return;

        // Determine previous and next mapping indices around currentTimeOfDay
        float totalHours = 24f;
        // Sort mappings if not already
        timeMappings.Sort((a, b) => a.hour.CompareTo(b.hour));

        SkyboxTimeMapping prev = timeMappings[0];
        SkyboxTimeMapping next = timeMappings[0];
        for (int i = 0; i < timeMappings.Count; i++)
        {
            var m = timeMappings[i];
            float mTime = m.hour / totalHours;
            if (currentTimeOfDay >= mTime)
            {
                prev = m;
                next = (i + 1 < timeMappings.Count) ? timeMappings[i + 1] : timeMappings[0];
            }
        }

        // Compute interpolation factor between prev.hour and next.hour
        float prevTime = prev.hour / totalHours;
        float nextTime = next.hour / totalHours;
        float lerpT;
        if (nextTime > prevTime)
            lerpT = Mathf.InverseLerp(prevTime, nextTime, currentTimeOfDay);
        else
        {
            // wrap past midnight
            float wrapped = (currentTimeOfDay < prevTime) ? currentTimeOfDay + 1f : currentTimeOfDay;
            lerpT = Mathf.InverseLerp(prevTime, prevTime + (1f - prevTime + nextTime), wrapped);
        }

        // Skybox mapping: choose mapping whose hour is nearest below currentHour
        //SkyboxTimeMapping currentMap = timeMappings.Find(m => m.hour == currentHour) ?? prev;
        Material currentSkybox = timeMappings[0].skyboxMaterial;
        
        foreach(SkyboxTimeMapping mapping in timeMappings){

            if(currentHour == mapping.hour){
                if(currentHour == 12 && weatherSystem.isSpecialWeather == false) continue;

                if(currentHour == 12 && weatherSystem.isSpecialWeather == true) weatherSystem.StartRain();

                if(currentHour == 15 && weatherSystem.isSpecialWeather == false) continue;

                if(currentHour == 16 && weatherSystem.isSpecialWeather == true) continue;

                if(currentHour == 17 && weatherSystem.isSpecialWeather == true) weatherSystem.StopRain();

                currentSkybox = mapping.skyboxMaterial;

                if(currentSkybox.shader != null){
                    if(currentSkybox.shader.name == "Custom/SkyboxTransition"){
                        blendedValue += Time.deltaTime;
                        blendedValue = Mathf.Clamp01(blendedValue);

                        currentSkybox.SetFloat("_TransitionFactor", blendedValue);
                    }
                    else blendedValue = 0;
                }

                break;
            }
        }

        if(currentSkybox != null){
            RenderSettings.skybox = currentSkybox;
        }
        
        
        // Weather conditionals
        if (currentHour == 12 && !weatherSystem.isSpecialWeather) return;
        if (currentHour == 12 && weatherSystem.isSpecialWeather) weatherSystem.StartRain();
        if (currentHour == 15 && !weatherSystem.isSpecialWeather) return;
        if (currentHour == 16 && weatherSystem.isSpecialWeather) return;
        if (currentHour == 17 && weatherSystem.isSpecialWeather) weatherSystem.StopRain();

        // Skybox transition
        Material skyMat = currentMap.skyboxMaterial;
        if (skyMat != null && skyMat.shader != null && skyMat.shader.name == "Custom/SkyboxTransition")
        {
            blendedValue = Mathf.Clamp01(blendedValue + Time.deltaTime);
            skyMat.SetFloat("_TransitionFactor", blendedValue);
        }
        else
        {
            blendedValue = 0f;
        }
        RenderSettings.skybox = skyMat;

        // Directional light override
        if (currentMap.overrideLightSettings)
        {
            directionalLight.color = currentMap.lightColor;
            directionalLight.intensity = currentMap.lightIntensity;
        }

        // Smooth fog color transition
        Color fogPrev = prev.lightColor;
        Color fogNext = next.lightColor;
        RenderSettings.fogColor = Color.Lerp(fogPrev, fogNext, lerpT);
    }
}

[Serializable]
public class SkyboxTimeMapping
{
    [Tooltip("Phase name (e.g. Dawn, Noon)")]
    public string phaseName;

    [Tooltip("Hour (0–23) when mapping applies")]
    [Range(0, 23)] public int hour;

    [Tooltip("Skybox material at this hour")]
    public Material skyboxMaterial;

    [Header("Optional Light Override")]
    public bool overrideLightSettings = false;
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;
}
