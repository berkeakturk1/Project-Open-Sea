using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Create this as a new script in your project
public static class TerrainSceneFlag
{
    // This static flag will persist between scene loads
    public static bool IsComingFromAnotherScene = false;
}

// Your SimpleSceneManager with the fixed implementation
public class SimpleSceneManager : MonoBehaviour
{
    // Method to load the scene by index
    public void LoadSceneWithIndex1()
    {
        // Set the flag before loading the new scene
        TerrainSceneFlag.IsComingFromAnotherScene = true;
        
        // Simple scene loading
        SceneManager.LoadScene(1);
    }
}