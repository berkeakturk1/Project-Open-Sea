using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class SimpleLadderSystem : MonoBehaviour
{
    [Header("Ladder Settings")]
    public float climbSpeed = 2.5f;       // How fast the player climbs
    public float horizontalPushback = 0.2f; // How much to push player toward ladder during climbing
    
    // Cache components
    private BoxCollider ladderCollider;
    private bool playerIsOnLadder = false;
    private Rigidbody playerRigidbody;
    private Transform playerTransform;
    
    private void Start()
    {
        // Get ladder collider
        ladderCollider = GetComponent<BoxCollider>();
        if (ladderCollider == null)
        {
            Debug.LogError("Ladder needs a BoxCollider component");
            enabled = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if this is the player
        RigidbodyFirstPersonController controller = other.GetComponent<RigidbodyFirstPersonController>();
        if (controller != null)
        {
            // Cache references to player components
            playerRigidbody = controller.GetComponent<Rigidbody>();
            playerTransform = controller.transform;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Skip if we don't have player references
        if (playerRigidbody == null || playerTransform == null) return;
        
        // Check if this is the player
        if (other.GetComponent<RigidbodyFirstPersonController>() != null)
        {
            // Check if player is looking towards the ladder (forward axis is -Z)
            bool facingLadder = Vector3.Dot(playerTransform.forward, -transform.forward) > 0;
            
            // Get input
            float verticalInput = Input.GetAxis("Vertical");
            
            // Only climb if there's vertical input
            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                // Temporarily override physics
                playerRigidbody.useGravity = false;
                
                // Calculate direction - flip based on whether player is facing ladder
                float climbDirection = facingLadder ? verticalInput : -verticalInput;
                
                // Apply vertical movement
                Vector3 climbVelocity = new Vector3(
                    0f,
                    climbDirection * climbSpeed,
                    0f
                );
                
                // Apply velocity directly
                playerRigidbody.velocity = new Vector3(
                    playerRigidbody.velocity.x * 0.9f, // Gradually reduce horizontal momentum
                    climbVelocity.y,
                    playerRigidbody.velocity.z * 0.9f
                );
                
                // Keep player close to ladder with a gentle push
                Vector3 ladderCenter = transform.position;
                Vector3 directionToLadder = (ladderCenter - playerTransform.position).normalized;
                directionToLadder.y = 0; // Only push horizontally
                
                playerRigidbody.AddForce(directionToLadder * horizontalPushback, ForceMode.VelocityChange);
                
                playerIsOnLadder = true;
            }
            else if (playerIsOnLadder)
            {
                // Restore normal physics when not climbing
                playerRigidbody.useGravity = true;
                playerIsOnLadder = false;
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Reset physics when leaving ladder
        if (other.GetComponent<RigidbodyFirstPersonController>() != null)
        {
            if (playerRigidbody != null)
            {
                playerRigidbody.useGravity = true;
            }
            
            playerIsOnLadder = false;
            
            // Clear references
            playerRigidbody = null;
            playerTransform = null;
        }
    }
}