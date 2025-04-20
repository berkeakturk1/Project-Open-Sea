using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
 
 
 
public class ItemSlot : MonoBehaviour, IDropHandler
{
 
   /* public GameObject Item
    {
        get
        {
            if (transform.childCount > 0 )
            {
                return transform.GetChild(0).gameObject;
            }
 
            return null;
        }
    }*/
 
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop");
 
        //eğer slot boşsa yapıcak
        if (transform.childCount <=1)
        {
 
            DragDrop.itemBeingDragged.transform.SetParent(transform);
            DragDrop.itemBeingDragged.transform.localPosition = new Vector2(0, 0);

            if(transform.CompareTag("QuickSlot") == false)
            {
                DragDrop.itemBeingDragged.GetComponent<InventoryItem>().isInsideQuickSlot = false;
            }

            if(transform.CompareTag("QuickSlot"))
            {
                DragDrop.itemBeingDragged.GetComponent<InventoryItem>().isInsideQuickSlot = true;
            }
        }
        //Slot boş değilse
        else
        {
            InventoryItem draggedItem = DragDrop.itemBeingDragged.GetComponent<InventoryItem>();
            //Bu iki item da aynımı kontrolü ve limit aşılmadı kontrolü
            if(draggedItem.thisName == GetStoredItem().thisName && IsLimitExceded(draggedItem) == false)
            {
                //Dragged item i mergeleme ve store lama işlemi
                GetStoredItem().amountInInventory += draggedItem.amountInInventory;
                DestroyImmediate(DragDrop.itemBeingDragged);
            }
        }
    }
    InventoryItem GetStoredItem()
    {
        return transform.GetChild(0).GetComponent<InventoryItem>();
    }

    //slotun Dolumu boşmu kontrolü 
    bool IsLimitExceded(InventoryItem draggedItem)
    {
        if((draggedItem.amountInInventory + GetStoredItem().amountInInventory)>InventorySystem.Instance.stackLimit)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}