using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
 
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }
 
    // -- UI -- //
    public GameObject quickSlotsPanel;
 
    public List<GameObject> quickSlotsList = new List<GameObject>();
    public List<string> itemList = new List<string>();
 
   public GameObject numbersHolder;
   public int selectedNumber = -1;
   public GameObject selectedItem;

   public GameObject toolHolder;
   public GameObject selectedItemModel;//tekrar 1 e basınca item elimizden gitmesi için veya diğer itemlar için global değişken

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
 
 
    private void Start()
    {
        PopulateSlotList();
    }


    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectQuickSlot(1);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectQuickSlot(2);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectQuickSlot(3);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectQuickSlot(4);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectQuickSlot(5);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectQuickSlot(6);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            SelectQuickSlot(7);
        }
        

    }


void SelectQuickSlot(int number)
{
    // First check if the slot number is valid
    if (number < 1 || number > quickSlotsList.Count)
    {
        Debug.LogWarning("Trying to select invalid slot number: " + number);
        return;
    }

    // Check if slot is full before proceeding
    if (checkIfSlotIsFull(number))
    {
        if (selectedNumber != number)
        {
            selectedNumber = number;
            
            // Previous item check is selected?
            if (selectedItem != null)
            {
                InventoryItem inventoryItem = selectedItem.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    inventoryItem.isSelected = false;
                }
            }

            // New item selected
            selectedItem = getSelectedItem(number);
            if (selectedItem != null)
            {
                InventoryItem inventoryItem = selectedItem.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    inventoryItem.isSelected = true;
                }
            }

            //Yeni fonk
            SetEquippedModel(selectedItem);



            // Changing Color - with null checks
            if (numbersHolder != null)
            {
                foreach (Transform child in numbersHolder.transform)
                {
                    Transform textTransform = child.transform.Find("Text");
                    if (textTransform != null)
                    {
                        TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            textComponent.color = Color.gray;
                        }
                    }
                }
                
                Transform numberTransform = numbersHolder.transform.Find("number" + number);
                if (numberTransform != null)
                {
                    Debug.Log("pressed number " + number);
                    Transform textTransform = numberTransform.transform.GetChild(0);
                    if (textTransform != null)
                    {
                        Debug.Log("found Item");
                        TextMeshProUGUI toBeChanged = textTransform.GetComponent<TextMeshProUGUI>();
                        if (toBeChanged != null)
                        {
                            toBeChanged.color = Color.white;
                        }
                    }
                }
            }
        }
        else // If we are trying to select the same slot
        {
            selectedNumber = -1; // Set to -1 instead of decrementing to avoid issues
            
            // Previous item check is selected?
            if (selectedItem != null)
            {
                InventoryItem inventoryItem = selectedItem.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    inventoryItem.isSelected = false;
                }
                selectedItem = null; // Clear selected item
            }

            //item elimizdeyken aynı sayıya basarsak elimizden gidecek
            if(selectedItemModel != null)
            {
                DestroyImmediate(selectedItemModel.gameObject);
                selectedItemModel = null;
            }

            // Changing Color - with null checks
            if (numbersHolder != null)
            {
                foreach (Transform child in numbersHolder.transform)
                {
                    Transform textTransform = child.transform.GetChild(0);
                    if (textTransform != null)
                    {
                        TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            textComponent.color = Color.gray;
                        }
                    }
                }
            }
        }
    }
}
    //YENİ FONKSİYONN
    private void SetEquippedModel(GameObject selectedItem)
    {
         if(selectedItemModel != null)
          {
              DestroyImmediate(selectedItemModel.gameObject);
              selectedItemModel = null;
          }

        string selectedItemName = selectedItem.name.Replace("(Clone)","");
        //From resoursec folder
        selectedItemModel = Instantiate(Resources.Load<GameObject>(selectedItemName+"_Model"),
        new Vector3(0.72f,-0.25f,0.98f),Quaternion.Euler(0,180f,90f));
        selectedItemModel.transform.SetParent(toolHolder.transform,false);
    }

    GameObject getSelectedItem(int slotNumber)
    {
        return quickSlotsList[slotNumber-1].transform.GetChild(0).gameObject;
    }

    bool checkIfSlotIsFull(int slotNumber)
    {
        if(quickSlotsList[slotNumber-1].transform.childCount >0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    private void PopulateSlotList()
    {
        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot"))
            {
                quickSlotsList.Add(child.gameObject);
            }
        }
    }
 
    public void AddToQuickSlots(GameObject itemToEquip)
    {
        // Find next free slot
        GameObject availableSlot = FindNextEmptySlot();
        // Set transform of our object
        itemToEquip.transform.SetParent(availableSlot.transform, false);
        // Getting clean name
        string cleanName = itemToEquip.name.Replace("(Clone)", "");
        // Adding item to list
        itemList.Add(cleanName);
 
        //InventorySystem.Instance.ReCalculateList();
 
    }
 
 
    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return new GameObject();
    }
 
    public bool CheckIfFull()
    {
 
        int counter = 0;
 
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }
 
        if (counter == 7)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}