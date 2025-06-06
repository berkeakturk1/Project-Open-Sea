using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper Instance;

    [Header("Player Interaction")]
    public bool playerInRange;
    public bool isTalkingWithPlayer;

    [Header("UI References")]
    public GameObject shopkeeperDialogUI;
    public Button buyBTN;
    public Button upgradeBTN;
    public Button exitBTN;

    public GameObject buyPanelUI;
    public GameObject upgradePanelUI;

    public GameObject dot; 
    public TextMeshProUGUI interaction_Info_UI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        if (shopkeeperDialogUI != null)
            shopkeeperDialogUI.SetActive(false);

        if (buyBTN != null)
            buyBTN.onClick.AddListener(BuyMode);
        if (upgradeBTN != null)
        {
            upgradeBTN.onClick.AddListener(UpgradeMode);
            Debug.Log("Upgrade button listener added");
        }
        else
        {
            Debug.LogError("UpgradeBTN is not assigned in the inspector!");
        }
        
        if (exitBTN != null)
            exitBTN.onClick.AddListener(StopTalking);

        if (interaction_Info_UI != null)
            interaction_Info_UI.gameObject.SetActive(false);

        // Initialize all panels as inactive
        buyPanelUI?.SetActive(false);
        upgradePanelUI?.SetActive(false);
        
        // Debug panel assignments
        Debug.Log($"BuyPanelUI assigned: {buyPanelUI != null}");
        Debug.Log($"UpgradePanelUI assigned: {upgradePanelUI != null}");
    }

    private void Update()
    {
        if (playerInRange && !isTalkingWithPlayer && Input.GetKeyDown(KeyCode.F))
        {
            Talk();
        }
        
        // Optional: Allow ESC to close any open panels
        if (isTalkingWithPlayer && Input.GetKeyDown(KeyCode.Escape))
        {
            StopTalking();
        }
    }

    private void BuyMode()
    {
        // Close upgrade panel
        upgradePanelUI?.SetActive(false);
        
        // Open buy panel
        buyPanelUI?.SetActive(true);
        
        HideDialogUI();
        HideDot();
    }

    private void UpgradeMode()
    {
        Debug.Log("UpgradeMode() called");
        
        // Close buy panel
        if (buyPanelUI != null)
        {
            buyPanelUI.SetActive(false);
            Debug.Log("Buy panel closed");
        }
        
        // Open upgrade panel
        if (upgradePanelUI != null)
        {
            upgradePanelUI.SetActive(true);
            Debug.Log($"Upgrade panel opened. Active: {upgradePanelUI.activeSelf}, ActiveInHierarchy: {upgradePanelUI.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("UpgradePanelUI is not assigned in the inspector!");
        }
        
        HideDialogUI();
        HideDot();
    }

    public void DialogMode()
    {
        // Close all panels
        CloseAllPanels();
        
        // Show dialog UI
        DisplayDialogUI();
        ShowDot();
    }

    public void Talk()
    {
        isTalkingWithPlayer = true;
        DisplayDialogUI();
        ShowDot();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StopTalking()
    {
        isTalkingWithPlayer = false;
        
        // Close all UI elements
        HideDialogUI();
        CloseAllPanels();
        ShowDot();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CloseAllPanels()
    {
        buyPanelUI?.SetActive(false);
        upgradePanelUI?.SetActive(false);
    }

    private void DisplayDialogUI()
    {
        shopkeeperDialogUI?.SetActive(true);
    }

    private void HideDialogUI()
    {
        shopkeeperDialogUI?.SetActive(false);
    }

    private void ShowDot()
    {
        if (dot != null)
            dot.SetActive(true);
    }

    private void HideDot()
    {
        if (dot != null)
            dot.SetActive(false);
    }

    // Method to check if any panel is currently open
    public bool IsAnyPanelOpen()
    {
        return (buyPanelUI != null && buyPanelUI.activeSelf) ||
               (upgradePanelUI != null && upgradePanelUI.activeSelf);
    }

    // Method to check if specifically the upgrade panel is open
    public bool IsUpgradePanelOpen()
    {
        return upgradePanelUI != null && upgradePanelUI.activeSelf;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (interaction_Info_UI != null)
            {
                interaction_Info_UI.text = "Press [F] to Talk";
                interaction_Info_UI.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (isTalkingWithPlayer)
            {
                StopTalking();
            }

            if (interaction_Info_UI != null)
            {
                interaction_Info_UI.gameObject.SetActive(false);
            }
        }
    }
}