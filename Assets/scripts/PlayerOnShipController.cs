using System;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Mathematics.math;

public class PlayerOnShipController : MonoBehaviour
{
    
    private Rigidbody rb;
    public bool isOnShip = false;
    


    void Start()
    {

        rb = GetComponent<Rigidbody>();
        
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

