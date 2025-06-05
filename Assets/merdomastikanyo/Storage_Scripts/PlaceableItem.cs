using System.Collections.Generic;
using UnityEngine;
 
public class PlacebleItem : MonoBehaviour
{
    // Validation
    [SerializeField] bool isGrounded;
    [SerializeField] bool isOverlappingItems;
    public bool isValidToBeBuilt;
 
    [SerializeField] BoxCollider solidCollider;
    private Outline outline;
 
    private void Start()
    {
        outline = GetComponent<Outline>();
    }
 
    void Update()
    {
        if (isGrounded && isOverlappingItems == false)
        {
            isValidToBeBuilt = true;
        }
        else
        {
            isValidToBeBuilt = false;
        }
 
        // Raycast from the box's position towards its center
 
        var boxHeight = transform.lossyScale.y;
        RaycastHit groundHit;
        if (Physics.Raycast(transform.position, Vector3.down, out groundHit, boxHeight * 0.5f, LayerMask.GetMask("Terrain")))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
 
    }
 
    #region || --- On Triggers --- |
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Terrain") && PlacementSystem.Instance.inPlacementMode)
        {
            // Making sure the item is parallel to the ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
            {
                // Align the box's rotation with the ground normal
                Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                transform.rotation = newRotation;
 
                isGrounded = true;
            }
        }
 
        /*if (other.CompareTag("Tree") || other.CompareTag("pickable"))
        {
            isOverlappingItems = true;
        }*/
    }
    #endregion
 
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Terrain") && PlacementSystem.Instance.inPlacementMode)
        {
            isGrounded = false;
        }
 
        /*if (other.CompareTag("Tree") || other.CompareTag("pickable") && PlacementSystem.Instance.inPlacementMode)
        {
            isOverlappingItems = false;
        }*/
    }
 
    #region || --- Set Outline Colors --- |
    public void SetInvalidColor()
    {
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = Color.red;
        }
 
    }
 
    public void SetValidColor()
    {
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = Color.green;
        }
    }
 
    public void SetDefaultColor()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
    #endregion
}