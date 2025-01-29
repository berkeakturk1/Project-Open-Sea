using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour,IDropHandler
{
    public void OnDrop(PointerEventData eventData){
        if(transform.childCount == 0){
            InventoryItem ınventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
            ınventoryItem.parentAfterDrag = transform;
        }

        /*GameObject dropped = eventData.pointerDrag;

        InventoryItem ınventoryItem = dropped.GetComponent<InventoryItem>();
        ınventoryItem.parentAfterDrag = transform;*/
    }
}
