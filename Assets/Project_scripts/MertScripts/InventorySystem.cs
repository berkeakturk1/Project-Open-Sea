using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
 
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; set; }
 
    public GameObject inventoryScreenUI;
    
    [Header("Currency")]
    [SerializeField] internal int currentCoins = 100;
    public TextMeshProUGUI currencyUI;
    
    public List<GameObject> slotList = new List<GameObject>();
    public List<string> itemList = new List<string>();

    private GameObject itemToAdd;
    private GameObject whatSlotToEquip;
    public int stackLimit = 3;

    public bool isOpen;
 
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
 
    void Start()
    {
        isOpen = false;
        PopulateSlotList();
        UpdateCurrencyUI(); // Initialize currency display
    }

    // Method to update the currency UI
    public void UpdateCurrencyUI()
    {
        if (currencyUI != null)
        {
            currencyUI.text = currentCoins.ToString();
        }
        else
        {
            Debug.LogWarning("Currency UI reference is missing!");
        }
    }

    // Method to modify currency with validation
    public void ModifyCurrency(int amount)
    {
        currentCoins += amount;
        
        // Optional: Prevent negative currency
        if (currentCoins < 0)
        {
            currentCoins = 0;
        }
        
        UpdateCurrencyUI();
        Debug.Log($"Currency modified by {amount}. New total: {currentCoins}");
    }

    // Method to check if player can afford an item
    public bool CanAfford(int price)
    {
        return currentCoins >= price;
    }

    /// <summary>
    /// Gets the amount of a specific item in inventory
    /// </summary>
    /// <param name="itemName">Name of the item to check</param>
    /// <returns>Amount of the item in inventory</returns>
    public int GetItemAmount(string itemName)
    {
        Debug.Log($"[InventorySystem] GetItemAmount called for: '{itemName}'");
        
        foreach (GameObject slot in slotList)
        {
            if (slot == null) continue;
            
            InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
            if (inventorySlot != null && inventorySlot.itemInSlot != null)
            {
                Debug.Log($"[InventorySystem] Found item in slot: '{inventorySlot.itemInSlot.thisName}' (comparing with '{itemName}')");
                
                if (inventorySlot.itemInSlot.thisName == itemName)
                {
                    Debug.Log($"[InventorySystem] MATCH! Found {inventorySlot.itemInSlot.amountInInventory} of {itemName}");
                    return inventorySlot.itemInSlot.amountInInventory;
                }
            }
        }
        
        Debug.Log($"[InventorySystem] No {itemName} found in inventory");
        return 0;
    }

    /// <summary>
    /// Removes a specific amount of an item from inventory
    /// </summary>
    /// <param name="itemName">Name of the item to remove</param>
    /// <param name="amountToRemove">Amount to remove</param>
    /// <returns>True if successful, false if not enough items</returns>
    public bool RemoveFromInventory(string itemName, int amountToRemove)
    {
        // First check if we have enough items
        int currentAmount = GetItemAmount(itemName);
        if (currentAmount < amountToRemove)
        {
            Debug.LogWarning($"Not enough {itemName} in inventory. Have: {currentAmount}, Need: {amountToRemove}");
            return false;
        }

        // Find the item slot and remove the items
        foreach (GameObject slot in slotList)
        {
            if (slot == null) continue;
            
            InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
            if (inventorySlot != null && inventorySlot.itemInSlot != null)
            {
                if (inventorySlot.itemInSlot.thisName == itemName)
                {
                    InventoryItem item = inventorySlot.itemInSlot;
                    
                    if (item.amountInInventory > amountToRemove)
                    {
                        // Remove partial amount
                        item.amountInInventory -= amountToRemove;
                        Debug.Log($"Removed {amountToRemove} {itemName}. Remaining: {item.amountInInventory}");
                        return true;
                    }
                    else if (item.amountInInventory == amountToRemove)
                    {
                        // Remove all of this item
                        Debug.Log($"Removed all {amountToRemove} {itemName}");
                        
                        // Remove from itemList if this was the last of this item type
                        if (itemList.Contains(itemName))
                        {
                            itemList.Remove(itemName);
                        }
                        
                        Destroy(item.gameObject);
                        inventorySlot.UpdateItemInSlot();
                        return true;
                    }
                    else
                    {
                        // This shouldn't happen due to our initial check, but handle it anyway
                        amountToRemove -= item.amountInInventory;
                        Debug.Log($"Removed {item.amountInInventory} {itemName}. Still need to remove: {amountToRemove}");
                        
                        // Remove from itemList if this was the last of this item type
                        if (itemList.Contains(itemName))
                        {
                            itemList.Remove(itemName);
                        }
                        
                        Destroy(item.gameObject);
                        inventorySlot.UpdateItemInSlot();
                        // Continue to next slot if we still have items to remove
                    }
                }
            }
        }

        // If we get here and amountToRemove is still > 0, something went wrong
        if (amountToRemove > 0)
        {
            Debug.LogError($"Failed to remove all {itemName}. Still need to remove: {amountToRemove}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets all items in inventory (useful for debugging or advanced features)
    /// </summary>
    /// <returns>List of all items with their amounts</returns>
    public List<(string itemName, int amount)> GetAllInventoryItems()
    {
        Debug.Log($"[InventorySystem] GetAllInventoryItems called. SlotList count: {slotList.Count}");
        List<(string, int)> items = new List<(string, int)>();
        
        for (int i = 0; i < slotList.Count; i++)
        {
            GameObject slot = slotList[i];
            Debug.Log($"[InventorySystem] Checking slot {i}: {(slot != null ? slot.name : "NULL")}");
            
            if (slot == null) 
            {
                Debug.Log($"[InventorySystem] Slot {i} is null, skipping");
                continue;
            }
            
            InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
            if (inventorySlot == null)
            {
                Debug.Log($"[InventorySystem] Slot {i} has no InventorySlot component");
                continue;
            }
            
            Debug.Log($"[InventorySystem] Slot {i} InventorySlot found. itemInSlot: {(inventorySlot.itemInSlot != null ? inventorySlot.itemInSlot.name : "NULL")}");
            
            if (inventorySlot.itemInSlot != null)
            {
                Debug.Log($"[InventorySystem] Slot {i} has item: {inventorySlot.itemInSlot.thisName} x{inventorySlot.itemInSlot.amountInInventory}");
                items.Add((inventorySlot.itemInSlot.thisName, inventorySlot.itemInSlot.amountInInventory));
            }
            else
            {
                Debug.Log($"[InventorySystem] Slot {i} itemInSlot is null - slot appears empty");
            }
        }
        
        Debug.Log($"[InventorySystem] GetAllInventoryItems complete. Found {items.Count} different items");
        return items;
    }

    private void PopulateSlotList()
    {
        foreach(Transform child in inventoryScreenUI.transform)
        {
            if(child.CompareTag("Slot"))
            {
                slotList.Add(child.gameObject); 
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isOpen)
        {
            Debug.Log("i is pressed");
            inventoryScreenUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            isOpen = true;
        }
        else if (Input.GetKeyDown(KeyCode.Tab) && isOpen)
        {
            inventoryScreenUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            isOpen = false;
        }
    }

    public void AddToInventory(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogError("AddToInventory was called with null/empty item name");
            return;
        }

        GameObject stack = CheckIfStackExists(itemName);
        if(stack != null)
        {
            InventorySlot slotComponent = stack.GetComponent<InventorySlot>();
            if (slotComponent != null && slotComponent.itemInSlot != null)
            {
                slotComponent.itemInSlot.amountInInventory += 1;
                slotComponent.UpdateItemInSlot();
                Debug.Log($"Added {itemName} to existing stack. New amount: {slotComponent.itemInSlot.amountInInventory}");
            }
            else
            {
                Debug.LogError("Stack found but InventorySlot or itemInSlot is null!");
            }
        }
        else // No existing stack, create new item in empty slot
        {
            whatSlotToEquip = FindNextEmptySlot();
            if (whatSlotToEquip != null)
            {
                itemToAdd = Instantiate(Resources.Load<GameObject>(itemName), whatSlotToEquip.transform.position, whatSlotToEquip.transform.rotation);
                if (itemToAdd != null)
                {
                    itemToAdd.transform.SetParent(whatSlotToEquip.transform);
                    itemList.Add(itemName);
                    Debug.Log($"Added new {itemName} to empty slot");
                }
                else
                {
                    Debug.LogError($"Could not load resource: {itemName}");
                }
            }
            else
            {
                Debug.LogWarning("No empty slots available!");
            }
        }   
    }

    private GameObject CheckIfStackExists(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("CheckIfStackExists was called with null/empty item name");
            return null;
        }
    
        foreach(GameObject slot in slotList)
        {
            if (slot == null)
            {
                continue;
            }
        
            InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
            if (inventorySlot == null)
            {
                continue;
            }
        
            if (inventorySlot.itemInSlot != null)
            {
                inventorySlot.UpdateItemInSlot();
            
                if (inventorySlot.itemInSlot.thisName == itemName && 
                    inventorySlot.itemInSlot.amountInInventory < stackLimit)
                {
                    return slot;
                }
            }
        }
        return null;
    }

    // FIXED: Improved method to find truly empty slots
    public GameObject FindNextEmptySlot()
    {
        foreach(GameObject slot in slotList)
        {
            if (slot == null) continue;
            
            // Check if slot is truly empty
            if (IsSlotEmpty(slot))
            {
                Debug.Log($"Found empty slot: {slot.name}");
                return slot;
            }
        }
        
        Debug.LogWarning("No empty slots found!");
        return null; // Return null instead of new GameObject()
    }

    // Helper method to check if a slot is truly empty
    private bool IsSlotEmpty(GameObject slot)
    {
        // Method 1: Check child count (should only have the AmountTXT child)
        if (slot.transform.childCount > 1)
        {
            return false; // Has more than just the AmountTXT, so not empty
        }
        
        // Method 2: Check InventorySlot component
        InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
        if (inventorySlot != null)
        {
            // Force update the slot to make sure itemInSlot is current
            inventorySlot.UpdateItemInSlot();
            
            // If itemInSlot is not null, slot is occupied
            if (inventorySlot.itemInSlot != null)
            {
                return false;
            }
        }
        
        // Method 3: Double-check by looking for InventoryItem components in children
        foreach (Transform child in slot.transform)
        {
            if (child.GetComponent<InventoryItem>() != null)
            {
                return false; // Found an item, slot is not empty
            }
        }
        
        return true; // Slot is truly empty
    }

    // IMPROVED: Better CheckIfFull method
    public bool CheckIfFull()
    {
        int occupiedSlots = 0;

        foreach(GameObject slot in slotList)
        {
            if (slot != null && !IsSlotEmpty(slot))
            {
                occupiedSlots++;
            }
        }

        bool isFull = occupiedSlots >= slotList.Count;
        
        if (isFull)
        {
            Debug.Log($"Inventory is full! {occupiedSlots}/{slotList.Count} slots occupied");
        }
        
        return isFull;
    }
}