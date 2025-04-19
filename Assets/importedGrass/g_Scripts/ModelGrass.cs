using System;
using System.Collections;
using System.Collections.Generic;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine;

/// <summary>
/// ModelGrass component that generates and renders grass for a terrain chunk
/// Modified to work with the procedural chunk generation system
/// </summary>
public class ModelGrass : MonoBehaviour {
    public int fieldSize = 100;
    public int chunkDensity = 1;
    public int numChunks = 10;
    public float displacementStrength = 200.0f;
    public Material grassMaterial;
    public Mesh grassMesh;
    public Mesh grassLODMesh;
    public Texture heightMap;

    [Range(0, 1000.0f)]
    public float lodCutoff = 1000.0f;

    [Range(0, 1000.0f)]
    public float distanceCutoff = 1000.0f;

    [Header("Wind")]
    public float windSpeed = 1.0f;
    public float frequency = 1.0f;
    public float windStrength = 1.0f;

    private ComputeShader initializeGrassShader, generateWindShader, cullGrassShader;
    private ComputeBuffer voteBuffer, scanBuffer, groupSumArrayBuffer, scannedGroupSumBuffer;

    private RenderTexture wind;

    private int numInstancesPerChunk, chunkDimension, numThreadGroups, numVoteThreadGroups, numGroupScanThreadGroups, numWindThreadGroups, numGrassInitThreadGroups;
    private int windNoiseKernelIndex = 0;

    private struct GrassData {
        public Vector4 position;
        public Vector2 uv;
        public float displacement;
    }

    private struct GrassChunk {
        public ComputeBuffer argsBuffer;
        public ComputeBuffer argsBufferLOD;
        public ComputeBuffer positionsBuffer;
        public ComputeBuffer culledPositionsBuffer;
        public Bounds bounds;
        public Material material;
    }

    GrassChunk[] chunks;
    uint[] args;
    uint[] argsLOD;

    Bounds fieldBounds;
    
    // Flag to use fallback wind generation if compute shader fails
    private bool useWindFallback = false;
    private Texture2D tempWindTex;

    private void Start()
    {
        StartCoroutine(DelayedPostSetup(0.01f));
    }

    void OnEnable() {
        
    }
    
    IEnumerator DelayedPostSetup(float delaySeconds)
    {
        // Wait for the specified delay.
        yield return new WaitForSeconds(delaySeconds);
    
        try {
            SetupGrassSystem();
        }
        catch (Exception e) {
            Debug.LogError($"Failed to initialize grass system: {e.Message}\n{e.StackTrace}");
            enabled = false;
        }
    }
    
