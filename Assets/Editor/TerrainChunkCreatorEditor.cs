using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainChunkCreator))]
public class TerrainChunkCreatorEditor : Editor
{
    private bool showGenerationSettings = true;
    private bool showPrefabSettings = true;
    private bool showPreviewSettings = true;
    
    public override void OnInspectorGUI()
    {
        TerrainChunkCreator creator = (TerrainChunkCreator)target;
        
        // Apply style
        GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
        headerStyle.fontStyle = FontStyle.Bold;
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Terrain Chunk Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool allows you to create terrain chunk prefabs in editor mode.", MessageType.Info);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
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
            EditorGUILayout.LabelField("Low Detail", GUILayout.Width(70));
            creator.previewLOD = EditorGUILayout.IntSlider(creator.previewLOD, 0, 6);
            EditorGUILayout.LabelField("High Detail", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            
            creator.generateCollider = EditorGUILayout.Toggle("Generate Collider", creator.generateCollider);
            creator.includeTreesInPrefab = EditorGUILayout.Toggle("Include Trees", creator.includeTreesInPrefab);
            creator.includeVegetationInPrefab = EditorGUILayout.Toggle("Include Vegetation", creator.includeVegetationInPrefab);
            
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
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Generate Button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Generate Terrain Chunk Prefab", GUILayout.Height(30), GUILayout.Width(250)))
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
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }
}
#endif