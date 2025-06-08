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
    public Button sellBTN;  // NEW: Sell button
    public Button upgradeBTN;
    public Button exitBTN;

    public GameObject buyPanelUI;
    public GameObject sellPanelUI;  // NEW: Sell panel
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
        
        // NEW: Add sell button listener
        if (sellBTN != null)
            sellBTN.onClick.AddListener(SellMode);
        else
            Debug.LogError("SellBTN is not assigned in the inspector!");
        
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
        sellPanelUI?.SetActive(false);  // NEW: Initialize sell panel
        upgradePanelUI?.SetActive(false);
        
        // Debug panel assignments
        Debug.Log($"BuyPanelUI assigned: {buyPanelUI != null}");
        Debug.Log($"SellPanelUI assigned: {sellPanelUI != null}");  // NEW: Debug sell panel
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
        // Close other panels
        sellPanelUI?.SetActive(false);
        upgradePanelUI?.SetActive(false);
        
        // Open buy panel
        buyPanelUI?.SetActive(true);
        
        HideDialogUI();
        HideDot();
    }

    // NEW: Sell mode method
    private void SellMode()
    {
        Debug.Log("[ShopKeeper] SellMode() called");
        
        // Close other panels
        buyPanelUI?.SetActive(false);
        upgradePanelUI?.SetActive(false);
        
        // Open sell panel
        if (sellPanelUI != null)
        {
            sellPanelUI.SetActive(true);
            Debug.Log($"[ShopKeeper] Sell panel opened. Active: {sellPanelUI.activeSelf}");
            
            // Refresh sell list when panel opens
            if (SellSystem.Instance != null)
            {
                SellSystem.Instance.RefreshSellList();
                Debug.Log("[ShopKeeper] Sell list refreshed");
            }
            else
            {
                Debug.LogError("[ShopKeeper] SellSystem.Instance is null! Cannot refresh sell panel");
            }
        }
        else
        {
            Debug.LogError("[ShopKeeper] SellPanelUI is not assigned in the inspector!");
        }
        
        HideDialogUI();
        HideDot();
    }

    private void UpgradeMode()
    {
        Debug.Log("[ShopKeeper] UpgradeMode() called");
        
        // Close other panels
        buyPanelUI?.SetActive(false);
        sellPanelUI?.SetActive(false);  // NEW: Close sell panel
        
        // Open upgrade panel
        if (upgradePanelUI != null)
        {
            upgradePanelUI.SetActive(true);
            Debug.Log($"[ShopKeeper] Upgrade panel opened. Active: {upgradePanelUI.activeSelf}, ActiveInHierarchy: {upgradePanelUI.activeInHierarchy}");
            
            // ALWAYS refresh materials and display every time the panel opens
            if (ShipUpgradeSystem.Instance != null)
            {
                ShipUpgradeSystem.Instance.OnPanelOpened();
                Debug.Log("[ShopKeeper] Called OnPanelOpened - materials recounted and display refreshed");
            }
            else
            {
                Debug.LogError("[ShopKeeper] ShipUpgradeSystem.Instance is null! Cannot refresh upgrade panel");
            }
        }
        else
        {
            Debug.LogError("[ShopKeeper] UpgradePanelUI is not assigned in the inspector!");
        }
        
        HideDialogUI();
        HideDot();
    }

    public void DialogMode()
    {
        Debug.Log("[ShopKeeper] DialogMode() called");
        
        // Close all panels
        CloseAllPanels();
        
        // Show dialog UI
        DisplayDialogUI();
        ShowDot();
    }

    public void Talk()
    {
        Debug.Log("[ShopKeeper] Talk() called");
        isTalkingWithPlayer = true;
        DisplayDialogUI();
        ShowDot();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StopTalking()
    {
        Debug.Log("[ShopKeeper] StopTalking() called");
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
        sellPanelUI?.SetActive(false);  // NEW: Close sell panel
        upgradePanelUI?.SetActive(false);
        Debug.Log("[ShopKeeper] All panels closed");
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
               (sellPanelUI != null && sellPanelUI.activeSelf) ||  // NEW: Include sell panel
               (upgradePanelUI != null && upgradePanelUI.activeSelf);
    }

    // Method to check if specifically the upgrade panel is open
    public bool IsUpgradePanelOpen()
    {
        return upgradePanelUI != null && upgradePanelUI.activeSelf;
    }

    // NEW: Method to check if sell panel is open
    public bool IsSellPanelOpen()
    {
        return sellPanelUI != null && sellPanelUI.activeSelf;
    }

    // NEW: Method to manually refresh upgrade panel (useful for debugging)
    public void RefreshUpgradePanel()
    {
        if (IsUpgradePanelOpen() && ShipUpgradeSystem.Instance != null)
        {
            ShipUpgradeSystem.Instance.OnPanelOpened();
            Debug.Log("[ShopKeeper] Manual refresh of upgrade panel triggered");
        }
        else
        {
            Debug.LogWarning("[ShopKeeper] Cannot refresh - upgrade panel not open or ShipUpgradeSystem not found");
        }
    }

    // NEW: Method to manually refresh sell panel (useful for debugging)
    public void RefreshSellPanel()
    {
        if (IsSellPanelOpen() && SellSystem.Instance != null)
        {
            Debug.Log("[ShopKeeper] Manual refresh of sell panel triggered");
            // Force update all inventory slots first
            if (InventorySystem.Instance != null)
            {
                foreach (GameObject slot in InventorySystem.Instance.slotList)
                {
                    if (slot != null)
                    {
                        InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
                        if (inventorySlot != null)
                        {
                            inventorySlot.UpdateItemInSlot();
                        }
                    }
                }
            }
            
            SellSystem.Instance.RefreshSellList();
        }
        else
        {
            Debug.LogWarning("[ShopKeeper] Cannot refresh - sell panel not open or SellSystem not found");
        }
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