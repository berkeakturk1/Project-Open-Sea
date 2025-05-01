using UnityEngine;
using System.Collections;

public class AxeController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string swingAnimationBool = "IsSwinging";
    [SerializeField] private float cooldownTime = 0.5f;
    [SerializeField] private float swingDuration = 0.5f; // Duration to keep the bool true
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // Left mouse button
    
    [Header("Collider Settings")]
    [SerializeField] private Collider axeCollider;
    [SerializeField] private bool enableColliderDuringAnimation = true;
    [SerializeField] private float colliderActiveTime = 0.3f;
    
    // Internal variables
    private bool canAttack = true;
    private bool isSwinging = false;
    private static readonly int SwingingBool = Animator.StringToHash("IsSwinging");

    private void Start()
    {
        // If animator not assigned in inspector, try to find it
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }
        }
        
        // If axe collider not assigned, try to find it
        if (axeCollider == null)
        {
            axeCollider = GetComponent<Collider>();
        }
        
        // Disable collider initially if we're using the enable-during-animation feature
        if (enableColliderDuringAnimation && axeCollider != null)
        {
            axeCollider.enabled = false;
        }
        
        // Make sure swing bool is initially false
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(swingAnimationBool))
            {
                animator.SetBool(swingAnimationBool, false);
            }
            else
            {
                animator.SetBool(SwingingBool, false);
            }
        }
        
        Debug.Log("AxeController initialized");
    }

    private void Update()
    {
        // Check for attack input
        if (canAttack && !isSwinging && Input.GetKeyDown(attackKey))
        {
            SwingAxe();
        }
    }
    
    public void SwingAxe()
    {
        if (!canAttack || isSwinging) return;
        
        Debug.Log("Swinging axe");
        isSwinging = true;
        
        // Set animation boolean
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(swingAnimationBool))
            {
                animator.SetBool(swingAnimationBool, true);
            }
            else
            {
                animator.SetBool(SwingingBool, true);
            }
            
            // Start coroutine to turn off the boolean after animation time
            StartCoroutine(EndSwingAnimation());
        }
        
        // Handle collider activation if needed
        if (enableColliderDuringAnimation && axeCollider != null)
        {
            StartCoroutine(ActivateColliderDuringSwing());
        }
        
        // Start cooldown
        StartCoroutine(AttackCooldown());
    }
    
    private IEnumerator EndSwingAnimation()
    {
        // Wait for the swing duration
        yield return new WaitForSeconds(swingDuration);
        
        // Turn off the swing animation
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(swingAnimationBool))
            {
                animator.SetBool(swingAnimationBool, false);
            }
            else
            {
                animator.SetBool(SwingingBool, false);
            }
        }
    }
    
    private IEnumerator ActivateColliderDuringSwing()
    {
        axeCollider.enabled = true;
        
        yield return new WaitForSeconds(colliderActiveTime);
        
        axeCollider.enabled = false;
    }
    
    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        
        yield return new WaitForSeconds(cooldownTime);
        
        canAttack = true;
        isSwinging = false;
    }
    
    // Optional: Animation Event Method
    // Can be called directly from animation keyframes
    public void OnAnimationHitFrame()
    {
        Debug.Log("Hit frame reached in animation!");
        
        // You can activate collider here as an alternative to the timed activation
        if (!enableColliderDuringAnimation && axeCollider != null)
        {
            StartCoroutine(ActivateColliderForFrame());
        }
    }
    
    private IEnumerator ActivateColliderForFrame()
    {
        axeCollider.enabled = true;
        
        // Wait for just one frame
        yield return null;
        
        axeCollider.enabled = false;
    }
}