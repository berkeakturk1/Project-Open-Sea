using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
public static class EditorObjectCreator
{
    /// <summary>
    /// Creates a new GameObject in the editor with the specified properties
    /// </summary>
    /// <param name="name">Name of the object to create</param>
    /// <param name="position">World position for the new object</param>
    /// <param name="parent">Optional parent transform (null for root objects)</param>
    /// <param name="objectPrefab">Optional template object to base this on</param>
    /// <param name="components">Optional list of component types to add</param>
    /// <returns>The created GameObject</returns>
    public static GameObject CreateEditorObject(
        string name,
        Vector3 position,
        Transform parent = null,
        GameObject objectPrefab = null,
        List<System.Type> components = null)
    {
        GameObject newObject;
        
        // If a prefab was provided, use it as a template
        if (objectPrefab != null)
        {
            // Create a new object based on the prefab
            newObject = PrefabUtility.InstantiatePrefab(objectPrefab) as GameObject;
            if (newObject == null)
            {
                // If the provided object wasn't a prefab, duplicate it
                newObject = GameObject.Instantiate(objectPrefab);
                newObject.name = name;
            }
            else
            {
                newObject.name = name;
            }
        }
        else
        {
            // Create a new empty object
            newObject = new GameObject(name);
            
            // Add requested components
            if (components != null)
            {
                foreach (System.Type componentType in components)
                {
                    newObject.AddComponent(componentType);
                }
            }
        }
        
        // Set parent if provided
        if (parent != null)
        {
            newObject.transform.SetParent(parent, false);
        }
        
        // Set position
        newObject.transform.position = position;
        
        // Register creation for undo
        Undo.RegisterCreatedObjectUndo(newObject, "Create " + name);
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(newObject.scene);
        
        return newObject;
    }
    
    /// <summary>
    /// Creates a new GameObject in the editor with the specified mesh and material
    /// </summary>
    /// <param name="name">Name for the new object</param>
    /// <param name="position">World position for the new object</param>
    /// <param name="mesh">Mesh to assign to the object</param>
    /// <param name="material">Material to use with the mesh</param>
    /// <param name="parent">Optional parent transform</param>
    /// <returns>The created GameObject with MeshFilter and MeshRenderer</returns>
    public static GameObject CreateEditorMeshObject(
        string name,
        Vector3 position,
        Mesh mesh,
        Material material,
        Transform parent = null)
    {
        // Create base object with required components
        List<System.Type> components = new List<System.Type>
        {
            typeof(MeshFilter),
            typeof(MeshRenderer)
        };
        
        GameObject newObject = CreateEditorObject(name, position, parent, null, components);
        
        // Assign mesh and material
        newObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        newObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        
        return newObject;
    }
    
    /// <summary>
    /// Creates a grouped set of objects in the editor
    /// </summary>
    /// <param name="parentName">Name for the parent container</param>
    /// <param name="position">World position for the parent</param>
    /// <param name="childPrefabs">Array of prefabs to include as children</param>
    /// <param name="parent">Optional parent transform</param>
    /// <returns>The parent GameObject containing all child objects</returns>
    public static GameObject CreateEditorGroup(
        string parentName, 
        Vector3 position, 
        GameObject[] childPrefabs, 
        Transform parent = null)
    {
        // Create parent container
        GameObject parentObject = CreateEditorObject(parentName, position, parent);
        
        // Add children
        if (childPrefabs != null)
        {
            for (int i = 0; i < childPrefabs.Length; i++)
            {
                if (childPrefabs[i] != null)
                {
                    string childName = childPrefabs[i].name + "_" + i;
                    CreateEditorObject(childName, Vector3.zero, parentObject.transform, childPrefabs[i]);
                }
            }
        }
        
        return parentObject;
    }
    
    /// <summary>
    /// Creates a predefined object based on templates stored in a ScriptableObject
    /// </summary>
    /// <param name="templateName">Name of the template to use</param>
    /// <param name="position">World position for the new object</param>
    /// <param name="parent">Optional parent transform</param>
    /// <returns>The created GameObject</returns>
    public static GameObject CreateFromTemplate(
        string templateName,
        Vector3 position,
        Transform parent = null)
    {
        // This would reference a ScriptableObject database of templates
        // For this example, we'll use a simple lookup approach
        
        // Find all template assets (assumes you have a ScriptableObject for templates)
        string[] guids = AssetDatabase.FindAssets("t:EditorObjectTemplate");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorObjectTemplate template = AssetDatabase.LoadAssetAtPath<EditorObjectTemplate>(path);
            
            if (template != null && template.templateName == templateName)
            {
                // Found matching template
                GameObject newObject = CreateEditorObject(
                    template.objectName,
                    position,
                    parent,
                    template.basePrefab,
                    template.componentsToAdd
                );
                
                // Apply additional template properties
                if (template.customizeObject != null)
                {
                    template.customizeObject.Invoke(newObject);
                }
                
                return newObject;
            }
        }
        
        Debug.LogWarning($"No template found with name: {templateName}");
        return null;
    }
}

// Example template definition (you would create these as ScriptableObjects)
[CreateAssetMenu(fileName = "New Object Template", menuName = "Editor Tools/Object Template")]
public class EditorObjectTemplate : ScriptableObject
{
    public string templateName;
    public string objectName;
    public GameObject basePrefab;
    public List<System.Type> componentsToAdd;
    
    // Optional delegate for additional customization
    public System.Action<GameObject> customizeObject;
}

// Example editor menu integration
public class EditorObjectMenu
{
    [MenuItem("GameObject/Editor Objects/Create Basic Cube", false, 10)]
    static void CreateBasicCube()
    {
        // Example of creating a basic cube at the scene view camera position
        Vector3 position = SceneView.lastActiveSceneView.camera.transform.position + 
                          SceneView.lastActiveSceneView.camera.transform.forward * 5f;
        
        GameObject cube = EditorObjectCreator.CreateEditorMeshObject(
            "EditorCube",
            position,
            Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
            AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat")
        );
        
        // Select the created object
        Selection.activeGameObject = cube;
    }
    
    [MenuItem("GameObject/Editor Objects/Create Tree Group", false, 11)]
    static void CreateTreeGroup()
    {
        // Example of creating a group of trees
        Vector3 position = SceneView.lastActiveSceneView.camera.transform.position + 
                          SceneView.lastActiveSceneView.camera.transform.forward * 10f;
        
        // This assumes you have tree prefabs already defined
        // You would need to find these dynamically or define them elsewhere
        GameObject[] treePrefabs = new GameObject[0]; // Replace with your tree prefabs
        
        // Find tree prefabs in project (just an example approach)
        string[] guids = AssetDatabase.FindAssets("t:Prefab tree");
        List<GameObject> treeList = new List<GameObject>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (treePrefab != null)
            {
                treeList.Add(treePrefab);
            }
        }
        
        if (treeList.Count > 0)
        {
            treePrefabs = treeList.ToArray();
            GameObject treeGroup = EditorObjectCreator.CreateEditorGroup("TreeGroup", position, treePrefabs);
            Selection.activeGameObject = treeGroup;
        }
        else
        {
            Debug.LogWarning("No tree prefabs found in project.");
        }
    }
}
#endif