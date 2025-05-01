using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShipTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform teleportDestination;
    public float interactionDistance = 60f; // Increased to match your actual distances
    public string shipName = "Ship";
    public KeyCode interactionKey = KeyCode.F;
    
    [Header("UI Settings")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI promptText;
    
    // For storing the player reference
    private GameObject playerObject;
    
    // Added collision detection as a backup for looking at ship
    private bool playerInTriggerZone = false;
    
    void Start()
    {
        // Find player by tag
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Player object not found! Make sure your player has the 'Player' tag");
        }
        
        // Hide UI at start
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // Make sure there's a collider for trigger detection
        Collider shipCollider = GetComponent<Collider>();
        if (shipCollider == null)
        {
            Debug.LogError("Ship needs a collider! Adding a box collider.");
            BoxCollider newCollider = gameObject.AddComponent<BoxCollider>();
            newCollider.isTrigger = true;
            newCollider.size = new Vector3(5, 5, 5); // Adjust size as needed
        }
        else if (!shipCollider.isTrigger)
        {
            // Create a separate trigger collider if the main collider isn't a trigger
            GameObject triggerObject = new GameObject("TriggerZone");
            triggerObject.transform.parent = transform;
            triggerObject.transform.localPosition = Vector3.zero;
            
            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(10, 10, 10); // Make it larger than the ship
        }
        
        Debug.Log("ShipTeleporter initialized on " + gameObject.name);
    }
    
    void Update()
    {
        bool canInteract = CheckIfPlayerCanInteract();
        UpdateUI(canInteract);
        
        // Handle teleportation if F is pressed and player can interact
        if (canInteract && Input.GetKeyDown(interactionKey))
        {
            TeleportPlayer();
        }
    }
    
    bool CheckIfPlayerCanInteract()
    {
        if (playerObject == null) return false;
        
        // Check if player is in range
        float distance = Vector3.Distance(transform.position, playerObject.transform.position);
        bool isInRange = (distance <= interactionDistance);
        
        // Use either the trigger zone OR distance check
        return playerInTriggerZone || isInRange;
    }
    
    void UpdateUI(bool canInteract)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(canInteract);
            
            if (canInteract && promptText != null)
            {
                promptText.text = $"Press {interactionKey} to board {shipName}";
            }
        }
    }
    
    void TeleportPlayer()
    {
        if (teleportDestination == null || playerObject == null)
        {
            Debug.LogError("Teleport failed: Missing teleport destination or player reference");
            return;
        }
        
        Debug.Log($"Teleporting player to {teleportDestination.position}");
        
        // Teleport the player
        playerObject.transform.position = teleportDestination.position;
        
        
        
        // Fix for Character Controller if present
        CharacterController cc = playerObject.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.enabled = true;
        }
        
        Debug.Log("Player teleported to " + shipName);
    }
    
    // Trigger detection as backup for "looking at ship"
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTriggerZone = true;
            Debug.Log("Player entered ship trigger zone");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTriggerZone = false;
            Debug.Log("Player exited ship trigger zone");
        }
    }
    
    // Draw the interaction range in the editor
    void OnDrawGizmos()
    {
        // Interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Teleport destination
        if (teleportDestination != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(teleportDestination.position, 1f);
            Gizmos.DrawLine(transform.position, teleportDestination.position);
        }
    }
}