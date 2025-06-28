using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class RuntimeNavMeshBuilder : MonoBehaviour
{
    [Header("NavMesh Build Settings")]
    public float agentRadius = 0.5f;
    public float agentHeight = 2f;
    public float agentSlope = 45f;
    public float agentClimb = 0.4f;
    public LayerMask navMeshLayers = -1;
    
    private Dictionary<Vector2, NavMeshDataInstance> chunkNavMeshes = new Dictionary<Vector2, NavMeshDataInstance>();
    private Queue<NavMeshBuildRequest> buildQueue = new Queue<NavMeshBuildRequest>();
    private bool isBuilding = false;
    
    public System.Action<Vector2> OnNavMeshBuilt;
    
    void Start()
    {
        Debug.Log("RuntimeNavMeshBuilder initialized");
    }
    
    public void RequestNavMeshBuild(Vector2 chunkPosition, Vector3 chunkCenter, float chunkSize)
    {
        NavMeshBuildRequest request = new NavMeshBuildRequest
        {
            chunkPosition = chunkPosition,
            chunkCenter = chunkCenter,
            chunkSize = chunkSize
        };
        
        buildQueue.Enqueue(request);
        
        if (!isBuilding)
        {
            StartCoroutine(ProcessBuildQueue());
        }
    }
    
    IEnumerator ProcessBuildQueue()
    {
        isBuilding = true;
        
        while (buildQueue.Count > 0)
        {
            NavMeshBuildRequest request = buildQueue.Dequeue();
            yield return StartCoroutine(BuildNavMeshForChunk(request));
            yield return new WaitForSeconds(0.1f);
        }
        
        isBuilding = false;
    }
    
    IEnumerator BuildNavMeshForChunk(NavMeshBuildRequest request)
    {
        // Wait a moment for the terrain mesh to be fully set up
        yield return new WaitForSeconds(0.5f);
        
        Vector3 chunkCenter = request.chunkCenter;
        float chunkSize = request.chunkSize;
        Vector2 chunkPosition = request.chunkPosition;
        
        // Define the bounds for this chunk
        Bounds chunkBounds = new Bounds(chunkCenter, new Vector3(chunkSize, 200f, chunkSize));
        
        // Build settings
        NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);
        buildSettings.agentRadius = agentRadius;
        buildSettings.agentHeight = agentHeight;
        buildSettings.agentSlope = agentSlope;
        buildSettings.agentClimb = agentClimb;
        
        // Collect geometry sources
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            chunkBounds, 
            navMeshLayers, 
            NavMeshCollectGeometry.RenderMeshes, 
            0, 
            new List<NavMeshBuildMarkup>(), 
            sources
        );
        
        if (sources.Count > 0)
        {
            // Build the NavMesh data
            NavMeshData chunkNavMeshData = NavMeshBuilder.BuildNavMeshData(
                buildSettings, 
                sources, 
                chunkBounds, 
                chunkCenter, 
                Quaternion.identity
            );
            
            if (chunkNavMeshData != null)
            {
                // Remove old NavMesh if it exists
                if (chunkNavMeshes.TryGetValue(chunkPosition, out NavMeshDataInstance oldInstance))
                {
                    if (oldInstance.valid)
                        NavMesh.RemoveNavMeshData(oldInstance);
                }
                
                // Add the new NavMesh
                NavMeshDataInstance newInstance = NavMesh.AddNavMeshData(chunkNavMeshData);
                chunkNavMeshes[chunkPosition] = newInstance;
                
                Debug.Log($"✓ NavMesh built for chunk {chunkPosition}");
                
                // Notify that NavMesh is ready
                OnNavMeshBuilt?.Invoke(chunkPosition);
            }
            else
            {
                Debug.LogWarning($"Failed to build NavMesh data for chunk {chunkPosition}");
            }
        }
        else
        {
            Debug.LogWarning($"No geometry sources found for chunk {chunkPosition}");
        }
    }
    
    public void RemoveNavMeshForChunk(Vector2 chunkPosition)
    {
        if (chunkNavMeshes.TryGetValue(chunkPosition, out NavMeshDataInstance instance))
        {
            if (instance.valid)
            {
                NavMesh.RemoveNavMeshData(instance);
                Debug.Log($"✓ NavMesh removed for chunk {chunkPosition}");
            }
            chunkNavMeshes.Remove(chunkPosition);
        }
    }
    
    public bool HasNavMeshForChunk(Vector2 chunkPosition)
    {
        return chunkNavMeshes.ContainsKey(chunkPosition) && chunkNavMeshes[chunkPosition].valid;
    }
    
    void OnDestroy()
    {
        // Clean up all NavMesh instances
        foreach (var instance in chunkNavMeshes.Values)
        {
            if (instance.valid)
                NavMesh.RemoveNavMeshData(instance);
        }
        chunkNavMeshes.Clear();
    }
    
    struct NavMeshBuildRequest
    {
        public Vector2 chunkPosition;
        public Vector3 chunkCenter;
        public float chunkSize;
    }
}