using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Complete WFC Scene Management System - All classes in one file
/// Save this as "WFCSceneSystem.cs"
/// </summary>

#region DATA PERSISTENCE SYSTEM

/// <summary>
/// Singleton to persist data between scenes
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    
    [Header("Generation Settings")]
    public int seed = 12345;
    public int numberOfStructures = 10;
    public float distributionRadius = 50f;
    public bool useCustomSettings = false;
    
    // Additional settings that can be passed between scenes
    public Vector3Int minStructureSize = new Vector3Int(8, 3, 8);
    public Vector3Int maxStructureSize = new Vector3Int(10, 5, 10);
    public bool enableChestSpawning = true;
    
    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Set generation parameters from main menu
    /// </summary>
    public void SetGenerationSettings(int newSeed, int structures = 10, float radius = 50f)
    {
        seed = newSeed;
        numberOfStructures = structures;
        distributionRadius = radius;
        useCustomSettings = true;
        
        Debug.Log($"GameDataManager: Settings updated - Seed: {seed}, Structures: {numberOfStructures}");
    }
    
    /// <summary>
    /// Load the generation scene with current settings
    /// </summary>
    public void LoadGenerationScene(string sceneName = "GenerationScene")
    {
        Debug.Log($"GameDataManager: Loading scene '{sceneName}' with seed {seed}");
        SceneManager.LoadScene(sceneName);
    }
}

#endregion

#region MAIN MENU CONTROLLER

/// <summary>
/// Main menu UI controller for setting generation parameters
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private TMP_InputField structureCountInput;
    [SerializeField] private TMP_InputField radiusInput;
    [SerializeField] private Button generateButton;
    [SerializeField] private Button randomSeedButton;
    
    [Header("Default Settings")]
    [SerializeField] private int defaultSeed = 12345;
    [SerializeField] private int defaultStructures = 10;
    [SerializeField] private float defaultRadius = 50f;
    
    [Header("Scene Settings")]
    [SerializeField] private string generationSceneName = "GenerationScene";
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }
    
    private void InitializeUI()
    {
        // Set default values
        if (seedInputField != null)
            seedInputField.text = defaultSeed.ToString();
            
        if (structureCountInput != null)
            structureCountInput.text = defaultStructures.ToString();
            
        if (radiusInput != null)
            radiusInput.text = defaultRadius.ToString();
    }
    
    private void SetupEventListeners()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(StartGeneration);
            
        if (randomSeedButton != null)
            randomSeedButton.onClick.AddListener(GenerateRandomSeed);
    }
    
    public void GenerateRandomSeed()
    {
        int randomSeed = Random.Range(1, 999999);
        if (seedInputField != null)
            seedInputField.text = randomSeed.ToString();
    }
    
    public void StartGeneration()
    {
        // Parse input values
        int seed = defaultSeed;
        int structures = defaultStructures;
        float radius = defaultRadius;
        
        if (seedInputField != null && int.TryParse(seedInputField.text, out int parsedSeed))
            seed = parsedSeed;
            
        if (structureCountInput != null && int.TryParse(structureCountInput.text, out int parsedStructures))
            structures = Mathf.Clamp(parsedStructures, 1, 100);
            
        if (radiusInput != null && float.TryParse(radiusInput.text, out float parsedRadius))
            radius = Mathf.Clamp(parsedRadius, 10f, 200f);
        
        // Ensure GameDataManager exists
        if (GameDataManager.Instance == null)
        {
            GameObject dataManager = new GameObject("GameDataManager");
            dataManager.AddComponent<GameDataManager>();
        }
        
        // Set the generation settings
        GameDataManager.Instance.SetGenerationSettings(seed, structures, radius);
        
        // Load the generation scene
        GameDataManager.Instance.LoadGenerationScene(generationSceneName);
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (generateButton != null)
            generateButton.onClick.RemoveListener(StartGeneration);
            
        if (randomSeedButton != null)
            randomSeedButton.onClick.RemoveListener(GenerateRandomSeed);
    }
}

#endregion

#region LOADING SCREEN CONTROLLER

