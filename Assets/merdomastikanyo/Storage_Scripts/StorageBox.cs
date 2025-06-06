using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageBox : MonoBehaviour{

    public bool playerInRange;

    [SerializeField] public List<string> items;
    
    
    [Header("Compactor Settings (Only used if isCompactor = true)")]
    public string requiredItemType = ""; // Empty = accept any item type
    public bool acceptAnyItemType = true; // If true, ignores requiredItemType
    
    [Header("Type Settings")]
    public bool isCompactor = false; 
    
    public enum BoxType{
        smallBox,
        bigBox
    }

    public BoxType thisBoxType;

    private void Update(){
        if (PlayerState.Instance == null || PlayerState.Instance.playerBody == null) return;
        float distance = Vector3.Distance(PlayerState.Instance.playerBody.transform.position, transform.position);

        if(distance < 10f){
            playerInRange = true;
        }
        else{
            playerInRange = false;
        }
    }

    public void appendToList(string itemName)
    {
        items.Add(itemName);
    }
}
