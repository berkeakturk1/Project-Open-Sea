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

    // New method to update the currency UI
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

    // New method to modify currency with validation
    public void ModifyCurrency(int amount)
    {
        currentCoins += amount;
        
        // Optional: Prevent negative currency
        if (currentCoins < 0)
        {
            currentCoins = 0;
        }
        
        UpdateCurrencyUI();
    }

    // Add method to check if player can afford an item
    public bool CanAfford(int price)
    {
        return currentCoins >= price;
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
            }
            else
            {
                Debug.LogError("Stack found but InventorySlot or itemInSlot is null!");
            }
        }
        else // stack yoksa yapıcak
        {
            whatSlotToEquip = FindNextEmptySlot();
            if (whatSlotToEquip != null)
            {
                itemToAdd = Instantiate(Resources.Load<GameObject>(itemName), whatSlotToEquip.transform.position, whatSlotToEquip.transform.rotation);
                if (itemToAdd != null)
                {
                    itemToAdd.transform.SetParent(whatSlotToEquip.transform);
                    itemList.Add(itemName);
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

    public GameObject FindNextEmptySlot()
    {
        foreach(GameObject slot in slotList)
        {
            if(slot.transform.childCount <= 1)
            {
                return slot;
            }
        }
        return new GameObject();
    }

    public bool CheckIfFull()
    {
        int counter = 0;

        foreach(GameObject slot in slotList)
        {
            if(slot.transform.childCount > 1)
            {
                counter++;
            }
        }

        if(counter == 21)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}