using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class Fog : MonoBehaviour {
    [Header("Fog")]
    public Shader fogShader;
    public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Range(0.0f, 1.0f)]
    public float fogDensity = 0.5f;
    
    [Range(0.0f, 100.0f)]
    public float fogOffset = 10.0f;
    
    private Material fogMat;
    private Camera cam;

    void Start() {
        // Create material
        if (fogMat == null && fogShader != null) {
            fogMat = new Material(fogShader);
            fogMat.hideFlags = HideFlags.HideAndDontSave;
        }

        // Setup camera
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        
        // For URP, also need to make sure depth is enabled in camera data
        var additionalCamData = cam.GetUniversalAdditionalCameraData();
        if (additionalCamData != null) {
            additionalCamData.requiresDepthTexture = true;
        }
        
        // Set up command buffer to run after rendering is done
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Custom Fog Effect";
        
        // Create render textures
        int rtW = cam.pixelWidth;
        int rtH = cam.pixelHeight;
        RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(rtW, rtH);
        rtDesc.colorFormat = RenderTextureFormat.ARGB32;
        rtDesc.depthBufferBits = 0;
        
        int tempRT = Shader.PropertyToID("_TempRT");
        
        // Execute post-processing
        cmd.GetTemporaryRT(tempRT, rtDesc);
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempRT);
        cmd.Blit(tempRT, BuiltinRenderTextureType.CameraTarget, fogMat);
        cmd.ReleaseTemporaryRT(tempRT);
        
        // Add command buffer to camera
        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cmd);
    }
    
    void Update() {
        if (fogMat != null) {
            // Update shader parameters every frame
            fogMat.SetColor("_FogColor", fogColor);
            fogMat.SetFloat("_FogDensity", fogDensity);
            fogMat.SetFloat("_FogOffset", fogOffset);
        }
    }
    
    void OnDisable() {
        // Clean up
        if (cam != null) {
            cam.RemoveAllCommandBuffers();
        }
    }
    
    void OnDestroy() {
        // Clean up material
        if (fogMat != null) {
            DestroyImmediate(fogMat);
        }
    }
}