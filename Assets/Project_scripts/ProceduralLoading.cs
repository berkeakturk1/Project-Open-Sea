using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Transform proceduralContainer;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    
    [Header("Procedural Animation")]
    [SerializeField] private int gridSize = 8;
    [SerializeField] private float cellSize = 20f;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Color[] waveColors = { Color.blue, Color.cyan, Color.white, Color.yellow };
    [SerializeField] private float waveSpeed = 2f;
    [SerializeField] private float waveAmplitude = 0.5f;
    
    [Header("Loading Messages")]
    [SerializeField] private string[] loadingMessages = {
        "Calculating structure positions...",
        "Generating procedural structures...",
        "Building architectural elements...",
        "Spawning details and items...",
        "Finalizing world generation...",
        "Almost ready..."
    };
    
    [Header("Auto-Start Settings")]
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private float delayBeforeAutoStart = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private static LoadingScreenManager instance;
    public static LoadingScreenManager Instance => instance;
    
    private Image[,] proceduralGrid;
    private float currentProgress = 0f;
    private bool isGenerationComplete = false;
    private bool hasGenerationStarted = false;
    private Coroutine loadingCoroutine;
    private Coroutine animationCoroutine;
    
    // Generation tracking
    private int expectedStructures = 0;
    private int completedStructures = 0;
    
    void Awake()
    {
        // Simple singleton for same-scene usage
        if (instance == null)
        {
            instance = this;
            InitializeProceduralAnimation();
            SetupCanvas();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void SetupCanvas()
    {
        // Ensure loading screen appears on top of everything
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        
        canvas.sortingOrder = 9999; // Highest priority
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        
        // Ensure we have a GraphicRaycaster for UI interactions
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        DebugLog("Canvas setup complete - loading screen ready");
    }
    
    void Start()
    {
        // Initially hide the loading screen
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.gameObject.SetActive(true);
        }

        
        // Validate UI components
        ValidateUIComponents();
        
        // Auto-start if enabled
        if (autoStartOnAwake)
        {
            StartCoroutine(AutoStartAfterDelay());
        }
    }
    
    private void ValidateUIComponents()
    {
        DebugLog("=== VALIDATING UI COMPONENTS ===");
        DebugLog($"Loading Canvas Group: {(loadingCanvasGroup != null ? "OK" : "MISSING!")}");
        DebugLog($"Progress Bar: {(progressBar != null ? "OK" : "MISSING!")}");
        DebugLog($"Progress Text: {(progressText != null ? "OK" : "MISSING!")}");
        DebugLog($"Loading Text: {(loadingText != null ? "OK" : "MISSING!")}");
        DebugLog($"Procedural Container: {(proceduralContainer != null ? "OK" : "MISSING!")}");
        DebugLog($"Cell Prefab: {(cellPrefab != null ? "OK" : "MISSING!")}");
        DebugLog($"Loading Messages Count: {loadingMessages.Length}");
        DebugLog($"Wave Colors Count: {waveColors.Length}");
        DebugLog("=== END UI VALIDATION ===");
        
        if (progressBar == null)
            Debug.LogError("[LoadingScreen] Progress Bar is not assigned! The progress bar won't update.");
        if (progressText == null)
            Debug.LogError("[LoadingScreen] Progress Text is not assigned! The percentage won't update.");
        if (loadingText == null)
            Debug.LogError("[LoadingScreen] Loading Text is not assigned! The loading messages won't update.");
    }
    
    private IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeAutoStart);
        
        // Look for WFCBatchGenerator in the scene
        WFCBatchGenerator generator = FindObjectOfType<WFCBatchGenerator>();
        if (generator != null)
        {
            // Check if generator has auto-start enabled to avoid duplicates
            if (generator.autoStartGeneration)
            {
                DebugLog("WFCBatchGenerator has auto-start enabled - skipping LoadingScreen auto-start to avoid duplicates");
                yield break; // Use yield break instead of return in coroutines
            }
            
            DebugLog("Auto-starting loading screen for WFC generation");
            StartLoadingForGenerator(generator);
        }
        else
        {
            DebugLog("No WFCBatchGenerator found in scene for auto-start");
        }
    }
    
    void InitializeProceduralAnimation()
    {
        if (proceduralContainer == null || cellPrefab == null) return;
        
        proceduralGrid = new Image[gridSize, gridSize];
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject cell = Instantiate(cellPrefab, proceduralContainer);
                RectTransform rect = cell.GetComponent<RectTransform>();
                Image image = cell.GetComponent<Image>();
                
                if (rect != null)
                {
                    float posX = (x - gridSize * 0.5f) * cellSize;
                    float posY = (y - gridSize * 0.5f) * cellSize;
                    rect.anchoredPosition = new Vector2(posX, posY);
                    rect.sizeDelta = new Vector2(cellSize * 0.8f, cellSize * 0.8f);
                }
                
                if (image != null)
                {
                    proceduralGrid[x, y] = image;
                    image.color = Color.clear;
                }
            }
        }
    }
    
    /// <summary>
    /// Start the loading screen for a specific WFCBatchGenerator
    /// </summary>
    public void StartLoadingForGenerator(WFCBatchGenerator generator)
    {
        if (generator == null)
        {
            Debug.LogError("Cannot start loading screen: WFCBatchGenerator is null!");
            return;
        }
        
        DebugLog($"Starting loading screen for generator with {generator.numberOfStructures} structures");
        
        // Reset all tracking
        completedStructures = 0;
        expectedStructures = generator.numberOfStructures;
        currentProgress = 0f;
        isGenerationComplete = false;
        hasGenerationStarted = false;
        
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        
        loadingCoroutine = StartCoroutine(LoadingSequence(generator));
    }
    
    /// <summary>
    /// Manually start the loading screen with expected structure count
    /// </summary>
    public void StartLoading(int structureCount = 10)
    {
        DebugLog($"Manually starting loading screen for {structureCount} structures");
        
        // Reset all tracking
        completedStructures = 0;
        expectedStructures = structureCount > 0 ? structureCount : 1;
        currentProgress = 0f;
        isGenerationComplete = false;
        hasGenerationStarted = false;
        
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        
        loadingCoroutine = StartCoroutine(LoadingSequence(null));
    }
    
    private IEnumerator LoadingSequence(WFCBatchGenerator generator)
    {
        DebugLog("Loading sequence started");
        
        // Fade in the loading screen
        //yield return StartCoroutine(FadeIn());
        
        // Start procedural animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(ProceduralWaveAnimation());
        
        // If we have a generator reference, trigger its generation
        if (generator != null)
        {
            DebugLog("Triggering generation on provided generator");
            
            // Set up event callbacks
            generator.OnGenerationProgress += OnStructureGenerated;
            generator.OnGenerationComplete += OnGenerationComplete;
            
            // Start generation
            generator.GenerateStructures();
            OnGenerationStarted(expectedStructures);
        }
        else
        {
            DebugLog("No generator provided - waiting for external calls to update progress");
        }
        
        // Wait for generation to complete
        float maxWaitTime = 300f; // 5 minutes timeout
        float waitTime = 0f;
        
        while (!isGenerationComplete && waitTime < maxWaitTime)
        {
            // DON'T override progress here - let the WFC generator control it
            // Only update UI to reflect the current progress set by external calls
            UpdateUI();
            
            // Log progress periodically
            if (Time.frameCount % 240 == 0) // Every 4 seconds at 60fps
            {
                DebugLog($"Waiting for completion - Progress: {currentProgress:F2} ({currentProgress * 100f:F0}%) | Started: {hasGenerationStarted} | Complete: {isGenerationComplete}");
                DebugLog($"Structures: {completedStructures}/{expectedStructures}");
            }
            
            waitTime += Time.deltaTime;
            yield return null;
        }
        
        if (waitTime >= maxWaitTime)
        {
            DebugLog("WARNING: Generation timeout reached! Proceeding to hide loading screen...");
        }
        
        // Ensure 100% is shown
        currentProgress = 1f;
        UpdateUI();
        
        DebugLog("Generation complete! Starting fade out...");
        
        yield return new WaitForSeconds(0.5f); // Show 100% briefly
        
        // Stop animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        // Hide the loading screen
        loadingCanvasGroup.gameObject.SetActive(false);
        loadingCoroutine = null;
        
        DebugLog("Loading sequence finished - screen hidden");
    }
    
    private void UpdateUI()
    {
        DebugLog($"UpdateUI called - currentProgress: {currentProgress:F2}");
        
        if (progressBar != null)
        {
            float oldValue = progressBar.value;
            progressBar.value = currentProgress;
            DebugLog($"Progress Bar: {oldValue:F2} → {progressBar.value:F2}");
        }
        else
        {
            DebugLog("ERROR: Progress bar is NULL!");
        }
        
        if (progressText != null)
        {
            string oldText = progressText.text;
            progressText.text = $"{currentProgress * 100f:F0}%";
            DebugLog($"Progress Text: '{oldText}' → '{progressText.text}'");
        }
        else
        {
            DebugLog("ERROR: Progress text is NULL!");
        }
        
        if (loadingText != null && loadingMessages.Length > 0)
        {
            string oldText = loadingText.text;
            
            int messageIndex = 0;
            
            if (!hasGenerationStarted)
            {
                messageIndex = 0; // "Calculating structure positions..."
            }
            else if (currentProgress < 1.0f)
            {
                messageIndex = Mathf.FloorToInt(currentProgress * (loadingMessages.Length - 1));
                messageIndex = Mathf.Clamp(messageIndex, 1, loadingMessages.Length - 2);
            }
            else
            {
                messageIndex = loadingMessages.Length - 1; // "Almost ready..."
            }
            
            messageIndex = Mathf.Clamp(messageIndex, 0, loadingMessages.Length - 1);
            loadingText.text = loadingMessages[messageIndex];
            
            DebugLog($"Loading Text: '{oldText}' → '{loadingText.text}' (index: {messageIndex})");
        }
        else
        {
            if (loadingText == null)
                DebugLog("ERROR: Loading text is NULL!");
            if (loadingMessages.Length == 0)
                DebugLog("ERROR: Loading messages array is EMPTY!");
        }
        
        // Force Canvas to update
        if (loadingCanvasGroup != null)
        {
            Canvas canvas = loadingCanvasGroup.GetComponent<Canvas>();
            if (canvas != null)
            {
                Canvas.ForceUpdateCanvases(); // Call as static method
                DebugLog("Forced canvas update");
            }
        }
    }
    
    private IEnumerator FadeIn()
    {
        loadingCanvasGroup.gameObject.SetActive(true);
        loadingCanvasGroup.alpha = 0f;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        
        loadingCanvasGroup.alpha = 1f;
        DebugLog("Fade in complete");
    }
    
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        loadingCanvasGroup.alpha = 0f;
        DebugLog("Fade out complete");
    }
    
    private IEnumerator ProceduralWaveAnimation()
    {
         // Initialize random state
    // Initialize random state
    bool[,] cellState = new bool[gridSize, gridSize];
    for (int x = 0; x < gridSize; x++)
    {
        for (int y = 0; y < gridSize; y++)
        {
            cellState[x, y] = Random.value > 0.5f;
        }
    }
    
    while (loadingCanvasGroup.alpha > 0f)
    {
        // Apply cellular automata rules
        bool[,] newState = new bool[gridSize, gridSize];
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int neighbors = CountNeighbors(cellState, x, y);
                
                // Conway's Game of Life rules with modifications
                if (cellState[x, y])
                {
                    newState[x, y] = neighbors >= 2 && neighbors <= 3;
                }
                else
                {
                    newState[x, y] = neighbors == 3 || Random.value < currentProgress * 0.1f;
                }
            }
        }
        
        cellState = newState;
        
        // Visualize the cellular automata
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (proceduralGrid[x, y] == null) continue;
                
                if (cellState[x, y])
                {
                    // Alive cell
                    float intensity = Mathf.Lerp(0.5f, 1f, currentProgress);
                    int colorIndex = Mathf.FloorToInt(intensity * waveColors.Length);
                    colorIndex = Mathf.Clamp(colorIndex, 0, waveColors.Length - 1);
                    
                    Color targetColor = waveColors[colorIndex];
                    targetColor.a = intensity;
                    proceduralGrid[x, y].color = targetColor;
                }
                else
                {
                    // Dead cell
                    proceduralGrid[x, y].color = Color.clear;
                }
            }
        }
        
        yield return new WaitForSeconds(0.15f); // Slower update for cellular automata
    }
    
    }
    private int CountNeighbors(bool[,] state, int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
            
                int nx = x + dx;
                int ny = y + dy;
            
                if (nx >= 0 && nx < gridSize && ny >= 0 && ny < gridSize)
                {
                    if (state[nx, ny]) count++;
                }
            }
        }
        return count;
    }
    
    // Called by WFCBatchGenerator when generation starts
    public void OnGenerationStarted(int totalStructures)
    {
        hasGenerationStarted = true;
        expectedStructures = totalStructures > 0 ? totalStructures : 1;
        completedStructures = 0;
        DebugLog($"Generation started with {expectedStructures} structures");
    }
    
    // Called by WFCBatchGenerator as structures complete
    public void OnStructureGenerated(int completed, int total)
    {
        if (!hasGenerationStarted)
        {
            hasGenerationStarted = true;
            DebugLog("Generation started (detected from first structure completion)");
        }
        
        completedStructures = completed;
        expectedStructures = total > 0 ? total : expectedStructures;
        
        // Only update progress if it's a final structure completion
        // Don't override intermediate WFC progress updates
        float structureProgress = (float)completedStructures / expectedStructures;
        if (structureProgress > currentProgress)
        {
            currentProgress = Mathf.Clamp01(structureProgress);
            UpdateUI();
            DebugLog($"Structure progress: {completed}/{expectedStructures} - Final Progress: {currentProgress:F2}");
        }
        else
        {
            DebugLog($"Structure progress: {completed}/{expectedStructures} - Keeping current progress: {currentProgress:F2}");
        }
    }
    
    // Called when all generation is complete
    public void OnGenerationComplete()
    {
        isGenerationComplete = true;
        DebugLog("Generation confirmed complete! Loading screen will fade out.");
    }
    
    /// <summary>
    /// Manually set progress (0.0 to 1.0) - prevents going backwards
    /// </summary>
    public void SetProgress(float progress)
    {
        float newProgress = Mathf.Clamp01(progress);
        
        // Prevent progress from going backwards (except for resets)
        if (newProgress < currentProgress && currentProgress > 0.01f)
        {
            // Allow small backwards movement for smoothness, but log significant drops
            if (currentProgress - newProgress > 0.1f)
            {
                DebugLog($"WARNING: Progress trying to go backwards from {currentProgress:F2} to {newProgress:F2} - ignoring");
                return;
            }
        }
        
        currentProgress = newProgress;
        UpdateUI(); // Force UI update immediately
        
        if (!hasGenerationStarted && progress > 0)
        {
            hasGenerationStarted = true;
        }
        
        if (progress >= 1.0f && !isGenerationComplete)
        {
            OnGenerationComplete();
        }
        
        // Only log significant progress changes to reduce spam
        if (Time.frameCount % 120 == 0 || Mathf.Abs(currentProgress - progress) > 0.05f)
        {
            DebugLog($"Manual progress set to: {currentProgress:F2} ({currentProgress * 100:F0}%)");
        }
    }
    
    /// <summary>
    /// Manually hide the loading screen
    /// </summary>
    public void HideLoadingScreen()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        StartCoroutine(FadeOutAndHide());
    }
    
    private IEnumerator FadeOutAndHide()
    {
        yield return StartCoroutine(FadeOut());
        loadingCanvasGroup.gameObject.SetActive(false);
        DebugLog("Loading screen manually hidden");
    }
    
    /// <summary>
    /// Manual test method - call this from inspector or another script to test UI updates
    /// </summary>
    [ContextMenu("Test UI Update")]
    public void TestUIUpdate()
    {
        DebugLog("=== TESTING UI UPDATE ===");
        
        // Test with 50% progress
        currentProgress = 0.5f;
        hasGenerationStarted = true;
        expectedStructures = 10;
        completedStructures = 5;
        
        UpdateUI();
        
        DebugLog("=== END UI TEST ===");
    }
    
    /// <summary>
    /// Force show loading screen for testing
    /// </summary>
    [ContextMenu("Show Loading Screen")]
    public void ForceShowLoadingScreen()
    {
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.gameObject.SetActive(true);
            loadingCanvasGroup.alpha = 1f;
            
            // Test with some progress
            currentProgress = 0.75f;
            hasGenerationStarted = true;
            UpdateUI();
            
            DebugLog("Loading screen forced to show with 75% progress");
        }
    }
    
    /// <summary>
    /// Check if the loading screen is currently active
    /// </summary>
    public bool IsLoadingActive()
    {
        return loadingCanvasGroup != null && loadingCanvasGroup.gameObject.activeSelf && loadingCanvasGroup.alpha > 0;
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SameSceneLoading] {message}");
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}