using UnityEngine;

public class FirstPersonControllerWithAnimator : MonoBehaviour
{
    // References
    public Animator animator; // Drag your Animator component here
    

    // Movement settings
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = -9.81f;

    // State variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSwimming;

    void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator not assigned!");
        }
    }

    void Update()
    {
        
        HandleAnimation();
    }

    

    private void HandleAnimation()
    {
        // Set animator parameters based on movement and states

        // Idle
        if (isGrounded && velocity.magnitude < 0.1f)
        {
            animator.SetBool("Idle_land", true);
            animator.SetBool("Running", false);
            animator.SetBool("Walking", false);
        }
        else
        {
            animator.SetBool("Idle_land", false);
        }

        // Walking
        if (isGrounded && velocity.magnitude > 0.1f && !Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetBool("Walking", true);
            animator.SetBool("Running", false);
        }
        else
        {
            animator.SetBool("Walking", false);
        }

        // Running
        if (isGrounded && velocity.magnitude > 0.1f && Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetBool("Running", true);
            animator.SetBool("Walking", false);
        }
        else
        {
            animator.SetBool("Running", false);
        }

        // Jumping
        if (!isGrounded)
        {
            animator.SetTrigger("jump");
        }

        // Swimming
        animator.SetBool("Swimming", isSwimming);
        if (isSwimming)
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Running", false);
            animator.SetBool("Idle_land", false);
        }

        // Transition to treading water
        if (isSwimming && velocity.magnitude < 0.1f)
        {
            animator.SetBool("Treading", true);
        }
        else
        {
            animator.SetBool("Treading", false);
        }
    }
}
