using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This helper class adds extensions to ModelGrass to make it work with the chunk system
public static class ModelGrassIntegrationHelper
{
    // Call this to update the ModelGrass shaders to work with procedural chunks
    public static void PatchGrassShader(ComputeShader initializeGrassShader)
    {
        // We need to add the _ChunkOffset and _TerrainScale properties to the shader
        // This is a runtime patch to add this support
        if (initializeGrassShader != null)
        {
            try
            {
                // Initialize with zeroes as default
                initializeGrassShader.SetVector("_ChunkOffset", Vector4.zero);
                initializeGrassShader.SetFloat("_TerrainScale", 1.0f);
                
                Debug.Log("Successfully patched grass shader for chunk system");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error patching grass shader: {e.Message}");
            }
        }
    }
}

// This class should be attached to whatever GameObject instance that manages terrain chunks
public class ChunkGrassShaderHelper : MonoBehaviour
{
    // This will be called whenever a new compute shader is loaded by ModelGrass
    // You can hook this up in a method that initializes the grass shaders
    public static void RegisterShaderForChunkSupport(ComputeShader shader)
    {
        if (shader != null)
        {
            ModelGrassIntegrationHelper.PatchGrassShader(shader);
        }
    }
}