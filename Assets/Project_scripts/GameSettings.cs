using UnityEngine;

public static class GameSettings
{
    public static int seed = 9; // Default seed value
    
    // Updated: Add other persistent settings here
    public static int numberOfStructures = 10;
    public static float distributionRadius = 50f;
    public static bool shouldGenerate = false; // Flag to trigger generation
    
    // Method to set seed from main menu
    public static void SetSeed(int newSeed)
    {
        seed = newSeed;
        Debug.Log($"Game seed set to: {seed}");
    }
    
    // Method to get seed in generator scene
    public static int GetSeed()
    {
        return seed;
    }
    
    // Method to set structure count
    public static void SetStructureCount(int count)
    {
        numberOfStructures = Mathf.Clamp(count, 1, 50);
        Debug.Log($"Structure count set to: {numberOfStructures}");
    }
    
    // Method to trigger generation
    public static void SetShouldGenerate(bool generate)
    {
        shouldGenerate = generate;
    }
    
    // Method to check if should generate
    public static bool ShouldGenerate()
    {
        return shouldGenerate;
    }
    
    // Reset the generation flag after use
    public static void ResetGenerationFlag()
    {
        shouldGenerate = false;
    }
}