using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace

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
    public TextMeshProUGUI goldDisplay; // Changed from Text to TextMeshProUGUI
    
    public GarbageCompactorManager garbageCompactorManager; // Reference to the garbage compactor manager
    // Property to get upgrade materials from inventory
    public int PlayerGold;

    void Start()
    {
        backButton.onClick.AddListener(ExitUpgradeMode);
        
        PlayerGold = garbageCompactorManager.upgradeMatCount;
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

    void Update()
    {
        // Continuously update the gold display to reflect inventory changes
        UpdateGoldDisplay();
    }

    private void InitializeDefaultUpgrades()
    {
        // Speed and Movement Upgrades
        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Reinforced Paddles",
            upgradeDescription = "Increase paddling speed by 50%",
            upgradePrice = 150,
            upgradeType = UpgradeType.PaddlingSpeed,
            upgradeValue = 1.5f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Improved Sails",
            upgradeDescription = "Increase wind sailing speed by 25%",
            upgradePrice = 200,
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
            upgradePrice = 175,
            upgradeType = UpgradeType.Acceleration,
            upgradeValue = 1.3f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Enhanced Rudder",
            upgradeDescription = "Improved turning speed and responsiveness",
            upgradePrice = 125,
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
            upgradePrice = 100,
            upgradeType = UpgradeType.HelmResponse,
            upgradeValue = 1.5f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Wind Reader's Compass",
            upgradeDescription = "Better wind utilization efficiency",
            upgradePrice = 250,
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
            upgradePrice = 500,
            upgradeType = UpgradeType.ExtraGear,
            upgradeValue = 1f,
            isRepeatable = false
        });

        currentUpgradeList.Add(new ShipUpgradeData
        {
            upgradeName = "Navigator's Blessing",
            upgradeDescription = "Complete ship handling mastery package",
            upgradePrice = 750,
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

                // Setting the Description
                //upgradeSlot.upgradeDescriptionUI.text = upgradeItem.upgradeDescription;

                // Setting the Price
                int currentPrice = CalculateUpgradePrice(upgradeItem);
                upgradeSlot.upgradePriceUI.text = $"{currentPrice} Upgrade Points";

                // Set purchase button
                upgradeSlot.purchaseButton.onClick.RemoveAllListeners();
                upgradeSlot.purchaseButton.onClick.AddListener(() => PurchaseUpgrade(upgradeItem));

                // Check if affordable using inventory system
                upgradeSlot.purchaseButton.interactable = PlayerGold >= currentPrice;
                
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
            GarbageCompactorManager.Instance.upgradeMatCount -= price;
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
    if (shipController == null) return;

    // Check for the specific upgrade names first
    if (upgrade.upgradeName == "Ship Speed Upgrade")
    {
        shipController.UpgradeShipSpeed();
        return;
    }
    
    if (upgrade.upgradeName == "Ship Handling Upgrade")
    {
        shipController.UpgradeShipHandling();
        return;
    }

    // Original upgrade type handling
    switch (upgrade.upgradeType)
    {
        case UpgradeType.PaddlingSpeed:
            shipController.paddlingSpeed *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.WindSpeed:
            shipController.windSpeed *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.Acceleration:
            shipController.accelerationRate *= upgrade.upgradeValue;
            shipController.decelerationRate *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.TurnRate:
            shipController.maxTurnRate *= upgrade.upgradeValue;
            shipController.turnAcceleration *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.HelmResponse:
            shipController.helmReturnSpeed *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.WindEfficiency:
            // This would require a modification to the wind propulsion calculation
            // For now, we'll increase wind speed as a substitute
            shipController.windSpeed *= upgrade.upgradeValue;
            break;
            
        case UpgradeType.ExtraGear:
            // This would require adding a 4th gear to the ship controller
            Debug.Log("4th Gear Unlocked! (Requires ship controller modification)");
            break;
            
        case UpgradeType.MasterUpgrade:
            // Apply multiple small bonuses
            shipController.paddlingSpeed *= upgrade.upgradeValue;
            shipController.windSpeed *= upgrade.upgradeValue;
            shipController.maxTurnRate *= upgrade.upgradeValue;
            break;
    }
    }

    private void UpdateGoldDisplay()
    {
        if (goldDisplay != null)
        {
            goldDisplay.text = $"Upgrade Materials: {PlayerGold}";
        }
    }

    // Method to get the count of upgrade materials from inventory
    private int GetUpgradeMaterialCount()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem instance not found!");
            return 0;
        }

        int totalCount = 0;
        
        Debug.Log($"Checking {InventorySystem.Instance.slotList.Count} inventory slots for upgradeMat");
        
        foreach (GameObject slot in InventorySystem.Instance.slotList)
        {
            if (slot != null && slot.transform.childCount > 1)
            {
                InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
                if (inventorySlot != null && inventorySlot.itemInSlot != null)
                {
                    Debug.Log($"Found item: {inventorySlot.itemInSlot.thisName} with amount: {inventorySlot.itemInSlot.amountInInventory}");
                    
                    if (inventorySlot.itemInSlot.thisName == "debris")
                    {
                        totalCount += inventorySlot.itemInSlot.amountInInventory;
                        Debug.Log($"Added {inventorySlot.itemInSlot.amountInInventory} upgrade materials. Total: {totalCount}");
                    }
                }
            }
        }
        
        Debug.Log($"Final upgrade material count: {totalCount}");
        return totalCount;
    }

    // Method to remove upgrade materials from inventory
    private void RemoveUpgradeMaterials(int amount)
{
    Debug.Log($"Attempting to remove {amount} upgrade materials");
    
    int remainingToRemove = amount;
    
    // Find all GameObjects with the name "upgradeMat"
    GameObject[] upgradeMatObjects = GameObject.FindObjectsOfType<GameObject>()
        .Where(obj => obj.name == "upgradeMat")
        .ToArray();
    
    foreach (GameObject upgradeMatObj in upgradeMatObjects)
    {
        if (remainingToRemove <= 0) break;
        
        // Get the InventoryItem component
        InventoryItem inventoryItem = upgradeMatObj.GetComponent<InventoryItem>();
        if (inventoryItem != null)
        {
            int currentAmount = inventoryItem.amountInInventory;
            int toRemove = Mathf.Min(currentAmount, remainingToRemove);
            
            Debug.Log($"Removing {toRemove} from {upgradeMatObj.name} with {currentAmount} materials");
            
            inventoryItem.amountInInventory -= toRemove;
            remainingToRemove -= toRemove;
            
            // If the stack is empty, destroy the object
            if (inventoryItem.amountInInventory <= 0)
            {
                Debug.Log("Stack empty, destroying upgrade material object");
                
                // Remove from inventory system's item list if it exists
                if (InventorySystem.Instance != null)
                {
                    for (int j = InventorySystem.Instance.itemList.Count - 1; j >= 0; j--)
                    {
                        if (InventorySystem.Instance.itemList[j] == "upgradeMat")
                        {
                            InventorySystem.Instance.itemList.RemoveAt(j);
                            break;
                        }
                    }
                }
                
                Destroy(upgradeMatObj);
            }
            else
            {
                // Update the item display if it has an update method
                // This assumes there's some way to refresh the UI
                Transform parentSlot = upgradeMatObj.transform.parent;
                if (parentSlot != null)
                {
                    InventorySlot inventorySlot = parentSlot.GetComponent<InventorySlot>();
                    if (inventorySlot != null)
                    {
                        inventorySlot.UpdateItemInSlot();
                    }
                }
            }
        }
    }
    
    if (remainingToRemove > 0)
    {
        Debug.LogWarning($"Could not remove all required upgrade materials. {remainingToRemove} materials still needed.");
    }
    else
    {
        Debug.Log($"Successfully removed {amount} upgrade materials");
    }
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

// Companion script for individual upgrade slots
// Attach this to your upgradeItemPrefab GameObject