using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using SimpleJSON;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Prototype
{
    public string mesh_name;
    public int mesh_rotation;
    public string posX, negX, posY, negY, posZ, negZ;
    public string constrain_to, constrain_from;
    public int weight;
    public List<List<string>> valid_neighbours;
}

public class WFCController : MonoBehaviour
{
    [SerializeField] private GameObject temporaryMeshPrefab; // Assign in Inspector
    [SerializeField] private float spacing = 0f; // Grid spacing
    public int seed = 9;
    public List<GameObject> instantiatedMeshes = new List<GameObject>();
    public WFC3D_Model wfcModel;
    private Dictionary<string, Prototype> prototypes;
    
    public bool seeUpdates = true; // Visualize intermediate steps
    public Vector3Int size = new Vector3Int(8, 3, 8); // Grid size
    private Stopwatch stopwatch;
    
    public bool IsFullyInitialized { get; private set; } = false;
    
    [Header("Chest Spawning")]
    [SerializeField] public GameObject chestPrefab; // Assign chest prefab in Inspector
    [SerializeField] private float chestSpawnHeight = 0.5f; // Height above surface to spawn chest
    [SerializeField] private float minChestDistance = 2f; // Minimum distance between chests
    [SerializeField] private int maxChests = 10; // Maximum number of chests to spawn
    [SerializeField] private LayerMask raycastLayerMask = -1; // What layers to raycast against
    [SerializeField] private bool useEfficientSpawning = true; // Toggle between efficient and raycast methods
    
    public List<GameObject> spawnedChests = new List<GameObject>();
    private List<Vector3> cachedValidSurfaces = new List<Vector3>(); // For cached method
    
    [Header("Item Assignment")]
    [SerializeField] private int itemsPerChest = 4; // Number of items to assign to each chest
    [SerializeField] private bool allowDuplicateItems = true; // Allow same item multiple times in one chest
    
    // List of all possible items that can be assigned to chests
    private List<string> availableItems = new List<string>
    {
        "Potion",
        "Apple",
        "Diamond",
        "Axe",
        "Meat",
        "Stone",
        "Logs"
    };

    // Material assignment ruleset
    private Dictionary<string, string[]> materialRules = new Dictionary<string, string[]>
    {
        { "wfc_module_0.001", new[] { "Grass" } },
        { "wfc_module_0", new[] { "Grass" } },
        { "wfc_module_1", new[] { "Rock", "Grass" } },
        {  "wfc_module_2", new[] { "Grass" } },
        { "wfc_module_3", new[] { "Beach", "Grass" } },
        { "wfc_module_4", new[] { "Grass", "Rock" } },
        { "wfc_module_5", new[] { "Grass", "Rock" } },
        { "wfc_module_6", new[] { "Rock", "Grass" } },
        { "wfc_module_7", new[] { "Rock", "Grass" } },
        { "wfc_module_8", new[] { "Grass", "Rock" } },
        { "wfc_module_9", new[] { "Grass", "Beach" } },
        { "wfc_module_10", new[] { "Beach", "Grass" } },
        { "wfc_module_11", new[] { "Rock", "Grass" } },
        { "wfc_module_12", new[] { "Grass", "Rock" } },
        { "wfc_module_13", new[] { "Grass", "Rock" } },
        { "wfc_module_14", new[] { "Rock", "Grass" } },
        { "wfc_module_15", new[] { "Grass", "Rock" } },
        { "wfc_module_16", new[] { "Grass", "Rock" } },
        { "wfc_module_17", new[] { "Grass" } },
        { "wfc_module_18", new[] { "Grass", "Rock" } },
        { "wfc_module_19", new[] { "Grass" } },
        { "wfc_module_20", new[] { "Rock", "Grass" } },
        { "wfc_module_21", new[] { "Grass", "Rock" } },
        { "wfc_module_22", new[] { "Beach", "Grass", "Rock" } },
        { "wfc_module_23", new[] { "Grass", "Rock" } },
        { "wfc_module_24", new[] { "Rock", "Grass" } },
        { "wfc_module_25", new[] { "Rock" } },
        { "wfc_module_26", new[] { "Rock" } },
        { "wfc_module_27", new[] { "Rock", "Grass", "Beach"} },
        { "wfc_module_28", new[] { "Rock", "Beach" } },
        { "wfc_module_29", new[] { "Rock", "Beach" } },
        { "wfc_module_30", new[] { "Rock", "Grass"} },
        { "wfc_module_31", new[] { "Rock", "Grass" } },
        { "wfc_module_32", new[] { "Grass", "Rock" } },
        { "wfc_module_33", new[] { "Grass", "Rock" } },
        { "wfc_module_34", new[] { "Rock", "Grass" } },
        { "wfc_module_35", new[] { "Rock", "Grass" } },
        { "wfc_module_36", new[] { "Beach", "Rock"} },
        { "wfc_module_37", new[] { "Beach", "Rock" } },
        { "wfc_module_38", new[] { "Grass", "Rock" } }
    };