/// <summary>
/// Manages the loading screen UI during generation
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI seedInfoText;
    
    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadingTime = 2f;
    [SerializeField] private bool showProgressBar = true;
    [SerializeField] private bool showProgressText = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private float loadingStartTime;
    private bool generationComplete = false;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // Ensure we have a CanvasGroup for fading
        if (loadingPanel != null)
        {
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        }
    }
    
    private void Start()
    {
        ShowLoadingScreen();
    }
    
    public void ShowLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
            
        loadingStartTime = Time.time;
        generationComplete = false;
        
        // Display seed information
        if (seedInfoText != null && GameDataManager.Instance != null)
        {
            seedInfoText.text = $"Generating with Seed: {GameDataManager.Instance.seed}";
        }
        
        if (statusText != null)
            statusText.text = "Initializing generation...";
            
        UpdateProgress(0, 1, "Starting...");
    }
    
    public void UpdateProgress(int current, int total, string status = "")
    {
        float progress = total > 0 ? (float)current / total : 0f;
        
        if (progressBar != null && showProgressBar)
        {
            progressBar.value = progress;
        }
        
        if (progressText != null && showProgressText)
        {
            progressText.text = $"{current}/{total} ({progress:P0})";
        }
        
        if (statusText != null && !string.IsNullOrEmpty(status))
        {
            statusText.text = status;
        }
    }
    
    public void OnGenerationComplete()
    {
        generationComplete = true;
        
        if (statusText != null)
            statusText.text = "Generation complete!";
            
        UpdateProgress(1, 1, "Finalizing...");
        
        // Ensure minimum loading time for smooth UX
        float elapsedTime = Time.time - loadingStartTime;
        if (elapsedTime < minimumLoadingTime)
        {
            StartCoroutine(DelayedHideLoading(minimumLoadingTime - elapsedTime));
        }
        else
        {
            HideLoadingScreen();
        }
    }
    
    private IEnumerator DelayedHideLoading(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingScreen();
    }
    
    public void HideLoadingScreen()
    {
        if (loadingPanel != null)
        {
            StartCoroutine(FadeOutLoading());
        }
    }
    
    private IEnumerator FadeOutLoading()
    {
        if (canvasGroup == null) 
        {
            loadingPanel.SetActive(false);
            yield break;
        }
            
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        loadingPanel.SetActive(false);
        canvasGroup.alpha = 1f; // Reset for next use
    }
    
    /// <summary>
    /// Force hide loading screen immediately (useful for emergency cases)
    /// </summary>
    public void ForceHide()
    {
        StopAllCoroutines();
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}

#endregion

#region SCENE CONTROLLER

/// <summary>
/// Controls the generation scene initialization and coordination
/// </summary>
public class GenerationSceneController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private WFCBatchGenerator batchGenerator;
    [SerializeField] private LoadingScreenController loadingScreen;
    
    [Header("Auto-Setup")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private bool startGenerationOnLoad = true;
    [SerializeField] private float initializationDelay = 0.1f;
    
    [Header("Fallback Settings")]
    [SerializeField] private int fallbackSeed = 12345;
    [SerializeField] private int fallbackStructures = 10;
    [SerializeField] private float fallbackRadius = 50f;
    
    private bool generationStarted = false;
    
    private void Start()
    {
        if (autoFindComponents)
        {
            AutoFindComponents();
        }
        
        if (startGenerationOnLoad)
        {
            StartCoroutine(InitializeGeneration());
        }
    }
    
    private void AutoFindComponents()
    {
        if (batchGenerator == null)
            batchGenerator = FindObjectOfType<WFCBatchGenerator>();
            
        if (loadingScreen == null)
            loadingScreen = FindObjectOfType<LoadingScreenController>();
            
        Debug.Log($"Auto-found components - BatchGenerator: {batchGenerator != null}, LoadingScreen: {loadingScreen != null}");
    }
    
    private IEnumerator InitializeGeneration()
    {
        // Wait for initialization
        yield return new WaitForSeconds(initializationDelay);
        
        if (generationStarted)
            yield break;
            
        generationStarted = true;
        
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("No GameDataManager found! Using fallback settings.");
            StartGeneration(fallbackSeed, fallbackStructures, fallbackRadius);
            yield break;
        }
        
        // Apply settings from GameDataManager
        var data = GameDataManager.Instance;
        StartGeneration(data.seed, data.numberOfStructures, data.distributionRadius);
    }
    
    /// <summary>
    /// Manual generation trigger (can be called from UI button)
    /// </summary>
    public void TriggerGeneration()
    {
        if (!generationStarted)
        {
            StartCoroutine(InitializeGeneration());
        }
    }
    
    private void StartGeneration(int seed, int structures, float radius)
    {
        if (batchGenerator == null)
        {
            Debug.LogError("WFCBatchGenerator not found! Cannot start generation.");
            if (loadingScreen != null)
                loadingScreen.ForceHide();
            return;
        }
        
        Debug.Log($"Starting generation with Seed: {seed}, Structures: {structures}, Radius: {radius}");
        
        // Configure batch generator
        batchGenerator.setSeed(seed);
        
        // Set generation parameters using reflection (safer approach)
        SetBatchGeneratorField("numberOfStructures", structures);
        SetBatchGeneratorField("distributionRadius", radius);
        
        // Apply additional settings if available
        if (GameDataManager.Instance != null)
        {
            var data = GameDataManager.Instance;
            SetBatchGeneratorField("minStructureSize", data.minStructureSize);
            SetBatchGeneratorField("maxStructureSize", data.maxStructureSize);
            SetBatchGeneratorField("enableChestSpawning", data.enableChestSpawning);
        }
        
        // Subscribe to events
        batchGenerator.OnGenerationProgress += OnGenerationProgress;
        batchGenerator.OnGenerationComplete += OnGenerationComplete;
        batchGenerator.OnStructureGenerated += OnStructureGenerated;
        
        // Start generation
        batchGenerator.GenerateStructures();
    }
    
    private void SetBatchGeneratorField(string fieldName, object value)
    {
        try
        {
            var field = typeof(WFCBatchGenerator).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(batchGenerator, value);
            Debug.Log($"Set {fieldName} to {value}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to set field {fieldName}: {ex.Message}");
        }
    }
    
    private void OnGenerationProgress(int current, int total)
    {
        Debug.Log($"Generation Progress: {current}/{total}");
        
        if (loadingScreen != null)
        {
            string status = $"Generating structure {current} of {total}...";
            loadingScreen.UpdateProgress(current, total, status);
        }
    }
    
    private void OnStructureGenerated(GameObject structure)
    {
        Debug.Log($"Structure generated: {structure.name}");
    }
    
    private void OnGenerationComplete()
    {
        Debug.Log("All structures generated successfully!");
        
        if (loadingScreen != null)
        {
            loadingScreen.OnGenerationComplete();
        }
        
        // Unsubscribe from events
        UnsubscribeFromEvents();
    }
    
    private void UnsubscribeFromEvents()
    {
        if (batchGenerator != null)
        {
            batchGenerator.OnGenerationProgress -= OnGenerationProgress;
            batchGenerator.OnGenerationComplete -= OnGenerationComplete;
            batchGenerator.OnStructureGenerated -= OnStructureGenerated;
        }
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// Public method to restart generation with new settings
    /// </summary>
    public void RestartGeneration(int newSeed, int newStructures, float newRadius)
    {
        UnsubscribeFromEvents();
        generationStarted = false;
        
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetGenerationSettings(newSeed, newStructures, newRadius);
        }
        
        if (loadingScreen != null)
        {
            loadingScreen.ShowLoadingScreen();
        }
        
        StartGeneration(newSeed, newStructures, newRadius);
    }
}

