using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI amountTXT;
    public InventoryItem itemInSlot;

    // Add this method to handle clicks on the slot
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && itemInSlot != null)
        {
            // If the item is consumable, consume it
            if (itemInSlot.isConsumable)
            {
                ConsumeItem(itemInSlot);
            }
            // If the item is usable, use it
            else if (itemInSlot.isUsable)
            {
                UseItem(itemInSlot);
            }
        }
    }

    private void ConsumeItem(InventoryItem item)
    {
        // Apply the consumption effects
        ApplyConsumptionEffects(item);
        
        // Reduce the amount or destroy the item
        item.amountInInventory--;
        if (item.amountInInventory <= 0)
        {
            Destroy(item.gameObject);
        }
    }

    private void UseItem(InventoryItem item)
    {
        // Call the item's use function through reflection to avoid errors
        var useItemMethod = item.GetType().GetMethod("useItem", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (useItemMethod != null)
        {
            useItemMethod.Invoke(item, null);
        }
    }

    private void ApplyConsumptionEffects(InventoryItem item)
    {
        PlayerState playerState = PlayerState.Instance;
        
        // Apply health effect
        if (item.healthEffect != 0)
        {
            float newHealth = Mathf.Min(playerState.currentHealth + item.healthEffect, 
                                      playerState.maxHealth);
            playerState.currentHealth = newHealth;
            Debug.Log($"Health effect applied: +{item.healthEffect} → Current Health: {playerState.currentHealth}/{playerState.maxHealth}");
        }

        // Apply armor effect
        if (item.armorEffect != 0)
        {
            playerState.AddArmor(item.armorEffect);
        }

        // Note: Your PlayerState doesn't have calories or hydration systems
        // If you want to add them later, you would need to:
        // 1. Add currentCalories, maxCalories, currentHydration, maxHydration fields to PlayerState
        // 2. Uncomment and modify the code below:
        
        /*
        // Apply calories effect
        if (item.caloriesEffect != 0)
        {
            float newCalories = Mathf.Min(playerState.currentCalories + item.caloriesEffect, 
                                        playerState.maxCalories);
            playerState.currentCalories = newCalories;
            Debug.Log($"Calories effect applied: +{item.caloriesEffect} → Current Calories: {playerState.currentCalories}/{playerState.maxCalories}");
        }

        // Apply hydration effect
        if (item.hydrationEffect != 0)
        {
            float newHydration = Mathf.Min(playerState.currentHydration + item.hydrationEffect, 
                                         playerState.maxHydration);
            playerState.currentHydration = newHydration;
            Debug.Log($"Hydration effect applied: +{item.hydrationEffect} → Current Hydration: {playerState.currentHydration}/{playerState.maxHydration}");
        }
        */
    }

    // Rest of your existing code...
    private void Update()
    {
        InventoryItem item = CheckInventoryItem();

        if(item != null)
        {
            itemInSlot = item;
        }
        else
        {
            itemInSlot = null;
        }
        
        if(itemInSlot != null)
        {
            amountTXT.gameObject.SetActive(true);
            amountTXT.text = $"{itemInSlot.amountInInventory}";
            amountTXT.transform.SetAsLastSibling();
        }
        else
        {
            amountTXT.gameObject.SetActive(false);
        }
    }

    private InventoryItem CheckInventoryItem()
    {
        foreach(Transform child in transform)
        {
            InventoryItem inventoryItem = child.GetComponent<InventoryItem>();
            if(inventoryItem != null)
            {
                return inventoryItem;
            }
        }
        return null;
    }

    public void UpdateItemInSlot()
    {
        itemInSlot = CheckInventoryItem();
    }
}