using UnityEngine;
using System.Collections.Generic;

public class OceanDebrisSpawner : MonoBehaviour
{
    [Header("Debris Settings")]
    public GameObject[] debrisPrefabs;       // Array of debris prefabs (should have Floater component)
    public int maxDebrisCount = 50;          // Maximum number of debris objects
    public float spawnRadius = 100f;         // Radius around the ship to spawn debris
    public float minSpawnDistance = 20f;     // Minimum distance from ship to spawn debris
    public float despawnDistance = 150f;     // Distance at which debris gets despawned
    
    [Header("Spawn Rate")]
    public float spawnInterval = 2f;         // Time between debris spawns (in seconds)
    public int debrisPerSpawn = 1;           // Number of debris to spawn per interval
    
    [Header("Position Settings")]
    public Transform ship;                   // Reference to the ship
    public float spawnHeightOffset = 2f;     // Height above water surface to spawn debris
    
    [Header("Ocean Reference")]
    public OceanGridGenerator oceanGridGenerator; // Reference to ocean system
    
    [Header("Debris Physics")]
    public float initialVelocityRange = 2f;  // Random initial velocity for spawned debris
    
    private List<GameObject> activeDebris;   // List to track active debris
    private float nextSpawnTime;             // Time for next spawn
    private GerstnerWaveManager waveManager; // Wave manager for surface height calculation
    
    void Start()
    {
        activeDebris = new List<GameObject>();
        nextSpawnTime = Time.time + spawnInterval;
        
        // Validate ship reference
        if (ship == null)
        {
            Debug.LogWarning("Ship reference not set! Using this transform as ship.");
            ship = transform;
        }
        
        // Validate debris prefabs
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            Debug.LogError("No debris prefabs assigned!");
        }
        
        // Get ocean grid generator if not assigned
        if (oceanGridGenerator == null)
        {
            oceanGridGenerator = GameObject.Find("OceanGridGenerator").GetComponent<OceanGridGenerator>();
            if (oceanGridGenerator == null)
            {
                Debug.LogError("OceanGridGenerator not found! Debris spawning may not work correctly.");
            }
        }
        
        // Get wave manager
        if (oceanGridGenerator != null)
        {
            waveManager = oceanGridGenerator.getGerstnerWaveManager();
        }
    }
    
    void Update()
    {
        // Spawn new debris if conditions are met
        if (Time.time >= nextSpawnTime && activeDebris.Count < maxDebrisCount)
        {
            SpawnDebris();
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        // Clean up distant debris
        CleanupDistantDebris();
    }
    
    void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;
        
        for (int i = 0; i < debrisPerSpawn && activeDebris.Count < maxDebrisCount; i++)
        {
            Vector3 spawnPosition = GetValidSpawnPosition();
            if (spawnPosition != Vector3.zero)
            {
                // Select random debris prefab
                GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                
                // Spawn debris with random rotation
                GameObject debris = Instantiate(prefab, spawnPosition, Random.rotation);
                
                // Setup debris components
                SetupDebrisComponents(debris);
                
                // Add to active list
                activeDebris.Add(debris);
            }
        }
    }
    
    void SetupDebrisComponents(GameObject debris)
    {
        // Ensure debris has Rigidbody
        Rigidbody rb = debris.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = debris.AddComponent<Rigidbody>();
        }
        
        
        // Add some initial random velocity for more dynamic spawning
        if (initialVelocityRange > 0f)
        {
            Vector3 randomVelocity = new Vector3(
                Random.Range(-initialVelocityRange, initialVelocityRange),
                Random.Range(0f, initialVelocityRange * 0.5f),
                Random.Range(-initialVelocityRange, initialVelocityRange)
            );
            rb.velocity = randomVelocity;
        }
    }
    
    Vector3 GetValidSpawnPosition()
    {
        int attempts = 0;
        int maxAttempts = 20;
        
        while (attempts < maxAttempts)
        {
            // Generate random position around ship
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, spawnRadius);
            Vector3 spawnPos = ship.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Get ocean surface height at this position
            float oceanHeight = GetOceanSurfaceHeight(spawnPos);
            spawnPos.y = oceanHeight + spawnHeightOffset;
            
            // Check if position is valid (not too close to other debris)
            if (IsValidSpawnPosition(spawnPos))
            {
                return spawnPos;
            }
            
            attempts++;
        }
        
        return Vector3.zero; // Failed to find valid position
    }
    
    float GetOceanSurfaceHeight(Vector3 position)
    {
        // Use the wave manager to get accurate water surface height
        if (waveManager != null)
        {
            Vector3 waveData = waveManager.CalculateGerstnerWave(position.x, position.z, Time.time);
            return waveData.y;
        }
        
        // Fallback: assume ocean is at ship's y level
        return ship.position.y;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        // Check minimum distance from other debris
        foreach (GameObject debris in activeDebris)
        {
            if (debris != null && Vector3.Distance(position, debris.transform.position) < 5f)
            {
                return false;
            }
        }
        return true;
    }
    
    void CleanupDistantDebris()
    {
        for (int i = activeDebris.Count - 1; i >= 0; i--)
        {
            if (activeDebris[i] == null)
            {
                activeDebris.RemoveAt(i);
            }
            else
            {
                float distance = Vector3.Distance(ship.position, activeDebris[i].transform.position);
                if (distance > despawnDistance)
                {
                    Destroy(activeDebris[i]);
                    activeDebris.RemoveAt(i);
                }
            }
        }
    }
    
    // Public method to manually spawn debris at a specific location
    public void SpawnDebrisAt(Vector3 position)
    {
        if (debrisPrefabs != null && debrisPrefabs.Length > 0 && activeDebris.Count < maxDebrisCount)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            GameObject debris = Instantiate(prefab, position, Random.rotation);
            SetupDebrisComponents(debris);
            activeDebris.Add(debris);
        }
    }
    
    // Public method to clear all debris
    public void ClearAllDebris()
    {
        foreach (GameObject debris in activeDebris)
        {
            if (debris != null)
            {
                Destroy(debris);
            }
        }
        activeDebris.Clear();
    }
    
    // Gizmos for visualization
    void OnDrawGizmosSelected()
    {
        if (ship == null) return;
        
        // Draw spawn radius
        Gizmos.color = Color.yellow;
        DrawWireCircle(ship.position, spawnRadius);
        
        // Draw minimum spawn distance
        Gizmos.color = Color.red;
        DrawWireCircle(ship.position, minSpawnDistance);
        
        // Draw despawn distance
        Gizmos.color = Color.gray;
        DrawWireCircle(ship.position, despawnDistance);
    }
    
    // Helper method to draw wire circle with Gizmos
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}