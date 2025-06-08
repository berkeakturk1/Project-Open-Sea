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
    
    public void TakeDamage()
    {
        if (isDead) return;
        
        currentHealth -= damagePerHit;
        Debug.Log($"Enemy took {damagePerHit} damage. Health: {currentHealth}/{maxHealth}");
        
        // Trigger bear damage state
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
        
        // Check if enemy should die
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Trigger bear damage state
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
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Enemy has died!");
        
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
        
        // Handle animator - Use isDead boolean instead of trigger for bear AI
        if (animator != null)
        {
            // Set isDead boolean for bear state machine
            animator.SetBool("isDead", true);
            
            // Also disable other bear states
            animator.SetBool("isAttacking", false);
            animator.SetBool("isChasing", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("isTakingDamage", false);
            
            // Optional: Still use death trigger if you have a specific death animation
            if (!string.IsNullOrEmpty(deathAnimationTrigger))
            {
                animator.SetTrigger(deathAnimationTrigger);
            }
            
            // The BearIsDeadState will handle stopping the animator, so we don't do it here
            // This allows the death state to play properly before stopping
        }
        
        // Disable collider so it can't take more damage
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Start death rotation
        StartDeathRotation();
        
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