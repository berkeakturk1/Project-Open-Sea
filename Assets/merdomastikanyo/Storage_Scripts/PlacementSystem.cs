using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; set; }

    public GameObject placementHoldingSpot; // Drag our construcionHoldingSpot or a new placementHoldingSpot
    public Transform Test;
    public GameObject ChestPrefab;

    public bool inPlacementMode;
    [SerializeField] bool isValidPlacement;

    [SerializeField] GameObject itemToBePlaced;
    public GameObject inventoryItemToDestory;

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

    public void ActivatePlacementMode(string itemToPlace)
    {
        // Validation
        if (string.IsNullOrEmpty(itemToPlace))
        {
            Debug.LogError("[PlacementSystem] Item to place is null or empty");
            return;
        }

        if (placementHoldingSpot == null)
        {
            Debug.LogError("[PlacementSystem] Placement holding spot is not assigned");
            return;
        }

        // Clean up previous item
        if (itemToBePlaced != null)
        {
            DestroyImmediate(itemToBePlaced);
        }

        // Get the correct prefab based on itemToPlace
        GameObject prefabToUse = ChestPrefab;
        if (prefabToUse == null)
        {
            Debug.LogError($"[PlacementSystem] No prefab found for item: {itemToPlace}");
            return;
        }

        // Instantiate the correct item
        GameObject item = Instantiate(prefabToUse);
        item.name = itemToPlace;
        item.transform.SetParent(placementHoldingSpot.transform, false);

        itemToBePlaced = item;
        inPlacementMode = true;
    }

    private GameObject GetPrefabByName(string itemName)
    {
        // You'll need to implement this based on how you store your prefabs
        // Could be a Dictionary<string, GameObject> or similar
        return null; // Replace with actual implementation
    }



    private void Update()
    {

        if (inPlacementMode)
        {
           // Display UI for player, letting him know how to place item
        }
        else
        {
            // Disable UI
        }

        if (itemToBePlaced != null && inPlacementMode)
        {
            if (IsCheckValidPlacement())
            {
                isValidPlacement = true;
                itemToBePlaced.GetComponent<PlacebleItem>().SetValidColor();
            }
            else
            {
                isValidPlacement = false;
                itemToBePlaced.GetComponent<PlacebleItem>().SetInvalidColor();
            }
        }

        // Left Mouse Click to Place item
        if (Input.GetMouseButtonDown(0) && inPlacementMode && isValidPlacement)
        {
            PlaceItemFreeStyle();
            DestroyItem(inventoryItemToDestory);
        }

        // Cancel Placement                     //TODO - don't destroy the ui item until you actually placed it.
        if (Input.GetKeyDown(KeyCode.X))
        {
            inventoryItemToDestory.SetActive(true);
            inventoryItemToDestory = null;
            DestroyItem(itemToBePlaced);
            itemToBePlaced = null;
            inPlacementMode = false;
        }
    }

    private bool IsCheckValidPlacement()
    {
        if (itemToBePlaced != null)
        {
            return itemToBePlaced.GetComponent<PlacebleItem>().isValidToBeBuilt;
        }

        return false;
    }

    private void PlaceItemFreeStyle()
    {
        // Setting the parent to be the root of our scene
        itemToBePlaced.transform.SetParent(Test, true);

        // Setting the default color/material
        itemToBePlaced.GetComponent<PlacebleItem>().SetDefaultColor();
        itemToBePlaced.GetComponent<PlacebleItem>().enabled = false;

        itemToBePlaced = null;

        StartCoroutine(delay());
    }

    IEnumerator delay(){
        yield return new WaitForSeconds(1f);

        inPlacementMode = false;
    }

    private void DestroyItem(GameObject item)
    {
        DestroyImmediate(item);
        //InventorySystem.Instance.ReCalculateList();
        //CraftingSystem.Instance.RefreshNeededItems();
    }
}