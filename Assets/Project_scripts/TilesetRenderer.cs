using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;



public class TilesetRenderer : MonoBehaviour
{
    [SerializeField] private GameObject temporaryMeshPrefab; // Assign in Inspector
    [SerializeField] private float spacing = 2.0f; // Grid spacing

    private Dictionary<string, Prototype> prototypes;

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

    public void Start()
    {
        Debug.Log("Start method called!");

        prototypes = LoadPrototypeData();

        if (prototypes.Count == 0)
        {
            Debug.LogError("No prototypes were loaded.");
            return;
        }

        InstantiatePrototypesGrid();
    }

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


}