    void SetupGrassSystem() {
        numInstancesPerChunk = Mathf.CeilToInt(fieldSize / numChunks) * chunkDensity;
        chunkDimension = numInstancesPerChunk;
        numInstancesPerChunk *= numInstancesPerChunk;
        
        numThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 128.0f);
        if (numThreadGroups > 128) {
            int powerOfTwo = 128;
            while (powerOfTwo < numThreadGroups)
                powerOfTwo *= 2;
            
            numThreadGroups = powerOfTwo;
        } else {
            while (128 % numThreadGroups != 0)
                numThreadGroups++;
        }
        numVoteThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 128.0f);
        numGroupScanThreadGroups = Mathf.CeilToInt(numInstancesPerChunk / 1024.0f);

        // Load compute shaders
        LoadComputeShaders();

        // Create compute buffers
        voteBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        scanBuffer = new ComputeBuffer(numInstancesPerChunk, 4);
        groupSumArrayBuffer = new ComputeBuffer(numThreadGroups, 4);
        scannedGroupSumBuffer = new ComputeBuffer(numThreadGroups, 4);

        // Set up shader parameters
        ConfigureGrassShader();

        // Set up wind texture
        SetupWindTexture();

        // Set up draw arguments
        SetupDrawArguments();

        // Initialize grass chunks
        InitializeChunks();

        // Set field bounds based on the parent terrain chunk
        Vector3 worldPos = (transform.parent != null) ? transform.parent.position : transform.position;
        float terrainScale = (transform.parent != null) ? transform.parent.localScale.x : 1.0f;

        // Use the actual Y position from the terrain and account for full displacement range
        fieldBounds = new Bounds(
            worldPos, 
            new Vector3(fieldSize * terrainScale, displacementStrength * 2, fieldSize * terrainScale)
        );
    }
    
    void LoadComputeShaders() {
        // Load grass initialization shader
        initializeGrassShader = Resources.Load<ComputeShader>("GrassChunkPoint");
        if (initializeGrassShader == null) {
            Debug.LogError("Failed to load GrassChunkPoint compute shader from Resources folder");
            throw new Exception("Missing GrassChunkPoint shader");
        }
        
        // Load wind noise shader
        generateWindShader = Resources.Load<ComputeShader>("WindNoise");
        if (generateWindShader == null) {
            Debug.LogError("Failed to load WindNoise compute shader from Resources folder");
            throw new Exception("Missing WindNoise shader");
        }
        
        // Try to find the correct kernel
        try {
            windNoiseKernelIndex = generateWindShader.FindKernel("WindNoise");
            Debug.Log($"Found wind noise kernel 'WindNoise' at index {windNoiseKernelIndex}");
        }
        catch (Exception e) {
            Debug.LogError($"Failed to find 'WindNoise' kernel: {e.Message}");
            throw;
        }
        
        // Load culling shader
        cullGrassShader = Resources.Load<ComputeShader>("CullGrass");
        if (cullGrassShader == null) {
            Debug.LogError("Failed to load CullGrass compute shader from Resources folder");
            throw new Exception("Missing CullGrass shader");
        }
    }
    
    void ConfigureGrassShader() {
        initializeGrassShader.SetInt("_Dimension", fieldSize);
        initializeGrassShader.SetInt("_ChunkDimension", chunkDimension);
        initializeGrassShader.SetInt("_Scale", chunkDensity);
        
        // Get world position from parent transform (terrain chunk) - preserve Y coordinate
        Vector3 worldPosition = (transform.parent != null) ? transform.parent.position : transform.position;
        float terrainScale = (transform.parent != null) ? transform.parent.localScale.x : 1.0f;

        // Set chunk offset including Y position for proper elevation
        initializeGrassShader.SetVector("_ChunkOffset", new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 0));
        initializeGrassShader.SetFloat("_TerrainScale", terrainScale);
        
        if (heightMap != null) {
            initializeGrassShader.SetTexture(0, "_HeightMap", heightMap);
        }
        else {
            Debug.LogWarning("No height map assigned to grass system");
        }
        
        initializeGrassShader.SetFloat("_DisplacementStrength", displacementStrength);
    }
    
    void SetupWindTexture() {
        // Check if we should use a shared wind texture from ChunkGrassManager
        ChunkGrassManager grassManager = FindObjectOfType<ChunkGrassManager>();
        if (grassManager != null && grassMaterial != null && grassMaterial.HasProperty("_WindTex")) {
            // Use the existing wind texture from the material
            wind = grassMaterial.GetTexture("_WindTex") as RenderTexture;
            
            if (wind == null) {
                // Create a new wind texture if not found
                wind = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                wind.enableRandomWrite = true;
                wind.Create();
            }
        } else {
            // Create a new wind texture
            wind = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            wind.enableRandomWrite = true;
            wind.Create();
        }
        
        numWindThreadGroups = Mathf.CeilToInt(wind.height / 8.0f);
    }
    
    void SetupDrawArguments() {
        // Main mesh arguments
        args = new uint[5] { 0, 0, 0, 0, 0 };
        if (grassMesh != null) {
            args[0] = (uint)grassMesh.GetIndexCount(0);
            args[2] = (uint)grassMesh.GetIndexStart(0);
            args[3] = (uint)grassMesh.GetBaseVertex(0);
        }
        else {
            Debug.LogError("Grass mesh is null");
            throw new Exception("Missing grass mesh");
        }

        // LOD mesh arguments
        argsLOD = new uint[5] { 0, 0, 0, 0, 0 };
        if (grassLODMesh != null) {
            argsLOD[0] = (uint)grassLODMesh.GetIndexCount(0);
            argsLOD[2] = (uint)grassLODMesh.GetIndexStart(0);
            argsLOD[3] = (uint)grassLODMesh.GetBaseVertex(0);
        }
        else {
            Debug.LogWarning("Grass LOD mesh is null, using main grass mesh for LOD");
            argsLOD = args;
        }
    }

    void InitializeChunks() {
        chunks = new GrassChunk[numChunks * numChunks];

        for (int x = 0; x < numChunks; ++x) {
            for (int y = 0; y < numChunks; ++y) {
                chunks[x + y * numChunks] = InitializeGrassChunk(x, y);
            }
        }
    }

    GrassChunk InitializeGrassChunk(int xOffset, int yOffset) {
        GrassChunk chunk = new GrassChunk();

        chunk.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        chunk.argsBufferLOD = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        chunk.argsBuffer.SetData(args);
        chunk.argsBufferLOD.SetData(argsLOD);

        chunk.positionsBuffer = new ComputeBuffer(numInstancesPerChunk, SizeOf(typeof(GrassData)));
        chunk.culledPositionsBuffer = new ComputeBuffer(numInstancesPerChunk, SizeOf(typeof(GrassData)));
        int chunkDim = Mathf.CeilToInt(fieldSize / numChunks);
        
        // Get world position from parent transform (terrain chunk)
        Vector3 worldPosition = (transform.parent != null) ? transform.parent.position : transform.position;
        float terrainScale = (transform.parent != null) ? transform.parent.localScale.x : 1.0f;

        // Calculate center position for bounds - preserve Y component for proper elevation
        Vector3 c = worldPosition;
        c.x += (-(chunkDim * 0.5f * numChunks) + chunkDim * xOffset + chunkDim * 0.5f) * terrainScale;
        c.z += (-(chunkDim * 0.5f * numChunks) + chunkDim * yOffset + chunkDim * 0.5f) * terrainScale;
        
        // Create bounds with full height range to account for displacement
        chunk.bounds = new Bounds(c, new Vector3(chunkDim * terrainScale, displacementStrength * 2, chunkDim * terrainScale));

        initializeGrassShader.SetInt("_XOffset", xOffset);
        initializeGrassShader.SetInt("_YOffset", yOffset);
        initializeGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.positionsBuffer);
        
        try {
            // Calculate proper dispatch size based on chunk dimension
            int dispatchSize = Mathf.CeilToInt(chunkDimension / 8.0f);
            initializeGrassShader.Dispatch(0, dispatchSize, dispatchSize, 1);
        }
        catch (Exception e) {
            Debug.LogError($"Error dispatching grass initialization shader: {e.Message}");
            throw;
        }

        if (grassMaterial != null) {
            chunk.material = new Material(grassMaterial);
            chunk.material.SetBuffer("positionBuffer", chunk.culledPositionsBuffer);
            chunk.material.SetFloat("_DisplacementStrength", displacementStrength);
            chunk.material.SetTexture("_WindTex", wind);
            chunk.material.SetInt("_ChunkNum", xOffset + yOffset * numChunks);
            
            // Set the chunk offset in world space - preserve Y position
            chunk.material.SetVector("_ChunkOffset", new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 0));
            chunk.material.SetFloat("_TerrainScale", terrainScale);
        }
        else {
            Debug.LogError("Grass material is null");
            throw new Exception("Missing grass material");
        }

        return chunk;
    }

    void CullGrass(GrassChunk chunk, Matrix4x4 VP, bool noLOD) {
        try {
            //Reset Args
            if (noLOD)
                chunk.argsBuffer.SetData(args);
            else
                chunk.argsBufferLOD.SetData(argsLOD);

            // Vote
            cullGrassShader.SetMatrix("MATRIX_VP", VP);
            cullGrassShader.SetBuffer(0, "_GrassDataBuffer", chunk.positionsBuffer);
            cullGrassShader.SetBuffer(0, "_VoteBuffer", voteBuffer);
            cullGrassShader.SetVector("_CameraPosition", Camera.main.transform.position);
            cullGrassShader.SetFloat("_Distance", distanceCutoff);
            cullGrassShader.Dispatch(0, numVoteThreadGroups, 1, 1);

            // Scan Instances
            cullGrassShader.SetBuffer(1, "_VoteBuffer", voteBuffer);
            cullGrassShader.SetBuffer(1, "_ScanBuffer", scanBuffer);
            cullGrassShader.SetBuffer(1, "_GroupSumArray", groupSumArrayBuffer);
            cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

            // Scan Groups
            cullGrassShader.SetInt("_NumOfGroups", numThreadGroups);
            cullGrassShader.SetBuffer(2, "_GroupSumArrayIn", groupSumArrayBuffer);
            cullGrassShader.SetBuffer(2, "_GroupSumArrayOut", scannedGroupSumBuffer);
            cullGrassShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

            // Compact
            cullGrassShader.SetBuffer(3, "_GrassDataBuffer", chunk.positionsBuffer);
            cullGrassShader.SetBuffer(3, "_VoteBuffer", voteBuffer);
            cullGrassShader.SetBuffer(3, "_ScanBuffer", scanBuffer);
            cullGrassShader.SetBuffer(3, "_ArgsBuffer", noLOD ? chunk.argsBuffer : chunk.argsBufferLOD);
            cullGrassShader.SetBuffer(3, "_CulledGrassOutputBuffer", chunk.culledPositionsBuffer);
            cullGrassShader.SetBuffer(3, "_GroupSumArray", scannedGroupSumBuffer);
            cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
        }
        catch (Exception e) {
            Debug.LogError($"Error during grass culling: {e.Message}");
        }
    }

    void GenerateWind() {
        try {
            // Check if we should use ChunkGrassManager's wind texture
            ChunkGrassManager grassManager = FindObjectOfType<ChunkGrassManager>();
            if (grassManager != null && grassMaterial != null) {
                // Get the wind texture from the material (shared by all grass chunks)
                RenderTexture sharedWind = grassMaterial.GetTexture("_WindTex") as RenderTexture;
                if (sharedWind != null) {
                    // We're using a shared wind texture, no need to generate our own
                    wind = sharedWind;
                    return;
                }
            }
            
            // Wind generation fallback if shader fails
            if (useWindFallback) {
                GenerateWindFallback();
                return;
            }
            
            // Try to use the compute shader
            try {
                generateWindShader.SetTexture(windNoiseKernelIndex, "_WindMap", wind);
                generateWindShader.SetFloat("_Time", Time.time * windSpeed);
                generateWindShader.SetFloat("_Frequency", frequency);
                generateWindShader.SetFloat("_Amplitude", windStrength);
                generateWindShader.Dispatch(windNoiseKernelIndex, numWindThreadGroups, numWindThreadGroups, 1);
            }
            catch (Exception e) {
                Debug.LogWarning($"Wind shader error, switching to fallback: {e.Message}");
                useWindFallback = true;
                GenerateWindFallback();
            }
        }
        catch (Exception e) {
            Debug.LogError($"Failed to generate wind: {e.Message}");
        }
    }
    
    // Fallback wind generation when shader fails
    void GenerateWindFallback() {
        if (wind == null) return;
        
        // Create temporary texture if needed
        if (tempWindTex == null) {
            tempWindTex = new Texture2D(256, 256, TextureFormat.RGBAFloat, false);
            Debug.Log("Created fallback wind texture");
        }
        
        // Generate simple wind pattern
        float time = Time.time * windSpeed;
        Color[] pixels = new Color[256 * 256];
        
        for (int y = 0; y < 256; y++) {
            for (int x = 0; x < 256; x++) {
                float u = (float)x / 256;
                float v = (float)y / 256;
                
                // Simple wind pattern
                float windX = Mathf.Sin(u * frequency * 10 + time) * Mathf.Cos(v * frequency * 8 + time * 0.7f);
                float windY = Mathf.Sin(v * frequency * 12 + time * 1.3f) * Mathf.Cos(u * frequency * 9 + time * 0.9f);
                
                pixels[y * 256 + x] = new Color(windX * windStrength, windY * windStrength, 0, 1);
            }
        }
        
        tempWindTex.SetPixels(pixels);
        tempWindTex.Apply();
        
        // Copy to render texture
        RenderTexture prevRT = RenderTexture.active;
        RenderTexture.active = wind;
        Graphics.Blit(tempWindTex, wind);
        RenderTexture.active = prevRT;
    }

    void Update() {
        if (Camera.main == null) {
            Debug.LogWarning("No main camera found");
            return;
        }

        Matrix4x4 P = Camera.main.projectionMatrix;
        Matrix4x4 V = Camera.main.transform.worldToLocalMatrix;
        Matrix4x4 VP = P * V;

        GenerateWind();

        for (int i = 0; i < numChunks * numChunks; ++i)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, chunks[i].bounds.center);
            bool noLOD = dist < lodCutoff;

            CullGrass(chunks[i], VP, noLOD);
            
            try {
                if (noLOD)
                    Graphics.DrawMeshInstancedIndirect(grassMesh, 0, chunks[i].material, fieldBounds, chunks[i].argsBuffer);
                else if (grassLODMesh != null)
                    Graphics.DrawMeshInstancedIndirect(grassLODMesh, 0, chunks[i].material, fieldBounds, chunks[i].argsBufferLOD);
                else
                    Graphics.DrawMeshInstancedIndirect(grassMesh, 0, chunks[i].material, fieldBounds, chunks[i].argsBufferLOD);
            }
            catch (Exception e) {
                Debug.LogError($"Error rendering grass chunk {i}: {e.Message}");
            }
        }
    }
    
    void OnDisable() {
        CleanupBuffers();
    }

    void OnDestroy() {
        CleanupBuffers();
    }

    void CleanupBuffers() {
        if (tempWindTex != null) {
            Destroy(tempWindTex);
            tempWindTex = null;
        }
        
        if (voteBuffer != null) {
            voteBuffer.Release();
            voteBuffer = null;
        }
        
        if (scanBuffer != null) {
            scanBuffer.Release();
            scanBuffer = null;
        }
        
        if (groupSumArrayBuffer != null) {
            groupSumArrayBuffer.Release();
            groupSumArrayBuffer = null;
        }
        
        if (scannedGroupSumBuffer != null) {
            scannedGroupSumBuffer.Release();
            scannedGroupSumBuffer = null;
        }
        
        // Don't release the wind texture if it's shared
        bool isSharedWind = false;
        if (grassMaterial != null && wind != null) {
            RenderTexture matWind = grassMaterial.GetTexture("_WindTex") as RenderTexture;
            isSharedWind = (matWind == wind);
        }
        
        if (wind != null && !isSharedWind) {
            wind.Release();
            wind = null;
        }

        if (chunks != null) {
            for (int i = 0; i < numChunks * numChunks; ++i) {
                FreeChunk(chunks[i]);
            }
            chunks = null;
        }
    }

    void FreeChunk(GrassChunk chunk) {
        if (chunk.positionsBuffer != null) {
            chunk.positionsBuffer.Release();
            chunk.positionsBuffer = null;
        }
        
        if (chunk.culledPositionsBuffer != null) {
            chunk.culledPositionsBuffer.Release();
            chunk.culledPositionsBuffer = null;
        }
        
        if (chunk.argsBuffer != null) {
            chunk.argsBuffer.Release();
            chunk.argsBuffer = null;
        }
        
        if (chunk.argsBufferLOD != null) {
            chunk.argsBufferLOD.Release();
            chunk.argsBufferLOD = null;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        if (chunks != null) {
            for (int i = 0; i < numChunks * numChunks; ++i) {
                Gizmos.DrawWireCube(chunks[i].bounds.center, chunks[i].bounds.size);
            }
        }
    }
}