using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BearTakingDamageState : StateMachineBehaviour
{
    NavMeshAgent agent;
    Transform player;
    Enemy enemyScript;
    
    public float damageStateDuration = 1f; // How long to stay in damage state
    public float knockbackForce = 2f;
    public bool stopMovementDuringDamage = true;
    
    private float damageTimer;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = animator.GetComponent<NavMeshAgent>();
        enemyScript = animator.GetComponent<Enemy>();
        
        // Reset damage timer
        damageTimer = damageStateDuration;
        
        // Stop movement during damage if specified
        if (stopMovementDuringDamage && agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        
        // Optional: Apply knockback effect
        if (knockbackForce > 0 && agent != null)
        {
            Vector3 knockbackDirection = (animator.transform.position - player.position).normalized;
            agent.Move(knockbackDirection * knockbackForce * Time.deltaTime);
        }
        
        Debug.Log("Bear is taking damage!");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Check if bear should die
        if (enemyScript != null && enemyScript.IsDead())
        {
            animator.SetBool("isDead", true);
            animator.SetBool("isTakingDamage", false);
            return;
        }
        
        // Count down damage state timer
        damageTimer -= Time.deltaTime;
        
        // Exit damage state after timer expires
        if (damageTimer <= 0)
        {
            animator.SetBool("isTakingDamage", false);
            
            // Determine next state based on player distance
            float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);
            
            if (distanceFromPlayer <= 2.5f) // Attack range
            {
                animator.SetBool("isAttacking", true);
            }
            else if (distanceFromPlayer <= 21f) // Chase range
            {
                animator.SetBool("isChasing", true);
            }
            else
            {
                animator.SetBool("IsWalking", true);
            }
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Resume movement
        if (agent != null)
        {
            agent.isStopped = false;
        }
        
        Debug.Log("Bear finished taking damage");
    }
}


