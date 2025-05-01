using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BuySystem;

public class ShopItemSlot : MonoBehaviour
{
    [Header("UI")]
    public Image itemImageUI;
    public TextMeshProUGUI itemNameUI;
    public TextMeshProUGUI itemPriceUI;
    public Button buyButtonUI;

    [Header("Data")]
    public ShopItemData shopItemData;

    private void Start()
    {
        buyButtonUI.onClick.AddListener(BuyItem);
    }

    void Update()
    { 
        // Only update button state if inventory system exists
        if (InventorySystem.Instance != null)
        {
            // Use the new CanAfford method
            buyButtonUI.interactable = InventorySystem.Instance.CanAfford(shopItemData.itemPrice);
        }
    }

    public void BuyItem()
    {
        // Check for null references
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("InventorySystem.Instance is null!");
            return;
        }
        
        if (shopItemData == null || shopItemData.inventoryItem == null)
        {
            Debug.LogError("ShopItemData or inventoryItem is null!");
            return;
        }

        // Get inventory item component safely
        InventoryItem inventoryItem = shopItemData.inventoryItem.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            Debug.LogError("InventoryItem component not found!");
            return;
        }

        // Double check the player can afford it
        if (!InventorySystem.Instance.CanAfford(shopItemData.itemPrice))
        {
            Debug.LogWarning("Player cannot afford this item!");
            return;
        }

        // Use the ModifyCurrency method to deduct the cost
        InventorySystem.Instance.ModifyCurrency(-shopItemData.itemPrice);

        // Add items into inventory
        InventorySystem.Instance.AddToInventory(inventoryItem.thisName);

        Debug.Log("Bought " + inventoryItem.thisName);
    }
}