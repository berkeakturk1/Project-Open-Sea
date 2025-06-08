using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    public TextMeshProUGUI amountTXT;
    public InventoryItem itemInSlot;

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
            amountTXT.transform.SetAsLastSibling(); // item'ın adedinin txt'si her zaman sonda olması için gereken kod
        }
        else
        {
            amountTXT.gameObject.SetActive(false);
        }
    }

    private InventoryItem CheckInventoryItem()
    {
        Debug.Log($"[InventorySlot] CheckInventoryItem called on {gameObject.name}. Child count: {transform.childCount}");
        
        foreach(Transform child in transform)
        {
            Debug.Log($"[InventorySlot] Checking child: {child.name}");
            
            InventoryItem inventoryItem = child.GetComponent<InventoryItem>();
            if(inventoryItem != null)
            {
                Debug.Log($"[InventorySlot] Found InventoryItem: {inventoryItem.thisName} x{inventoryItem.amountInInventory}");
                return inventoryItem;
            }
            else
            {
                Debug.Log($"[InventorySlot] Child {child.name} has no InventoryItem component");
            }
        }
        
        Debug.Log($"[InventorySlot] No InventoryItem found in {gameObject.name}");
        return null;
    }

    public void UpdateItemInSlot()
    {
        Debug.Log($"[InventorySlot] UpdateItemInSlot called on {gameObject.name}");
        itemInSlot = CheckInventoryItem();
        Debug.Log($"[InventorySlot] UpdateItemInSlot result: {(itemInSlot != null ? itemInSlot.thisName : "NULL")}");
    }
}