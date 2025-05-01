using System.Collections.Generic;
using UnityEngine;

public class TreePoolManager : MonoBehaviour
{
    // Replace single prefab with array and dictionary for pooling
    [Tooltip("Default tree prefab if none assigned by MapGenerator")]
    public GameObject defaultTreePrefab;
    
    [Tooltip("Initial pool size per prefab type")]
    public int initialPoolSize = 200;
    
    [Tooltip("Maximum number of trees to keep in the pool per prefab type")]
    public int maxPoolSize = 1000;
    
    // Dictionary to store pools for different prefab types
    private Dictionary<int, Queue<GameObject>> treePools = new Dictionary<int, Queue<GameObject>>();
    
    // Array of available tree prefabs
    private GameObject[] treePrefabs;
    
    // Container for inactive trees
    private Transform poolContainer;
    
    // Singleton instance
    private static TreePoolManager _instance;
    
    public static TreePoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance
                _instance = FindObjectOfType<TreePoolManager>();
                
                // If no instance exists, create one
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TreePoolManager");
                    _instance = obj.AddComponent<TreePoolManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create container for inactive trees
        poolContainer = new GameObject("TreePoolContainer").transform;
        poolContainer.SetParent(transform);
        poolContainer.gameObject.SetActive(false);
    }
    
    // New method to setup tree prefabs from MapGenerator
    public void SetupTreePrefabs(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            if (defaultTreePrefab != null)
            {
                // Fall back to default prefab
                treePrefabs = new GameObject[] { defaultTreePrefab };
                Debug.LogWarning("Using default tree prefab as no prefabs provided.");
            }
            else
            {
                Debug.LogError("No tree prefabs assigned and no default prefab available!");
                return;
            }
        }
        else
        {
            treePrefabs = prefabs;
        }
        
        // Initialize pools for each prefab type
        foreach (GameObject prefab in treePrefabs)
        {
            if (prefab == null) continue;
            
            int prefabID = prefab.GetInstanceID();
            if (!treePools.ContainsKey(prefabID))
            {
                treePools[prefabID] = new Queue<GameObject>();
                InitializePool(prefab, prefabID);
            }
        }
        
        Debug.Log($"Tree pool setup complete with {treePrefabs.Length} prefab types");
    }
    
    private void InitializePool(GameObject prefab, int prefabID)
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateTreeForPool(prefab, prefabID);
        }
        
        Debug.Log($"Pool initialized with {initialPoolSize} instances of {prefab.name}");
    }
    
    private GameObject CreateTreeForPool(GameObject prefab, int prefabID)
    {
        GameObject tree = Instantiate(prefab, poolContainer);
        tree.SetActive(false);
        treePools[prefabID].Enqueue(tree);
        return tree;
    }
    
    /// <summary>
    /// Get a tree from the pool, specifying which prefab to use
    /// </summary>
    public GameObject GetTree(Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, GameObject prefab)
    {
        if (prefab == null)
        {
            if (treePrefabs != null && treePrefabs.Length > 0)
            {
                // Pick a random prefab if none specified
                prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            }
            else if (defaultTreePrefab != null)
            {
                prefab = defaultTreePrefab;
            }
            else
            {
                Debug.LogError("No tree prefab available!");
                return null;
            }
        }
        
        int prefabID = prefab.GetInstanceID();
        
        // Make sure we have a pool for this prefab
        if (!treePools.ContainsKey(prefabID))
        {
            treePools[prefabID] = new Queue<GameObject>();
            InitializePool(prefab, prefabID);
        }
        
        GameObject tree;
        
        if (treePools[prefabID].Count > 0)
        {
            // Get tree from pool
            tree = treePools[prefabID].Dequeue();
        }
        else
        {
            // Pool is empty, create a new tree
            tree = Instantiate(prefab);
        }
        
        // Set tree properties
        tree.transform.SetParent(parent);
        tree.transform.position = position;
        tree.transform.rotation = rotation;
        tree.transform.localScale = scale;
        tree.SetActive(true);
        
        return tree;
    }
    
    /// <summary>
    /// Return a tree to the pool
    /// </summary>
    public void ReturnTree(GameObject tree)
    {
        // Don't pool null trees
        if (tree == null) return;
        
        // Find which prefab this tree is an instance of
        GameObject matchingPrefab = null;
        int matchingPrefabID = 0;
        
        if (treePrefabs != null)
        {
            foreach (GameObject prefab in treePrefabs)
            {
                if (tree.name.StartsWith(prefab.name))
                {
                    matchingPrefab = prefab;
                    matchingPrefabID = prefab.GetInstanceID();
                    break;
                }
            }
        }
        
        // If we couldn't determine the prefab, use the first pool or destroy
        if (matchingPrefab == null)
        {
            // Just destroy it if we can't determine which pool it belongs to
            Destroy(tree);
            return;
        }
        
        // Check if we're already at max pool size
        if (treePools[matchingPrefabID].Count >= maxPoolSize)
        {
            // Destroy instead of pooling
            Destroy(tree);
            return;
        }
        
        // Reset and prepare for pooling
        tree.SetActive(false);
        tree.transform.SetParent(poolContainer);
        
        // Add back to pool
        treePools[matchingPrefabID].Enqueue(tree);
    }
    
    /// <summary>
    /// Return all trees in a container to the pool
    /// </summary>
    public void ReturnTrees(Transform container)
    {
        if (container == null) return;
        
        // Get a copy of the children since we'll be modifying the hierarchy
        int childCount = container.childCount;
        GameObject[] children = new GameObject[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            children[i] = container.GetChild(i).gameObject;
        }
        
        // Return each tree to the pool
        foreach (GameObject child in children)
        {
            ReturnTree(child);
        }
    }
}