using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BearDeathState : StateMachineBehaviour
{
    NavMeshAgent agent;
    Enemy enemyScript;
    
    public float deathDuration = 3f;
    private float deathTimer;
    private bool hasPlayedDeathSound = false;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        enemyScript = animator.GetComponent<Enemy>();
        
        // Stop all movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false; // Completely disable NavMeshAgent
        }
        
        // Reset death timer
        deathTimer = deathDuration;
        
        // Disable colliders to prevent further interactions
        Collider[] colliders = animator.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Optional: Play death sound effect
        AudioSource audioSource = animator.GetComponent<AudioSource>();
        if (audioSource != null && !hasPlayedDeathSound)
        {
            // Assuming you have a death sound clip assigned in the Enemy script
            hasPlayedDeathSound = true;
        }
        
        Debug.Log("Bear has died!");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // The Enemy script handles the death rotation and destruction
        // This state just ensures the animator stays in death state
        
        // Optional: Gradually fade out or apply other death effects
        deathTimer -= Time.deltaTime;
        
        // The Enemy script will handle destroying the GameObject
        // so we don't need to do anything else here
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // This state should not exit naturally - the GameObject will be destroyed
        Debug.Log("Bear death state exited (this shouldn't normally happen)");
    }
}
