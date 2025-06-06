using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StorageManager : MonoBehaviour
{
    public static StorageManager Instance { get; set; }

    [SerializeField] GameObject storageBoxSmallUI;
    [SerializeField] StorageBox selectedStorage;
    public bool storageUIOpen;

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

    public void OpenBox(StorageBox storage)
    {
        SetSelectedStorage(storage);

        PopulateStorage(GetRelevantUI(selectedStorage));

        GetRelevantUI(selectedStorage).SetActive(true);
        storageUIOpen = true;

        // Free cursor and disable camera rotation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable camera rotation if MouseLook exists
        DisableCameraRotation();

        SelectionManager.Instance.GetComponent<SelectionManager>().enabled = false;
    }

    private void PopulateStorage(GameObject storageUI)
    {
        // Get all slots of the ui
        List<GameObject> uiSlots = new List<GameObject>();

        foreach (Transform child in storageUI.transform)
        {
            uiSlots.Add(child.gameObject);
        }

        // Now, instantiate the prefab and set it as a child of each GameObject
        foreach (string name in selectedStorage.items)
        {
            foreach (GameObject slot in uiSlots)
            {
                if (slot.transform.childCount < 1)
                {
                    var itemToAdd = Instantiate(Resources.Load<GameObject>(name), slot.transform.position, slot.transform.rotation);
                    itemToAdd.name = name; // small fix for bugs
                    itemToAdd.transform.SetParent(slot.transform);
                    break;
                }
            }
        }
    }

    public void CloseBox()
    {
        RecalculateStorage(GetRelevantUI(selectedStorage));
        GetRelevantUI(selectedStorage).SetActive(false);
        storageUIOpen = false;

        // Lock cursor and re-enable camera rotation
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable camera rotation if MouseLook exists
        EnableCameraRotation();

        SelectionManager.Instance.GetComponent<SelectionManager>().enabled = true;
    }

    private void RecalculateStorage(GameObject storageUI)
    {
        List<GameObject> uiSlots = new List<GameObject>();

        foreach (Transform child in storageUI.transform)
        {
            uiSlots.Add(child.gameObject);
        }

        selectedStorage.items.Clear();

        List<GameObject> toBeDeleted = new List<GameObject>();

        foreach (GameObject slot in uiSlots)
        {
            if (slot.transform.childCount > 0)
            {
                // Remove "Clone" Text
                string name = slot.transform.GetChild(0).name;
                string str2 = "(Clone)";

                string result = name.Replace(str2, "");

                selectedStorage.items.Add(result);
                toBeDeleted.Add(slot.transform.GetChild(0).gameObject);
            }
        }

        foreach (GameObject obj in toBeDeleted)
        {
            Destroy(obj);
        }
    }

    public void SetSelectedStorage(StorageBox storage)
    {
        selectedStorage = storage;
    }

    private GameObject GetRelevantUI(StorageBox storage)
    {
        // Create a switch for other types
        return storageBoxSmallUI;
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