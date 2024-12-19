using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    
    //three modes: neutral, first gear(paddling), second gear (sailing with medium wind), third gear (sailing with strong wind)
    public float paddlingSpeed = 1.0f;
    public float windSpeed = 2.0f;
    public string[] windDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    public string currentWindDirection = "N";
    
    private Transform shipHelm;
    public Transform playerTransform;
    private CharacterController cc;
    private bool isOnShip;
    void Start()
    {
        cc = GameObject.Find("Player").GetComponent<CharacterController>();
        playerTransform = cc.transform;
        
        shipHelm = GameObject.Find("helm").GetComponent<Transform>();

        // Ensure the player starts as a child of the ship if on the ship
        if (isOnShip && playerTransform != null)
        {
            playerTransform.SetParent(gameObject.transform);
        }
    }

    void Update()
    {
        helmController();
        
    }

    
    

    public void helmController()
    {
        if (Input.GetKey(KeyCode.Q) && cc.getOnShip())
        {
            // Turn the helm left
            shipHelm.Rotate(Vector3.forward, -100.0f * Time.deltaTime);
            gameObject.transform.Rotate(Vector3.up, -5.0f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.E) && cc.getOnShip())
        {
            // Turn the helm right
            shipHelm.Rotate(Vector3.forward, 100.0f * Time.deltaTime);
            gameObject.transform.Rotate(Vector3.up, 5.0f * Time.deltaTime);
        }
    }

}
