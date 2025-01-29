using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneManager : MonoBehaviour
{
    // Method to load the scene by index
    public void LoadSceneWithIndex1()
    {
        SceneManager.LoadScene(1); // Loads the scene with index 1
    }
}