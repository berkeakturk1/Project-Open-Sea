using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Map Settings")]
    public Transform player;
    public Camera minimapCamera;
    public RawImage minimapImage;
    public float zoomLevel = 20f;
    
    [Header("Paper Effect")]
    public Material paperEffectMaterial;
    public RenderTexture minimapRenderTexture;
    public Texture2D paperTexture;
    [Range(0f, 1f)] public float paperBlendStrength = 0.2f;
    [Range(0f, 10f)] public float paperTextureScale = 1f;
    
    [Header("Style")]
    public bool rotateWithPlayer = false;
    public Color waterColor = new Color(0.2f, 0.5f, 0.8f);
    public Color landColor = new Color(0.3f, 0.6f, 0.3f);
    [Range(0f, 1f)] public float borderWidth = 0.05f;
    public Color borderColor = Color.black;
    
    private void Start()
    {
        if(minimapCamera == null)
        {
            Debug.LogError("Minimap Camera not assigned to MinimapController!");
            return;
        }
        
        // Setup render texture
        if(minimapRenderTexture == null)
        {
            minimapRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            minimapRenderTexture.Create();
        }
        
        minimapCamera.targetTexture = minimapRenderTexture;
        minimapImage.texture = minimapRenderTexture;
        
        // Set up camera parameters
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = zoomLevel;
        
        // Initialize paper effect material
        if(paperEffectMaterial != null && paperTexture != null)
        {
            paperEffectMaterial.SetTexture("_PaperTex", paperTexture);
            paperEffectMaterial.SetFloat("_BlendStrength", paperBlendStrength);
            paperEffectMaterial.SetFloat("_TextureScale", paperTextureScale);
            minimapImage.material = paperEffectMaterial;
        }
    }
    
    private void LateUpdate()
    {
        if(player == null || minimapCamera == null)
            return;
            
        // Update camera position to follow player
        Vector3 newPosition = player.position;
        newPosition.y = minimapCamera.transform.position.y; // Maintain height
        minimapCamera.transform.position = newPosition;
        
        // Handle rotation
        if(rotateWithPlayer)
        {
            minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
        else
        {
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
    
    // Call this method to change zoom level during gameplay
    public void SetZoomLevel(float newZoomLevel)
    {
        zoomLevel = Mathf.Clamp(newZoomLevel, 5f, 100f);
        if(minimapCamera != null)
            minimapCamera.orthographicSize = zoomLevel;
    }
}