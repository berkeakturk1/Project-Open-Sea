using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ShipUpgradeSystem;

public class ShipUpgradeSlot : MonoBehaviour
{
    [Header("UI")]
    public Image upgradeImageUI;
    public TextMeshProUGUI upgradeNameUI;
    public TextMeshProUGUI upgradeDescriptionUI;
    public TextMeshProUGUI upgradePriceUI;
    public Button purchaseButton;

    [Header("Default Upgrade Icon")]
    public Sprite defaultUpgradeIcon; // Assign a single icon for all upgrades

    [Header("Data")]
    public ShipUpgradeData shipUpgradeData;

    private void Start()
    {
        purchaseButton.onClick.AddListener(PurchaseUpgrade);
        
        // Set the default upgrade icon if assigned
        if (upgradeImageUI != null && defaultUpgradeIcon != null)
        {
            upgradeImageUI.sprite = defaultUpgradeIcon;
        }
    }

    void Update()
    { 
        // Only update button state if upgrade system and inventory system exist
        if (ShipUpgradeSystem.Instance != null && InventorySystem.Instance != null && shipUpgradeData != null)
        {
            // Calculate current price for this upgrade
            int currentPrice = CalculateUpgradePrice(shipUpgradeData);
            
            // Check if player can afford it and if upgrade is available
            bool canAfford = InventorySystem.Instance.CanAfford(currentPrice);
            bool isAvailable = IsUpgradeAvailable();
            
            purchaseButton.interactable = canAfford && isAvailable;
            
            // Update price display in case it changed (for repeatable upgrades)
            if (upgradePriceUI != null)
            {
                upgradePriceUI.text = $"{currentPrice} Gold";
            }
        }
    }

    private bool IsUpgradeAvailable()
    {
        if (shipUpgradeData == null) return false;
        
        // Check if it's a non-repeatable upgrade that's already purchased
        if (!shipUpgradeData.isRepeatable && shipUpgradeData.purchased)
            return false;
            
        // Check if it's a repeatable upgrade that's at max level
        if (shipUpgradeData.isRepeatable && shipUpgradeData.currentLevel >= shipUpgradeData.maxLevel)
            return false;
            
        return true;
    }

    private int CalculateUpgradePrice(ShipUpgradeData upgrade)
    {
        if (upgrade.isRepeatable)
        {
            return Mathf.RoundToInt(upgrade.upgradePrice * Mathf.Pow(1.5f, upgrade.currentLevel));
        }
        return upgrade.upgradePrice;
    }

    public void PurchaseUpgrade()
    {
        // Check for null references
        if (ShipUpgradeSystem.Instance == null)
        {
            Debug.LogError("ShipUpgradeSystem.Instance is null!");
            return;
        }
        
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("InventorySystem.Instance is null!");
            return;
        }
        
        if (shipUpgradeData == null)
        {
            Debug.LogError("ShipUpgradeData is null!");
            return;
        }

        // Calculate current price
        int currentPrice = CalculateUpgradePrice(shipUpgradeData);

        // Check if upgrade is available
        if (!IsUpgradeAvailable())
        {
            Debug.LogWarning($"Upgrade {shipUpgradeData.upgradeName} is not available!");
            return;
        }

        // Double check the player can afford it
        if (!InventorySystem.Instance.CanAfford(currentPrice))
        {
            Debug.LogWarning($"Player cannot afford {shipUpgradeData.upgradeName}! Cost: {currentPrice}");
            return;
        }

        // Use the ModifyCurrency method to deduct the cost
        InventorySystem.Instance.ModifyCurrency(-currentPrice);

        // Let the upgrade system handle the actual upgrade application
        ShipUpgradeSystem.Instance.PurchaseUpgrade(shipUpgradeData);

        Debug.Log($"Purchased upgrade: {shipUpgradeData.upgradeName} for {currentPrice} gold");
        
        // Optional: Add visual feedback
        ShowPurchaseEffect();
    }

    private void ShowPurchaseEffect()
    {
        // Optional: Add visual feedback when upgrade is purchased
        // You can add particle effects, sound, button animation, etc.
        
        // Example: Brief button color change
        StartCoroutine(PurchaseFlashEffect());
    }

    private IEnumerator PurchaseFlashEffect()
    {
        if (purchaseButton != null)
        {
            ColorBlock colors = purchaseButton.colors;
            Color originalColor = colors.normalColor;
            
            // Flash green briefly
            colors.normalColor = Color.green;
            purchaseButton.colors = colors;
            
            yield return new WaitForSeconds(0.2f);
            
            // Return to original color
            colors.normalColor = originalColor;
            purchaseButton.colors = colors;
        }
    }

    // Method to refresh the display (useful when upgrade levels change)
    public void RefreshDisplay()
    {
        if (shipUpgradeData == null) return;

        // Set the upgrade icon
        if (upgradeImageUI != null && defaultUpgradeIcon != null)
        {
            upgradeImageUI.sprite = defaultUpgradeIcon;
        }

        // Update name with level info if repeatable
        if (upgradeNameUI != null)
        {
            string displayName = shipUpgradeData.upgradeName;
            if (shipUpgradeData.isRepeatable && shipUpgradeData.currentLevel > 0)
            {
                displayName += $" (Level {shipUpgradeData.currentLevel + 1})";
            }
            upgradeNameUI.text = displayName;
        }

        // Update description
        if (upgradeDescriptionUI != null)
        {
            upgradeDescriptionUI.text = shipUpgradeData.upgradeDescription;
        }

        // Update price
        if (upgradePriceUI != null)
        {
            int currentPrice = CalculateUpgradePrice(shipUpgradeData);
            upgradePriceUI.text = $"{currentPrice} Gold";
        }
    }
}