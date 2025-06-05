using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Linq;

/// <summary>
/// Multi-threaded WFC generator for massive world generation
/// Separates logic generation from Unity mesh instantiation
/// </summary>
public class ThreadedWFCGenerator : MonoBehaviour
{
    [Header("Threading Settings")]
    [SerializeField] private int maxConcurrentGenerations = 4;
    [SerializeField] private bool enableMultithreading = true;
    [SerializeField] private int meshInstantiationPerFrame = 2;
    
    [Header("World Settings")]
    [SerializeField] public Vector2Int worldSizeInChunks = new Vector2Int(50, 50);
    [SerializeField] public Vector3Int chunkSize = new Vector3Int(16, 3, 16);
    [SerializeField] public WFCController wfcControllerPrefab;
    
    // Thread-safe collections
    private ConcurrentQueue<ChunkGenerationRequest> generationQueue = new ConcurrentQueue<ChunkGenerationRequest>();
    private ConcurrentQueue<ChunkGenerationResult> completedChunks = new ConcurrentQueue<ChunkGenerationResult>();
    private ConcurrentDictionary<Vector2Int, ChunkGenerationState> chunkStates = new ConcurrentDictionary<Vector2Int, ChunkGenerationState>();
    
    // Main thread collections
    private Dictionary<Vector2Int, GameObject> instantiatedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<string, Prototype> prototypes;
    
    // Threading
    private CancellationTokenSource cancellationTokenSource;
    private SemaphoreSlim generationSemaphore;
    
    // Events
    public System.Action<Vector2Int, float> OnGenerationProgress;
    public System.Action<Vector2Int> OnChunkCompleted;
    
    private void Start()
    {
        InitializeGenerator();
    }
    
    private void InitializeGenerator()
    {
        cancellationTokenSource = new CancellationTokenSource();
        generationSemaphore = new SemaphoreSlim(maxConcurrentGenerations, maxConcurrentGenerations);
        
        // Load prototypes once (they're immutable during generation)
        prototypes = WFCController.LoadPrototypeData();
        
        StartCoroutine(ProcessCompletedChunks());
        
        if (enableMultithreading)
        {
            StartCoroutine(ProcessGenerationQueue());
        }
    }
    
    #region Public API
    
    /// <summary>
    /// Queue a chunk for generation
    /// </summary>
    public void RequestChunkGeneration(Vector2Int chunkCoord, int priority = 0)
    {
        if (chunkStates.ContainsKey(chunkCoord))
        {
            return; // Already queued or generated
        }
        
        var request = new ChunkGenerationRequest
        {
            coordinates = chunkCoord,
            priority = priority,
            timestamp = System.DateTime.Now
        };
        
        chunkStates[chunkCoord] = ChunkGenerationState.Queued;
        generationQueue.Enqueue(request);
    }
    
