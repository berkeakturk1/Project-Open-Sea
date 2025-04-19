using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{

    public Slider slider;
    //public Text healthCounter;
    public TMP_Text healthCounter;

    public GameObject playerState;

    public float currentHealth,maxHealth;
 
    void Awake()
    {
        slider = GetComponent<Slider>();
    }

 
    void Update()
    {
        currentHealth = playerState.GetComponent<PlayerState>().currentHealth;
        maxHealth  = playerState.GetComponent<PlayerState>().maxHealth;

        float fillValue = currentHealth / maxHealth;
        slider.value = fillValue;

        healthCounter.text = currentHealth +"/"+ maxHealth; //100/100

    }
}
