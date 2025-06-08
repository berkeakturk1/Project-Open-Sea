//
// PASTE THIS ENTIRE SCRIPT TO REPLACE YOURS
//
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; set; }

    [Header("UI Elements")]
    public GameObject interaction_Info_UI;

    [Header("Raycast Settings")]
    public LayerMask interactionLayerMask; // We will set this in the Inspector
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

        if (playerCamera == null)
        {
            //playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError("SelectionManager: No camera assigned or tagged as MainCamera!");
        }
    }

    private void Start()
    {
        onTarget = false;
        if (interaction_Info_UI != null)
        {
            interaction_text = interaction_Info_UI.GetComponent<TMP_Text>();
            interaction_Info_UI.SetActive(false);
        }
    }

    void Update()
    {
        // At the start of every frame, assume we have no target and hide the UI
        onTarget = false;
        if (interaction_Info_UI != null)
        {
            interaction_Info_UI.SetActive(false);
        }
        
        // --- DEBUGGING LOGS START HERE ---

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("DEBUG: Pointer is over a UI element. Interaction check skipped.");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("DEBUG: Player Camera is NULL!");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactionLayerMask))
        {
            // THIS IS THE MOST IMPORTANT LOG. IT TELLS US WHAT WE HIT.
            Debug.Log("DEBUG: Raycast HIT object: '" + hit.collider.name + "' on Layer: '" + LayerMask.LayerToName(hit.collider.gameObject.layer) + "'");

            GameObject hitObject = hit.collider.gameObject;
            bool isInteracting = false;

            // InteractableObject logic
            InteractableObject interactable = hitObject.GetComponent<InteractableObject>();
            if (interactable != null) // Check if the component exists
            {
                Debug.Log("DEBUG: Found InteractableObject component.");
                if (interactable.playerInRange)
                {
                    onTarget = true;
                    interaction_text.text = interactable.GetItemName();
                    interaction_Info_UI.SetActive(true);
                    isInteracting = true;
                }
            }

            // StorageBox logic
            StorageBox storageBox = hitObject.GetComponent<StorageBox>();
            if (storageBox != null) // Check if the component exists
            {
                Debug.Log("DEBUG: Found StorageBox component.");
                if (storageBox.playerInRange && PlacementSystem.Instance.inPlacementMode == false)
                {
                    interaction_text.text = storageBox.isCompactor ? "Garbage Compactor" : "Chest";
                    interaction_Info_UI.SetActive(true);
                    selectedStorageBox = storageBox.gameObject;
                    isInteracting = true;

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (storageBox.isCompactor)
                        {
                            GarbageCompactorManager.Instance?.OpenCompactor(storageBox);
                        }
                        else
                        {
                            StorageManager.Instance?.OpenBox(storageBox);
                        }
                    }
                }
            }

            // --- Other components like ShopKeeper would go here ---

        }
        else
        {
            // If the raycast doesn't hit anything on the specified layer
            Debug.Log("DEBUG: Raycast MISS.");
        }
    }
}