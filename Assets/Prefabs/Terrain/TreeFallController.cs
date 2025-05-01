using UnityEngine;
using System.Collections;

public class TreeFallController : MonoBehaviour
{
    [SerializeField] private int maxHits = 5;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private string axeTag = "AxeCollider";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode axeAttackKey = KeyCode.Mouse0; // Left mouse button
    
    [Header("Drop Settings")]
    [SerializeField] private GameObject dropPrefab; // The prefab to drop when tree is destroyed
    [SerializeField] private int minDropCount = 1; // Minimum number of prefabs to drop
    [SerializeField] private int maxDropCount = 3; // Maximum number of prefabs to drop
    [SerializeField] private float dropRadius = 1.5f; // Radius in which drops will scatter
    [SerializeField] private float dropUpwardForce = 2f; // How much upward force to apply to drops
    
    // Internal variables
    private int currentHits = 0;
    private bool isFalling = false;
    private Rigidbody rb;
    private bool playerInRange = false;
    private GameObject currentPlayer = null;

    private void Start()
    {
        // Get or add a Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        Debug.Log("Tree initialized. Waiting for axe hits.");
    }

    private void Update()
    {
        // Check if player is in range and swinging axe
        if (playerInRange && currentPlayer != null)
        {
            if (Input.GetKeyDown(axeAttackKey))
            {
                Debug.Log("Player swung axe while in range of tree!");
                TakeHit();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;
        Debug.Log($"Collision detected with: {hitObject.name}, tag: {hitObject.tag}");
        
        // Track when player enters range
        if (hitObject.CompareTag(playerTag))
        {
            Debug.Log("Player in range of tree");
            playerInRange = true;
            currentPlayer = hitObject;
        }
        
        // Still check for direct axe hits as fallback
        CheckForAxeInHierarchy(hitObject);
    }
    
    private void OnCollisionExit(Collision collision)
    {
        // Track when player leaves range
        if (collision.gameObject.CompareTag(playerTag))
        {
            Debug.Log("Player left range of tree");
            playerInRange = false;
            currentPlayer = null;
        }
    }
    
    // Add similar logic for trigger if needed
    private void OnTriggerEnter(Collider other)
    {
        GameObject hitObject = other.gameObject;
        Debug.Log($"Trigger detected with: {hitObject.name}, tag: {hitObject.tag}");
        
        if (hitObject.CompareTag(playerTag))
        {
            Debug.Log("Player in trigger range of tree");
            playerInRange = true;
            currentPlayer = hitObject;
        }
        
        CheckForAxeInHierarchy(hitObject);
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player left trigger range of tree");
            playerInRange = false;
            currentPlayer = null;
        }
    }

    private void CheckForAxeInHierarchy(GameObject hitObject)
    {
        // Direct check if the object itself is the axe
        if (hitObject.CompareTag(axeTag) && !isFalling)
        {
            Debug.Log("Direct axe hit detected!");
            TakeHit();
            return;
        }
        
        // Check if this is the axe collider component of the player
        if (hitObject.name.Contains("Axe") || hitObject.name.Contains("axe"))
        {
            Debug.Log("Found object with 'axe' in the name: " + hitObject.name);
            TakeHit();
            return;
        }
    }

    private void TakeHit()
    {
        if (isFalling) return;
        
        currentHits++;
        Debug.Log($"Tree hit! Current hits: {currentHits}/{maxHits}");

        // Check if we've reached max hits
        if (currentHits >= maxHits)
        {
            FallDown();
        }
    }

    private void FallDown()
    {
        if (isFalling) return;
        isFalling = true;
        
        Debug.Log("Tree falling now!");

        // Set rigidbody to non-kinematic and enable gravity
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Add a slight torque to make it fall in a semi-random direction
        rb.AddTorque(new Vector3(Random.Range(-50f, 50f), 0, Random.Range(-50f, 50f)));

        // Start the destroy timer
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        
        // Drop resources before destroying the tree
        SpawnDrops();
        
        Debug.Log("Destroying fallen tree");
        Destroy(gameObject);
    }
    
    private void SpawnDrops()
    {
        // Only spawn drops if we have a prefab assigned
        if (dropPrefab == null)
        {
            Debug.LogWarning("No drop prefab assigned to tree!");
            return;
        }
        
        // Determine number of drops to spawn
        int dropCount = Random.Range(minDropCount, maxDropCount + 1);
        Debug.Log($"Spawning {dropCount} drops");
        
        for (int i = 0; i < dropCount; i++)
        {
            // Calculate random position within drop radius
            Vector3 randomOffset = Random.insideUnitSphere * dropRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // Keep drops above ground
            
            Vector3 dropPosition = transform.position + randomOffset;
            
            // Instantiate the drop
            GameObject drop = Instantiate(dropPrefab, dropPosition, Random.rotation);
            
            // Add physics force if the drop has a rigidbody
            Rigidbody dropRb = drop.GetComponent<Rigidbody>();
            if (dropRb != null)
            {
                // Add upward force and slight random direction
                Vector3 force = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    dropUpwardForce,
                    Random.Range(-0.5f, 0.5f)
                );
                
                dropRb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}