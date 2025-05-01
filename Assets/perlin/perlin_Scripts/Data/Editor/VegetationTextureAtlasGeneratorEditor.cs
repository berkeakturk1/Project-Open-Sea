using UnityEngine;
using UnityEditor;
using System.Collections;

#if UNITY_EDITOR
[CustomEditor(typeof(VegetationTextureAtlasGenerator))]
public class VegetationTextureAtlasGeneratorEditor : Editor {
    
    private bool showTextureSettings = true;
    private bool showAtlasSettings = true;
    private bool showMaterialSettings = true;
    
    public override void OnInspectorGUI() {
        VegetationTextureAtlasGenerator generator = (VegetationTextureAtlasGenerator)target;
        
        // Add a description
        EditorGUILayout.HelpBox("This tool creates texture atlases for vegetation. Add your source textures below and click Generate Atlas.", MessageType.Info);
        
        // Source texture foldout
        showTextureSettings = EditorGUILayout.Foldout(showTextureSettings, "Source Textures", true);
        if (showTextureSettings) {
            EditorGUI.indentLevel++;
            
            // List of source textures
            SerializedProperty texturesList = serializedObject.FindProperty("sourceTextures");
            
            EditorGUILayout.PropertyField(texturesList, true);
            
            if (GUILayout.Button("Add New Texture")) {
                texturesList.arraySize++;
                SerializedProperty newTexture = texturesList.GetArrayElementAtIndex(texturesList.arraySize - 1);
                newTexture.FindPropertyRelative("name").stringValue = "Vegetation_" + texturesList.arraySize;
                newTexture.FindPropertyRelative("tilesX").intValue = 1;
                newTexture.FindPropertyRelative("tilesY").intValue = 1;
            }
            
            EditorGUI.indentLevel--;
        }
        
        // Atlas settings foldout
        showAtlasSettings = EditorGUILayout.Foldout(showAtlasSettings, "Atlas Settings", true);
        if (showAtlasSettings) {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generateMipmaps"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wrapMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("filterMode"));
            
            EditorGUI.indentLevel--;
        }
        
        // Material generation foldout
        showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Material Generation", true);
        if (showMaterialSettings) {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vegetationShader"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createMaterials"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("assignToVegetationManager"));
            
            EditorGUI.indentLevel--;
        }
        
        // Atlas preview
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Atlas Preview", EditorStyles.boldLabel);
        
        if (generator.generatedAtlas != null) {
            EditorGUILayout.ObjectField(generator.generatedAtlas, typeof(Texture2D), false);
            
            Rect previewRect = GUILayoutUtility.GetRect(256, 256);
            EditorGUI.DrawPreviewTexture(previewRect, generator.generatedAtlas);
        } else {
            EditorGUILayout.LabelField("No atlas generated yet.");
        }
        
        // Generate button
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Atlas", GUILayout.Height(30))) {
            generator.GenerateAtlas();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif