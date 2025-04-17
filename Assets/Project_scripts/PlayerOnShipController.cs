using GinjaGaming.FinalCharacterController;
using UnityEngine;
using Cinemachine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerOnShipController : MonoBehaviour
{
    [Header("Ship Movement Controllers for Toggling")]
    [Tooltip("A reference to your normal PlayerController, if needed")]
    public PlayerController playerController;
    public CharacterController characterController;  // If you still need it, keep it
    public RigidbodyController rigidbodyController;  
    
    [Header("Status Flags")]
    public bool isOnShip = false;
    public bool isAtHelm = false;  // Is the player actively steering at the helm?
    private bool insideHelmTrigger = false;

    [Header("References")]
    public Transform shipHelm; // Target position for the helm
    
    public Transform shipRoot; // The root of the ship
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCamera normalCamera;
    [SerializeField] private CinemachineFreeLook shipCamera;

    private Rigidbody rb;  // If you still need a rigidbody on this object, keep it
    private RigidbodyFirstPersonController firstPersonController; // Reference to the first person controller
    
    [Header("UI Elements")]
    [SerializeField] private GameObject shipCanvasChild; // Reference to the child UI element

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        rigidbodyController = GetComponent<RigidbodyController>();
        characterController = GetComponent<CharacterController>();
        firstPersonController = GetComponent<RigidbodyFirstPersonController>();
        
        //toggleControllers();
        // Set initial priorities for Cinemachine cameras
        if (normalCamera != null) 
            normalCamera.Priority = 20; // Active camera
        if (shipCamera != null)
        {
            shipCamera.Priority = 10;   // Inactive by default
            shipCamera.m_XAxis.m_InputAxisName = "Mouse X";
            shipCamera.m_YAxis.m_InputAxisName = "Mouse Y";
        }
    }

    private void Update()
    {
        // Handle helm toggling only when inside helm trigger
        if (insideHelmTrigger && Input.GetKeyDown(KeyCode.H))
        {
            if (!isAtHelm)
                EnterHelm();
            else
                ExitHelm();
        }
    }

    // ---------------------------
    //       HELM METHODS
    // ---------------------------
    private void EnterHelm()
    {
        Debug.Log("Snapping to helm!");

        // Disable the first person controller to freeze movement
        if (firstPersonController != null)
            firstPersonController.enabled = false;

        // Snap player to helm position/rotation
        transform.position = shipHelm.position;
        transform.rotation = shipHelm.rotation;

        // Switch cameras
        if (normalCamera != null) normalCamera.Priority = 10;
        if (shipCamera != null) shipCamera.Priority = 20;

        isAtHelm = true; // Now steering

        Debug.Log("Entered helm: Player movement frozen");
    }

    private void ExitHelm()
    {
        Debug.Log("Exiting helm!");
        
        // Re-enable the first person controller
        if (firstPersonController != null)
            firstPersonController.enabled = true;
        
        transform.SetParent(shipRoot, true);
        // Switch camera priorities back
        if (normalCamera != null) normalCamera.Priority = 20;
        if (shipCamera != null)   shipCamera.Priority = 10;

        isAtHelm = false; // No longer steering
    }

    // ---------------------------
    //      TRIGGER METHODS
    // ---------------------------
    private void OnTriggerEnter(Collider other)
    {
        // If we've hit the ship's trigger
        if (other.CompareTag("playerTrigger"))
        {
            isOnShip = true;
            //toggleControllers();
            EnableShipUI();
            Debug.Log("Player is on the ship.");
            shipRoot = other.transform.parent;
            // Simply parent to the ship's root (or the trigger's parent)
            transform.SetParent(shipRoot, true);
        }

        if (other.CompareTag("helmTrigger"))
        {
            insideHelmTrigger = true;
            Debug.Log("Entered helm trigger.");
            transform.SetParent(shipRoot, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If the player exits the ship's trigger
        if (other.CompareTag("playerTrigger"))
        {
            // Check if the player is either at the helm or inside the helm trigger
            if (!isAtHelm && !insideHelmTrigger)
            {
                isOnShip = false;
                DisableShipUI();
                //toggleControllers();
                Debug.Log("Player has left the ship.");
                
                transform.SetParent(null, true);
            }
        }
        // If the player exits the helm trigger
        else if (other.CompareTag("helmTrigger"))
        {
            insideHelmTrigger = false;
            Debug.Log("Player left the helm area.");

            // Exit helm mode only if the player is actually steering
            if (isAtHelm)
            {
                ExitHelm();
            }
        }
    }

    private void toggleControllers()
    {
        if (isOnShip)
        {
            characterController.enabled = false;
            playerController.enabled = false;
            rigidbodyController.enabled = true;
        }
        else
        {
            characterController.enabled = true;
            playerController.enabled = true;
            rigidbodyController.enabled = false;
        }
    }
    
    private void EnableShipUI()
    {
        if (shipCanvasChild != null)
        {
            shipCanvasChild.SetActive(true); // Enable the child
        }
    }

    private void DisableShipUI()
    {
        if (shipCanvasChild != null)
        {
            shipCanvasChild.SetActive(false); // Disable the child
        }
    }

    // ---------------------------
    //      PUBLIC GETTERS
    // ---------------------------
    public bool getOnShip()
    {
        return isOnShip;
    }

    public bool checkHelm()
    {
        return isAtHelm;
    }
}