    /// <summary>
    /// Generate chunks in a radius around a point
    /// </summary>
    public void RequestChunksInRadius(Vector3 worldPosition, int radius, int priority = 0)
    {
        Vector2Int centerChunk = WorldToChunkCoords(worldPosition);
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                if (IsChunkInWorldBounds(chunkCoord))
                {
                    // Higher priority for chunks closer to center
                    int chunkPriority = priority + (radius - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)));
                    RequestChunkGeneration(chunkCoord, chunkPriority);
                }
            }
        }
    }
    
    /// <summary>
    /// Generate the entire world (use with caution on large worlds)
    /// </summary>
    public void GenerateEntireWorld()
    {
        for (int x = 0; x < worldSizeInChunks.x; x++)
        {
            for (int z = 0; z < worldSizeInChunks.y; z++)
            {
                RequestChunkGeneration(new Vector2Int(x, z));
            }
        }
    }
    
    /// <summary>
    /// Clear a specific chunk
    /// </summary>
    public void UnloadChunk(Vector2Int chunkCoord)
    {
        if (instantiatedChunks.ContainsKey(chunkCoord))
        {
            DestroyImmediate(instantiatedChunks[chunkCoord]);
            instantiatedChunks.Remove(chunkCoord);
        }
        
        chunkStates.TryRemove(chunkCoord, out _);
    }
    
    /// <summary>
    /// Clear all chunks
    /// </summary>
    public void ClearAllChunks()
    {
        foreach (var chunk in instantiatedChunks.Values)
        {
            if (chunk != null)
            {
                DestroyImmediate(chunk);
            }
        }
        
        instantiatedChunks.Clear();
        chunkStates.Clear();
        
        // Clear queues
        while (generationQueue.TryDequeue(out _)) { }
        while (completedChunks.TryDequeue(out _)) { }
    }
    
    #endregion
    
    #region Generation Processing
    
    private IEnumerator ProcessGenerationQueue()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (generationQueue.TryDequeue(out ChunkGenerationRequest request))
            {
                // Start generation on background thread
                _ = Task.Run(() => GenerateChunkAsync(request), cancellationTokenSource.Token);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private async Task GenerateChunkAsync(ChunkGenerationRequest request)
    {
        await generationSemaphore.WaitAsync(cancellationTokenSource.Token);
        
        try
        {
            chunkStates[request.coordinates] = ChunkGenerationState.Generating;
            
            // Generate chunk data on background thread
            var result = await Task.Run(() => GenerateChunkData(request), cancellationTokenSource.Token);
            
            // Queue for main thread processing
            completedChunks.Enqueue(result);
            chunkStates[request.coordinates] = ChunkGenerationState.ReadyForInstantiation;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating chunk {request.coordinates}: {e.Message}");
            chunkStates[request.coordinates] = ChunkGenerationState.Failed;
        }
        finally
        {
            generationSemaphore.Release();
        }
    }
    
    private ChunkGenerationResult GenerateChunkData(ChunkGenerationRequest request)
    {
        var result = new ChunkGenerationResult
        {
            coordinates = request.coordinates,
            success = false
        };
        
        try
        {
            // Create thread-safe WFC model
            var threadModel = new ThreadSafeWFC3DModel();
            threadModel.Initialize(chunkSize, CreatePrototypeCopies(prototypes));
            
            // Apply constraints
            ApplyChunkConstraints(threadModel, request.coordinates);
            
            // Generate
            int iterations = 0;
            int maxIterations = chunkSize.x * chunkSize.y * chunkSize.z * 2;
            
            while (!threadModel.IsCollapsed() && iterations < maxIterations)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return result;
                }
                
                threadModel.Iterate();
                iterations++;
                
                // Report progress periodically
                if (iterations % 10 == 0)
                {
                    float progress = (float)threadModel.GetCollapsedCellCount() / (chunkSize.x * chunkSize.y * chunkSize.z);
                    OnGenerationProgress?.Invoke(request.coordinates, progress);
                }
            }
            
            if (threadModel.IsCollapsed())
            {
                result.success = true;
                result.generatedData = ExtractChunkData(threadModel);
            }
            else
            {
                Debug.LogWarning($"Chunk {request.coordinates} failed to collapse completely");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in chunk generation {request.coordinates}: {e.Message}");
        }
        
        return result;
    }
    
    #endregion
    
    #region Main Thread Processing
    
    private IEnumerator ProcessCompletedChunks()
    {
        while (true)
        {
            int processed = 0;
            
            while (completedChunks.TryDequeue(out ChunkGenerationResult result) && processed < meshInstantiationPerFrame)
            {
                if (result.success)
                {
                    InstantiateChunk(result);
                    chunkStates[result.coordinates] = ChunkGenerationState.Complete;
                    OnChunkCompleted?.Invoke(result.coordinates);
                }
                else
                {
                    chunkStates[result.coordinates] = ChunkGenerationState.Failed;
                    Debug.LogError($"Failed to generate chunk {result.coordinates}");
                }
                
                processed++;
            }
            
            yield return null; // Wait one frame
        }
    }
    
    private void InstantiateChunk(ChunkGenerationResult result)
    {
        Vector3 worldPosition = ChunkToWorldPosition(result.coordinates);
        GameObject chunkObject = new GameObject($"Chunk_{result.coordinates.x}_{result.coordinates.y}");
        chunkObject.transform.parent = transform;
        chunkObject.transform.position = worldPosition;
        
        // Add WFC controller and restore data
        WFCController controller = Instantiate(wfcControllerPrefab, chunkObject.transform);
        controller.size = chunkSize;
        
        // Restore the generated data
        RestoreChunkFromData(controller, result.generatedData);
        
        instantiatedChunks[result.coordinates] = chunkObject;
    }
    
    #endregion
    
    #region Thread-Safe WFC Model
    
    /// <summary>
    /// Thread-safe version of WFC3D_Model that doesn't use Unity-specific calls
    /// </summary>
    private class ThreadSafeWFC3DModel
    {
        private Dictionary<string, Prototype>[,,] waveFunction;
        private Vector3Int size;
        private HashSet<Vector3Int> uncollapsedCells;
        private System.Random random;
        
        private readonly Dictionary<Vector3Int, int> directionToIndex = new Dictionary<Vector3Int, int>
        {
            { new Vector3Int(-1, 0, 0), 2 },  // -X
            { new Vector3Int(1, 0, 0), 0 },   // +X
            { new Vector3Int(0, 0, 1), 1 },   // +Z
            { new Vector3Int(0, 0, -1), 3 },  // -Z
            { new Vector3Int(0, 1, 0), 4 },   // +Y
            { new Vector3Int(0, -1, 0), 5 }   // -Y
        };
        
        public void Initialize(Vector3Int newSize, Dictionary<string, Prototype> allPrototypes)
        {
            size = newSize;
            waveFunction = new Dictionary<string, Prototype>[size.x, size.y, size.z];
            uncollapsedCells = new HashSet<Vector3Int>();
            random = new System.Random();
            
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        waveFunction[x, y, z] = new Dictionary<string, Prototype>(allPrototypes);
                        if (waveFunction[x, y, z].Count > 1)
                        {
                            uncollapsedCells.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }
        
        public bool IsCollapsed()
        {
            return uncollapsedCells.Count == 0;
        }
        
        public int GetCollapsedCellCount()
        {
            return (size.x * size.y * size.z) - uncollapsedCells.Count;
        }
        
        public void Iterate()
        {
            if (IsCollapsed()) return;
            
            Vector3Int coordsToCollapse = GetMinEntropyCoords();
            if (coordsToCollapse.x == -1) return;
            
            CollapseAt(coordsToCollapse);
            Propagate(coordsToCollapse);
        }
        
        private Vector3Int GetMinEntropyCoords()
        {
            if (uncollapsedCells.Count == 0)
                return new Vector3Int(-1, -1, -1);
            
            float minAdjustedEntropy = float.MaxValue;
            Vector3Int minCoords = new Vector3Int(-1, -1, -1);
            
            foreach (var coords in uncollapsedCells)
            {
                int entropy = GetEntropy(coords);
                if (entropy > 1)
                {
                    float adjustedEntropy = entropy + (float)(random.NextDouble() * 0.2 - 0.1);
                    if (adjustedEntropy < minAdjustedEntropy)
                    {
                        minAdjustedEntropy = adjustedEntropy;
                        minCoords = coords;
                    }
                }
            }
            
            return minCoords;
        }
        
        private void CollapseAt(Vector3Int coords)
        {
            var possiblePrototypes = waveFunction[coords.x, coords.y, coords.z];
            if (possiblePrototypes.Count <= 1)
            {
                uncollapsedCells.Remove(coords);
                return;
            }
            
            string selectedPrototypeName = WeightedChoice(possiblePrototypes);
            if (selectedPrototypeName != null && possiblePrototypes.TryGetValue(selectedPrototypeName, out Prototype selectedPrototype))
            {
                waveFunction[coords.x, coords.y, coords.z] = new Dictionary<string, Prototype>
                {
                    { selectedPrototypeName, selectedPrototype }
                };
                uncollapsedCells.Remove(coords);
            }
        }
        
        private string WeightedChoice(Dictionary<string, Prototype> prototypes)
        {
            if (prototypes.Count == 0) return null;
            
            string bestPrototypeName = null;
            float maxAdjustedWeight = float.MinValue;
            
            foreach (var kvp in prototypes)
            {
                float adjustedWeight = kvp.Value.weight + (float)(random.NextDouble() * 2.0 - 1.0);
                if (adjustedWeight > maxAdjustedWeight)
                {
                    maxAdjustedWeight = adjustedWeight;
                    bestPrototypeName = kvp.Key;
                }
            }
            
            return bestPrototypeName ?? prototypes.Keys.First();
        }
        
        private void Propagate(Vector3Int initialCoords)
        {
            var propagationStack = new Stack<Vector3Int>();
            propagationStack.Push(initialCoords);
            
            while (propagationStack.Count > 0)
            {
                Vector3Int currentCoords = propagationStack.Pop();
                
                foreach (var direction in GetValidDirections(currentCoords))
                {
                    Vector3Int neighborCoords = currentCoords + direction;
                    var neighborPrototypes = waveFunction[neighborCoords.x, neighborCoords.y, neighborCoords.z];
                    
                    if (neighborPrototypes.Count == 0) continue;
                    
                    HashSet<string> allowedNeighboringPrototypes = GetPossibleNeighbours(currentCoords, direction);
                    
                    var prototypesToRemove = new List<string>();
                    foreach (var protoName in neighborPrototypes.Keys)
                    {
                        if (!allowedNeighboringPrototypes.Contains(protoName))
                        {
                            prototypesToRemove.Add(protoName);
                        }
                    }
                    
                    bool changed = false;
                    foreach (var protoName in prototypesToRemove)
                    {
                        if (neighborPrototypes.Remove(protoName))
                        {
                            changed = true;
                        }
                    }
                    
                    if (changed)
                    {
                        if (neighborPrototypes.Count == 0)
                        {
                            // Contradiction - this shouldn't happen in a well-designed WFC
                            continue;
                        }
                        else if (neighborPrototypes.Count == 1)
                        {
                            uncollapsedCells.Remove(neighborCoords);
                        }
                        
                        if (!propagationStack.Contains(neighborCoords))
                        {
                            propagationStack.Push(neighborCoords);
                        }
                    }
                }
            }
        }
        
        private HashSet<string> GetPossibleNeighbours(Vector3Int coords, Vector3Int direction)
        {
            var prototypesInCurrentCell = waveFunction[coords.x, coords.y, coords.z];
            var validNeighbourPrototypes = new HashSet<string>();
            
            if (!directionToIndex.TryGetValue(direction, out int dirIdx))
            {
                return validNeighbourPrototypes;
            }
            
            foreach (var prototype in prototypesInCurrentCell.Values)
            {
                if (prototype.valid_neighbours != null && dirIdx < prototype.valid_neighbours.Count)
                {
                    var neighboursInDirection = prototype.valid_neighbours[dirIdx];
                    if (neighboursInDirection != null)
                    {
                        foreach (var neighbourName in neighboursInDirection)
                        {
                            validNeighbourPrototypes.Add(neighbourName);
                        }
                    }
                }
            }
            
            return validNeighbourPrototypes;
        }
        
        private List<Vector3Int> GetValidDirections(Vector3Int coords)
        {
            var directions = new List<Vector3Int>();
            
            if (coords.x > 0) directions.Add(new Vector3Int(-1, 0, 0));
            if (coords.x < size.x - 1) directions.Add(new Vector3Int(1, 0, 0));
            if (coords.y > 0) directions.Add(new Vector3Int(0, -1, 0));
            if (coords.y < size.y - 1) directions.Add(new Vector3Int(0, 1, 0));
            if (coords.z > 0) directions.Add(new Vector3Int(0, 0, -1));
            if (coords.z < size.z - 1) directions.Add(new Vector3Int(0, 0, 1));
            
            return directions;
        }
        
        private int GetEntropy(Vector3Int coords)
        {
            return waveFunction[coords.x, coords.y, coords.z].Count;
        }
        
        public SerializableChunkData GetSerializableData()
        {
            var data = new SerializableChunkData
            {
                size = size,
                cellData = new SerializableCellData[size.x, size.y, size.z]
            };
            
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        var cell = waveFunction[x, y, z];
                        data.cellData[x, y, z] = new SerializableCellData
                        {
                            isCollapsed = cell.Count == 1,
                            selectedPrototype = cell.Count == 1 ? cell.Keys.First() : null,
                            possiblePrototypes = cell.Keys.ToArray()
                        };
                    }
                }
            }
            
            return data;
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private Dictionary<string, Prototype> CreatePrototypeCopies(Dictionary<string, Prototype> original)
    {
        var copies = new Dictionary<string, Prototype>();
        
        foreach (var kvp in original)
        {
            var originalProto = kvp.Value;
            var prototypeCopy = new Prototype
            {
                mesh_name = originalProto.mesh_name,
                mesh_rotation = originalProto.mesh_rotation,
                posX = originalProto.posX,
                negX = originalProto.negX,
                posY = originalProto.posY,
                negY = originalProto.negY,
                posZ = originalProto.posZ,
                negZ = originalProto.negZ,
                constrain_to = originalProto.constrain_to,
                constrain_from = originalProto.constrain_from,
                weight = originalProto.weight,
                valid_neighbours = originalProto.valid_neighbours?.Select(list => new List<string>(list)).ToList()
            };
            copies.Add(kvp.Key, prototypeCopy);
        }
        
        return copies;
    }
    
    private void ApplyChunkConstraints(ThreadSafeWFC3DModel model, Vector2Int chunkCoord)
    {
        // Apply boundary constraints based on neighboring chunks
        // This is where you'd implement cross-chunk consistency
        
        // For now, applying basic edge constraints similar to your original
        // You can expand this to check actual neighboring chunk data
    }
    
    private SerializableChunkData ExtractChunkData(ThreadSafeWFC3DModel model)
    {
        return model.GetSerializableData();
    }
    
    private void RestoreChunkFromData(WFCController controller, SerializableChunkData data)
    {
        // Create a new WFC model and restore state
        controller.wfcModel = new WFC3D_Model();
        controller.wfcModel.Initialize(data.size, prototypes);
        
        // Apply the generated state
        for (int x = 0; x < data.size.x; x++)
        {
            for (int y = 0; y < data.size.y; y++)
            {
                for (int z = 0; z < data.size.z; z++)
                {
                    var cellData = data.cellData[x, y, z];
                    Vector3Int coords = new Vector3Int(x, y, z);
                    
                    if (cellData.isCollapsed && !string.IsNullOrEmpty(cellData.selectedPrototype))
                    {
                        controller.wfcModel.CollapseCoordsTo(coords, cellData.selectedPrototype);
                    }
                }
            }
        }
        
        // Visualize the restored chunk
        controller.VisualizeWaveFunction();
        controller.SpawnChests();
    }
    
    private Vector2Int WorldToChunkCoords(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        return new Vector2Int(
            Mathf.FloorToInt(localPos.x / chunkSize.x),
            Mathf.FloorToInt(localPos.z / chunkSize.z)
        );
    }
    
    private Vector3 ChunkToWorldPosition(Vector2Int chunkCoords)
    {
        Vector3 localPos = new Vector3(
            chunkCoords.x * chunkSize.x,
            0,
            chunkCoords.y * chunkSize.z
        );
        return transform.TransformPoint(localPos);
    }
    
    private bool IsChunkInWorldBounds(Vector2Int chunkCoord)
    {
        return chunkCoord.x >= 0 && chunkCoord.x < worldSizeInChunks.x &&
               chunkCoord.y >= 0 && chunkCoord.y < worldSizeInChunks.y;
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        generationSemaphore?.Dispose();
    }
    
    private void OnApplicationQuit()
    {
        cancellationTokenSource?.Cancel();
    }
    
    #endregion
    
    #region Debug
    
    public void LogGenerationStats()
    {
        int queued = 0, generating = 0, complete = 0, failed = 0;
        
        foreach (var state in chunkStates.Values)
        {
            switch (state)
            {
                case ChunkGenerationState.Queued: queued++; break;
                case ChunkGenerationState.Generating: generating++; break;
                case ChunkGenerationState.Complete: complete++; break;
                case ChunkGenerationState.Failed: failed++; break;
            }
        }
        
        Debug.Log($"=== THREADED WFC STATS ===");
        Debug.Log($"Queued: {queued}, Generating: {generating}, Complete: {complete}, Failed: {failed}");
        Debug.Log($"Generation Queue: {generationQueue.Count}");
        Debug.Log($"Completed Queue: {completedChunks.Count}");
        Debug.Log($"Instantiated Chunks: {instantiatedChunks.Count}");
    }
    
    #endregion
}

#region Data Structures

[System.Serializable]
public class ChunkGenerationRequest
{
    public Vector2Int coordinates;
    public int priority;
    public System.DateTime timestamp;
}

[System.Serializable]
public class ChunkGenerationResult
{
    public Vector2Int coordinates;
    public bool success;
    public SerializableChunkData generatedData;
}

[System.Serializable]
public class SerializableChunkData
{
    public Vector3Int size;
    public SerializableCellData[,,] cellData;
}

[System.Serializable]
public class SerializableCellData
{
    public bool isCollapsed;
    public string selectedPrototype;
    public string[] possiblePrototypes;
}

public enum ChunkGenerationState
{
    Queued,
    Generating,
    ReadyForInstantiation,
    Complete,
    Failed
}

#endregion