using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherSystem : MonoBehaviour{

    [Range(0f, 1f)]
    public float chanceToRainSpring = 0.3f;
    [Range(0f, 1f)]
    public float chanceToRainSummer = 0.00f;
    
    [Range(0f, 1f)]
    public float chanceToRainFall = 0.4f;

    [Range(0f, 1f)]
    public float chanceToRainWinter = 0.7f;

    public float blendedValue = 0.0f;

    public GameObject rainEffect;
    public Material rainSkybox;
    public Material dayToRainSkybox;

    public DayNightSystem dayNightSystem;

    public bool isSpecialWeather;

    public enum WeatherCondition{

        Sunny,
        Rainy
    }
    private WeatherCondition currentWeather = WeatherCondition.Sunny;
    private void Start(){
        TimeManager.Instance.OnDayPass.AddListener(GenerateRandomWeather);
    }

    private void GenerateRandomWeather(){

        TimeManager.Season currentSeason = TimeManager.Instance.currentSeason;

        float chanceToRain = 0f;

        switch (currentSeason){

            case TimeManager.Season.Spring:
                chanceToRain = chanceToRainSpring;
                break;
            
            case TimeManager.Season.Summer:
                chanceToRain = chanceToRainSummer;
                break;
            
            case TimeManager.Season.Fall:
                chanceToRain = chanceToRainFall;
                break;

            case TimeManager.Season.Winter:
                chanceToRain = chanceToRainWinter;
                break;

        }


        if(UnityEngine.Random.value <= chanceToRain){
            currentWeather = WeatherCondition.Rainy;
            isSpecialWeather = true;

            //Invoke("StartRain", 1f);
        }
        else{
            currentWeather = WeatherCondition.Sunny;
            isSpecialWeather = false;

            //StopRain();
        }
    }

    public void StartRain(){
        //RenderSettings.skybox = rainSkybox;

        rainEffect.SetActive(true);
    }

    public void StopRain(){
        rainEffect.SetActive(false);        
    }
}
