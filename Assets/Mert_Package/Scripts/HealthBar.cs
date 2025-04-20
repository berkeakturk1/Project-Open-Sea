using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public TMP_Text healthCounter;
    public GameObject playerState;

    public float currentHealth, maxHealth;
 
    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        // Check if playerState still exists before accessing it
        if (playerState != null)
        {
            PlayerState playerStateComponent = playerState.GetComponent<PlayerState>();
            
            // Make sure the component exists too
            if (playerStateComponent != null)
            {
                currentHealth = playerStateComponent.currentHealth;
                maxHealth = playerStateComponent.maxHealth;

                float fillValue = currentHealth / maxHealth;
                slider.value = fillValue;

                healthCounter.text = currentHealth + "/" + maxHealth;
            }
        }
        // Optional: Handle the case when playerState is destroyed
        else
        {
            Debug.LogWarning("PlayerState reference is null in HealthBar");
            // You could choose to disable this component, destroy the gameObject, or take other actions
            // this.enabled = false; // Uncomment to disable this script when playerState is null
        }
    }
}