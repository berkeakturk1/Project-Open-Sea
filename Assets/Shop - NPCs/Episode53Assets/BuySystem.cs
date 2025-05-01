using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuySystem : MonoBehaviour
{
    #region || -- Singelton -- ||
    public static BuySystem Instance { get; set; }
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

    [Header("ShopKeeper")]
    public ShopKeeper ShopKeeper;

    [Header("UI")]
    public Transform contentTransform;
    public GameObject shopItemPrefab;
    public Button backButton;

    [Header("Current List")]
    public List<ShopItemData> currentShopList;

    void Start()
    {
        backButton.onClick.AddListener(ExitBuyMode);

        
            // Set spring list as our list
            //currentShopList = springList;

            // Initialize list
        InitializeBuyList(currentShopList);
        

    }

    private void InitializeBuyList(List<ShopItemData> shopList)
    {


        foreach (ShopItemData listItem in shopList)
        {
            GameObject prefab = Instantiate(shopItemPrefab, contentTransform);

            ShopItemSlot shopItemSlot = prefab.GetComponent<ShopItemSlot>();

            InventoryItem inventoryItem = listItem.inventoryItem.GetComponent<InventoryItem>();

            // Set the actual data
            shopItemSlot.shopItemData = listItem;

            // Setting the Name
            shopItemSlot.itemNameUI.text = inventoryItem.thisName;
            // Setting the Sprite
            shopItemSlot.itemImageUI.sprite = listItem.inventoryItem.GetComponent<Image>().sprite;
            // Setting the Price
            shopItemSlot.itemPriceUI.text = $"{listItem.itemPrice}";
        }
    }

    public void ExitBuyMode()
    {
        ShopKeeper.DialogMode();
    }

    [System.Serializable]
    public class ShopItemData
    {
        public GameObject inventoryItem;
        public int itemPrice;
    }
}
