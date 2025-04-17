using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleJSON;

public class WFCRotationFix : MonoBehaviour
{
    [SerializeField] private string jsonFilePath = "Assets/Resources/prototype_data_new.json";
    [SerializeField] private string outputPath = "Assets/Resources/prototype_data_fixed.json";
    
    // Map Unity rotation to the correct WFC rotation
    private static readonly Dictionary<int, int> RotationMap = new Dictionary<int, int>
    {
        { 0, 0 },  // No rotation
        { 1, 3 },  // 90 degrees in Unity = 270 degrees in WFC
        { 2, 2 },  // 180 degrees stays the same
        { 3, 1 }   // 270 degrees in Unity = 90 degrees in WFC
    };

    [ContextMenu("Fix Rotations")]
    public void FixPrototypeRotations()
    {
        // Read JSON file
        string jsonText;
        try
        {
            jsonText = File.ReadAllText(jsonFilePath);
            Debug.Log($"Successfully read {jsonFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading JSON file: {e.Message}");
            return;
        }

        // Parse JSON
        JSONNode rootNode;
        try
        {
            rootNode = JSON.Parse(jsonText);
            Debug.Log($"Successfully parsed JSON with {rootNode.Count} prototypes");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
            return;
        }

        // Fix rotations
        int fixedCount = 0;
        foreach (var kvp in rootNode)
        {
            string protoKey = kvp.Key;
            JSONNode protoNode = kvp.Value;

            if (protoNode["mesh_rotation"] != null)
            {
                int originalRotation = protoNode["mesh_rotation"].AsInt;
                
                // Skip if it's p-1 (empty prototype)
                if (protoKey == "p-1" || protoNode["mesh_name"] == "-1")
                    continue;
                
                // Log the original rotation
                Debug.Log($"Proto {protoKey}: Original rotation {originalRotation}");
                
                // Apply the rotation mapping
                if (RotationMap.TryGetValue(originalRotation, out int correctedRotation))
                {
                    // Update only if necessary
                    if (originalRotation != correctedRotation)
                    {
                        protoNode["mesh_rotation"] = correctedRotation;
                        fixedCount++;
                        Debug.Log($"Proto {protoKey}: Changed rotation from {originalRotation} to {correctedRotation}");
                    }
                }
            }
        }

        // Write out the corrected JSON
        try
        {
            string outputJson = rootNode.ToString(2); // Pretty-print with 2-space indentation
            File.WriteAllText(outputPath, outputJson);
            Debug.Log($"Successfully fixed {fixedCount} rotations and saved to {outputPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error writing fixed JSON: {e.Message}");
        }
    }

    [ContextMenu("Fix Visualization")]
    public void FixVisualizationMethod()
    {
        Debug.Log("Add this modified VisualizeWaveFunction method to your WFCController class:");
        Debug.Log(@"
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
                    Debug.Log($""Tile at ({x}, {y}, {z}) is not collapsed."");
                    continue; // Skip non-collapsed tiles
                }

                // Render the single collapsed prototype
                foreach (var kvp in possibilities)
                {
                    Prototype prototype = kvp.Value;
                    string meshName = prototype.mesh_name;
                    int meshRotation = prototype.mesh_rotation;

                    // Skip if the mesh is ""-1"" (invalid or empty)
                    if (meshName == ""-1"")
                        continue;

                    // Instantiate and configure the tile mesh
                    GameObject newMesh = Instantiate(temporaryMeshPrefab);
                    newMesh.name = meshName;
                    newMesh.transform.position = new Vector3(x * spacing, y * spacing, z * spacing);
                    
                    // Convert from WFC rotation to Unity rotation
                    // WFC: 0=0°, 1=90°, 2=180°, 3=270°
                    // Unity: 0=0°, 1=270°, 2=180°, 3=90°
                    int unityRotation;
                    switch (meshRotation)
                    {
                        case 0: unityRotation = 0; break;  // 0° stays 0°
                        case 1: unityRotation = 3; break;  // 90° becomes 270°
                        case 2: unityRotation = 2; break;  // 180° stays 180°
                        case 3: unityRotation = 1; break;  // 270° becomes 90°
                        default: unityRotation = 0; break; // Default case
                    }
                    
                    newMesh.transform.rotation = Quaternion.Euler(-90, unityRotation * 90, 0);

                    // Load and assign the mesh
                    Mesh mesh = LoadMesh(meshName);
                    if (mesh != null)
                    {
                        newMesh.GetComponent<MeshFilter>().mesh = mesh;
                    }
                    else
                    {
                        Debug.LogError($""Mesh not found for: {meshName}"");
                    }

                    // Load and apply materials
                    Material[] materials = LoadAndSortMaterialsForMesh(meshName);
                    if (materials.Length > 0)
                    {
                        newMesh.GetComponent<MeshRenderer>().materials = materials;
                    }
                    else
                    {
                        Debug.LogError($""Materials not found for: {meshName}"");
                    }

                    // Store the instantiated object for future clearing
                    instantiatedMeshes.Add(newMesh);
                }
            }
        }
    }
}");
    }
}