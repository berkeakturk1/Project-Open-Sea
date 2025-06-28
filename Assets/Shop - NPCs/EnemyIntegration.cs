using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Helper script to integrate the bear AI with the Enemy health system
public class BearEnemyIntegration : MonoBehaviour
{
    private Animator bearAnimator;
    private Enemy enemyScript;
    private NavMeshAgent agent;
    public bool isZombie;
    
    [Header("Integration Settings")]
    public bool enableDamageState = true;
    public float damageStateThreshold = 0.5f; // Trigger damage state when taking damage
    
    private int lastKnownHealth;
    
    void Start()
    {
        bearAnimator = GetComponent<Animator>();
        enemyScript = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
        
        if (enemyScript != null)
        {
            lastKnownHealth = enemyScript.GetCurrentHealth();
        }
    }
    
    void Update()
    {
        if (enemyScript == null || bearAnimator == null) return;
        
        // Don't update states if dead - let the death state handle everything
        if (enemyScript.IsDead()) return;
        
        // Check if bear took damage
        if (enableDamageState)
        {
            int currentHealth = enemyScript.GetCurrentHealth();
            
            if (!isZombie && (currentHealth < lastKnownHealth))
            {
                // Bear took damage, trigger damage state
                bearAnimator.SetBool("isChasing", false);
                bearAnimator.SetBool("IsWalking", false);
                
                lastKnownHealth = currentHealth;
            }
            else if (currentHealth < lastKnownHealth)
            {
                bearAnimator.SetBool("isChasing", false);
                bearAnimator.SetBool("IsWalking", false);
                lastKnownHealth = currentHealth;
            }
        }
    }
    
    // This method can be called by the Enemy script when taking damage
    public void OnTakeDamage()
    {
        // Don't trigger damage states if dead
        if (enemyScript != null && enemyScript.IsDead()) return;
        
        if (isZombie) return;
        
        if (enableDamageState && bearAnimator != null)
        {
            bearAnimator.SetBool("isTakingDamage", true);
            bearAnimator.SetBool("isAttacking", false);
        }
    }
    
    // New method to handle death - called by Enemy script
    public void OnDeath()
    {
        if (bearAnimator == null) return;
        
        // Immediately stop all movement and actions
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        
        // Clear all animation states except death
        bearAnimator.SetBool("isAttacking", false);
        bearAnimator.SetBool("isChasing", false);
        bearAnimator.SetBool("IsWalking", false);
        bearAnimator.SetBool("isTakingDamage", false);
        
        // Set death state
        bearAnimator.SetBool("isDead", true);
        
        // Force animator update to process the change immediately
        bearAnimator.Update(0f);
        
        Debug.Log("Bear death state activated");
    }
}