using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject generationPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    [Header("Generation Panel Elements")]
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private Button generateButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI seedLabel;
    
    [Header("Generation Settings")]
    [SerializeField] private TMP_InputField structureCountField;
    [SerializeField] private Slider structureCountSlider;
    
    [Header("Scene Settings")]
    [SerializeField] private string mainGameSceneName = "MainGame"; // The scene with WFCBatchGenerator
    [SerializeField] private int defaultSeed = 9;
    [SerializeField] private int defaultStructureCount = 10;
    [SerializeField] private int minStructureCount = 1;
    [SerializeField] private int maxStructureCount = 50;
    private int mainGameSceneIndex = 1;
    
       
        
    
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        DebugLog("UIManager Start() called");
        InitializeUI();
        SetupButtonListeners();
        ValidateReferences();
    }
    
    private void InitializeUI()
    {
        DebugLog("Initializing UI...");
        
        ShowMainMenu();
        
        // Set default values
        if (seedInputField != null)
        {
            seedInputField.text = defaultSeed.ToString();
            DebugLog($"Set seed input field to: {defaultSeed}");
        }
        
        if (structureCountField != null)
        {
            structureCountField.text = defaultStructureCount.ToString();
        }
        
        if (structureCountSlider != null)
        {
            structureCountSlider.value = defaultStructureCount;
            structureCountSlider.minValue = minStructureCount;
            structureCountSlider.maxValue = maxStructureCount;
            structureCountSlider.wholeNumbers = true;
        }
        
        UpdateSeedLabel();
    }
    
    private void SetupButtonListeners()
    {
        DebugLog("Setting up button listeners...");
        
        // Main Menu Buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonPressed);
            DebugLog("Play button listener added successfully");
        }
            
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitButtonPressed);
            DebugLog("Quit button listener added successfully");
        }
        
        // Generation Panel Buttons
        if (generateButton != null)
        {
            generateButton.onClick.RemoveAllListeners();
            generateButton.onClick.AddListener(OnGenerateButtonPressed);
            DebugLog("Generate button listener added successfully");
        }
            
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonPressed);
            DebugLog("Back button listener added successfully");
        }
        
        // Input Field Listeners
        if (seedInputField != null)
        {
            seedInputField.onValueChanged.RemoveAllListeners();
            seedInputField.onEndEdit.RemoveAllListeners();
            seedInputField.onValueChanged.AddListener(OnSeedInputChanged);
            seedInputField.onEndEdit.AddListener(OnSeedInputEndEdit);
            DebugLog("Seed input field listeners added successfully");
        }
        
        // Structure count listeners
        if (structureCountSlider != null)
        {
            structureCountSlider.onValueChanged.RemoveAllListeners();
            structureCountSlider.onValueChanged.AddListener(OnStructureCountSliderChanged);
        }
        
        if (structureCountField != null)
        {
            structureCountField.onValueChanged.RemoveAllListeners();
            structureCountField.onValueChanged.AddListener(OnStructureCountFieldChanged);
        }
    }
    
    private void ValidateReferences()
    {
        DebugLog("=== VALIDATING UI REFERENCES ===");
        DebugLog($"Main Menu Panel: {(mainMenuPanel != null ? "OK" : "NULL")}");
        DebugLog($"Generation Panel: {(generationPanel != null ? "OK" : "NULL")}");
        DebugLog($"Play Button: {(playButton != null ? "OK" : "NULL")}");
        DebugLog($"Generate Button: {(generateButton != null ? "OK" : "NULL")}");
        DebugLog($"Seed Input Field: {(seedInputField != null ? "OK" : "NULL")}");
        DebugLog($"Main Game Scene Index: {mainGameSceneIndex}");
        DebugLog("=== END VALIDATION ===");
    }
    
    #region Button Event Handlers
    
    private void OnPlayButtonPressed()
    {
        DebugLog("=== PLAY BUTTON PRESSED ===");
        ShowGenerationPanel();
    }
    
    private void OnQuitButtonPressed()
    {
        DebugLog("Quit button pressed");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnGenerateButtonPressed()
    {
        DebugLog("=== GENERATE BUTTON PRESSED ===");
        
        // Get values from UI
        int seed = GetSeedFromInput();
        int structureCount = GetStructureCountFromInput();
        
        DebugLog($"Seed from input: {seed}");
        DebugLog($"Structure count from input: {structureCount}");
        
        // Store settings in GameSettings for the next scene to pick up
        GameSettings.SetSeed(seed);
        GameSettings.SetShouldGenerate(true);
        GameSettings.numberOfStructures = structureCount;
        
        DebugLog($"Stored seed in GameSettings: {GameSettings.GetSeed()}");
        DebugLog($"Stored structure count in GameSettings: {GameSettings.numberOfStructures}");
        
        // Load the main game scene
        LoadMainGameScene();
    }
    
    private void OnBackButtonPressed()
    {
        DebugLog("=== BACK BUTTON PRESSED ===");
        ShowMainMenu();
    }
    
    #endregion
    
    #region Input Handlers
    
    private void OnSeedInputChanged(string value)
    {
        UpdateSeedLabel();
    }
    
    private void OnSeedInputEndEdit(string value)
    {
        int seed = GetSeedFromInput();
        seedInputField.text = seed.ToString();
        UpdateSeedLabel();
    }
    
    private void OnStructureCountSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        if (structureCountField != null)
        {
            structureCountField.text = intValue.ToString();
        }
    }
    
    private void OnStructureCountFieldChanged(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            intValue = Mathf.Clamp(intValue, minStructureCount, maxStructureCount);
            if (structureCountSlider != null)
            {
                structureCountSlider.value = intValue;
            }
        }
    }
    
    #endregion
    
    #region Panel Management
    
    private void ShowMainMenu()
    {
        DebugLog("=== SHOWING MAIN MENU ===");
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            DebugLog("Main menu panel activated");
        }
            
        if (generationPanel != null)
        {
            generationPanel.SetActive(false);
            DebugLog("Generation panel deactivated");
        }
    }
    
    private void ShowGenerationPanel()
    {
        DebugLog("=== SHOWING GENERATION PANEL ===");
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            DebugLog("Main menu panel deactivated");
        }
            
        if (generationPanel != null)
        {
            generationPanel.SetActive(true);
            DebugLog("Generation panel activated");
            
            if (seedInputField != null)
            {
                seedInputField.Select();
                seedInputField.ActivateInputField();
                DebugLog("Seed input field focused");
            }
        }
    }
    
    #endregion
    
    #region Scene Loading
    
    private void LoadMainGameScene()
    {
        // Validate scene index exists in build settings
        if (mainGameSceneIndex < 0 || mainGameSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            DebugLog($"ERROR: Scene index {mainGameSceneIndex} is out of range! Total scenes in build: {SceneManager.sceneCountInBuildSettings}");
            return;
        }
        
        // Get scene name for logging
        string scenePath = SceneUtility.GetScenePathByBuildIndex(mainGameSceneIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        
        DebugLog($"Loading main game scene at index {mainGameSceneIndex}: {sceneName}");
        DebugLog($"Generation will start automatically with seed: {GameSettings.GetSeed()}");
        DebugLog($"Structure count: {GameSettings.numberOfStructures}");
        
        try
        {
            SceneManager.LoadScene(mainGameSceneIndex);
        }
        catch (System.Exception e)
        {
            DebugLog($"ERROR: Failed to load scene at index {mainGameSceneIndex}: {e.Message}");
        }
    }
    #endregion
    
    #region Utility Methods
    
    private int GetSeedFromInput()
    {
        if (seedInputField == null || string.IsNullOrEmpty(seedInputField.text))
        {
            return defaultSeed;
        }
        
        if (int.TryParse(seedInputField.text, out int seed))
        {
            return Mathf.Clamp(seed, -999999, 999999);
        }
        else
        {
            DebugLog($"Invalid seed input: '{seedInputField.text}'. Using default seed: {defaultSeed}");
            return defaultSeed;
        }
    }
    
    private int GetStructureCountFromInput()
    {
        if (structureCountField != null && int.TryParse(structureCountField.text, out int count))
        {
            return Mathf.Clamp(count, minStructureCount, maxStructureCount);
        }
        return defaultStructureCount;
    }
    
    private void UpdateSeedLabel()
    {
        if (seedLabel != null)
        {
            int currentSeed = GetSeedFromInput();
            seedLabel.text = $"Seed: {currentSeed}";
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UIManager] {message}");
        }
    }
    
    #endregion
}