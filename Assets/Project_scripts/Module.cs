using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Module : MonoBehaviour
{
    public Mesh mesh;
    public Dictionary<string, object> prototype;
    public Text debugText;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Awake()
    {
        // Initialize MeshFilter and MeshCollider components
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
    }

    // Setter for the mesh, also updates the mesh of the instance
    public void SetMesh(Mesh newMesh)
    {
        mesh = newMesh;
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    // Mouse Enter event
    void OnMouseEnter()
    {
        if (debugText != null)
        {
            debugText.text = PrototypeToString();
        }
    }

    // Mouse Exit event
    void OnMouseExit()
    {
        if (debugText != null && !string.IsNullOrEmpty(debugText.text))
        {
            debugText.text = "";
        }
    }

    // Converts the prototype dictionary to a string for display
    private string PrototypeToString()
    {
        string result = "";
        foreach (var kvp in prototype)
        {
            result += $"{kvp.Key}: {kvp.Value}\n";
        }
        return result;
    }
}