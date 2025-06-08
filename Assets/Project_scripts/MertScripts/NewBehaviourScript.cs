using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWithoutRotation : MonoBehaviour 
{
    public Transform target;
    public Vector3 offset = Vector3.zero;
    
    void Update() 
    {
        if (target != null) 
        {
            transform.position = target.position + offset;
            // Rotation is not affected
        }
    }
}
