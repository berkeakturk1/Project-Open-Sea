using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreatheBar : MonoBehaviour
{
     public Slider slider;
    //public Text healthCounter;
    public TMP_Text breathCounter;

    public GameObject playerState;

    public float currentBreath,maxBreath;
 
    void Awake()
    {
        slider = GetComponent<Slider>();
    }

 
    void Update()
    {
        currentBreath = playerState.GetComponent<PlayerState>().currentBreath;
        maxBreath  = playerState.GetComponent<PlayerState>().maxBreath;

        float fillValue = currentBreath / maxBreath;
        slider.value = fillValue;

        breathCounter.text = currentBreath +"/"+ maxBreath; //100/100

    }
}
