using UnityEngine;
using System.Collections;
using perlin_Scripts.Data;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class StandaloneTerrainChunk : MonoBehaviour
{
    [Header("Terrain References")]
    [Tooltip("Reference to the TextureData scriptable object used by your terrain system")]
    public TextureData textureData;
    [Tooltip("Reference to the TerrainData scriptable object used by your terrain system")]
    public TerrainData terrainData;
    
    [Header("Tree Management")]
    [Tooltip("Should trees be checked for valid placement at runtime?")]
    public bool validateTreesAtRuntime = false;
    [Tooltip("Maximum angle for tree placement")]
    public float maxTreeAngle = 30f;
    
    private MeshRenderer meshRenderer;
    private Material terrainMaterialInstance;
    
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Create a material instance to avoid modifying the shared material
        if (meshRenderer.sharedMaterial != null)
        {
            terrainMaterialInstance = new Material(meshRenderer.sharedMaterial);
            meshRenderer.material = terrainMaterialInstance;
        }
        
        // Apply texture settings if available
        if (textureData != null && terrainMaterialInstance != null)
        {
            textureData.ApplyToMaterial(terrainMaterialInstance);
            
            if (terrainData != null)
            {
                textureData.UpdateMeshHeights(terrainMaterialInstance, terrainData.minHeight, terrainData.maxHeight);
            }
        }
        
        // Validate tree placement if requested
        if (validateTreesAtRuntime)
        {
            StartCoroutine(ValidateTreesDelayed());
        }
    }
    
    private IEnumerator ValidateTreesDelayed()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        Transform treesContainer = transform.Find("Trees");
        if (treesContainer != null)
        {
            int validatedCount = 0;
            int removedCount = 0;
            
            for (int i = treesContainer.childCount - 1; i >= 0; i--)
            {
                Transform tree = treesContainer.GetChild(i);
                
                // Raycast to check ground alignment
                Ray ray = new Ray(tree.position + Vector3.up * 5f, Vector3.down);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 10f))
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    
                    if (angle > maxTreeAngle)
                    {
                        // Tree is on too steep a slope, remove it
                        Destroy(tree.gameObject);
                        removedCount++;
                    }
                    else
                    {
                        // Adjust position to ground
                        tree.position = hit.point;
                        
                        // Align with terrain normal if needed
                        if (angle > 5f)
                        {
                            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            tree.rotation = normalRotation * Quaternion.Euler(0, tree.rotation.eulerAngles.y, 0);
                        }
                        
                        validatedCount++;
                    }
                }
            }
            
            Debug.Log($"Tree validation complete: {validatedCount} validated, {removedCount} removed");
        }
    }
    
    public void UpdateMaterialSettings()
    {
        if (textureData != null && terrainMaterialInstance != null)
        {
            textureData.ApplyToMaterial(terrainMaterialInstance);
            
            if (terrainData != null)
            {
                textureData.UpdateMeshHeights(terrainMaterialInstance, terrainData.minHeight, terrainData.maxHeight);
            }
        }
    }
    
    public void ApplyTextureData(TextureData newTextureData)
    {
        textureData = newTextureData;
        UpdateMaterialSettings();
    }
    
    private void OnValidate()
    {
        if (terrainMaterialInstance != null)
        {
            UpdateMaterialSettings();
        }
    }
}