    void Start()
    {
       //TestWithChests(); // Start with initial test run
    }
    
    
    public void setSize(Vector3Int size)
    {
        this.size = size;
    }
    
    public void Test()
    {
        ClearMeshes(); // Clear old meshes
        Random.InitState(seed); // Set random seed
        
        stopwatch = Stopwatch.StartNew();
        
        var prototypes = LoadPrototypeData();
        wfcModel = new WFC3D_Model();
        wfcModel.Initialize(size, prototypes);

        ApplyCustomConstraints(); // Apply constraints before collapsing

        if (seeUpdates)
        {
            StartCoroutine(IterativeCollapse());
        }
        else
        {
            RegenNoUpdate();
            VisualizeWaveFunction();
            LogBenchmark();
        }
    }

    private IEnumerator IterativeCollapse()
    {
        while (!wfcModel.IsCollapsed())
        {
            // Perform one iteration
            wfcModel.Iterate();

            // Clear previous meshes and visualize the current state
            ClearMeshes();
            VisualizeWaveFunction(onlyCollapsed: false); // Visualize every step

            // Wait for the next frame
            yield return null;
        }

        wfcModel.collapseCounter = 0;
        // Final visualization after collapse
        ClearMeshes();
        VisualizeWaveFunction(); // Final visualization
        LogBenchmark();
    }

    public void RegenNoUpdate()
    {
        while (wfcModel.collapseCounter != 192)
        {
            wfcModel.Iterate();
        }
    }
    
    public void ClearMeshes()
    {
        // Iterate through the list of instantiated meshes and destroy them
        foreach (var mesh in instantiatedMeshes)
        {
            if (mesh != null)
            {
                Destroy(mesh); // Destroy the GameObject
            }
        }

        // Clear the list to avoid keeping references to destroyed objects
        instantiatedMeshes.Clear();
        cachedValidSurfaces.Clear(); // Clear cached surfaces when meshes are cleared
    }

