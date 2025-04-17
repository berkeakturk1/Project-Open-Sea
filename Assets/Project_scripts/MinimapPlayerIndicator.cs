using UnityEngine;
using UnityEngine.UI;

public class MinimapPlayerIndicator : MonoBehaviour
{
    public Transform player;
    public RectTransform minimapRect;
    public RectTransform playerIndicator;
    public IslandMinimapGenerator minimapGenerator;
    
    [Header("Indicator Settings")]
    public float indicatorSize = 10f;
    public Color indicatorColor = Color.red;
    public Sprite indicatorSprite;
    public bool rotateWithPlayer = true;
    
    private Image indicatorImage;
    
    void Start()
    {
        // If player indicator doesn't exist, create one
        if (playerIndicator == null)
        {
            GameObject indicator = new GameObject("Player Indicator");
            playerIndicator = indicator.AddComponent<RectTransform>();
            indicator.transform.SetParent(minimapRect, false);
            
            // Add image component
            indicatorImage = indicator.AddComponent<Image>();
            
            // Set default sprite if none is provided
            if (indicatorSprite == null)
            {
                // Create a default triangle sprite
                indicatorImage.sprite = CreateDefaultIndicatorSprite();
            }
            else
            {
                indicatorImage.sprite = indicatorSprite;
            }
            
            indicatorImage.color = indicatorColor;
            
            // Set size
            playerIndicator.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        }
        else
        {
            indicatorImage = playerIndicator.GetComponent<Image>();
            if (indicatorImage == null)
            {
                indicatorImage = playerIndicator.gameObject.AddComponent<Image>();
            }
            
            indicatorImage.color = indicatorColor;
            
            if (indicatorSprite != null)
                indicatorImage.sprite = indicatorSprite;
        }
    }
    
    void LateUpdate()
    {
        if (player == null || minimapRect == null || playerIndicator == null || minimapGenerator == null)
            return;
            
        // Get player position on minimap
        Vector2 minimapPos = minimapGenerator.ConvertWorldToMinimapPosition(player.position);
        playerIndicator.anchoredPosition = minimapPos;
        
        // Handle rotation if needed
        if (rotateWithPlayer)
        {
            playerIndicator.rotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);
        }
    }
    
    // Creates a simple triangle sprite for the player indicator
    Sprite CreateDefaultIndicatorSprite()
    {
        // Create a small texture for the triangle
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        
        // Clear texture with transparent pixels
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        texture.SetPixels(colors);
        
        // Draw triangle
        for (int y = 0; y < 16; y++)
        {
            for (int x = 8 - y/2; x <= 8 + y/2; x++)
            {
                texture.SetPixel(16 + x, 8 + y, Color.white);
                texture.SetPixel(16 - x, 8 + y, Color.white);
            }
        }
        
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
}