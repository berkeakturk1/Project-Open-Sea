using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour{

    public Transform player;
    public Vector3 offset;

    private void LateUpdate(){

        if(player != null){
            Vector3 targetPosition = player.position + offset;

            transform.position = targetPosition;
        }
    }
}
