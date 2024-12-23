using System;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 8f;
    public float sprintSpeed = 14f;
    public float jumpForce = 8f;
    public float gravityMultiplier = 2f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;
    public float maxLookAngle = 85f;

    private Rigidbody rb;
    private bool isGrounded;
    private float xRotation = 0f;

    public bool isOnShip = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent the Rigidbody from rotating due to physics

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleJump();
    }

    void HandleMovement()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Determine movement speed (sprint or walk)
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // Calculate movement direction
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Apply movement
        Vector3 velocity = new Vector3(move.x * speed, rb.velocity.y, move.z * speed);
        rb.velocity = velocity;
    }

    void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust vertical rotation (clamp to prevent flipping)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // Apply rotations
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleJump()
    {
        // Perform a raycast slightly below the player to check for ground
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);

        // Debugging the raycast
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.2f, Color.red);

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Reset Y velocity before jumping
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    

    void FixedUpdate()
    {
        // Apply extra gravity manually to make falling more natural
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform ship = other.gameObject.transform.parent.GetComponent<Transform>();
        if (other.gameObject.tag.Equals("playerTrigger"))
        {
            isOnShip = true;
            transform.SetParent(ship);

        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.Equals("playerTrigger"))
        {
            isOnShip = false;
            transform.SetParent(null);

            // Get the current Y rotation of the player
            float currentYRotation = transform.rotation.eulerAngles.y;

            // Create a new rotation with 0 for X and Z, and keep the current Y rotation
            Quaternion newRotation = Quaternion.Euler(0f, currentYRotation, 0f);

            // Apply the new position and rotation
            transform.SetPositionAndRotation(transform.position, newRotation);
        }
    }


    public bool getOnShip()
    {
        return isOnShip;
    }
}
