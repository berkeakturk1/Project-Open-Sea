using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipUpgradeSystem : MonoBehaviour
{
    #region || -- Singleton -- ||
    public static ShipUpgradeSystem Instance { get; set; }
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

    [Header("ShipKeeper/Dock Master")]
    public ShopKeeper shipUpgradeKeeper;

    [Header("UI")]
    public Transform contentTransform;
    public GameObject upgradeItemPrefab;
    public Button backButton;

    [Header("Ship Reference")]
    public ShipController shipController;

    [Header("Current Upgrade List")]
    public List<ShipUpgradeData> currentUpgradeList;

    [Header("Player Resources")]
    public TextMeshProUGUI goldDisplay;
    
    // Property to get upgrade materials from inventory
    public int PlayerGold
    {
        get
        {
            return GetUpgradeMaterialCount();
        }
    }

    void Start()
    {
        backButton.onClick.AddListener(ExitUpgradeMode);
        
        // Initialize the upgrade list with default upgrades
        if (currentUpgradeList.Count == 0)
        {
            InitializeDefaultUpgrades();
        }

        // Only initialize if prefab is assigned
        if (upgradeItemPrefab != null)
        {
            InitializeUpgradeList(currentUpgradeList);
        }
        else
        {
            Debug.LogWarning("UpgradeItemPrefab not assigned! Please create and assign the upgrade item prefab.");
        }
        
        UpdateGoldDisplay();
    }

    // NEW: Method to refresh everything when panel opens
    public void OnPanelOpened()
    {
        Debug.Log("[ShipUpgradeSystem] Panel opened - refreshing display");
        
        // Force refresh inventory slots
        RefreshInventorySlots();
        
        // Update the gold display
        UpdateGoldDisplay();
        
        // Refresh all upgrade slots (button states, prices, etc.)
        RefreshAllUpgradeSlots();
        
        Debug.Log($"[ShipUpgradeSystem] Panel refresh complete - Current materials: {PlayerGold}");
    }

    // NEW: Force all inventory slots to update
    private void RefreshInventorySlots()
    {
        if (InventorySystem.Instance == null) return;

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

    // NEW: Refresh all upgrade slot displays without recreating them
    private void RefreshAllUpgradeSlots()
    {
        foreach (Transform child in contentTransform)
        {
            ShipUpgradeSlot upgradeSlot = child.GetComponent<ShipUpgradeSlot>();
            if (upgradeSlot != null)
            {
                upgradeSlot.RefreshDisplay();
                
                // Update button interactability
                if (upgradeSlot.shipUpgradeData != null)
                {
                    int currentPrice = CalculateUpgradePrice(upgradeSlot.shipUpgradeData);
                    bool canAfford = PlayerGold >= currentPrice;
                    bool isAvailable = IsUpgradeAvailable(upgradeSlot.shipUpgradeData);
                    
                    upgradeSlot.purchaseButton.interactable = canAfford && isAvailable;
                }
            }
        }
    }

    // NEW: Helper method to check if upgrade is available
    private bool IsUpgradeAvailable(ShipUpgradeData upgrade)
    {
        if (upgrade == null) return false;
        
        // Check if it's a non-repeatable upgrade that's already purchased
        if (!upgrade.isRepeatable && upgrade.purchased)
            return false;
            
        // Check if it's a repeatable upgrade that's at max level
        if (upgrade.isRepeatable && upgrade.currentLevel >= upgrade.maxLevel)
            return false;
            
        return true;
    }

    private void InitializeDefaultUpgrades()
    {
        // Speed and Movement Upgrades
        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Reinforced Paddles",
            upgradeDescription = "Increase paddling speed by 50%",
            upgradePrice = 1,
            upgradeType = UpgradeType.PaddlingSpeed,
            upgradeValue = 1.5f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Improved Sails",
            upgradeDescription = "Increase wind sailing speed by 25%",
            upgradePrice = 1,
            upgradeType = UpgradeType.WindSpeed,
            upgradeValue = 1.25f,
            isRepeatable = true,
            maxLevel = 3
        });

        // Acceleration and Responsiveness
        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Lightweight Hull",
            upgradeDescription = "Faster acceleration and deceleration",
            upgradePrice = 1,
            upgradeType = UpgradeType.Acceleration,
            upgradeValue = 1.3f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Enhanced Rudder",
            upgradeDescription = "Improved turning speed and responsiveness",
            upgradePrice = 1,
            upgradeType = UpgradeType.TurnRate,
            upgradeValue = 1.4f,
            isRepeatable = true,
            maxLevel = 2
        });

        // Advanced Upgrades
        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Master Helm",
            upgradeDescription = "Faster helm response and return speed",
            upgradePrice = 1,
            upgradeType = UpgradeType.HelmResponse,
            upgradeValue = 1.5f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Wind Reader's Compass",
            upgradeDescription = "Better wind utilization efficiency",
            upgradePrice = 1,
            upgradeType = UpgradeType.WindEfficiency,
            upgradeValue = 1.2f,
            isRepeatable = true,
            maxLevel = 2
        });

        // Premium Upgrades
        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Storm Sails",
            upgradeDescription = "Unlock 4th gear - extreme wind sailing",
            upgradePrice = 1,
            upgradeType = UpgradeType.ExtraGear,
            upgradeValue = 1f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Navigator's Blessing",
            upgradeDescription = "Complete ship handling mastery package",
            upgradePrice = 1,
            upgradeType = UpgradeType.MasterUpgrade,
            upgradeValue = 1.2f,
            isRepeatable = false
        });
    }

    private void InitializeUpgradeList(List<ShipUpgradeData> upgradeList)
    {
        Debug.Log($"Initializing upgrade list with {upgradeList.Count} total upgrades");
        
        // Clear existing items
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // Force layout rebuild after clearing
        if (contentTransform.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform.GetComponent<RectTransform>());
        }

        int visibleUpgrades = 0;
        foreach (ShipUpgradeData upgradeItem in upgradeList)
        {
            Debug.Log($"Processing upgrade: {upgradeItem.upgradeName}, Purchased: {upgradeItem.purchased}, Level: {upgradeItem.currentLevel}/{upgradeItem.maxLevel}");
            
            // Skip if max level reached
            if (!upgradeItem.isRepeatable && upgradeItem.purchased) 
            {
                Debug.Log($"Skipping {upgradeItem.upgradeName} - already purchased");
                continue;
            }
            if (upgradeItem.isRepeatable && upgradeItem.currentLevel >= upgradeItem.maxLevel) 
            {
                Debug.Log($"Skipping {upgradeItem.upgradeName} - max level reached ({upgradeItem.currentLevel}/{upgradeItem.maxLevel})");
                continue;
            }

            Debug.Log($"Creating upgrade slot for: {upgradeItem.upgradeName}");
            
            GameObject prefab = Instantiate(upgradeItemPrefab, contentTransform);
            ShipUpgradeSlot upgradeSlot = prefab.GetComponent<ShipUpgradeSlot>();

            if (upgradeSlot != null)
            {
                visibleUpgrades++;
                Debug.Log($"Successfully created slot #{visibleUpgrades} for {upgradeItem.upgradeName}");
                
                // Set the actual data
                upgradeSlot.shipUpgradeData = upgradeItem;

                // Setting the upgrade icon (if available)
                if (upgradeSlot.upgradeImageUI != null && upgradeSlot.defaultUpgradeIcon != null)
                {
                    upgradeSlot.upgradeImageUI.sprite = upgradeSlot.defaultUpgradeIcon;
                }

                // Setting the Name
                string displayName = upgradeItem.upgradeName;
                if (upgradeItem.isRepeatable && upgradeItem.currentLevel > 0)
                {
                    displayName += $" (Level {upgradeItem.currentLevel + 1})";
                }
                upgradeSlot.upgradeNameUI.text = displayName;

                // Setting the Price
                int currentPrice = CalculateUpgradePrice(upgradeItem);
                upgradeSlot.upgradePriceUI.text = $"{currentPrice} Upgrade Points";

                // Set purchase button with debug
                upgradeSlot.purchaseButton.onClick.RemoveAllListeners();
                upgradeSlot.purchaseButton.onClick.AddListener(() => {
                    Debug.Log($"[ShipUpgradeSystem] Button clicked for: {upgradeItem.upgradeName}");
                    PurchaseUpgrade(upgradeItem);
                });

                // Check if affordable using inventory system
                upgradeSlot.purchaseButton.interactable = PlayerGold >= currentPrice;
                
                Debug.Log($"[ShipUpgradeSystem] Button setup for {upgradeItem.upgradeName} - Interactable: {upgradeSlot.purchaseButton.interactable}");
                
                // Force the layout to update
                prefab.SetActive(false);
                prefab.SetActive(true);
            }
            else
            {
                Debug.LogError($"ShipUpgradeSlot component not found on instantiated prefab for {upgradeItem.upgradeName}");
                Destroy(prefab); // Clean up the failed prefab
            }
        }
        
        // Force layout rebuild after adding all items
        if (contentTransform.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform.GetComponent<RectTransform>());
        }
        
        Debug.Log($"Created {visibleUpgrades} visible upgrade slots");
    }

    private int CalculateUpgradePrice(ShipUpgradeData upgrade)
    {
        if (upgrade.isRepeatable)
        {
            return Mathf.RoundToInt(upgrade.upgradePrice * Mathf.Pow(1.5f, upgrade.currentLevel));
        }
        return upgrade.upgradePrice;
    }

    public void PurchaseUpgrade(ShipUpgradeData upgrade)
    {
        int price = CalculateUpgradePrice(upgrade);
        
        if (PlayerGold >= price)
        {
            // Remove upgrade materials from inventory
            RemoveUpgradeMaterials(price);
            ApplyUpgrade(upgrade);
            
            if (upgrade.isRepeatable)
            {
                upgrade.currentLevel++;
            }
            else
            {
                upgrade.purchased = true;
            }

            // Refresh the upgrade list and gold display
            InitializeUpgradeList(currentUpgradeList);
            UpdateGoldDisplay();
            
            Debug.Log($"Purchased upgrade: {upgrade.upgradeName}");
        }
        else
        {
            Debug.LogWarning($"Cannot purchase {upgrade.upgradeName}. Not enough upgrade materials.");
        }
    }

    private void ApplyUpgrade(ShipUpgradeData upgrade)
    {
        if (shipController == null) 
        {
            Debug.LogError("[ApplyUpgrade] ShipController is null!");
            return;
        }

        // Incremental upgrade multiplier (1.3x per level)
        float upgradeMultiplier = 1.3f;
        
        Debug.Log($"[ApplyUpgrade] Applying {upgrade.upgradeName} - Type: {upgrade.upgradeType}");
        Debug.Log($"[ApplyUpgrade] Before upgrade - Checking ship values...");

        switch (upgrade.upgradeType)
        {
            case UpgradeType.PaddlingSpeed:
                float oldPaddlingSpeed = shipController.paddlingSpeed;
                shipController.paddlingSpeed *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Paddling Speed: {oldPaddlingSpeed:F2} → {shipController.paddlingSpeed:F2}");
                break;
                
            case UpgradeType.WindSpeed:
                float oldWindSpeed = shipController.windSpeed;
                shipController.windSpeed *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Wind Speed: {oldWindSpeed:F2} → {shipController.windSpeed:F2}");
                break;
                
            case UpgradeType.Acceleration:
                float oldAcceleration = shipController.accelerationRate;
                float oldDeceleration = shipController.decelerationRate;
                shipController.accelerationRate *= upgradeMultiplier;
                shipController.decelerationRate *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Acceleration: {oldAcceleration:F2} → {shipController.accelerationRate:F2}");
                Debug.Log($"[ApplyUpgrade] Deceleration: {oldDeceleration:F2} → {shipController.decelerationRate:F2}");
                break;
                
            case UpgradeType.TurnRate:
                float oldMaxTurnRate = shipController.maxTurnRate;
                float oldTurnAcceleration = shipController.turnAcceleration;
                shipController.maxTurnRate *= upgradeMultiplier;
                shipController.turnAcceleration *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Max Turn Rate: {oldMaxTurnRate:F2} → {shipController.maxTurnRate:F2}");
                Debug.Log($"[ApplyUpgrade] Turn Acceleration: {oldTurnAcceleration:F2} → {shipController.turnAcceleration:F2}");
                break;
                
            case UpgradeType.HelmResponse:
                float oldHelmReturnSpeed = shipController.helmReturnSpeed;
                shipController.helmReturnSpeed *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Helm Return Speed: {oldHelmReturnSpeed:F2} → {shipController.helmReturnSpeed:F2}");
                break;
                
            case UpgradeType.WindEfficiency:
                // For wind efficiency, we'll boost wind speed as a substitute
                float oldWindSpeedEff = shipController.windSpeed;
                shipController.windSpeed *= upgradeMultiplier;
                Debug.Log($"[ApplyUpgrade] Wind Efficiency (via Wind Speed): {oldWindSpeedEff:F2} → {shipController.windSpeed:F2}");
                break;
                
            case UpgradeType.ExtraGear:
                // This would require modification to ship controller to add 4th gear
                // For now, just boost wind speed significantly
                float oldWindSpeedGear = shipController.windSpeed;
                shipController.windSpeed *= 1.5f; // Special boost for "4th gear"
                Debug.Log($"[ApplyUpgrade] 4th Gear Unlocked! Wind Speed boosted: {oldWindSpeedGear:F2} → {shipController.windSpeed:F2}");
                Debug.Log("[ApplyUpgrade] Note: Full 4th gear implementation requires ShipController modification");
                break;
                
            case UpgradeType.MasterUpgrade:
                // Master upgrade affects multiple attributes
                float oldPaddlingMaster = shipController.paddlingSpeed;
                float oldWindMaster = shipController.windSpeed;
                float oldTurnMaster = shipController.maxTurnRate;
                float oldAccelMaster = shipController.accelerationRate;
                
                shipController.paddlingSpeed *= upgradeMultiplier;
                shipController.windSpeed *= upgradeMultiplier;
                shipController.maxTurnRate *= upgradeMultiplier;
                shipController.accelerationRate *= upgradeMultiplier;
                
                Debug.Log($"[ApplyUpgrade] MASTER UPGRADE - Multiple improvements:");
                Debug.Log($"  Paddling Speed: {oldPaddlingMaster:F2} → {shipController.paddlingSpeed:F2}");
                Debug.Log($"  Wind Speed: {oldWindMaster:F2} → {shipController.windSpeed:F2}");
                Debug.Log($"  Turn Rate: {oldTurnMaster:F2} → {shipController.maxTurnRate:F2}");
                Debug.Log($"  Acceleration: {oldAccelMaster:F2} → {shipController.accelerationRate:F2}");
                break;
                
            default:
                Debug.LogWarning($"[ApplyUpgrade] Unknown upgrade type: {upgrade.upgradeType}");
                break;
        }
        
        Debug.Log($"[ApplyUpgrade] Successfully applied {upgrade.upgradeName}!");
    }

    private void UpdateGoldDisplay()
    {
        if (goldDisplay != null)
        {
            goldDisplay.text = $"{PlayerGold}";
        }
    }

    // Method to get the count of upgrade materials from inventory
    private int GetUpgradeMaterialCount()
    {
        if (InventorySystem.Instance == null)
        {
            return 0;
        }

        // Force all inventory slots to update their item references first
        foreach (GameObject slot in InventorySystem.Instance.slotList)
        {
            if (slot != null)
            {
                InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
                if (inventorySlot != null)
                {
                    inventorySlot.UpdateItemInSlot(); // Force update before counting
                }
            }
        }

        int totalCount = 0;
    
        foreach (GameObject slot in InventorySystem.Instance.slotList)
        {
            if (slot != null && slot.transform.childCount > 1)
            {
                InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
                if (inventorySlot != null && inventorySlot.itemInSlot != null)
                {
                    if (inventorySlot.itemInSlot.thisName == "UpgradeMaterial")
                    {
                        totalCount += inventorySlot.itemInSlot.amountInInventory;
                    }
                }
            }
        }
    
        return totalCount;
    }

    // Method to remove upgrade materials from inventory
    private void RemoveUpgradeMaterials(int amount)
    {
        Debug.Log($"[RemoveUpgradeMaterials] Starting removal of {amount} materials");
        
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("[RemoveUpgradeMaterials] InventorySystem instance not found!");
            return;
        }
    
        int remainingToRemove = amount;
        
        for (int i = 0; i < InventorySystem.Instance.slotList.Count && remainingToRemove > 0; i++)
        {
            GameObject slot = InventorySystem.Instance.slotList[i];
            if (slot != null && slot.transform.childCount > 1)
            {
                InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
                if (inventorySlot != null && inventorySlot.itemInSlot != null)
                {
                    if (inventorySlot.itemInSlot.thisName == "UpgradeMaterial")
                    {
                        int currentAmount = inventorySlot.itemInSlot.amountInInventory;
                        int toRemove = Mathf.Min(currentAmount, remainingToRemove);
                        
                        Debug.Log($"[RemoveUpgradeMaterials] Removing {toRemove} from slot {i} (had {currentAmount})");
                        
                        inventorySlot.itemInSlot.amountInInventory -= toRemove;
                        remainingToRemove -= toRemove;
                        
                        // If the stack is empty, remove the item from the slot
                        if (inventorySlot.itemInSlot.amountInInventory <= 0)
                        {
                            Debug.Log($"[RemoveUpgradeMaterials] Stack empty in slot {i}, destroying GameObject");
                            
                            // Use DestroyImmediate for immediate destruction
                            if (slot.transform.childCount > 1)
                            {
                                DestroyImmediate(slot.transform.GetChild(1).gameObject);
                            }
                            
                            // Clear the reference immediately
                            inventorySlot.itemInSlot = null;
                        }
                        else
                        {
                            Debug.Log($"[RemoveUpgradeMaterials] Updated slot {i}, {inventorySlot.itemInSlot.amountInInventory} remaining");
                            inventorySlot.UpdateItemInSlot();
                        }
                    }
                }
            }
        }
        
        if (remainingToRemove > 0)
        {
            Debug.LogWarning($"[RemoveUpgradeMaterials] Could not remove all required upgrade materials. {remainingToRemove} materials still needed.");
        }
        
        Debug.Log($"[RemoveUpgradeMaterials] Removal complete. Remaining to remove: {remainingToRemove}");
    }

    public void ExitUpgradeMode()
    {
        if (shipUpgradeKeeper != null)
        {
            shipUpgradeKeeper.DialogMode();
        }
    }

    [System.Serializable]
    public class ShipUpgradeData
    {
        public string upgradeName;
        public string upgradeDescription;
        public int upgradePrice;
        public UpgradeType upgradeType;
        public float upgradeValue;
        public bool isRepeatable = false;
        public int maxLevel = 1;
        public int currentLevel = 0;
        public bool purchased = false;
    }

    public enum UpgradeType
    {
        PaddlingSpeed,
        WindSpeed,
        Acceleration,
        TurnRate,
        HelmResponse,
        WindEfficiency,
        ExtraGear,
        MasterUpgrade
    }
}