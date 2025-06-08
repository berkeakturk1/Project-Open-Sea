// SellSystem.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SellSystem : MonoBehaviour
{
    #region || -- Singleton -- ||
    public static SellSystem Instance { get; set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    #endregion

    [Header("ShopKeeper")]
    public ShopKeeper ShopKeeper;

    [Header("UI")]
    public Transform contentTransform;
    public GameObject sellItemPrefab;
    public Button backButton;

    [Header("Sell Configuration")]
    [Range(0.1f, 1.0f)]
    public float sellPriceMultiplier = 0.5f; // Players get 50% of original price when selling
    
    [Header("Price Display Options")]
    [Tooltip("If true, shows the base price. If false, shows the actual price after multiplier")]
    public bool showBasePriceInUI = true;

    [Header("Current Sellable Items")]
    public List<SellItemData> sellableItems;

    void Start()
    {
        Debug.Log("[SellSystem] Start() called");
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(ExitSellMode);
            Debug.Log("[SellSystem] Back button listener added");
        }
        else
        {
            Debug.LogError("[SellSystem] backButton is null!");
        }

        // Debug configuration
        Debug.Log($"[SellSystem] Configuration check:");
        Debug.Log($"  - ShopKeeper assigned: {ShopKeeper != null}");
        Debug.Log($"  - contentTransform assigned: {contentTransform != null}");
        Debug.Log($"  - sellItemPrefab assigned: {sellItemPrefab != null}");
        Debug.Log($"  - sellPriceMultiplier: {sellPriceMultiplier}");
        Debug.Log($"  - sellableItems count: {sellableItems.Count}");
        
        for (int i = 0; i < sellableItems.Count; i++)
        {
            var item = sellableItems[i];
            Debug.Log($"  - Sellable item {i}: {(item.inventoryItem != null ? item.inventoryItem.name : "NULL")} - Price: {item.baseSellPrice}");
        }
    }

    void OnEnable()
    {
        // Refresh the sell list whenever the panel becomes active
        Debug.Log("[SellSystem] OnEnable called - starting delayed refresh");
        StartCoroutine(RefreshSellListDelayed());
    }
    
    private System.Collections.IEnumerator RefreshSellListDelayed()
    {
        // Wait a frame to ensure all inventory slots are properly updated
        yield return new WaitForEndOfFrame();
        
        // Force update all inventory slots first
        if (InventorySystem.Instance != null)
        {
            Debug.Log("[SellSystem] Forcing inventory slot updates");
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
        
        // Wait another frame
        yield return new WaitForEndOfFrame();
        
        // Now refresh the sell list
        RefreshSellList();
    }

    public void RefreshSellList()
    {
        Debug.Log("[SellSystem] RefreshSellList() called");
        
        // Clear existing items in the sell panel
        ClearSellList();

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[SellSystem] InventorySystem.Instance is null!");
            return;
        }

        Debug.Log($"[SellSystem] InventorySystem found. Checking {sellableItems.Count} sellable items");

        // Debug: Show all items in player's inventory
        var allItems = InventorySystem.Instance.GetAllInventoryItems();
        Debug.Log($"[SellSystem] Player has {allItems.Count} different items in inventory:");
        foreach (var item in allItems)
        {
            Debug.Log($"[SellSystem] Inventory contains: {item.itemName} x{item.amount}");
        }

        int slotsCreated = 0;

        // Get all items from player's inventory that can be sold
        foreach (SellItemData sellData in sellableItems)
        {
            Debug.Log($"[SellSystem] Checking sellable item {sellableItems.IndexOf(sellData)}");
            
            if (sellData.inventoryItem == null)
            {
                Debug.LogWarning($"[SellSystem] SellItemData {sellableItems.IndexOf(sellData)} has null inventoryItem!");
                continue;
            }

            InventoryItem inventoryItem = sellData.inventoryItem.GetComponent<InventoryItem>();
            if (inventoryItem == null)
            {
                Debug.LogWarning($"[SellSystem] SellItemData {sellableItems.IndexOf(sellData)} inventoryItem has no InventoryItem component!");
                continue;
            }

            Debug.Log($"[SellSystem] Checking for item: {inventoryItem.thisName}");

            // Check if player has this item in inventory
            int amountInInventory = InventorySystem.Instance.GetItemAmount(inventoryItem.thisName);
            Debug.Log($"[SellSystem] Player has {amountInInventory} of {inventoryItem.thisName}");
            
            if (amountInInventory > 0)
            {
                Debug.Log($"[SellSystem] Creating sell slot for {inventoryItem.thisName}");
                // Create sell slot for this item
                CreateSellSlot(sellData, inventoryItem, amountInInventory);
                slotsCreated++;
            }
            else
            {
                Debug.Log($"[SellSystem] Player doesn't have {inventoryItem.thisName}, skipping");
            }
        }

        Debug.Log($"[SellSystem] RefreshSellList complete. Created {slotsCreated} sell slots");
        
        if (slotsCreated == 0)
        {
            Debug.LogWarning("[SellSystem] No sell slots were created! Check:");
            Debug.LogWarning("1. Do you have sellable items configured in the SellSystem?");
            Debug.LogWarning("2. Do those items exist in the player's inventory?");
            Debug.LogWarning("3. Do the item names match exactly?");
        }
    }

    private void CreateSellSlot(SellItemData sellData, InventoryItem inventoryItem, int amountInInventory)
    {
        Debug.Log($"[SellSystem] CreateSellSlot called for {inventoryItem.thisName}");
        
        if (sellItemPrefab == null)
        {
            Debug.LogError("[SellSystem] sellItemPrefab is null! Cannot create sell slot");
            return;
        }

        if (contentTransform == null)
        {
            Debug.LogError("[SellSystem] contentTransform is null! Cannot create sell slot");
            return;
        }

        GameObject prefab = Instantiate(sellItemPrefab, contentTransform);
        Debug.Log($"[SellSystem] Instantiated sell slot prefab: {prefab.name}");
        
        SellItemSlot sellItemSlot = prefab.GetComponent<SellItemSlot>();

        if (sellItemSlot != null)
        {
            // Calculate actual sell price (what player gets)
            int actualSellPrice = Mathf.RoundToInt(sellData.baseSellPrice * sellPriceMultiplier);
            
            // Calculate display price (what to show in UI)
            int displayPrice = showBasePriceInUI ? sellData.baseSellPrice : actualSellPrice;
            
            Debug.Log($"[SellSystem] Price calculation:");
            Debug.Log($"  - Base price: {sellData.baseSellPrice}");
            Debug.Log($"  - Multiplier: {sellPriceMultiplier}");
            Debug.Log($"  - Actual sell price: {actualSellPrice}");
            Debug.Log($"  - Display price: {displayPrice}");
            Debug.Log($"  - Show base price in UI: {showBasePriceInUI}");
            
            // Set up the sell slot with actual price for selling, display price for UI
            sellItemSlot.SetupSellSlot(inventoryItem, amountInInventory, actualSellPrice, displayPrice);
            Debug.Log($"[SellSystem] Successfully set up sell slot for {inventoryItem.thisName}");
        }
        else
        {
            Debug.LogError($"[SellSystem] SellItemSlot component not found on prefab {prefab.name}!");
            Destroy(prefab);
        }
    }

    private void ClearSellList()
    {
        Debug.Log($"[SellSystem] ClearSellList called. Content has {contentTransform.childCount} children");
        
        // Destroy all existing sell slot items
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = contentTransform.GetChild(i);
            Debug.Log($"[SellSystem] Destroying child: {child.name}");
            Destroy(child.gameObject);
        }
        
        Debug.Log("[SellSystem] ClearSellList complete");
    }

    public void ExitSellMode()
    {
        ShopKeeper.DialogMode();
    }

    [System.Serializable]
    public class SellItemData
    {
        public GameObject inventoryItem;
        public int baseSellPrice; // Base price before multiplier
    }
}

