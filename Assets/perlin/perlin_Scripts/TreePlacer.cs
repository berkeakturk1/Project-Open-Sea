using UnityEngine;
using System.Collections;

public class TreePlacer : MonoBehaviour {
    private bool isPlaced = false;
    private float placementTime = 0f;
    private Vector3 originalPosition;
    private float maxPlacementTime = 1f; // Extended time to try placing
    private float additionalDownwardForce = 100f; // Strong downward force
    private float groundOffset = -0.25f; // Negative offset to push tree into ground
    
    private void Start() {
        // Store original position
        originalPosition = transform.position;
        
        // Add rigid body for physics placement
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 50f; // Heavier mass to fall more convincingly
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        
        // Add sphere collider at the bottom for better terrain detection
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.radius = 0.1f; // Smaller radius for more precise placement
        col.center = Vector3.zero; // At the base of the tree
        
        // Start placement coroutine
        StartCoroutine(PlaceTree());
    }
    
    private IEnumerator PlaceTree() {
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // Wait a frame to ensure physics is initialized
        yield return null;
        
        while (!isPlaced && placementTime < maxPlacementTime) {
            placementTime += Time.deltaTime;
            
            // Apply constant downward force to overcome any small obstacles
            rb.AddForce(Vector3.down * additionalDownwardForce, ForceMode.Force);
            
            // Check if the tree has stopped moving (has settled)
            if (rb.velocity.magnitude < 0.01f && placementTime > 0.5f) {
                // If we've been still for a bit, count as placed
                isPlaced = true;
                
                // Push tree slightly into the ground for better visual grounding
                transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y + groundOffset,
                    transform.position.z
                );
                
                // Clean up physics components
                FinalizePlacement();
            }
            
            // Debug visualization
            Debug.DrawRay(transform.position, Vector3.down * 2f, Color.red);
            
            yield return null;
        }
        
        // If time expired and not placed, force placement at original height but try a final raycast
        if (!isPlaced) {
            // Try one final raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(originalPosition + Vector3.up * 10f, Vector3.down, out hit, 20f)) {
                transform.position = new Vector3(
                    originalPosition.x, 
                    hit.point.y + groundOffset, 
                    originalPosition.z
                );
            } else {
                // If all else fails, use original position
                transform.position = originalPosition;
            }
            
            FinalizePlacement();
        }
    }
    
    private void FinalizePlacement() {
        // Remove physics components
        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<Collider>());
        
        // Remove this component as well
        Destroy(this);
    }
    
    // Log any collision events for debugging
    /*private void OnCollisionEnter(Collision collision) {
        Debug.Log($"Tree collided with: {collision.gameObject.name}");
    }*/
}