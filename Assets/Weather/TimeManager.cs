using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour{

    public static TimeManager Instance {get; set;}
    public UnityEvent OnDayPass = new UnityEvent();

    public enum Season{

        Spring,
        Summer,
        Fall,
        Winter,
    }

    public Season currentSeason = Season.Spring;
    private int daysPerSeason = 1;
    private int daysInCurrentSeason = 1;
    private void Awake(){

        if(Instance != null && Instance != this){
            Destroy(gameObject);
        }
        else{
            Instance = this;
        }
    }

    public int dayInGame = 1;

    public void TriggerNextDay(){

        dayInGame += 1;
        daysInCurrentSeason += 1;

        if(daysInCurrentSeason > daysPerSeason){
            daysInCurrentSeason = 1;
            currentSeason = GetNextSeason();
        }

        OnDayPass.Invoke();
    }

    private Season GetNextSeason(){
        int currentSeasonIndex = (int)currentSeason;

        int nextSeasonIndex = (currentSeasonIndex + 1) % 4;

        return (Season)nextSeasonIndex;
    }
}