    public void VisualizeWaveFunction(bool onlyCollapsed = true)
    {
        ClearMeshes(); // Clear previously instantiated meshes

        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var possibilities = wfcModel.GetPossibilities(new Vector3Int(x, y, z));

                    // Skip non-collapsed tiles if `onlyCollapsed` is true
                    if (onlyCollapsed && possibilities.Count > 1)
                        continue;

                    // If the tile is not collapsed, optionally display an empty tile placeholder (or skip rendering)
                    if (possibilities.Count > 1)
                    {
                        Debug.Log($"Tile at ({x}, {y}, {z}) is not collapsed.");
                        continue; // Skip non-collapsed tiles
                    }

                    // Render the single collapsed prototype
                    foreach (var kvp in possibilities)
                    {
                        Prototype prototype = kvp.Value;
                        string meshName = prototype.mesh_name;
                        int meshRotation = prototype.mesh_rotation;

                        // Skip if the mesh is "-1" (invalid or empty)
                        if (meshName == "-1")
                            continue;

                        // Instantiate and configure the tile mesh
                        GameObject newMesh = Instantiate(temporaryMeshPrefab, transform);
                        newMesh.name = $"{meshName}_({x},{y},{z})";
                        newMesh.transform.localPosition = new Vector3(x * spacing, y * spacing, z * spacing);
                        newMesh.transform.localRotation = Quaternion.Euler(-90, meshRotation * 90, 0);
                        
                        // Load and assign the mesh
                        Mesh mesh = LoadMesh(meshName);
                        
                        if (mesh != null)
                        {
                            newMesh.GetComponent<MeshCollider>().sharedMesh = mesh;
                            newMesh.GetComponent<MeshFilter>().mesh = mesh;
                        }
                        else
                        {
                            Debug.LogError($"Mesh not found for: {meshName}");
                        }

                        // Load and apply materials
                        Material[] materials = LoadAndSortMaterialsForMesh(meshName);
                        if (materials.Length > 0)
                        {
                            newMesh.GetComponent<MeshRenderer>().materials = materials;
                        }
                        else
                        {
                            Debug.LogError($"Materials not found for: {meshName}");
                        }

                        // Store the instantiated object for future clearing
                        instantiatedMeshes.Add(newMesh);
                        
                        // Cache valid surfaces during generation for efficient spawning
                        CacheSurfaceIfValid(newMesh, x, y, z);
                    }
                }
            }
        }
    }

    private void LogBenchmark()
    {
        stopwatch.Stop();
        Debug.Log($"WFC completed in {stopwatch.ElapsedMilliseconds} ms");
    }

    private Mesh LoadMesh(string meshName)
    {
        return Resources.Load<Mesh>($"Meshes/{meshName}");
    }

    private Material[] LoadAndSortMaterialsForMesh(string meshName)
    {
        if (!materialRules.ContainsKey(meshName))
        {
            Debug.LogError($"No material rule found for: {meshName}");
            return Array.Empty<Material>();
        }

        List<Material> materials = new List<Material>();
        foreach (string materialName in materialRules[meshName])
        {
            Material mat = Resources.Load<Material>($"Materials/{materialName}");
            if (mat != null)
            {
                materials.Add(mat);
            }
            else
            {
                Debug.LogError($"Failed to load material: {materialName}");
            }
        }

        return materials.ToArray();
    }

    public static Dictionary<string, Prototype> LoadPrototypeData()
    {
        Debug.Log("Attempting to load 'prototype_data.json'...");
        TextAsset prototypeData = Resources.Load<TextAsset>("new_prototype_data");

        if (prototypeData == null)
        {
            Debug.LogError("Failed to load 'prototype_data.json' from Resources folder.");
            return new Dictionary<string, Prototype>();
        }

        JSONNode json = JSON.Parse(prototypeData.text);
        var prototypesDict = new Dictionary<string, Prototype>();

        foreach (var kvp in json)
        {
            string key = kvp.Key;
            JSONNode prototypeNode = kvp.Value;

            Prototype prototype = new Prototype
            {
                mesh_name = prototypeNode["mesh_name"],
                mesh_rotation = prototypeNode["mesh_rotation"].AsInt,
                posX = prototypeNode["posX"],
                negX = prototypeNode["negX"],
                posY = prototypeNode["posY"],
                negY = prototypeNode["negY"],
                posZ = prototypeNode["posZ"],
                negZ = prototypeNode["negZ"],
                constrain_to = prototypeNode["constrain_to"],
                constrain_from = prototypeNode["constrain_from"],
                weight = prototypeNode["weight"].AsInt,
                valid_neighbours = ParseValidNeighbours(prototypeNode["valid_neighbours"])
            };

            prototypesDict.Add(key, prototype);
        }

        Debug.Log($"Loaded {prototypesDict.Count} prototypes.");
        return prototypesDict;
    }

    private static List<List<string>> ParseValidNeighbours(JSONNode neighboursNode)
    {
        var neighboursList = new List<List<string>>();
        foreach (JSONNode group in neighboursNode)
        {
            var innerList = new List<string>();
            foreach (JSONNode neighbour in group)
            {
                innerList.Add(neighbour.Value);
            }
            neighboursList.Add(innerList);
        }
        return neighboursList;
    }

    private void EncapsulateWithCube(GameObject meshObject, Prototype prototype)
    {
        Vector3 center = meshObject.transform.position;
        Vector3 halfSize = new Vector3(1, 1, 1) * 0.25f * spacing; // Fixed cube size

        // Draw the cube edges using LineRenderers
        Vector3[] corners = GetCubeCorners(center, halfSize);
        for (int i = 0; i < 4; i++)
        {
            DrawLine(corners[i], corners[(i + 1) % 4]);        // Bottom face
            DrawLine(corners[i + 4], corners[(i + 1) % 4 + 4]); // Top face
            DrawLine(corners[i], corners[i + 4]);               // Vertical edges
        }

        // Add text to each face with the correct axis conversions
        AddFaceText(center + Vector3.right * halfSize.x, $" {prototype.posX}", new Vector3(0, -90, 0));   // Right face
        AddFaceText(center + Vector3.left * halfSize.x, $" {prototype.negX}", new Vector3(0, 90, 0));  // Left face
        AddFaceText(center + Vector3.up * halfSize.y, $" {prototype.posZ}", new Vector3(90, 0, 0));      // Top face
        AddFaceText(center + Vector3.down * halfSize.y, $" {prototype.negZ}", new Vector3(90, 0, 0));  // Bottom face
        AddFaceText(center + Vector3.forward * halfSize.z, $" {prototype.posY}",new Vector3(0, 180, 0));         // Front face
        AddFaceText(center + Vector3.back * halfSize.z, $" {prototype.negY}",  Vector3.zero);  // Back face
    }

    private void AddFaceText(Vector3 position, string textContent, Vector3 rotation)
    {
        // Create a new GameObject for the text
        GameObject textObj = new GameObject("FaceText");
        textObj.transform.position = position;
        textObj.transform.rotation = Quaternion.Euler(rotation); // Rotate the text to face outward

        // Add TextMeshPro component
        TMPro.TextMeshPro textMeshPro = textObj.AddComponent<TMPro.TextMeshPro>();
        textMeshPro.text = textContent;

        // Set text alignment and font size
        textMeshPro.fontSize = 1.0f; // Manually control font size
        textMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
        textMeshPro.color = Color.white;

        // Set the size of the text container and add margins for better spacing
        textMeshPro.rectTransform.sizeDelta = new Vector2(2, 2);
        textMeshPro.margin = new Vector4(0.2f, 0.2f, 0.2f, 0.2f);
    }

    private Vector3[] GetCubeCorners(Vector3 center, Vector3 halfSize)
    {
        return new Vector3[]
        {
            center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
            center + new Vector3(-halfSize.x, halfSize.y, halfSize.z)
        };
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("EdgeLine");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.025f;
        line.endWidth = 0.025f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.red;
        line.endColor = Color.red;
    }

    public void ApplyCustomConstraints()
    {
        // Iterate over the entire wave function grid
        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector3Int coords = new Vector3Int(x, y, z);
                    var possibilities = wfcModel.GetPossibilities(coords);

                    // Constrain the top layer (y == size.y - 1)
                    if (y == size.y - 1)
                    {
                        RemoveInvalidPrototypes(possibilities, 4, "p-1"); // Check posZ neighbors
                    }

                    // Constrain non-bottom layers (y > 0)
                    if (y > 0)
                    {
                        RemoveByConstraint(possibilities, "bot");
                    }

                    // Constrain non-top layers (y < size.y - 1)
                    if (y < size.y - 1)
                    {
                        RemoveByConstraint(possibilities, "top");
                    }

                    // Constrain +X edge (x == size.x - 1)
                    if (x == size.x - 1)
                    {
                        RemoveInvalidPrototypes(possibilities, 0, "p-1"); // Check posX neighbors
                    }

                    // Constrain -X edge (x == 0)
                    if (x == 0)
                    {
                        RemoveInvalidPrototypes(possibilities, 2, "p-1"); // Check negX neighbors
                    }

                    // Constrain +Z edge (z == size.z - 1)
                    if (z == size.z - 1)
                    {
                        RemoveInvalidPrototypes(possibilities, 1, "p-1"); // Check posY (Godot Z+)
                    }

                    // Constrain -Z edge (z == 0)
                    if (z == 0)
                    {
                        RemoveInvalidPrototypes(possibilities, 3, "p-1"); // Check negY (Godot Z-)
                    }
                }
            }
        }
    }

    // Helper function to remove prototypes based on neighbor validity
    private void RemoveInvalidPrototypes(Dictionary<string, Prototype> possibilities, int direction, string requiredNeighbor)
    {
        var keysToRemove = new List<string>();

        foreach (var prototypeName in possibilities.Keys)
        {
            var neighbors = possibilities[prototypeName].valid_neighbours[direction];
            if (!neighbors.Contains(requiredNeighbor))
            {
                keysToRemove.Add(prototypeName);
            }
        }

        // Avoid full elimination; ensure at least one prototype remains.
        if (keysToRemove.Count < possibilities.Count)
        {
            foreach (var key in keysToRemove)
            {
                possibilities.Remove(key);
            }
        }
    }

    // Helper function to remove prototypes based on custom constraints
    private void RemoveByConstraint(Dictionary<string, Prototype> possibilities, string constraint)
    {
        foreach (var prototypeName in new List<string>(possibilities.Keys))
        {
            string prototypeConstraint = possibilities[prototypeName].constrain_to;
            if (prototypeConstraint == constraint)
            {
                possibilities.Remove(prototypeName);
            }
        }
    }

    public int GetSeed()
    {
        return seed;
    }

    public void SetSeed(int newSeed)
    {
        seed = newSeed;
    }

    #region EFFICIENT CHEST SPAWNING METHODS

    /// <summary>
    /// Main chest spawning method that chooses between efficient and raycast methods
    /// </summary>
    public void SpawnChests()
    {
        SpawnChestsGridBased();
    }

    /// <summary>
    /// METHOD 1: Grid-based analysis (Most Efficient)
    /// Only checks positions where we know meshes exist
    /// </summary>
    public void SpawnChestsGridBased()
    {
        ClearChests();
        List<Vector3> validPositions = new List<Vector3>();

        // Only check grid positions where meshes were actually instantiated
        for (int z = 0; z < size.z; z++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    // Find the mesh at this grid position
                    GameObject meshAtPosition = GetMeshAtGridPosition(x, y, z);
                    if (meshAtPosition != null)
                    {
                        // Calculate spawn position on top of this mesh
                        Bounds bounds = meshAtPosition.GetComponent<MeshRenderer>().bounds;
                        Vector3 spawnPos = new Vector3(bounds.center.x, bounds.max.y + chestSpawnHeight, bounds.center.z);

                        // Check if there's clearance above (no mesh in the position above)
                        if (y == size.y - 1 || GetMeshAtGridPosition(x, y + 1, z) == null)
                        {
                            if (IsWithinBounds(spawnPos))
                            {
                                validPositions.Add(spawnPos);
                            }
                        }
                    }
                }
            }
        }

        SpawnChestsFromPositions(validPositions, "Grid-Based Analysis");
    }

    /// <summary>
    /// METHOD 2: Direct mesh analysis
    /// Analyzes existing instantiated meshes directly
    /// </summary>
    public void SpawnChestsDirectAnalysis()
    {
        ClearChests();
        List<Vector3> validPositions = new List<Vector3>();

        // Analyze each instantiated mesh for spawn positions
        foreach (GameObject meshObj in instantiatedMeshes)
        {
            if (meshObj == null) continue;

            // Get mesh bounds and check if it's a valid surface
            MeshRenderer renderer = meshObj.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            Bounds bounds = renderer.bounds;
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            Vector3 spawnPos = topCenter + Vector3.up * chestSpawnHeight;

            // Quick validation without expensive physics checks
            if (IsWithinBounds(spawnPos) && HasClearanceAbove(spawnPos, meshObj))
            {
                validPositions.Add(spawnPos);
            }
        }

        SpawnChestsFromPositions(validPositions, "Direct Mesh Analysis");
    }

    /// <summary>
    /// METHOD 3: Cached surfaces (Fastest after first generation)
    /// Uses pre-calculated valid surfaces
    /// </summary>
    public void SpawnChestsFromCache()
    {
        ClearChests();
        SpawnChestsFromPositions(new List<Vector3>(cachedValidSurfaces), "Cached Surfaces");
    }

    /// <summary>
    /// ORIGINAL METHOD: Raycast-based (Slower but comprehensive)
    /// </summary>
    public void SpawnChestsRaycast()
    {
        ClearChests();
        List<Vector3> validPositions = FindValidChestPositionsRaycast();
        SpawnChestsFromPositions(validPositions, "Raycast Analysis");
    }

    #endregion

    #region HELPER METHODS FOR EFFICIENT SPAWNING

    private GameObject GetMeshAtGridPosition(int x, int y, int z)
    {
        // Find mesh by name pattern (since you name them with coordinates)
        string expectedName = $"_({x},{y},{z})";
        return instantiatedMeshes.Find(mesh => mesh != null && mesh.name.EndsWith(expectedName));
    }

    private bool IsWithinBounds(Vector3 position)
    {
        Vector3 localPos = transform.InverseTransformPoint(position);
        return localPos.x >= -1 && localPos.x <= size.x * spacing + 1 &&
               localPos.z >= -1 && localPos.z <= size.z * spacing + 1;
    }

    private bool HasClearanceAbove(Vector3 spawnPos, GameObject currentMesh)
    {
        // Check if there's another mesh directly above this position
        Collider[] overlapping = Physics.OverlapSphere(spawnPos + Vector3.up * 1f, 0.5f, raycastLayerMask);
        return overlapping.Length <= 1; // Only the current mesh should be detected
    }

    private void CacheSurfaceIfValid(GameObject newMesh, int x, int y, int z)
    {
        // Only cache if this is the top mesh or has clearance above
        if (y == size.y - 1 || GetMeshAtGridPosition(x, y + 1, z) == null)
        {
            Bounds bounds = newMesh.GetComponent<MeshRenderer>().bounds;
            Vector3 surfacePos = new Vector3(bounds.center.x, bounds.max.y + chestSpawnHeight, bounds.center.z);
            cachedValidSurfaces.Add(surfacePos);
        }
    }

    private void SpawnChestsFromPositions(List<Vector3> validPositions, string methodName)
    {
        if (validPositions.Count == 0)
        {
            Debug.LogWarning($"No valid chest spawn positions found using {methodName}!");
            return;
        }

        // Filter by distance and shuffle
        List<Vector3> filteredPositions = FilterPositionsByDistance(validPositions);

        // Shuffle for randomness
        for (int i = 0; i < filteredPositions.Count; i++)
        {
            Vector3 temp = filteredPositions[i];
            int randomIndex = Random.Range(i, filteredPositions.Count);
            filteredPositions[i] = filteredPositions[randomIndex];
            filteredPositions[randomIndex] = temp;
        }

        // Spawn chests
        int chestsToSpawn = Mathf.Min(maxChests, filteredPositions.Count);
        for (int i = 0; i < chestsToSpawn; i++)
        {
            SpawnChestAtPosition(filteredPositions[i]);
        }

        Debug.Log($"Efficiently spawned {chestsToSpawn} chests out of {filteredPositions.Count} valid positions using {methodName}");
    }

    #endregion

    #region ORIGINAL RAYCAST METHODS (FOR COMPARISON)

    private List<Vector3> FindValidChestPositionsRaycast()
    {
        List<Vector3> validPositions = new List<Vector3>();

        // Calculate the bounds of our WFC grid
        Vector3 gridStart = transform.position;
        Vector3 gridEnd = gridStart + new Vector3(
            (size.x - 1) * spacing,
            (size.y - 1) * spacing,
            (size.z - 1) * spacing
        );

        // Define raycast resolution (how many rays per unit)
        float raycastResolution = 0.5f; // One ray every 0.5 units

        // Raycast across the entire grid area
        for (float x = gridStart.x; x <= gridEnd.x + spacing; x += raycastResolution)
        {
            for (float z = gridStart.z; z <= gridEnd.z + spacing; z += raycastResolution)
            {
                Vector3 rayStart = new Vector3(x, gridStart.y + 50f, z); // Increased raycast height
                Vector3 rayDirection = Vector3.down;

                RaycastHit hit;
                if (Physics.Raycast(rayStart, rayDirection, out hit, 50f + size.y * spacing, raycastLayerMask))
                {
                    Vector3 potentialSpawnPos = hit.point + Vector3.up * chestSpawnHeight;

                    // Validate the spawn position
                    if (IsValidChestSpawnPosition(potentialSpawnPos, hit))
                    {
                        validPositions.Add(potentialSpawnPos);
                    }
                }
            }
        }

        // Filter positions to maintain minimum distance between chests
        return FilterPositionsByDistance(validPositions);
    }

    private bool IsValidChestSpawnPosition(Vector3 position, RaycastHit hit)
    {
        // Check if the surface is relatively flat (normal pointing mostly upward)
        if (Vector3.Dot(hit.normal, Vector3.up) < 0.7f)
        {
            return false;
        }

        // Check if there's enough space above the spawn position for the chest
        Vector3 checkStart = position;
        Vector3 checkEnd = position + Vector3.up * 2f; // Assume chest is 2 units tall

        if (Physics.CheckCapsule(checkStart, checkEnd, 0.5f, raycastLayerMask))
        {
            return false; // Something is blocking the space where chest would be
        }

        // Check if the position is within our grid bounds
        Vector3 localPos = transform.InverseTransformPoint(position);
        if (localPos.x < -1 || localPos.x > size.x * spacing + 1 ||
            localPos.z < -1 || localPos.z > size.z * spacing + 1)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region CHEST SPAWNING CORE METHODS

    /// <summary>
    /// Filters positions to maintain minimum distance between spawn points
    /// </summary>
    private List<Vector3> FilterPositionsByDistance(List<Vector3> positions)
    {
        if (positions.Count == 0) return positions;

        List<Vector3> filteredPositions = new List<Vector3>();
        filteredPositions.Add(positions[0]); // Always add the first position

        for (int i = 1; i < positions.Count; i++)
        {
            bool tooClose = false;

            foreach (Vector3 existingPos in filteredPositions)
            {
                if (Vector3.Distance(positions[i], existingPos) < minChestDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                filteredPositions.Add(positions[i]);
            }
        }

        return filteredPositions;
    }

    /// <summary>
    /// Spawns a chest at the specified position and assigns random items to it
    /// </summary>
    private void SpawnChestAtPosition(Vector3 position)
    {
        if (chestPrefab == null)
        {
            Debug.LogError("Chest prefab is not assigned!");
            return;
        }

        GameObject chest = Instantiate(chestPrefab, position, Quaternion.identity, transform);

        // Optionally rotate chest to face a random direction
        chest.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Add some random variation to chest position
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.1f, 0.1f),
            0,
            Random.Range(-0.1f, 0.1f)
        );
        chest.transform.position += randomOffset;

        // Get the StorageBox component and assign random items
        StorageBox storageBox = chest.GetComponent<StorageBox>();
        if (storageBox != null)
        {
            AssignRandomItemsToChest(storageBox);
        }
        else
        {
            Debug.LogWarning($"Chest at {position} does not have a StorageBox component!");
        }

        spawnedChests.Add(chest);

        Debug.Log($"Spawned chest at position: {position} with {itemsPerChest} random items");
    }

    /// <summary>
    /// Assigns random items from the available items list to a chest
    /// </summary>
    private void AssignRandomItemsToChest(StorageBox storageBox)
    {
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("No available items to assign to chests!");
            return;
        }

        // Clear any existing items in the chest
        storageBox.items.Clear();

        List<string> itemsToAssign = new List<string>();

        if (allowDuplicateItems)
        {
            // Simple random selection with potential duplicates
            for (int i = 0; i < itemsPerChest; i++)
            {
                string randomItem = availableItems[Random.Range(0, availableItems.Count)];
                itemsToAssign.Add(randomItem);
            }
        }
        else
        {
            // Ensure no duplicate items in the same chest
            List<string> availableItemsCopy = new List<string>(availableItems);
            
            for (int i = 0; i < itemsPerChest && availableItemsCopy.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, availableItemsCopy.Count);
                string selectedItem = availableItemsCopy[randomIndex];
                itemsToAssign.Add(selectedItem);
                availableItemsCopy.RemoveAt(randomIndex); // Remove to prevent duplicates
            }
        }

        // Assign items to the chest
        foreach (string item in itemsToAssign)
        {
            storageBox.appendToList(item);
        }

        Debug.Log($"Assigned items to chest: {string.Join(", ", itemsToAssign)}");
    }

    /// <summary>
    /// Public method to add a new item to the available items list
    /// </summary>
    public void AddAvailableItem(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName) && !availableItems.Contains(itemName))
        {
            availableItems.Add(itemName);
            Debug.Log($"Added new item to available items: {itemName}");
        }
    }

    /// <summary>
    /// Public method to remove an item from the available items list
    /// </summary>
    public void RemoveAvailableItem(string itemName)
    {
        if (availableItems.Contains(itemName))
        {
            availableItems.Remove(itemName);
            Debug.Log($"Removed item from available items: {itemName}");
        }
    }

    /// <summary>
    /// Public method to set the number of items per chest
    /// </summary>
    public void SetItemsPerChest(int count)
    {
        itemsPerChest = Mathf.Max(0, count);
        Debug.Log($"Items per chest set to: {itemsPerChest}");
    }

    /// <summary>
    /// Public method to toggle duplicate items setting
    /// </summary>
    public void ToggleAllowDuplicateItems()
    {
        allowDuplicateItems = !allowDuplicateItems;
        Debug.Log($"Allow duplicate items: {allowDuplicateItems}");
    }

    /// <summary>
    /// Public method to reassign items to all existing chests
    /// </summary>
    public void ReassignItemsToAllChests()
    {
        foreach (GameObject chest in spawnedChests)
        {
            if (chest != null)
            {
                StorageBox storageBox = chest.GetComponent<StorageBox>();
                if (storageBox != null)
                {
                    AssignRandomItemsToChest(storageBox);
                }
            }
        }
        Debug.Log($"Reassigned items to {spawnedChests.Count} chests");
    }

    /// <summary>
    /// Clears all spawned chests
    /// </summary>
    public void ClearChests()
    {
        foreach (GameObject chest in spawnedChests)
        {
            if (chest != null)
            {
                DestroyImmediate(chest);
            }
        }
        spawnedChests.Clear();
    }

    #endregion

    #region MAIN TEST METHODS

    /// <summary>
    /// Updated Test method to include chest spawning
    /// </summary>
    public void TestWithChests()
    {
        ClearMeshes(); // Clear old meshes
        ClearChests(); // Clear old chests
        Random.InitState(seed); // Set random seed

        stopwatch = Stopwatch.StartNew();

        var prototypes = LoadPrototypeData();
        wfcModel = new WFC3D_Model();
        wfcModel.Initialize(size, prototypes);

        ApplyCustomConstraints(); // Apply constraints before collapsing

        if (seeUpdates)
        {
            StartCoroutine(IterativeCollapseWithChests());
        }
        else
        {
            RegenNoUpdate();
            VisualizeWaveFunction();
            SpawnChests(); // Spawn chests after generation
            LogBenchmark();
        }
    }

    /// <summary>
    /// Coroutine version that spawns chests after completion
    /// </summary>
    public IEnumerator IterativeCollapseWithChests()
    {
        while (!wfcModel.IsCollapsed())
        {
            wfcModel.Iterate();
            ClearMeshes();
            VisualizeWaveFunction(onlyCollapsed: false);
            yield return null;
        }

        wfcModel.collapseCounter = 0;
        ClearMeshes();
        VisualizeWaveFunction();

        // Wait a frame before spawning chests to ensure all meshes are properly instantiated
        yield return null;
        SpawnChests();

        LogBenchmark();
    }

    #endregion

    #region DEBUG AND VISUALIZATION

    /// <summary>
    /// Debug function to visualize raycast positions in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw valid chest positions as green spheres
        Gizmos.color = Color.green;
        foreach (GameObject chest in spawnedChests)
        {
            if (chest != null)
            {
                Gizmos.DrawWireSphere(chest.transform.position, 0.5f);
            }
        }

        // Draw cached valid surfaces as yellow spheres
        Gizmos.color = Color.yellow;
        foreach (Vector3 surface in cachedValidSurfaces)
        {
            Gizmos.DrawWireSphere(surface, 0.3f);
        }

        // Draw grid bounds
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(
            size.x * spacing * 0.5f,
            size.y * spacing * 0.5f,
            size.z * spacing * 0.5f
        );
        Vector3 gridSize = new Vector3(size.x * spacing, size.y * spacing, size.z * spacing);
        Gizmos.DrawWireCube(center, gridSize);
    }

    #endregion

    #region PUBLIC UTILITY METHODS

    /// <summary>
    /// Public method to switch between spawning methods at runtime
    /// </summary>
    public void ToggleSpawningMethod()
    {
        useEfficientSpawning = !useEfficientSpawning;
        Debug.Log($"Switched to {(useEfficientSpawning ? "Efficient" : "Raycast")} spawning method");
    }

    /// <summary>
    /// Public method to respawn chests with current settings
    /// </summary>
    public void RespawnChests()
    {
        SpawnChests();
    }

    /// <summary>
    /// Get statistics about the current spawning setup
    /// </summary>
    public void LogSpawningStats()
    {
        Debug.Log($"=== CHEST SPAWNING STATS ===");
        Debug.Log($"Method: {(useEfficientSpawning ? "Efficient Grid-Based" : "Raycast-Based")}");
        Debug.Log($"Current Chests: {spawnedChests.Count}");
        Debug.Log($"Max Chests: {maxChests}");
        Debug.Log($"Min Distance: {minChestDistance}");
        Debug.Log($"Cached Surfaces: {cachedValidSurfaces.Count}");
        Debug.Log($"Instantiated Meshes: {instantiatedMeshes.Count}");
    }

    #endregion
}