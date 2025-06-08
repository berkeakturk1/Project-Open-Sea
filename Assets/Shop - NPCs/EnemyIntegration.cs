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
        
        // Check if bear died
        if (enemyScript.IsDead() && !bearAnimator.GetBool("isDead"))
        {
            // Trigger death state
            bearAnimator.SetBool("isDead", true);
            bearAnimator.SetBool("isAttacking", false);
            bearAnimator.SetBool("isChasing", false);
            bearAnimator.SetBool("IsWalking", false);
            bearAnimator.SetBool("isTakingDamage", false);
        }
        
        // Check if bear took damage
        if (enableDamageState && !enemyScript.IsDead())
        {
            int currentHealth = enemyScript.GetCurrentHealth();
            
            if (currentHealth < lastKnownHealth)
            {
                // Bear took damage, trigger damage state
                bearAnimator.SetBool("isTakingDamage", true);
                bearAnimator.SetBool("isAttacking", false);
                bearAnimator.SetBool("isChasing", false);
                bearAnimator.SetBool("IsWalking", false);
                
                lastKnownHealth = currentHealth;
            }
        }
    }
    
    // This method can be called by the Enemy script when taking damage
    public void OnTakeDamage()
    {
        if (enableDamageState && bearAnimator != null && !enemyScript.IsDead())
        {
            bearAnimator.SetBool("isTakingDamage", true);
            bearAnimator.SetBool("isAttacking", false);
        }
    }
}
