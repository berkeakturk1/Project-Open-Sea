using UnityEngine;
using TMPro;
using UnityEngine.UI; // For Button component

public class SeedInputManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField seedInputField; // Reference to the TMP InputField
    [SerializeField] private Button confirmButton; // Reference to the Confirm Button
    [SerializeField] private WFCController wfcController; // Reference to the WFCController

    private void Start()
    {
        // Add a listener to the confirm button to call OnConfirm when clicked
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
        }

        // Optionally, set a default seed in the input field
        if (seedInputField != null && wfcController != null)
        {
            
        }
    }

    private void OnConfirm()
    {
        if (seedInputField != null && wfcController != null)
        {
            if (int.TryParse(seedInputField.text, out int newSeed))
            {
                wfcController.SetSeed(newSeed);
                wfcController.Test();
                Debug.Log($"New seed applied: {newSeed}");
            }
            else
            {
                int seed = wfcController.GetSeed();
                 wfcController.SetSeed(++seed);
                
                seedInputField.text = seed.ToString();
                
                wfcController.Test();
            }
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
        }
    }
}