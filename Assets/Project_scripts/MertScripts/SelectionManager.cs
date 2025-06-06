using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; set; }

    [Header("UI Elements")]
    public GameObject interaction_Info_UI;
    
    [Header("Raycast Settings")]
    public LayerMask interactionLayerMask = Physics.DefaultRaycastLayers;
    public float raycastDistance = 100f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [HideInInspector] public bool onTarget;
    
    private TMP_Text interaction_text;
    public GameObject selectedStorageBox;

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
        
        // Fallback to Camera.main if no camera is assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError("SelectionManager: No camera assigned or tagged as MainCamera!");
        }
    }

    private void Start()
    {
        onTarget = false;
        
        // Get TMP_Text component from interaction_Info_UI
        if (interaction_Info_UI != null)
        {
            interaction_text = interaction_Info_UI.GetComponent<TMP_Text>();
            if (interaction_text == null)
            {
                Debug.LogError("TMP_Text component not found on interaction_Info_UI!");
            }
            interaction_Info_UI.SetActive(false);
        }
        else
        {
            Debug.LogError("SelectionManager: interaction_Info_UI is not assigned!");
        }
    }

    void Update()
    {
        onTarget = false;
        
        if (interaction_Info_UI != null)
            interaction_Info_UI.SetActive(false);

        // Check if the pointer is over UI
        bool isOverUI = false;
        if (EventSystem.current != null)
        {
            isOverUI = EventSystem.current.IsPointerOverGameObject();
            #if UNITY_IOS || UNITY_ANDROID
            isOverUI = Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            #endif
        }

        if (isOverUI)
            return;

        // Make sure we have a camera
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, raycastDistance, interactionLayerMask))
        {
            var selectionTransform = hit.transform;
            GameObject hitObject = selectionTransform.gameObject;
            bool isInteracting = false;

            // Shopkeeper logic with F key
            ShopKeeper shop = hitObject.GetComponent<ShopKeeper>();
            if (shop && shop.playerInRange)
            {
                if (interaction_text != null)
                {
                    interaction_text.text = "Press F to Talk";
                    interaction_Info_UI.SetActive(true);
                }
                isInteracting = true;

                if (Input.GetKeyDown(KeyCode.F) && !shop.isTalkingWithPlayer)
                {
                    shop.Talk();
                }
            }

            // InteractableObject logic
            InteractableObject interactable = selectionTransform.GetComponent<InteractableObject>();
            if (interactable && interactable.playerInRange)
            {
                onTarget = true;
                if (interaction_text != null)
                {
                    interaction_text.text = interactable.GetItemName();
                    interaction_Info_UI.SetActive(true);
                }
                isInteracting = true;
            }

            // StorageBox logic - Now handles both regular storage and compactors
            StorageBox storageBox = selectionTransform.GetComponent<StorageBox>();
            if (storageBox && storageBox.playerInRange && PlacementSystem.Instance.inPlacementMode == false)
            {
                // Check if this is a compactor or regular storage
                bool isCompactor = storageBox.isCompactor; // You'll need to add this field to StorageBox
                
                if (interaction_text != null)
                {
                    // Show different text based on type
                    interaction_text.text = isCompactor ? "Garbage Compactor" : "Chest";
                    interaction_Info_UI.SetActive(true);
                }
                selectedStorageBox = storageBox.gameObject;
                isInteracting = true;

                if (Input.GetMouseButtonDown(0))
                {
                    // Route to appropriate manager based on type
                    if (isCompactor)
                    {
                        if (GarbageCompactorManager.Instance != null)
                        {
                            GarbageCompactorManager.Instance.OpenCompactor(storageBox);
                        }
                        else
                        {
                            Debug.LogError("GarbageCompactorManager not found! Make sure it's in the scene.");
                        }
                    }
                    else
                    {
                        StorageManager.Instance.OpenBox(storageBox);
                    }
                }
            }
            else
            {
                if (selectedStorageBox != null && !isInteracting)
                {
                    selectedStorageBox = null;
                }
            }

            // If no interaction is happening, hide the UI
            if (!isInteracting && interaction_Info_UI != null)
            {
                interaction_Info_UI.SetActive(false);
            }
        }
        else
        {
            onTarget = false;
            if (interaction_Info_UI != null)
                interaction_Info_UI.SetActive(false);
            
            if (selectedStorageBox != null)
            {
                selectedStorageBox = null;
            }
        }
    }
}