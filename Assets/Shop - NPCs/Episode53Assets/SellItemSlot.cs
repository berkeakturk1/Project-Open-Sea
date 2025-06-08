// SellItemSlot.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellItemSlot : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemImageUI;
    public TextMeshProUGUI itemNameUI;
    public TextMeshProUGUI itemAmountUI;
    public TextMeshProUGUI sellPriceUI;
    public Button sellOneButton;

    [Header("Data")]
    private InventoryItem inventoryItem;
    private int currentAmount;
    private int actualSellPrice;  // What the player actually gets
    private int displayPrice;     // What's shown in the UI

    private void Start()
    {
        Debug.Log("[SellItemSlot] Start() called");
        
        // Add listeners to buttons
        if (sellOneButton != null)
        {
            sellOneButton.onClick.AddListener(() => {
                Debug.Log("[SellItemSlot] Sell One button clicked");
                SellItem(1);
            });
        }
        else
        {
            Debug.LogError("[SellItemSlot] sellOneButton is null!");
        }
    }

    public void SetupSellSlot(InventoryItem item, int amount, int actualPrice, int uiDisplayPrice)
    {
        Debug.Log($"[SellItemSlot] SetupSellSlot called for {item.thisName}");
        Debug.Log($"  - Amount: {amount}");
        Debug.Log($"  - Actual sell price: {actualPrice}");
        Debug.Log($"  - Display price: {uiDisplayPrice}");
        
        inventoryItem = item;
        currentAmount = amount;
        actualSellPrice = actualPrice;
        displayPrice = uiDisplayPrice;

        UpdateUI();
        Debug.Log($"[SellItemSlot] SetupSellSlot complete for {item.thisName}");
    }
    
    // Backward compatibility overload
    public void SetupSellSlot(InventoryItem item, int amount, int price)
    {
        SetupSellSlot(item, amount, price, price);
    }

    private void UpdateUI()
    {
        if (inventoryItem == null)
        {
            Debug.LogError("[SellItemSlot] UpdateUI called but inventoryItem is null!");
            return;
        }

        Debug.Log($"[SellItemSlot] UpdateUI called for {inventoryItem.thisName}");

        // Set item name
        if (itemNameUI != null)
        {
            itemNameUI.text = inventoryItem.thisName;
            Debug.Log($"[SellItemSlot] Set item name: {inventoryItem.thisName}");
        }
        else
        {
            Debug.LogError("[SellItemSlot] itemNameUI is null!");
        }

        // Set item sprite
        if (itemImageUI != null)
        {
            Image itemImage = inventoryItem.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImageUI.sprite = itemImage.sprite;
                Debug.Log($"[SellItemSlot] Set item sprite");
            }
            else
            {
                Debug.LogWarning($"[SellItemSlot] No Image component found on {inventoryItem.thisName}");
            }
        }
        else
        {
            Debug.LogError("[SellItemSlot] itemImageUI is null!");
        }

        // Set amount
        if (itemAmountUI != null)
        {
            itemAmountUI.text = $"x{currentAmount}";
            Debug.Log($"[SellItemSlot] Set amount: x{currentAmount}");
        }
        else
        {
            Debug.LogError("[SellItemSlot] itemAmountUI is null!");
        }

        // Set sell price
        if (sellPriceUI != null)
        {
            sellPriceUI.text = $"{displayPrice} each";
            Debug.Log($"[SellItemSlot] Set price: {displayPrice} each (actual sell price: {actualSellPrice})");
        }
        else
        {
            Debug.LogError("[SellItemSlot] sellPriceUI is null!");
        }

        // Update button states
        UpdateButtonStates();
        
        Debug.Log($"[SellItemSlot] UpdateUI complete for {inventoryItem.thisName}");
    }

    private void UpdateButtonStates()
    {
        bool hasItems = currentAmount > 0;
        
        if (sellOneButton != null)
        {
            sellOneButton.interactable = hasItems;
            Debug.Log($"[SellItemSlot] Set sellOneButton.interactable = {hasItems} (currentAmount: {currentAmount})");
        }
    }

    public void SellItem(int amountToSell)
    {
        Debug.Log($"[SellItemSlot] === SellItem CALLED ===");
        Debug.Log($"[SellItemSlot] Parameter amountToSell: {amountToSell}");
        Debug.Log($"[SellItemSlot] Current currentAmount: {currentAmount}");
        Debug.Log($"[SellItemSlot] Item name: {(inventoryItem != null ? inventoryItem.thisName : "NULL")}");
        
        if (inventoryItem == null || InventorySystem.Instance == null)
        {
            Debug.LogError("InventoryItem or InventorySystem.Instance is null!");
            return;
        }

        // Double-check the amount in inventory right now
        int inventoryAmount = InventorySystem.Instance.GetItemAmount(inventoryItem.thisName);
        Debug.Log($"[SellItemSlot] Double-check - inventory currently has: {inventoryAmount} of {inventoryItem.thisName}");

        // Ensure we don't sell more than we have
        int originalAmount = amountToSell;
        amountToSell = Mathf.Min(amountToSell, currentAmount);
        amountToSell = Mathf.Min(amountToSell, inventoryAmount); // Also check against actual inventory
        
        Debug.Log($"[SellItemSlot] Amount adjustments:");
        Debug.Log($"  - Original request: {originalAmount}");
        Debug.Log($"  - After min with currentAmount: {Mathf.Min(originalAmount, currentAmount)}");
        Debug.Log($"  - After min with inventoryAmount: {amountToSell}");
        
        if (amountToSell <= 0)
        {
            Debug.LogWarning("No items to sell!");
            return;
        }

        Debug.Log($"[SellItemSlot] Calling RemoveFromInventory('{inventoryItem.thisName}', {amountToSell})");
        
        // Remove items from inventory
        bool success = InventorySystem.Instance.RemoveFromInventory(inventoryItem.thisName, amountToSell);
        
        Debug.Log($"[SellItemSlot] RemoveFromInventory result: {success}");
        
        if (success)
        {
            // Add money to player (use actual sell price, not display price)
            int totalEarned = actualSellPrice * amountToSell;
            InventorySystem.Instance.ModifyCurrency(totalEarned);

            Debug.Log($"Sold {amountToSell} {inventoryItem.thisName} for {totalEarned} currency (price: {actualSellPrice} each)");

            // Update current amount and refresh UI
            currentAmount -= amountToSell;
            
            Debug.Log($"[SellItemSlot] New currentAmount: {currentAmount}");
            
            if (currentAmount <= 0)
            {
                // Disable buttons instead of destroying slot
                Debug.Log($"[SellItemSlot] No items left, disabling buttons");
                currentAmount = 0; // Ensure it's exactly 0
                UpdateUI(); // This will disable the buttons
            }
            else
            {
                UpdateUI();
            }

            // Optionally refresh the entire sell list to ensure accuracy
            if (SellSystem.Instance != null)
            {
                StartCoroutine(RefreshSellListDelayed());
            }
        }
        else
        {
            Debug.LogWarning($"Failed to remove {amountToSell} {inventoryItem.thisName} from inventory!");
        }
    }

    private IEnumerator RefreshSellListDelayed()
    {
        yield return new WaitForEndOfFrame();
        SellSystem.Instance.RefreshSellList();
    }

    void Update()
    {
        // Update current amount from inventory in real-time
        if (inventoryItem != null && InventorySystem.Instance != null)
        {
            int actualAmount = InventorySystem.Instance.GetItemAmount(inventoryItem.thisName);
            
            // Only log if there's a change to reduce spam
            if (actualAmount != currentAmount)
            {
                Debug.Log($"[SellItemSlot] Update check - currentAmount: {currentAmount}, actualAmount: {actualAmount}");
                currentAmount = actualAmount;
                
                Debug.Log($"[SellItemSlot] Amount changed to {currentAmount}");
                
                if (currentAmount <= 0)
                {
                    Debug.Log($"[SellItemSlot] No items left, disabling buttons");
                    currentAmount = 0; // Ensure it's exactly 0
                }
                
                UpdateUI(); // This will update the amount display and button states
            }
        }
    }
}