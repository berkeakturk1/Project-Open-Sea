using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BreatheBar : MonoBehaviour
{
    public Slider slider;
    public TMP_Text breathCounter;
    public GameObject playerState;

    public float currentBreath, maxBreath;
 
    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        // Get the current breath and max breath from PlayerState
        currentBreath = playerState.GetComponent<PlayerState>().currentBreath;
        maxBreath = playerState.GetComponent<PlayerState>().maxBreath;

        // Make sure max breath is not zero to avoid division by zero
        if (maxBreath <= 0)
        {
            maxBreath = 100f; // Fallback to default value
        }

        // Calculate fill value, clamping between 0 and 1
        float fillValue = Mathf.Clamp01(currentBreath / maxBreath);
        slider.value = fillValue;

        // Update the text display
        breathCounter.text = Mathf.RoundToInt(currentBreath) + "/" + Mathf.RoundToInt(maxBreath);
    }
}