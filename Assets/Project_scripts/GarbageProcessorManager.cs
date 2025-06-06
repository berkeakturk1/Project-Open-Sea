using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GarbageCompactorManager : MonoBehaviour
{
    public static GarbageCompactorManager Instance { get; set; }

    [SerializeField] GameObject compactorUI;
    [SerializeField] StorageBox selectedCompactor;
    [SerializeField] GameObject outputSlot; // Special slot for the compacted result
    [SerializeField] AudioClip compactingSound;
    [SerializeField] ParticleSystem compactingEffect;
    [SerializeField] GameObject InteractionInfoUI;
    [SerializeField] GameObject dot;

    public int upgradeMatCount = 0;
    
    [Header("Compactor Settings")]
    [SerializeField] string requiredItemType = ""; // Empty = accept any item type
    [SerializeField] bool acceptAnyItemType = true; // If true, ignores requiredItemType
    [SerializeField] GameObject compactedResult; // What the compaction results in
    
    public bool compactorUIOpen;
    public bool isCompacting;
    
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

    public void OpenCompactor(StorageBox compactor)
    {
        Debug.Log("OpenCompactor called");
        
        if (compactor == null)
        {
            Debug.LogError("Compactor StorageBox is null!");
            return;
        }
        
        if (compactorUI == null)
        {
            Debug.LogError("CompactorUI is not assigned in the inspector!");
            return;
        }
        
        SetSelectedCompactor(compactor);
        
        Debug.Log($"Selected compactor has {selectedCompactor.items.Count} items");
        
        // Set the required item type from the StorageBox if it has one
        if (!compactor.acceptAnyItemType && !string.IsNullOrEmpty(compactor.requiredItemType))
        {
            requiredItemType = compactor.requiredItemType;
            acceptAnyItemType = false;
        }
        else
        {
            acceptAnyItemType = compactor.acceptAnyItemType;
            requiredItemType = compactor.requiredItemType;
        }
        
        PopulateCompactor(compactorUI);
        compactorUI.SetActive(true);
        compactorUIOpen = true;

        // Toggle off interaction UI elements
        if (InteractionInfoUI != null)
        {
            InteractionInfoUI.SetActive(false);
        }
        
        if (dot != null)
        {
            dot.SetActive(false);
        }

        // Free cursor and disable camera rotation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable camera rotation if MouseLook exists
        DisableCameraRotation();

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
        }
        
        // Also disable StorageManager UI state
        if (StorageManager.Instance != null)
        {
            StorageManager.Instance.storageUIOpen = true;
        }
        
        Debug.Log("OpenCompactor completed successfully");
    }

    private void PopulateCompactor(GameObject compactorUI)
    {
        // Clear any existing items from input slots first
        ClearInputSlots();
        
        List<GameObject> inputSlots = GetInputSlots(compactorUI);

        // Populate input slots with items from storage
        int itemIndex = 0;
        foreach (string itemName in selectedCompactor.items)
        {
            if (itemIndex < inputSlots.Count)
            {
                // Skip UI elements that might have been stored as "items"
                if (IsUIElement(itemName))
                {
                    Debug.LogWarning($"Skipping UI element: {itemName}");
                    continue;
                }
                
                // Try to load the item from Resources
                GameObject itemPrefab = Resources.Load<GameObject>(itemName);
                
                if (itemPrefab != null)
                {
                    var itemToAdd = Instantiate(itemPrefab, 
                                              inputSlots[itemIndex].transform.position, 
                                              inputSlots[itemIndex].transform.rotation);
                    itemToAdd.name = itemName;
                    itemToAdd.transform.SetParent(inputSlots[itemIndex].transform);
                    itemIndex++;
                }
                else
                {
                    Debug.LogError($"Could not load item '{itemName}' from Resources folder. Make sure the prefab exists in a Resources folder.");
                }
            }
        }
    }
    
    private bool IsUIElement(string itemName)
    {
        // Check if the item name contains common UI element indicators
        return itemName.Contains("Text") || 
               itemName.Contains("Button") || 
               itemName.Contains("Image") || 
               itemName.Contains("Panel") ||
               itemName.Contains("Canvas") ||
               itemName.Contains("TMP") ||
               itemName.Contains("UI");
    }
    
    private void ClearInputSlots()
    {
        if (compactorUI == null) return;
        
        List<GameObject> inputSlots = GetInputSlots(compactorUI);
        
        foreach (GameObject slot in inputSlots)
        {
            // Only destroy actual item objects, not UI elements
            for (int i = slot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = slot.transform.GetChild(i);
                
                // Only destroy objects that were instantiated items, not permanent UI
                if (child.name.Contains("(Clone)") || !IsUIElement(child.name))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    public void CloseCompactor()
    {
        if (selectedCompactor != null)
        {
            RecalculateCompactor(compactorUI);
        }
        
        compactorUI.SetActive(false);
        compactorUIOpen = false;

        // Toggle on interaction UI elements
        if (InteractionInfoUI != null)
        {
            InteractionInfoUI.SetActive(true);
        }
        
        if (dot != null)
        {
            dot.SetActive(true);
        }

        // Lock cursor and re-enable camera rotation
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable camera rotation if MouseLook exists
        EnableCameraRotation();

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
        }
        
        // Also reset StorageManager UI state
        if (StorageManager.Instance != null)
        {
            StorageManager.Instance.storageUIOpen = false;
        }
    }

    private void RecalculateCompactor(GameObject compactorUI)
    {
        if (selectedCompactor == null) return;
        
        List<GameObject> inputSlots = GetInputSlots(compactorUI);
        selectedCompactor.items.Clear();
        List<GameObject> toBeDeleted = new List<GameObject>();

        // Collect items from input slots (excluding UI elements)
        foreach (GameObject slot in inputSlots)
        {
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                Transform child = slot.transform.GetChild(i);
                
                // Only collect actual items, not UI elements
                if (!IsUIElement(child.name))
                {
                    string name = child.name;
                    string cleanName = name.Replace("(Clone)", "");
                    selectedCompactor.items.Add(cleanName);
                    toBeDeleted.Add(child.gameObject);
                }
            }
        }

        // Also check output slot for any completed item
        if (outputSlot != null)
        {
            for (int i = 0; i < outputSlot.transform.childCount; i++)
            {
                Transform child = outputSlot.transform.GetChild(i);
                
                // Only collect actual items, not UI elements
                if (!IsUIElement(child.name))
                {
                    string name = child.name;
                    string cleanName = name.Replace("(Clone)", "");
                    selectedCompactor.items.Add(cleanName);
                    toBeDeleted.Add(child.gameObject);
                }
            }
        }

        // Clean up instantiated item objects only
        foreach (GameObject obj in toBeDeleted)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        // Reset compactor state
        selectedCompactor = null;
    }

    public void TryCompactItems()
    {
        if (isCompacting) return;

        List<GameObject> inputSlots = GetInputSlots(compactorUI);
        Dictionary<string, List<GameObject>> itemGroups = new Dictionary<string, List<GameObject>>();

        // Group items by type - check ALL children, not just the first one
        foreach (GameObject slot in inputSlots)
        {
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                Transform child = slot.transform.GetChild(i);
                
                // Skip UI elements, only count actual items
                if (!IsUIElement(child.name))
                {
                    string itemName = child.name.Replace("(Clone)", "");
                    
                    if (!itemGroups.ContainsKey(itemName))
                    {
                        itemGroups[itemName] = new List<GameObject>();
                    }
                    
                    itemGroups[itemName].Add(child.gameObject);
                }
            }
        }

        Debug.Log($"Item counts found:");
        foreach (var kvp in itemGroups)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value.Count} items");
        }

        // Check if we have exactly 4 of the required item type
        foreach (var kvp in itemGroups)
        {
            if (kvp.Value.Count == 4)  // Changed from 5 to 4
            {
                // Check if this item type is allowed
                if (acceptAnyItemType || string.IsNullOrEmpty(requiredItemType) || kvp.Key == requiredItemType)
                {
                    // Instant compaction - no coroutine
                    CompactItemsInstant(kvp.Key, kvp.Value);
                    upgradeMatCount++;
                    return;
                }
                else
                {
                    // Wrong item type
                    Debug.Log($"Compaction failed: This compactor only accepts {requiredItemType}, but you have 4 {kvp.Key}.");
                    ShowCompactionFailedFeedback($"This compactor only accepts {requiredItemType}");
                    return;
                }
            }
        }

        // If we reach here, compaction failed due to count
        if (acceptAnyItemType || string.IsNullOrEmpty(requiredItemType))
        {
            Debug.Log("Compaction failed: Need exactly 4 identical items.");
            ShowCompactionFailedFeedback("Need exactly 4 identical items");
        }
        else
        {
            Debug.Log($"Compaction failed: Need exactly 4 {requiredItemType} items.");
            ShowCompactionFailedFeedback($"Need exactly 4 {requiredItemType} items");
        }
    }

    private void CompactItemsInstant(string inputItem, List<GameObject> itemsToRemove)
    {
        isCompacting = true;
        
        // Play compacting effects (but don't wait)
        if (compactingSound != null)
            AudioSource.PlayClipAtPoint(compactingSound, transform.position);
        
        if (compactingEffect != null)
            compactingEffect.Play();

        // Remove the 4 input items immediately
        foreach (GameObject item in itemsToRemove)
        {
            if (item != null)
                Destroy(item);
        }

        // Create 1 output item immediately
        CreateCompactedOutputItem();

        isCompacting = false;
        
        Debug.Log($"Successfully compacted {itemsToRemove.Count} {inputItem} into 1 compacted result!");
        ShowCompactionSuccessFeedback();
    }

    private IEnumerator CompactItemsCoroutine(string inputItem, List<GameObject> itemsToRemove)
    {
        isCompacting = true;
        
        // Play compacting effects
        if (compactingSound != null)
            AudioSource.PlayClipAtPoint(compactingSound, transform.position);
        
        if (compactingEffect != null)
            compactingEffect.Play();

        // Wait for compaction animation/effect
        yield return new WaitForSeconds(2f);

        // Remove the 4 input items
        foreach (GameObject item in itemsToRemove)
        {
            if (item != null)
                Destroy(item);
        }

        // Create 1 output item (same type as input, just compacted)
        CreateOutputItem(inputItem);

        isCompacting = false;
        
        Debug.Log($"Successfully compacted {itemsToRemove.Count} {inputItem} into 1 output item!");
        ShowCompactionSuccessFeedback();
    }

    private void CreateCompactedOutputItem()
    {
        if (outputSlot == null)
        {
            Debug.LogError("Output slot is not assigned!");
            return;
        }
        
        if (compactedResult == null)
        {
            Debug.LogError("Compacted result prefab is not assigned in the inspector!");
            return;
        }
        
        // Clear output slot of any existing items (but preserve UI elements)
        for (int i = outputSlot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = outputSlot.transform.GetChild(i);
            
            // Only destroy actual items, not UI elements
            if (!IsUIElement(child.name))
            {
                Destroy(child.gameObject);
            }
        }

        // Create the specified compacted result item
        var newOutputItem = Instantiate(compactedResult, outputSlot.transform.position, outputSlot.transform.rotation);
        newOutputItem.name = compactedResult.name;
        newOutputItem.transform.SetParent(outputSlot.transform);
    }

    private void CreateOutputItem(string outputItemName)
    {
        // Clear output slot first
        if (outputSlot.transform.childCount > 0)
        {
            Destroy(outputSlot.transform.GetChild(0).gameObject);
        }

        // Create new output item (same as input item)
        var outputItem = Instantiate(Resources.Load<GameObject>(outputItemName), 
                                   outputSlot.transform.position, 
                                   outputSlot.transform.rotation);
        outputItem.name = outputItemName;
        outputItem.transform.SetParent(outputSlot.transform);
    }

    private List<GameObject> GetInputSlots(GameObject compactorUI)
    {
        List<GameObject> inputSlots = new List<GameObject>();
        
        if (compactorUI == null)
        {
            Debug.LogError("CompactorUI is null!");
            return inputSlots;
        }
        
        if (outputSlot == null)
        {
            Debug.LogError("OutputSlot is not assigned in the inspector!");
            return inputSlots;
        }
        
        // Get all child objects that are actual input slots
        foreach (Transform child in compactorUI.transform)
        {
            // Skip the output slot
            if (child.gameObject == outputSlot)
                continue;
                
            // Only include objects that are specifically input slots
            // Option 1: Use naming convention
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("inputslot") || 
                lowerName.Contains("input_slot") || 
                lowerName.Contains("slot") && !lowerName.Contains("output"))
            {
                inputSlots.Add(child.gameObject);
                continue;
            }
            
            // Option 2: Skip known non-slot UI elements
            if (IsNonSlotUIElement(child.name))
                continue;
                
            // Option 3: If it's not clearly a UI element and not the output slot, 
            // assume it's an input slot (fallback)
            if (!IsUIElement(child.name))
            {
                inputSlots.Add(child.gameObject);
            }
        }
        
        Debug.Log($"Found {inputSlots.Count} input slots");
        foreach (GameObject slot in inputSlots)
        {
            Debug.Log($"Input slot: {slot.name}");
        }
        return inputSlots;
    }
    
    private bool IsNonSlotUIElement(string objectName)
    {
        // Check if the object is a UI element that's NOT a slot
        string lowerName = objectName.ToLower();
        
        return lowerName.Contains("button") || 
               lowerName.Contains("background") || 
               lowerName.Contains("panel") ||
               lowerName.Contains("title") ||
               lowerName.Contains("header") ||
               lowerName.Contains("text") ||
               lowerName.Contains("label") ||
               lowerName.Contains("image") && !lowerName.Contains("slot");
    }

    private void ShowCompactionSuccessFeedback()
    {
        // Add visual/audio feedback for successful compaction
        // This could be UI notifications, screen effects, etc.
    }

    private void ShowCompactionFailedFeedback()
    {
        ShowCompactionFailedFeedback("Compaction failed");
    }

    private void ShowCompactionFailedFeedback(string message)
    {
        // Add feedback for failed compaction attempts
        // This could be UI notifications, error sounds, etc.
        Debug.Log(message);
        
        // You can add UI popup, sound effects, screen shake, etc. here
        // Example: ShowUIMessage(message);
    }

    public void SetSelectedCompactor(StorageBox compactor)
    {
        selectedCompactor = compactor;
    }
    public void removeMoney(int amount)
    {
        if (upgradeMatCount >= amount)
        {
            upgradeMatCount -= amount;
        }
        else
        {
            Debug.LogWarning("Not enough upgrade materials to remove!");
        }
    }
    private void DisableCameraRotation()
    {
        // Find and disable the RigidbodyFirstPersonController
        var firstPersonController = FindObjectOfType<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>();
        if (firstPersonController != null)
        {
            var mouseLook = firstPersonController.mouseLook;
            if (mouseLook != null)
            {
                mouseLook.SetCursorLock(false);
            }
            firstPersonController.enabled = false;
        }
        
        // Also try to find and disable any other camera rotation scripts as fallback
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Try to find any script with "Mouse" or "Camera" in the name
            var allScripts = mainCamera.GetComponents<MonoBehaviour>();
            foreach (var script in allScripts)
            {
                if (script.GetType().Name.Contains("Mouse") || 
                    script.GetType().Name.Contains("Camera") ||
                    script.GetType().Name.Contains("Look"))
                {
                    script.enabled = false;
                }
            }
        }
    }
    
    private void EnableCameraRotation()
    {
        // Find and re-enable the RigidbodyFirstPersonController
        var firstPersonController = FindObjectOfType<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>();
        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
            var mouseLook = firstPersonController.mouseLook;
            if (mouseLook != null)
            {
                mouseLook.SetCursorLock(true);
            }
        }
        
        // Also try to find and re-enable any other camera rotation scripts as fallback
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Try to find any script with "Mouse" or "Camera" in the name
            var allScripts = mainCamera.GetComponents<MonoBehaviour>();
            foreach (var script in allScripts)
            {
                if (script.GetType().Name.Contains("Mouse") || 
                    script.GetType().Name.Contains("Camera") ||
                    script.GetType().Name.Contains("Look"))
                {
                    script.enabled = true;
                }
            }
        }
    }
}