using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WFCBatchGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private GameObject wfcControllerPrefab; // Assign your WFCController prefab
    [SerializeField] public int numberOfStructures = 10;
    [SerializeField] private float distributionRadius = 50f;
    [SerializeField] private float minDistanceBetweenStructures = 5f;
    [SerializeField] private bool useRandomSeeds = true;
    [SerializeField] private int baseSeed = 9;
    
    [Header("Structure Settings")]
    [SerializeField] private Vector3Int minStructureSize = new Vector3Int(8, 3, 8);
    [SerializeField] private Vector3Int maxStructureSize = new Vector3Int(10, 5, 10);
    [SerializeField] private bool enableChestSpawning = true;
    [SerializeField] private bool showGenerationProgress = true;
    
    [Header("Performance Settings")]
    [SerializeField] private bool generateAsynchronously = true;
    [SerializeField] private int structuresPerFrame = 1; // How many to generate per frame when async
    [SerializeField] private float frameTimeLimit = 0.016f; // ~60 FPS target
    [SerializeField] private bool enableLOD = false; // Future LOD system
    
    [Header("Distribution Settings")]
    [SerializeField] private bool avoidOverlapping = true;
    [SerializeField] private float fixedYPosition = -22f; // Fixed Y position for all structures
    
    [Header("Loading Screen Integration")]
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] public bool autoStartGeneration = true; // Made public so LoadingScreen can check it
    [SerializeField] private float delayBeforeStart = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDistributionGizmos = true;
    [SerializeField] private bool logGenerationStats = true;
    
    // Runtime data
    private List<GameObject> generatedStructures = new List<GameObject>();
    private List<Vector3> occupiedPositions = new List<Vector3>();
    private System.Diagnostics.Stopwatch generationTimer;
    private Coroutine currentGenerationCoroutine;
    
    // Events for external systems
    public System.Action<int, int> OnGenerationProgress; // current, total
    public System.Action OnGenerationComplete;
    public System.Action<GameObject> OnStructureGenerated;
    
    // Track actual completion, not just instantiation
    private int structuresFullyCompleted = 0;
    private int totalStructuresToGenerate = 0;

    void Start()
    {
        
        // Get seed and settings from GameSettings if available
        if (GameSettings.ShouldGenerate())
        {
            // Use the seed from the main menu
            baseSeed = GameSettings.GetSeed();
            numberOfStructures = GameSettings.numberOfStructures;
            Debug.Log($"Using seed from GameSettings: {baseSeed}");
            Debug.Log($"Using structure count from GameSettings: {numberOfStructures}");
            
            // Reset the generation flag
            GameSettings.ResetGenerationFlag();
        }
        else
        {
            Debug.Log($"No seed from GameSettings, using default: {baseSeed}");
        }
        
        // Initialize tracking
        totalStructuresToGenerate = numberOfStructures;
        structuresFullyCompleted = 0;
        
        // Auto-start generation if enabled
        if (autoStartGeneration)
        {
            StartCoroutine(AutoStartGeneration());
        }
    }
    
    private IEnumerator AutoStartGeneration()
    {
        yield return new WaitForSeconds(delayBeforeStart);
        
        if (useLoadingScreen)
        {
            // Let the loading screen handle starting generation
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.StartLoadingForGenerator(this);
            }
            else
            {
                Debug.LogWarning("LoadingScreenManager not found! Starting generation without loading screen.");
                GenerateStructures();
            }
        }
        else
        {
            // Start generation directly
            GenerateStructures();
        }
    }

    #region PUBLIC METHODS

    /// <summary>
    /// Main method to generate all structures
    /// </summary>
    public void setSeed(int seed)
    {
        this.baseSeed = seed;
    }
    
    public void GenerateStructures()
    {
        if (currentGenerationCoroutine != null)
        {
            StopCoroutine(currentGenerationCoroutine);
        }
        
        // Reset completion tracking
        structuresFullyCompleted = 0;
        totalStructuresToGenerate = numberOfStructures;
        
        ClearExistingStructures();
        
        // ALWAYS notify loading screen and connect events
        if (LoadingScreenManager.Instance != null)
        {
            // Disconnect any existing events first
            OnGenerationProgress -= LoadingScreenManager.Instance.OnStructureGenerated;
            OnGenerationComplete -= LoadingScreenManager.Instance.OnGenerationComplete;
            
            // Notify that generation is starting
            LoadingScreenManager.Instance.OnGenerationStarted(totalStructuresToGenerate);
            
            // Connect events for progress updates
            OnGenerationProgress += LoadingScreenManager.Instance.OnStructureGenerated;
            OnGenerationComplete += LoadingScreenManager.Instance.OnGenerationComplete;
            
            Debug.Log($"[WFCBatchGenerator] Connected to LoadingScreenManager for {totalStructuresToGenerate} structures");
        }
        else
        {
            Debug.LogWarning("[WFCBatchGenerator] LoadingScreenManager.Instance is null!");
        }
        
        if (generateAsynchronously)
        {
            currentGenerationCoroutine = StartCoroutine(GenerateStructuresAsync());
        }
        else
        {
            GenerateStructuresSync();
        }
    }

    /// <summary>
    /// Clear all generated structures
    /// </summary>
    public void ClearExistingStructures()
    {
        foreach (GameObject structure in generatedStructures)
        {
            if (structure != null)
            {
                DestroyImmediate(structure);
            }
        }
        
        generatedStructures.Clear();
        occupiedPositions.Clear();
    }

    /// <summary>
    /// Add a new structure at runtime
    /// </summary>
    public void AddStructureAtPosition(Vector3 position)
    {
        if (IsValidPosition(position))
        {
            StartCoroutine(GenerateSingleStructureAsync(position));
        }
    }

    /// <summary>
    /// Get statistics about the current generation
    /// </summary>
    public GenerationStats GetGenerationStats()
    {
        return new GenerationStats
        {
            totalStructures = generatedStructures.Count,
            successfulPlacements = occupiedPositions.Count,
            averageDistance = CalculateAverageDistance(),
            distributionEfficiency = (float)occupiedPositions.Count / numberOfStructures
        };
    }

    #endregion

    #region SYNCHRONOUS GENERATION

    /// <summary>
    /// Generate all structures synchronously (blocking)
    /// </summary>
    private void GenerateStructuresSync()
    {
        generationTimer = System.Diagnostics.Stopwatch.StartNew();
        
        if (logGenerationStats)
        {
            Debug.Log($"Starting synchronous generation of {numberOfStructures} structures...");
        }

        List<Vector3> positions = GenerateValidPositions();
        
        for (int i = 0; i < positions.Count && i < numberOfStructures; i++)
        {
            CreateStructureAtPosition(positions[i], i);
            structuresFullyCompleted++;
            OnGenerationProgress?.Invoke(structuresFullyCompleted, numberOfStructures);
        }

        LogGenerationComplete();
        OnGenerationComplete?.Invoke();
    }

    #endregion

    #region ASYNCHRONOUS GENERATION

    /// <summary>
    /// Generate structures asynchronously to maintain framerate
    /// </summary>
    private IEnumerator GenerateStructuresAsync()
    {
        generationTimer = System.Diagnostics.Stopwatch.StartNew();
        
        if (logGenerationStats)
        {
            Debug.Log($"Starting asynchronous generation of {totalStructuresToGenerate} structures...");
        }

        // Generate positions first
        List<Vector3> positions = GenerateValidPositions();
        yield return null; // Yield after position calculation

        if (positions.Count == 0)
        {
            Debug.LogWarning("No valid positions found for structure generation!");
            LogGenerationComplete();
            OnGenerationComplete?.Invoke();
            yield break;
        }

        // Start all structure generations without waiting for each to complete
        List<Coroutine> generationCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < positions.Count && i < totalStructuresToGenerate; i++)
        {
            if (logGenerationStats)
            {
                Debug.Log($"Starting structure {i + 1}/{totalStructuresToGenerate} at position {positions[i]}");
            }

            // Start generation but don't wait for it to complete
            Coroutine structureCoroutine = StartCoroutine(GenerateSingleStructureAsync(positions[i], i));
            generationCoroutines.Add(structureCoroutine);
            
            // Small delay between starting each structure to prevent frame spikes
            yield return new WaitForSeconds(0.1f);
        }

        // Wait for all structures to complete
        if (logGenerationStats)
        {
            Debug.Log($"All {generationCoroutines.Count} structures started. Waiting for completion...");
        }

        // Wait for actual completion, not just coroutine finish
        while (structuresFullyCompleted < totalStructuresToGenerate)
        {
            yield return null;
        }

        LogGenerationComplete();
        OnGenerationComplete?.Invoke();
    }

    /// <summary>
    /// Generate a single structure asynchronously with smooth progress updates
    /// </summary>
    private IEnumerator GenerateSingleStructureAsync(Vector3 position, int index = -1)
    {
        if (logGenerationStats)
        {
            Debug.Log($"Starting structure generation at {position} with index {index}");
        }

        GameObject structure = CreateStructureAtPosition(position, index);
        
        if (structure != null)
        {
            if (logGenerationStats)
            {
                Debug.Log($"Structure instantiated successfully: {structure.name}");
            }

            WFCController controller = structure.GetComponent<WFCController>();
            
            if (controller != null)
            {
                if (logGenerationStats)
                {
                    Debug.Log($"Starting WFC generation for {structure.name}");
                }

                // Start a coroutine for time-based progress updates as fallback
                Coroutine progressCoroutine = StartCoroutine(UpdateProgressDuringGeneration(index));

                // Wait for WFC completion (this now includes its own progress updates)
                yield return StartCoroutine(WaitForWFCCompletion(controller));

                // Stop the fallback progress updates
                if (progressCoroutine != null)
                {
                    StopCoroutine(progressCoroutine);
                }

                if (logGenerationStats)
                {
                    Debug.Log($"WFC generation completed for {structure.name}");
                }
                
                // Spawn chests only once, after WFC is complete
                if (enableChestSpawning)
                {
                    controller.SpawnChests();
                    if (logGenerationStats)
                    {
                        Debug.Log($"Chests spawned for {structure.name}");
                    }
                }
            }
            else
            {
                Debug.LogError($"No WFCController found on instantiated structure: {structure.name}");
            }
        }
        else
        {
            Debug.LogError($"Failed to create structure at position {position}");
        }
        
        // Only count as completed AFTER WFC is fully done
        structuresFullyCompleted++;
        
        // Notify progress with detailed logging
        Debug.Log($"[WFCBatchGenerator] Structure {structuresFullyCompleted}/{totalStructuresToGenerate} FULLY completed");
        
        if (OnGenerationProgress != null)
        {
            OnGenerationProgress.Invoke(structuresFullyCompleted, totalStructuresToGenerate);
            Debug.Log($"[WFCBatchGenerator] Progress event fired: {structuresFullyCompleted}/{totalStructuresToGenerate}");
        }
        else
        {
            Debug.LogWarning("[WFCBatchGenerator] OnGenerationProgress event is null!");
        }
        
        OnStructureGenerated?.Invoke(structure);
        
        yield return null;
    }
    
    /// <summary>
    /// Fallback time-based progress updates during WFC generation
    /// </summary>
    private IEnumerator UpdateProgressDuringGeneration(int structureIndex)
    {
        float startTime = Time.time;
        float estimatedDuration = 15f; // Estimate 15 seconds per structure
        float lastUpdateTime = 0f;
        
        while (true)
        {
            float elapsed = Time.time - startTime;
            
            // Only update every 0.5 seconds to avoid too many updates
            if (elapsed - lastUpdateTime >= 0.5f)
            {
                lastUpdateTime = elapsed;
                
                float wfcProgress = Mathf.Clamp01(elapsed / estimatedDuration);
                
                // Calculate smooth overall progress
                float structureBaseProgress = (float)structuresFullyCompleted / totalStructuresToGenerate;
                float currentStructureProgress = wfcProgress / totalStructuresToGenerate;
                float overallProgress = structureBaseProgress + currentStructureProgress;
                
                // Update loading screen
                if (LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.SetProgress(overallProgress);
                }
                
                if (logGenerationStats && Mathf.FloorToInt(elapsed) % 3 == 0 && elapsed - lastUpdateTime < 0.6f) // Log every 3 seconds
                {
                    Debug.Log($"[Time-Based Progress] Structure {structureIndex + 1}/{totalStructuresToGenerate}: WFC {wfcProgress:F2} | Overall: {overallProgress:F2} ({overallProgress * 100:F0}%)");
                }
            }
            
            yield return new WaitForSeconds(0.5f); // Update every half second
        }
    }

    /// <summary>
    /// Wait for WFC controller to finish generation - IMPROVED VERSION WITH PROGRESS UPDATES
    /// </summary>
    private IEnumerator WaitForWFCCompletion(WFCController controller)
    {
        if (logGenerationStats)
        {
            Debug.Log($"Starting WFC completion wait for {controller.name}");
        }

        // Configure controller for non-visual generation for performance
        bool originalSeeUpdates = controller.seeUpdates;
        controller.seeUpdates = showGenerationProgress;
        
        // Start the test
        controller.Test();
        
        // Wait for the WFC model to be created
        int maxWaitFrames = 1000;
        int waitFrames = 0;
        
        while (controller.wfcModel == null && waitFrames < maxWaitFrames)
        {
            waitFrames++;
            yield return null;
        }
        
        if (controller.wfcModel == null)
        {
            Debug.LogError($"WFC Model failed to initialize for {controller.name} after {maxWaitFrames} frames");
            yield break;
        }
        
        // Wait for WFC to fully collapse WITH PROGRESS UPDATES
        waitFrames = 0;
        int totalCells = controller.size.x * controller.size.y * controller.size.z;
        int lastReportedProgress = -1;
        
        while (!controller.wfcModel.IsCollapsed() && waitFrames < maxWaitFrames)
        {
            waitFrames++;
            
            // Calculate WFC progress and update loading screen every 30 frames
            if (waitFrames % 30 == 0 && controller.wfcModel != null)
            {
                try
                {
                    // Get collapsed cell count (this depends on your WFC implementation)
                    int collapsedCells = 0;
                    var field = controller.wfcModel.GetType().GetField("collapsedCells");
                    if (field != null)
                    {
                        collapsedCells = (int)field.GetValue(controller.wfcModel);
                    }
                    else
                    {
                        // Fallback: estimate based on wait frames
                        collapsedCells = Mathf.RoundToInt((float)waitFrames / maxWaitFrames * totalCells);
                    }
                    
                    float wfcProgress = Mathf.Clamp01((float)collapsedCells / totalCells);
                    
                    // Calculate overall progress: 
                    // (completed structures + current structure WFC progress) / total structures
                    float structureBaseProgress = (float)structuresFullyCompleted / totalStructuresToGenerate;
                    float currentStructureProgress = wfcProgress / totalStructuresToGenerate;
                    float overallProgress = structureBaseProgress + currentStructureProgress;
                    
                    int progressPercent = Mathf.RoundToInt(overallProgress * 100);
                    
                    // Only update if progress changed significantly
                    if (progressPercent != lastReportedProgress)
                    {
                        lastReportedProgress = progressPercent;
                        
                        // Update loading screen with intermediate progress
                        if (LoadingScreenManager.Instance != null)
                        {
                            LoadingScreenManager.Instance.SetProgress(overallProgress);
                        }
                        
                        if (logGenerationStats)
                        {
                            Debug.Log($"WFC Progress for {controller.name}: {wfcProgress:F2} | Overall: {overallProgress:F2} ({progressPercent}%)");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    if (logGenerationStats)
                    {
                        Debug.LogWarning($"Could not get WFC progress: {e.Message}");
                    }
                }
            }
            
            yield return null;
        }
        
        if (!controller.wfcModel.IsCollapsed())
        {
            Debug.LogWarning($"WFC Model did not fully collapse for {controller.name} after {maxWaitFrames} frames");
        }
        
        // Wait additional frames to ensure meshes are fully instantiated
        yield return null;
        yield return null;
        
        // Restore original settings
        controller.seeUpdates = originalSeeUpdates;
        
        if (logGenerationStats)
        {
            Debug.Log($"WFC completion finished for {controller.name} after {waitFrames} frames. Instantiated meshes: {controller.instantiatedMeshes.Count}");
        }
    }

    #endregion

    #region POSITION GENERATION

    /// <summary>
    /// Generate valid positions within the distribution circle
    /// </summary>
    private List<Vector3> GenerateValidPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> candidates = GeneratePositionCandidates();
        
        // Filter candidates based on constraints
        foreach (Vector3 candidate in candidates)
        {
            if (IsValidPosition(candidate) && positions.Count < numberOfStructures)
            {
                positions.Add(candidate);
                occupiedPositions.Add(candidate);
            }
        }
        
        if (logGenerationStats)
        {
            Debug.Log($"Generated {positions.Count} valid positions out of {numberOfStructures} requested");
        }
        
        return positions;
    }

    /// <summary>
    /// Generate position candidates using Poisson disk sampling for better distribution
    /// </summary>
    private List<Vector3> GeneratePositionCandidates()
    {
        List<Vector3> candidates = new List<Vector3>();
        
        if (logGenerationStats)
        {
            Debug.Log($"Generating position candidates. AvoidOverlapping={avoidOverlapping}");
        }
        
        if (avoidOverlapping && minDistanceBetweenStructures > 0)
        {
            // Use Poisson disk sampling for better distribution
            candidates = PoissonDiskSampling(distributionRadius, minDistanceBetweenStructures, numberOfStructures * 3);
            if (logGenerationStats)
            {
                Debug.Log($"Poisson disk sampling generated {candidates.Count} candidates");
            }
        }
        else
        {
            // Simple random distribution
            for (int i = 0; i < numberOfStructures * 2; i++) // Generate extra candidates
            {
                Vector2 randomPoint = Random.insideUnitCircle * distributionRadius;
                Vector3 worldPos = transform.position + new Vector3(randomPoint.x, fixedYPosition, randomPoint.y);
                candidates.Add(worldPos);
            }
            if (logGenerationStats)
            {
                Debug.Log($"Random distribution generated {candidates.Count} candidates");
            }
        }
        
        return candidates;
    }

    /// <summary>
    /// Poisson disk sampling for even distribution
    /// </summary>
    private List<Vector3> PoissonDiskSampling(float radius, float minDistance, int maxCandidates)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> activeList = new List<Vector3>();
        
        // Add initial point at fixed Y position
        Vector3 firstPoint = new Vector3(
            transform.position.x + Random.Range(-radius * 0.5f, radius * 0.5f), 
            fixedYPosition, 
            transform.position.z + Random.Range(-radius * 0.5f, radius * 0.5f)
        );
        points.Add(firstPoint);
        activeList.Add(firstPoint);
        
        int attempts = 0;
        while (activeList.Count > 0 && points.Count < maxCandidates && attempts < maxCandidates * 10)
        {
            attempts++;
            int randomIndex = Random.Range(0, activeList.Count);
            Vector3 activePoint = activeList[randomIndex];
            
            bool foundValidPoint = false;
            
            // Try to generate points around the active point
            for (int i = 0; i < 10; i++) // Max attempts per active point
            {
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDistance, minDistance * 2f);
                
                Vector3 candidate = new Vector3(
                    activePoint.x + Mathf.Cos(angle) * distance,
                    fixedYPosition, // Always use fixed Y position
                    activePoint.z + Mathf.Sin(angle) * distance
                );
                
                if (IsPointInCircle(candidate, transform.position, radius) && 
                    IsPointValidForSampling(candidate, points, minDistance))
                {
                    points.Add(candidate);
                    activeList.Add(candidate);
                    foundValidPoint = true;
                    break;
                }
            }
            
            if (!foundValidPoint)
            {
                activeList.RemoveAt(randomIndex);
            }
        }
        
        return points;
    }

    private bool IsPointInCircle(Vector3 point, Vector3 center, float radius)
    {
        Vector2 flatPoint = new Vector2(point.x, point.z);
        Vector2 flatCenter = new Vector2(center.x, center.z);
        return Vector2.Distance(flatPoint, flatCenter) <= radius;
    }

    private bool IsPointValidForSampling(Vector3 candidate, List<Vector3> existingPoints, float minDistance)
    {
        foreach (Vector3 point in existingPoints)
        {
            if (Vector3.Distance(candidate, point) < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region POSITION VALIDATION

    /// <summary>
    /// Check if a position is valid for structure placement
    /// </summary>
    private bool IsValidPosition(Vector3 position)
    {
        // Check if within distribution radius
        Vector2 flatPos = new Vector2(position.x, position.z);
        Vector2 flatCenter = new Vector2(transform.position.x, transform.position.z);
        if (Vector2.Distance(flatPos, flatCenter) > distributionRadius)
        {
            if (logGenerationStats)
                Debug.Log($"Position {position} rejected: Outside radius ({Vector2.Distance(flatPos, flatCenter)} > {distributionRadius})");
            return false;
        }

        // Check minimum distance from existing structures
        if (avoidOverlapping)
        {
            foreach (Vector3 occupied in occupiedPositions)
            {
                if (Vector3.Distance(position, occupied) < minDistanceBetweenStructures)
                {
                    if (logGenerationStats)
                        Debug.Log($"Position {position} rejected: Too close to existing structure ({Vector3.Distance(position, occupied)} < {minDistanceBetweenStructures})");
                    return false;
                }
            }
        }

        if (logGenerationStats)
            Debug.Log($"Position {position} accepted as valid");
        return true;
    }

    #endregion

    #region STRUCTURE CREATION

    /// <summary>
    /// Generate a random structure size within the specified range
    /// </summary>
    private Vector3Int GenerateRandomStructureSize()
    {
        int x = Random.Range(minStructureSize.x, maxStructureSize.x + 1);
        int y = Random.Range(minStructureSize.y, maxStructureSize.y + 1);
        int z = Random.Range(minStructureSize.z, maxStructureSize.z + 1);
        
        Vector3Int randomSize = new Vector3Int(x, y, z);
        
        if (logGenerationStats)
        {
            Debug.Log($"Generated random structure size: {randomSize}");
        }
        
        return randomSize;
    }

    /// <summary>
    /// Create a structure at the specified position
    /// </summary>
    private GameObject CreateStructureAtPosition(Vector3 position, int index)
    {
        if (wfcControllerPrefab == null)
        {
            Debug.LogError("WFC Controller Prefab is not assigned!");
            return null;
        }

        // Ensure Y position is fixed
        position.y = fixedYPosition;

        // Instantiate structure
        GameObject structure = Instantiate(wfcControllerPrefab, position, Quaternion.identity, transform);
        structure.name = $"WFC_Structure_{index:000}";

        // Configure WFC Controller
        WFCController controller = structure.GetComponent<WFCController>();
        if (controller != null)
        {
            // Set random seed if enabled
            if (useRandomSeeds)
            {
                controller.SetSeed(baseSeed + index);
            }
            
            // Set random structure size
            Vector3Int randomSize = GenerateRandomStructureSize();
            controller.size = randomSize;
            
            if (logGenerationStats)
            {
                Debug.Log($"Structure {structure.name} configured with size: {randomSize}");
            }
            
            // Configure generation settings
            controller.seeUpdates = showGenerationProgress;
        }

        generatedStructures.Add(structure);
        return structure;
    }

    #endregion

    #region UTILITY METHODS

    private float CalculateAverageDistance()
    {
        if (occupiedPositions.Count < 2) return 0f;
        
        float totalDistance = 0f;
        int comparisons = 0;
        
        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            for (int j = i + 1; j < occupiedPositions.Count; j++)
            {
                totalDistance += Vector3.Distance(occupiedPositions[i], occupiedPositions[j]);
                comparisons++;
            }
        }
        
        return comparisons > 0 ? totalDistance / comparisons : 0f;
    }

    private void LogGenerationComplete()
    {
        if (logGenerationStats && generationTimer != null)
        {
            generationTimer.Stop();
            var stats = GetGenerationStats();
            
            Debug.Log($"=== WFC BATCH GENERATION COMPLETE ===\n" +
                     $"Total Time: {generationTimer.ElapsedMilliseconds} ms\n" +
                     $"Structures Generated: {stats.totalStructures}/{totalStructuresToGenerate}\n" +
                     $"Success Rate: {stats.distributionEfficiency:P1}\n" +
                     $"Average Distance: {stats.averageDistance:F2} units\n" +
                     $"Time per Structure: {generationTimer.ElapsedMilliseconds / Mathf.Max(1, stats.totalStructures):F1} ms");
        }
        
        // Fire completion event with detailed logging
        Debug.Log($"[WFCBatchGenerator] Generation complete! Firing OnGenerationComplete event...");
        
        if (OnGenerationComplete != null)
        {
            OnGenerationComplete.Invoke();
            Debug.Log($"[WFCBatchGenerator] OnGenerationComplete event fired successfully");
        }
        else
        {
            Debug.LogWarning("[WFCBatchGenerator] OnGenerationComplete event is null!");
        }
    }

    #endregion

    #region GIZMOS AND DEBUG

    private void OnDrawGizmosSelected()
    {
        if (!showDistributionGizmos) return;

        // Draw distribution circle
        Gizmos.color = Color.cyan;
        DrawCircle(transform.position, distributionRadius, 64);

        // Draw minimum distance circles around existing structures
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in occupiedPositions)
            {
                DrawCircle(pos, minDistanceBetweenStructures, 16);
            }

            // Draw structure positions
            Gizmos.color = Color.green;
            foreach (Vector3 pos in occupiedPositions)
            {
                Gizmos.DrawWireCube(pos, Vector3.one * 2f);
            }
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    #endregion

    #region DATA STRUCTURES

    [Serializable]
    public struct GenerationStats
    {
        public int totalStructures;
        public int successfulPlacements;
        public float averageDistance;
        public float distributionEfficiency;
    }

    #endregion
}