
using UnityEngine;

public class ZombieChunkTracker : MonoBehaviour
{
    public Vector2 chunkPosition;
    public ZombieSpawner spawner;
    
    void OnDestroy()
    {
        if (spawner != null)
        {
            // Note: Don't call RemoveZombie here as it would cause recursion
            // The spawner handles cleanup when chunks are destroyed
        }
    }
}