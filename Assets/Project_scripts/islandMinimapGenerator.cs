using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IslandMinimapGenerator : MonoBehaviour
{
    [Header("Minimap Settings")]
    public int mapResolution = 512;
    public float worldSize = 1000f; // Size of your world in world units
    public RawImage minimapImage;
    public Transform player;
    
    [Header("Camera Settings")]
    public Camera minimapCamera; // Now assignable in the inspector
    public float minimapHeight = 300f; // Height at which the minimap camera sits
    public bool createCameraIfMissing = true;
    
    [Header("Visual Settings")]
    public Material paperEffectMaterial;
    public Texture2D paperTexture;
    [Range(0f, 1f)] public float paperBlendStrength = 0.2f;
    [Range(0f, 1f)] public float sepiaTone = 0.3f;
    [Range(0f, 1f)] public float vignetteStrength = 0.3f;
    
    [Header("Layer Settings")]
    public LayerMask terrainLayers; // Set this to include all your island objects
    public LayerMask playerLayer; // Layer for the player
    public Color oceanColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    public Color islandColor = new Color(0.3f, 0.6f, 0.3f, 1f);
    public Color playerIndicatorColor = Color.red;
    
    // Private references
    private RenderTexture minimapRenderTexture;
    private GameObject minimapCameraObject;
    
    void Start()
    {
        // Create render texture for the minimap
        minimapRenderTexture = new RenderTexture(mapResolution, mapResolution, 16, RenderTextureFormat.ARGB32);
        minimapRenderTexture.Create();
        
        // Set up the minimap camera
        SetupMinimapCamera();
        
        // Set up the UI display
        SetupMinimapDisplay();
        
        // Generate the initial minimap
        StartCoroutine(UpdateMinimap());
    }
    
    void SetupMinimapCamera()
    {
        // Check if camera is already assigned
        if (minimapCamera == null && createCameraIfMissing)
        {
            // Create a new camera specifically for the minimap
            minimapCameraObject = new GameObject("Minimap Camera");
            minimapCamera = minimapCameraObject.AddComponent<Camera>();
            minimapCameraObject.transform.parent = transform;
        }
        
        if (minimapCamera != null)
        {
            // Configure camera settings
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = oceanColor;
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = worldSize / 2f;
            minimapCamera.cullingMask = terrainLayers | playerLayer; // Render both terrain and player
            
            // Only set position and rotation if we created the camera or it's not already positioned
            if (minimapCameraObject != null || minimapCamera.transform.position.y < 1)
            {
                minimapCamera.transform.position = new Vector3(0, minimapHeight, 0);
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look down
            }
            
            minimapCamera.targetTexture = minimapRenderTexture;
            minimapCamera.depth = -1;
            
            Debug.Log("Minimap camera set up successfully. Culling mask: " + minimapCamera.cullingMask);
        }
        else
        {
            Debug.LogError("No minimap camera assigned and createCameraIfMissing is false!");
        }
    }
    
    void SetupMinimapDisplay()
    {
        if (minimapImage == null)
        {
            Debug.LogError("Minimap RawImage not assigned!");
            return;
        }
        
        // Set the render texture to the UI image
        minimapImage.texture = minimapRenderTexture;
        
        // Set up the paper effect material if provided
        if (paperEffectMaterial != null)
        {
            // Create a new instance of the material to avoid changing the original
            Material newMaterial = new Material(paperEffectMaterial);
            
            if (paperTexture != null)
                newMaterial.SetTexture("_PaperTex", paperTexture);
                
            newMaterial.SetFloat("_BlendStrength", paperBlendStrength);
            newMaterial.SetFloat("_Sepia", sepiaTone);
            newMaterial.SetFloat("_Vignette", vignetteStrength);
            
            minimapImage.material = newMaterial;
        }
    }
    
    IEnumerator UpdateMinimap()
    {
        while (true)
        {
            // Update camera position to center on player
            if (player != null && minimapCamera != null)
            {
                Vector3 newPos = player.position;
                newPos.y = minimapHeight; // Keep the height constant
                minimapCamera.transform.position = newPos;
            }
            
            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }
    
    // Helper method to convert world position to minimap position
    public Vector2 ConvertWorldToMinimapPosition(Vector3 worldPos)
    {
        if (minimapCamera == null) return Vector2.zero;
        
        // This assumes the minimap is centered on the player
        Vector3 directionFromCamera = worldPos - minimapCamera.transform.position;
        float relativeX = directionFromCamera.x / (minimapCamera.orthographicSize * 2);
        float relativeZ = directionFromCamera.z / (minimapCamera.orthographicSize * 2);
        
        // Convert to minimap image coordinates
        RectTransform minimapRect = minimapImage.rectTransform;
        float minimapWidth = minimapRect.rect.width;
        float minimapHeight = minimapRect.rect.height;
        
        return new Vector2(
            relativeX * minimapWidth, 
            relativeZ * minimapHeight
        );
    }
    
    void OnDestroy()
    {
        // Clean up render texture when destroyed
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
            Destroy(minimapRenderTexture);
        }
        
        // Only destroy the camera object if we created it
        if (minimapCameraObject != null)
        {
            Destroy(minimapCameraObject);
        }
    }
    
    // Called when validation is needed (like in the editor)
    void OnValidate()
    {
        // Update the camera settings if it's already assigned
        if (minimapCamera != null && Application.isEditor && !Application.isPlaying)
        {
            minimapCamera.orthographicSize = worldSize / 2f;
            minimapCamera.cullingMask = terrainLayers | playerLayer;
        }
    }
}