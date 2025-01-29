using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required for handling UI elements

public class ShipController : MonoBehaviour
{
    // Three modes: neutral, first gear (paddling), second gear (sailing with medium wind), third gear (sailing with strong wind)
    public float paddlingSpeed = 1.0f;
    public float windSpeed = 2.0f;
    public string[] windDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    public string currentWindDirection = "N";

    private Transform shipHelm;
    public Transform playerTransform;
    private PlayerOnShipController onShipController;
    

    private int currentGear = 0; // 0: Neutral, 1: Paddling, 2: Medium Wind, 3: Strong Wind
    private bool sailsDropped = false;

    // Wind direction vectors
    private Dictionary<string, Vector3> windDirectionVectors = new Dictionary<string, Vector3>()
    {
        { "N", Vector3.forward },
        { "NE", (Vector3.forward + Vector3.right).normalized },
        { "E", Vector3.right },
        { "SE", (Vector3.back + Vector3.right).normalized },
        { "S", Vector3.back },
        { "SW", (Vector3.back + Vector3.left).normalized },
        { "W", Vector3.left },
        { "NW", (Vector3.forward + Vector3.left).normalized }
    };
    
    private Dictionary<string, Vector3> windLocalPositions = new Dictionary<string, Vector3>
    {
        { "N", new Vector3(-50f, 0f, 0f) },
        { "NE", new Vector3(-75f, 25f, 0f) }, // Midway between N and E
        { "E", new Vector3(-100f, 50f, 0f) },
        { "SE", new Vector3(-75f, 75f, 0f) }, // Midway between S and E
        { "S", new Vector3(-50f, 100f, 0f) },
        { "SW", new Vector3(-25f, 75f, 0f) }, // Midway between S and W
        { "W", new Vector3(0f, 50f, 0f) },
        { "NW", new Vector3(-25f, 25f, 0f) } // Midway between N and W
    };


    private Dictionary<string, Quaternion> windLocalRotations = new Dictionary<string, Quaternion>
    {
        { "N", Quaternion.Euler(0f, 0f, 270f) },
        { "W", Quaternion.Euler(0f, 0f, 90f) },
        { "S", Quaternion.Euler(0f, 0f, 270f) }, // Updated value for South
        { "E", Quaternion.Euler(0f, 0f, 90f) }
    };
    
    private Vector3 circleCenter = new Vector3(-50f, 50f, 0f); // Center of the circle
    private float circleRadius = 50f; // Radius of the circle
    private Dictionary<string, float> windDirectionAngles = new Dictionary<string, float>
    {
        { "N", 270f },  // North: Upward
        { "NE", 315f }, // Northeast: Between N and E
        { "E", 0f },    // East: Right
        { "SE", 45f },  // Southeast: Between S and E
        { "S", 90f },   // South: Downward
        { "SW", 135f }, // Southwest: Between S and W
        { "W", 180f },  // West: Left
        { "NW", 225f }  // Northwest: Between N and W
    };








    // UI Element for Wind Direction Indicator
    public RectTransform windIndicatorUI; // Assign this in the Inspector
    public RectTransform windCircleUI;    // The parent circle UI element

    void Start()
    {
        onShipController = GameObject.Find("ThirdPersonController").GetComponent<PlayerOnShipController>();
        playerTransform = onShipController.transform;

        shipHelm = GameObject.Find("helm").GetComponent<Transform>();

       
        
        StartCoroutine(CycleWindDirections());
    }

    void Update()
    {
        helmController();
        HandleGearChange();
        ApplyMovement();
        UpdateWindIndicator(); // Call to update the wind direction UI
    }

    public void helmController()
    {
        if (Input.GetKey(KeyCode.Q) && onShipController.checkHelm())
        {
            // Turn the helm left
            shipHelm.Rotate(Vector3.forward, 100.0f * Time.deltaTime);
            gameObject.transform.Rotate(Vector3.up, -15.0f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.E) && onShipController.checkHelm())
        {
            // Turn the helm right
            shipHelm.Rotate(Vector3.forward, -100.0f * Time.deltaTime);
            gameObject.transform.Rotate(Vector3.up, 15.0f * Time.deltaTime);
        }
    }

    void HandleGearChange()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentGear++;
            if (currentGear > 3)
            {
                currentGear = 0; // Reset to neutral after third gear
            }
            Debug.Log("Gear Increased: " + currentGear);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentGear--;
            if (currentGear < 0)
            {
                currentGear = 3; // Loop back to third gear when decreasing from neutral
            }
            Debug.Log("Gear Decreased: " + currentGear);
        }
    }

    void ApplyMovement()
    {
        float speed = 0f;

        switch (currentGear)
        {
            case 1:
                speed = paddlingSpeed;
                break;
            case 2:
                speed = windSpeed * GetWindPropulsionFactor();
                break;
            case 3:
                speed = windSpeed * 1.5f * GetWindPropulsionFactor();
                break;
            default:
                speed = 0f;
                break;
        }

        // Move the ship forward based on the current gear's speed
        gameObject.transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    float GetWindPropulsionFactor()
    {
        // Get the wind direction vector
        Vector3 windDirection = windDirectionVectors[currentWindDirection];

        // Calculate the angle between the ship's forward vector and the wind direction
        float angle = Vector3.Angle(transform.forward, windDirection);

        // Maximum propulsion when wind is directly behind (0 degrees)
        // No propulsion when wind is directly ahead (180 degrees)
        float factor = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));

        return factor;
    }

    public float lerpSpeed = 2.0f; // Adjust this for smoothness

    private float currentAngle = 0f; // Current angle of the wind indicator

    void UpdateWindIndicator()
    {
        if (windIndicatorUI != null && windCircleUI != null)
        {
            // Ensure the current wind direction exists in the dictionary
            if (windDirectionAngles.ContainsKey(currentWindDirection))
            {
                // Get the target angle for the current wind direction
                float targetAngle = windDirectionAngles[currentWindDirection];

                // Smoothly interpolate the angle
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * lerpSpeed);

                // Convert the angle to radians for trigonometric calculations
                float radians = currentAngle * Mathf.Deg2Rad;

                // Calculate the new local position along the circle's arc (assuming center is at (0, 0))
                Vector3 newPosition = new Vector3(
                    circleRadius * Mathf.Cos(radians),
                    circleRadius * Mathf.Sin(radians),
                    0f
                );

                // Update the wind indicator's position
                windIndicatorUI.localPosition = newPosition;

                // Calculate the direction vector pointing from the indicator to the circle center
                Vector3 directionToCenter = (-newPosition).normalized;

                // Calculate the rotation that makes the wind indicator's north side face the center
                float angleToCenter = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;

                // Apply the rotation to the wind indicator
                windIndicatorUI.localRotation = Quaternion.Euler(0f, 0f, angleToCenter - 90f);

                // Rotate the windCircleUI to counteract the ship's rotation
                windCircleUI.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.y);
            }
        }
    }







    
    void ChangeWindDirection(string newDirection)
    {
        if (windLocalPositions.ContainsKey(newDirection))
        {
            currentWindDirection = newDirection;
            Debug.Log("Wind direction changed to: " + newDirection);
        }
    }

    
   
    

    IEnumerator CycleWindDirections()
    {
        foreach (string direction in windLocalPositions.Keys)
        {
            // Set the current wind direction
            currentWindDirection = direction;

            Debug.Log($"Current Wind Direction: {currentWindDirection}");

            // Wait for a few seconds before switching to the next direction
            yield return new WaitForSeconds(3f); // Adjust the duration as needed
        }
    }






}
