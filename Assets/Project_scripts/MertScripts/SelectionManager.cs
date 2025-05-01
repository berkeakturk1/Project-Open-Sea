using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; set; }

    [Header("UI Elements")]
    public TextMeshProUGUI interaction_Info_UI;

    [Header("Raycast Settings")]
    public LayerMask interactionLayerMask = Physics.DefaultRaycastLayers; // More explicit than ~0
    public float raycastDistance = 100f;

    [Header("References")]
    [SerializeField] private Camera playerCamera; // Direct reference instead of Camera.main

    [HideInInspector] public bool onTarget;

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

        if (interaction_Info_UI == null)
        {
            Debug.LogError("SelectionManager: interaction_Info_UI (TextMeshProUGUI) is not assigned!");
        }
        else
        {
            interaction_Info_UI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        onTarget = false;
        
        if (interaction_Info_UI != null)
            interaction_Info_UI.gameObject.SetActive(false);

        // Check if the pointer is over UI
        bool isOverUI = false;
        if (EventSystem.current != null)
        {
            // More robust UI check
            isOverUI = EventSystem.current.IsPointerOverGameObject();
            #if UNITY_EDITOR || UNITY_STANDALONE
            isOverUI = EventSystem.current.IsPointerOverGameObject();
            #elif UNITY_IOS || UNITY_ANDROID
            isOverUI = Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            #endif
        }

        if (isOverUI)
            return;

        // Make sure we have a camera
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red); // Visual debug

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, raycastDistance, interactionLayerMask);
        
        if (hitSomething)
        {
            GameObject hitObject = hit.transform.gameObject;
            bool isInteracting = false;

            // Log in build (can be removed in final version)
            Debug.Log($"Hit object: {hitObject.name} at distance {hit.distance}");

            // Shopkeeper logic
            ShopKeeper shop = hitObject.GetComponent<ShopKeeper>();
            if (shop && shop.playerInRange)
            {
                /*if (interaction_Info_UI != null)
                {
                    interaction_Info_UI.text = "Talk";
                    interaction_Info_UI.gameObject.SetActive(true);
                }
                isInteracting = true;

                if (Input.GetMouseButtonDown(0) && !shop.isTalkingWithPlayer)
                {
                    shop.Talk();
                }*/
            }

            // Item logic
            InteractableObject interactable = hitObject.GetComponent<InteractableObject>();
            if (interactable && interactable.playerInRange)
            {
                onTarget = true;
                if (interaction_Info_UI != null)
                {
                    interaction_Info_UI.text = interactable.GetItemName();
                    interaction_Info_UI.gameObject.SetActive(true);
                }
                isInteracting = true;
            }

            if (!isInteracting && interaction_Info_UI != null)
            {
                interaction_Info_UI.gameObject.SetActive(false);
            }
        }
    }
}