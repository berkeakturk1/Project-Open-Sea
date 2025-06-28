using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using perlin_Scripts.Data;
using Vector2 = UnityEngine.Vector2;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    public GameObject[] zombiePrefabs;
    
    [Header("Spawn Settings")]
    [Range(0.001f, 0.1f)]
    public float zombieDensity = 0.02f;
    [Range(0f, 1f)]
    public float minHeightThreshold = 0.1f;
    [Range(0f, 1f)]
    public float maxHeightThreshold = 0.9f;
    [Range(0f, 60f)]
    public float maxSlopeAngle = 25f;
    [Range(2f, 10f)]
    public float minDistanceBetweenZombies = 3f;
    public int maxZombiesPerChunk = 20;
    
    [Header("References")]
    public Transform player;
    public RuntimeNavMeshBuilder navMeshBuilder;
    
    private Dictionary<Vector2, List<GameObject>> chunkZombies = new Dictionary<Vector2, List<GameObject>>();
    private MapGenerator mapGenerator;
    
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        
        if (navMeshBuilder == null)
            navMeshBuilder = FindObjectOfType<RuntimeNavMeshBuilder>();
        
        if (player == null && Camera.main != null)
            player = Camera.main.transform;
        
        // Subscribe to NavMesh completion events
        if (navMeshBuilder != null)
        {
            navMeshBuilder.OnNavMeshBuilt += OnNavMeshReady;
        }
        
        Debug.Log("ZombieSpawner initialized");
    }
    
    void OnNavMeshReady(Vector2 chunkPosition)
    {
        Debug.Log($"NavMesh ready for chunk {chunkPosition}, checking for pending zombie spawns...");
        // NavMesh is ready, we can spawn zombies for this chunk if needed
    }
    
    public void SpawnZombiesOnChunk(MapData mapData, Vector2 chunkPosition, int lod)
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("No zombie prefabs assigned!");
            return;
        }
        
        // Don't spawn if we already have zombies on this chunk
        if (chunkZombies.ContainsKey(chunkPosition))
        {
            Debug.Log($"Chunk {chunkPosition} already has zombies, skipping spawn");
            return;
        }
        
        StartCoroutine(SpawnZombiesCoroutine(mapData, chunkPosition, lod));
    }
    
    IEnumerator SpawnZombiesCoroutine(MapData mapData, Vector2 chunkPosition, int lod)
    {
        Debug.Log($"Starting zombie spawn for chunk {chunkPosition}");
        
        float[,] heightMap = mapData.heightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Create container for this chunk's zombies
        GameObject zombieContainer = new GameObject($"Zombies_{chunkPosition.x}_{chunkPosition.y}");
        zombieContainer.transform.SetParent(transform);
        zombieContainer.transform.position = new Vector3(chunkPosition.x, 0, chunkPosition.y);
        
        List<GameObject> chunkZombieList = new List<GameObject>();
        List<Vector3> zombiePositions = new List<Vector3>();
        
        // Use deterministic seed based on chunk position
        System.Random prng = new System.Random(
            (int)(chunkPosition.x * 1000 + chunkPosition.y * 10000 + mapGenerator.noiseData.seed)
        );
        
        float scale = mapGenerator.terrainData.uniformScale;
        float adjustedDensity = zombieDensity / (lod + 1);
        int step = Mathf.Max(1, Mathf.FloorToInt(1f / adjustedDensity / 5f));
        
        int zombieCount = 0;
        int terrainLayerMask = LayerMask.GetMask("Default");
        
        for (int y = 1; y < height - 1; y += step)
        {
            for (int x = 1; x < width - 1; x += step)
            {
                if (zombieCount >= maxZombiesPerChunk) break;
                
                if (prng.NextDouble() > adjustedDensity) continue;
                
                float heightValue = heightMap[x, y];
                
                // Check height thresholds
                if (heightValue < minHeightThreshold || heightValue > maxHeightThreshold)
                    continue;
                
                // Calculate world position
                float worldX = (x - width/2f);
                float worldZ = (y - height/2f);
                float worldY = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightValue) * mapGenerator.terrainData.meshHeightMultiplier;
                
                Vector3 worldPosition = new Vector3(
                    (worldX + chunkPosition.x) * scale,
                    worldY * scale,
                    (worldZ + chunkPosition.y) * scale
                );
                
                // Check slope
                Vector3 normal = CalculateNormal(x, y, heightMap);
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope > maxSlopeAngle) continue;
                
                // Check minimum distance from other zombies
                bool tooClose = false;
                foreach (Vector3 existingZombie in zombiePositions)
                {
                    if (Vector3.Distance(worldPosition, existingZombie) < minDistanceBetweenZombies)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
                
                // Use raycast for precise positioning
                Vector3 rayStart = worldPosition + Vector3.up * 5f;
                RaycastHit hit;
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, terrainLayerMask))
                {
                    worldPosition = hit.point;
                }
                
                // Small offset to prevent clipping
                worldPosition += Vector3.up * 0.1f;
                
                // Spawn zombie
                GameObject zombie = SpawnSingleZombie(worldPosition, zombieContainer.transform, prng);
                if (zombie != null)
                {
                    chunkZombieList.Add(zombie);
                    zombiePositions.Add(worldPosition);
                    zombieCount++;
                    
                    // Add NavMeshAgent
                    NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                    if (agent == null)
                    {
                        agent = zombie.AddComponent<NavMeshAgent>();
                    }
                    
                    // Add chunk tracker
                    ZombieChunkTracker tracker = zombie.GetComponent<ZombieChunkTracker>();
                    if (tracker == null)
                    {
                        tracker = zombie.AddComponent<ZombieChunkTracker>();
                    }
                    tracker.chunkPosition = chunkPosition;
                    tracker.spawner = this;
                }
                
                // Yield occasionally to prevent frame drops
                if (zombieCount % 5 == 0)
                    yield return null;
            }
            if (zombieCount >= maxZombiesPerChunk) break;
        }
        
        // Store the zombies for this chunk
        chunkZombies[chunkPosition] = chunkZombieList;
        
        Debug.Log($"✓ Spawned {zombieCount} zombies on chunk {chunkPosition}");
    }
    
    GameObject SpawnSingleZombie(Vector3 position, Transform parent, System.Random prng)
    {
        // Select random zombie prefab
        GameObject prefab = zombiePrefabs[prng.Next(0, zombiePrefabs.Length)];
        
        GameObject zombie = Instantiate(prefab, position, Quaternion.identity, parent);
        zombie.transform.rotation = Quaternion.Euler(0, (float)(prng.NextDouble() * 360), 0);
        
        return zombie;
    }
    
    Vector3 CalculateNormal(int x, int y, float[,] heightMap)
    {
        float heightL = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x-1, y]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightR = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x+1, y]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightD = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x, y-1]) * mapGenerator.terrainData.meshHeightMultiplier;
        float heightU = mapGenerator.terrainData.meshHeightCurve.Evaluate(heightMap[x, y+1]) * mapGenerator.terrainData.meshHeightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    public void ClearChunkZombies(Vector2 chunkPosition)
    {
        if (chunkZombies.TryGetValue(chunkPosition, out List<GameObject> zombies))
        {
            foreach (GameObject zombie in zombies)
            {
                if (zombie != null)
                    Destroy(zombie);
            }
            chunkZombies.Remove(chunkPosition);
            Debug.Log($"✓ Cleared zombies for chunk {chunkPosition}");
        }
    }
    
    public void RemoveZombie(GameObject zombie, Vector2 chunkPosition)
    {
        if (chunkZombies.TryGetValue(chunkPosition, out List<GameObject> zombies))
        {
            zombies.Remove(zombie);
        }
        Destroy(zombie);
    }
    
    public int GetZombieCount(Vector2 chunkPosition)
    {
        if (chunkZombies.TryGetValue(chunkPosition, out List<GameObject> zombies))
        {
            return zombies.Count;
        }
        return 0;
    }
    
    void OnDestroy()
    {
        if (navMeshBuilder != null)
        {
            navMeshBuilder.OnNavMeshBuilt -= OnNavMeshReady;
        }
    }
}

