using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainChunkCreatorDirect))]
public class TerrainChunkCreatorDirectEditor : Editor
{
    private bool showGenerationSettings = true;
    private bool showPrefabSettings = true;
    private bool showDebugSettings = true;
    
    public override void OnInspectorGUI()
    {
        TerrainChunkCreatorDirect creator = (TerrainChunkCreatorDirect)target;
        
        // Apply style
        GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
        headerStyle.fontStyle = FontStyle.Bold;
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Terrain Chunk Creator (Direct Method)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool generates terrain chunks directly and previews them before creating prefabs.", MessageType.Info);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Draw a horizontal line
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Terrain Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Terrain Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Ensure the MapGenerator is assigned
        creator.mapGenerator = (MapGenerator)EditorGUILayout.ObjectField("Map Generator", creator.mapGenerator, typeof(MapGenerator), true);
        if (creator.mapGenerator == null)
        {
            EditorGUILayout.HelpBox("MapGenerator reference is required!", MessageType.Error);
        }
        
        creator.terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material", creator.terrainMaterial, typeof(Material), false);
        if (creator.terrainMaterial == null && creator.mapGenerator != null)
        {
            EditorGUILayout.HelpBox("No terrain material assigned. Will use the one from Map Generator.", MessageType.Warning);
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Generation Settings
        showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "Generation Settings", headerStyle);
        if (showGenerationSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            
            creator.chunkPosition = EditorGUILayout.Vector2Field("Chunk Position", creator.chunkPosition);
            
            EditorGUILayout.LabelField("Level of Detail", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("High Detail", GUILayout.Width(70));
            creator.previewLOD = EditorGUILayout.IntSlider(creator.previewLOD, 0, 4);
            EditorGUILayout.LabelField("Low Detail", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            
            creator.generateCollider = EditorGUILayout.Toggle("Generate Collider", creator.generateCollider);
            creator.includeTreesInPrefab = EditorGUILayout.Toggle("Include Trees", creator.includeTreesInPrefab);
            creator.includeVegetationInPrefab = EditorGUILayout.Toggle("Include Vegetation", creator.includeVegetationInPrefab);
            
            EditorGUILayout.Space();
            creator.showPreviewInEditor = EditorGUILayout.Toggle("Show Preview", creator.showPreviewInEditor);
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Debug Information
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Information", headerStyle);
        if (showDebugSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Heightmap Width", serializedObject.FindProperty("heightMapWidth").intValue);
            EditorGUILayout.IntField("Heightmap Height", serializedObject.FindProperty("heightMapHeight").intValue);
            EditorGUILayout.IntField("Vertex Count", serializedObject.FindProperty("vertexCount").intValue);
            EditorGUILayout.IntField("Triangle Count", serializedObject.FindProperty("triangleCount").intValue);
            EditorGUILayout.Toggle("Mesh Generated", serializedObject.FindProperty("meshGenerated").boolValue);
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Prefab Settings
        showPrefabSettings = EditorGUILayout.Foldout(showPrefabSettings, "Prefab Settings", headerStyle);
        if (showPrefabSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            
            creator.prefabName = EditorGUILayout.TextField("Prefab Name", creator.prefabName);
            creator.prefabPath = EditorGUILayout.TextField("Prefab Path", creator.prefabPath);
            
            // Add a button to open the prefab folder
            if (GUILayout.Button("Open Prefab Directory", GUILayout.Height(20)))
            {
                string fullPath = Path.Combine(Application.dataPath, creator.prefabPath.Replace("Assets/", ""));
                if (Directory.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
                else
                {
                    Debug.LogWarning($"Directory does not exist: {fullPath}");
                }
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Draw a horizontal line
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Preview and Generate buttons
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1.0f);
        if (GUILayout.Button("Generate Mesh Preview", GUILayout.Height(30)))
        {
            if (creator.mapGenerator != null)
            {
                creator.GenerateMeshDirectly();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Map Generator reference is required!", "OK");
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Generate button
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Generate Terrain Chunk Prefab", GUILayout.Height(40)))
        {
            if (creator.mapGenerator != null)
            {
                creator.GenerateTerrainChunk();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Map Generator reference is required!", "OK");
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Allow debugging to update the preview
        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif