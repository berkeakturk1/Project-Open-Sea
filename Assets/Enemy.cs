using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int damagePerHit = 25;
    
    [Header("Death Settings")]
    [SerializeField] private float deathRotationAngle = 90f;
    [SerializeField] private float deathRotationSpeed = 2f;
    [SerializeField] private float destroyDelay = 3f;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathAnimationTrigger = "Death";
    [SerializeField] private bool stopAnimatorOnDeath = true;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Effects Settings")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private GameObject hitEffect;
    
    // Internal variables
    private int currentHealth;
    private bool isDead = false;
    private bool isRotating = false;
    private Quaternion targetRotation;
    private Quaternion initialRotation;
    private Collider enemyCollider;
    
    private void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get components if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        enemyCollider = GetComponent<Collider>();
        
        // Store initial rotation
        initialRotation = transform.rotation;
        
        Debug.Log($"Enemy initialized with {maxHealth} health");
    }
    
    private void Update()
    {
        // Handle death rotation
        if (isRotating && isDead)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                deathRotationSpeed * Time.deltaTime);
            
            // Check if rotation is complete
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to an axe
        AxeController axeController = other.GetComponent<AxeController>();
        if (axeController != null)
        {
            // Check if we're not already dead
            if (!isDead)
            {
                TakeDamage();
            }
        }
        
        // Alternative: Check by tag if you prefer
        // if (other.CompareTag("Axe") && !isDead)
        // {
        //     TakeDamage();
        // }
    }
    
    // Public method specifically for bullet/projectile damage
    public void TakeBulletDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;
        
        TakeDamage(Mathf.RoundToInt(damage));
        
        // Optional: Add specific bullet hit effects here
        Debug.Log($"Enemy hit by bullet at {hitPoint} for {damage} damage");
    }
    
    public void TakeDamage()
    {
        if (isDead) return;
        
        currentHealth -= damagePerHit;
        Debug.Log($"Enemy took {damagePerHit} damage. Health: {currentHealth}/{maxHealth}");
        
        // Check if enemy should die BEFORE triggering other states
        if (currentHealth <= 0)
        {
            Die();
            return; // Exit early to prevent other state changes
        }
        
        // Only trigger damage state if not dead
        BearEnemyIntegration bearIntegration = GetComponent<BearEnemyIntegration>();
        if (bearIntegration != null)
        {
            bearIntegration.OnTakeDamage();
        }
        
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Clean up effect after 2 seconds
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Check if enemy should die BEFORE triggering other states
        if (currentHealth <= 0)
        {
            Die();
            return; // Exit early to prevent other state changes
        }
        
        // Only trigger damage state if not dead
        BearEnemyIntegration bearIntegration = GetComponent<BearEnemyIntegration>();
        if (bearIntegration != null)
        {
            bearIntegration.OnTakeDamage();
        }
        
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Enemy has died!");
        
        // IMMEDIATELY set death state to override any current animation
        if (animator != null)
        {
            // Method 1: Direct state play (most reliable, bypasses transitions)
            animator.Play("Death", 0, 0f); // Replace "Death" with your exact death state name
            
            // Also set the boolean for any states that might check it
            animator.SetBool("isDead", true);
            
            // Clear all other states to prevent conflicts
            animator.SetBool("isAttacking", false);
            animator.SetBool("isChasing", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("isTakingDamage", false);
        }
        
        // Notify bear integration about death
        BearEnemyIntegration bearIntegration = GetComponent<BearEnemyIntegration>();
        if (bearIntegration != null)
        {
            bearIntegration.OnDeath();
        }
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Spawn death effect
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // Disable collider so it can't take more damage
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Start death rotation
        //StartDeathRotation();
        
        // Start destruction countdown
        StartCoroutine(DestroyAfterDelay());
    }
    
    private void StartDeathRotation()
    {
        // Calculate target rotation (90 degrees around Z-axis for "falling over")
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 targetEuler = new Vector3(currentEuler.x, currentEuler.y, currentEuler.z + deathRotationAngle);
        targetRotation = Quaternion.Euler(targetEuler);
        
        isRotating = true;
    }
    
    private IEnumerator EnsureDeathState()
    {
        // Wait one frame for the Any State transition to process
        yield return null;
        
        // If still not in death state after one frame, force it
        if (animator != null)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're not in death state (you may need to adjust the state name)
            if (!currentState.IsName("Death") && !currentState.IsName("BearIsDeadState"))
            {
                Debug.Log("Forcing death state transition");
                // Force play the death state directly
                animator.Play("Death", 0, 0f); // Replace "Death" with your actual death state name
            }
        }
    }
    
    private IEnumerator StopAnimatorAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Let death animation play briefly
        
        // Note: For bear AI, the BearIsDeadState handles stopping the animator
        // This method is kept for compatibility with non-bear enemies
        if (animator != null && stopAnimatorOnDeath)
        {
            // Check if this is a bear (has BearEnemyIntegration component)
            BearEnemyIntegration bearIntegration = GetComponent<BearEnemyIntegration>();
            if (bearIntegration == null)
            {
                // Only stop animator if this is NOT a bear
                animator.enabled = false;
            }
        }
    }
    
    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        
        Debug.Log("Destroying enemy");
        Destroy(gameObject);
    }
    
    // Public methods for external access
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    public void SetHealth(int health)
    {
        maxHealth = health;
        currentHealth = health;
    }
    
    // Optional: Heal method
    public void Heal(int healAmount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        Debug.Log($"Enemy healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
    }
}