#endregion

#region UTILITY EXTENSIONS

/// <summary>
/// Extension methods for easier configuration
/// </summary>
public static class WFCSceneSystemExtensions
{
    /// <summary>
    /// Configure the batch generator with external settings safely
    /// </summary>
    public static void ConfigureFromGameData(this WFCBatchGenerator generator, GameDataManager data)
    {
        if (data == null || generator == null) return;
        
        generator.setSeed(data.seed);
        
        // Use reflection to set private fields safely
        SetPrivateFieldSafe(generator, "numberOfStructures", data.numberOfStructures);
        SetPrivateFieldSafe(generator, "distributionRadius", data.distributionRadius);
        SetPrivateFieldSafe(generator, "minStructureSize", data.minStructureSize);
        SetPrivateFieldSafe(generator, "maxStructureSize", data.maxStructureSize);
        SetPrivateFieldSafe(generator, "enableChestSpawning", data.enableChestSpawning);
    }
    
    private static void SetPrivateFieldSafe(object obj, string fieldName, object value)
    {
        try
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to set field {fieldName}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Quick setup method for main menu
    /// </summary>
    public static void SetupMainMenu(this MainMenuController controller, 
        TMP_InputField seedField, TMP_InputField structuresField, TMP_InputField radiusField,
        Button generateBtn, Button randomBtn)
    {
        // This would need to be called after the controller is instantiated
        // Useful for runtime setup
    }
}

#endregion

#region DATA STRUCTURES

/// <summary>
/// Container for generation statistics and results
/// </summary>
[System.Serializable]
public class GenerationResult
{
    public int seed;
    public int totalStructuresRequested;
    public int totalStructuresGenerated;
    public float generationTime;
    public bool wasSuccessful;
    public string errorMessage;
    
    public GenerationResult(int seed, int requested, int generated, float time, bool success, string error = "")
    {
        this.seed = seed;
        this.totalStructuresRequested = requested;
        this.totalStructuresGenerated = generated;
        this.generationTime = time;
        this.wasSuccessful = success;
        this.errorMessage = error;
    }
}

/// <summary>
/// Event data for generation events
/// </summary>
[System.Serializable]
public class GenerationEventData
{
    public int currentStructure;
    public int totalStructures;
    public float progress;
    public string status;
    public GameObject generatedStructure;
    
    public GenerationEventData(int current, int total, string status = "", GameObject structure = null)
    {
        this.currentStructure = current;
        this.totalStructures = total;
        this.progress = total > 0 ? (float)current / total : 0f;
        this.status = status;
        this.generatedStructure = structure;
    }
}

#endregion