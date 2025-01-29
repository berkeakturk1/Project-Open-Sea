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
    private List<GameObject> instantiatedMeshes = new List<GameObject>();
    private WFC3D_Model wfcModel;
    private Dictionary<string, Prototype> prototypes;
    
    public bool seeUpdates = true; // Visualize intermediate steps
    public Vector3Int size = new Vector3Int(8, 3, 8); // Grid size
    private Stopwatch stopwatch;
    
    // Material assignment ruleset
    private Dictionary<string, string[]> materialRules = new Dictionary<string, string[]>
    {
        { "wfc_module_0.001", new[] { "Grass" } },
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
        { "wfc_module_27", new[] { "Rock", "Grass", "Roof" } },
        { "wfc_module_28", new[] { "Rock", "Grass", "Beach" } },
        { "wfc_module_29", new[] { "Rock", "Beach" } },
        { "wfc_module_30", new[] { "Rock", "Grass", "Beach" } },
        { "wfc_module_31", new[] { "Rock", "Grass" } },
        { "wfc_module_32", new[] { "Rock", "Grass" } },
        { "wfc_module_33", new[] { "Grass", "Rock" } },
        { "wfc_module_34", new[] { "Grass", "Rock" } },
        { "wfc_module_35", new[] { "Rock", "Grass" } },
        { "wfc_module_36", new[] { "Rock", "Grass", "Beach" } },
        { "wfc_module_37", new[] { "Beach", "Rock" } },
        { "wfc_module_38", new[] { "Grass", "Rock" } }
    };

    void Start()
    {
       Test(); // Start with initial test run
    }

    /*void Update()
    {
         Check for user input to restart with a new seed
        if (Input.GetKeyDown(KeyCode.Return)) // "Enter" key to restart
        {
            seed++;
            Test(); // Restart with new seed
        }
    }*/

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
                    GameObject newMesh = Instantiate(temporaryMeshPrefab);
                    newMesh.name = meshName;
                    newMesh.transform.position = new Vector3(x * spacing, y * spacing, z * spacing);
                    newMesh.transform.rotation = Quaternion.Euler(-90, meshRotation * 90, 0);

                    // Load and assign the mesh
                    Mesh mesh = LoadMesh(meshName);
                    if (mesh != null)
                    {
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
        TextAsset prototypeData = Resources.Load<TextAsset>("prototype_data");

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

        // Ensure the text is not mirrored by flipping its scale along the Z-axis
      //  textObj.transform.localScale = new Vector3(1, 1, 1); // Flip to face outward correctly

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

    
}



/*
    private void InstantiatePrototypesGrid()
    {
        Debug.Log("Instantiating prototypes in a grid...");

        int index = 0;
        int rowLength = Mathf.CeilToInt(Mathf.Sqrt(prototypes.Count));

        foreach (var kvp in prototypes)
        {
            string key = kvp.Key;
            Prototype prototype = kvp.Value;

            int x = index % rowLength;
            int y = index / rowLength;
            index++;

            GameObject newMesh = Instantiate(temporaryMeshPrefab);
            newMesh.transform.position = new Vector3(x * spacing, 0, y * spacing);
            newMesh.name = prototype.mesh_name;

            if (prototype.mesh_name.Equals("-1"))
            {
                EncapsulateWithCube(newMesh, prototype);

                newMesh.transform.rotation = Quaternion.Euler(-90, prototype.mesh_rotation * 90, 0);

                continue;
            }

            Mesh mesh = LoadMesh(prototype.mesh_name);
            if (mesh != null)
            {
                newMesh.GetComponent<MeshFilter>().mesh = mesh;
                EncapsulateWithCube(newMesh, prototype); // Call method to encapsulate with cube

            }

            else
            {
                Debug.LogError($"Mesh not found for: {prototype.mesh_name}");
            }

            Material[] materials = LoadAndSortMaterialsForMesh(prototype.mesh_name);
            if (materials.Length > 0)
            {
                newMesh.GetComponent<MeshRenderer>().materials = materials;
            }
            else
            {
                Debug.LogError($"Materials not found for: {prototype.mesh_name}");
            }

            newMesh.transform.rotation = Quaternion.Euler(-90, prototype.mesh_rotation * 90, 0);

            Debug.Log($"Instantiated {prototype.mesh_name} with materials at ({x}, {y})");
        }
    }
    */