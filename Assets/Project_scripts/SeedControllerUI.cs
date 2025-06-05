using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // For Button and Toggle components
using System.Collections;
using System.Collections.Generic;

public class SeedInputManager : MonoBehaviour
{
    [Header("Seed Controls")]
    [SerializeField] private TMP_InputField seedInputField; // Reference to the TMP InputField
    [SerializeField] private Button confirmButton; // Reference to the Confirm Button
    [SerializeField] private WFCController wfcController; // Reference to the WFCController
    [SerializeField] private WFCBatchGenerator batchGenerator;
    [Header("Additional Controls")]
    [SerializeField] private Toggle booleanToggle; // Reference to the Toggle component
    [SerializeField] private TMP_Dropdown comboBoxDropdown; // Reference to the TMP Dropdown component
    
    [SerializeField] OrbitCameraTopDown orbitCameraTopDown; // Reference to the OrbitCameraTopDown component
    [Header("Dropdown Options")]
    [SerializeField] private List<string> dropdownOptions = new List<string> 
    { 
        "8x3x8", 
        "10x5x10", 
        "5x3x5"
    }; // Configurable dropdown options
    
    [Header("Camera Tour Settings")]
    [SerializeField] private float tourDuration = 5f; // Time to stay at each structure
    [SerializeField] private float transitionSpeed = 2f; // Speed of camera movement between structures
    
    public Dictionary<int,Vector3Int> dropdownOptionsDict = new Dictionary<int, Vector3Int>
    {
        { 0, new Vector3Int(8, 3, 8) },
        { 1, new Vector3Int(10, 5, 10) },
        { 2, new Vector3Int(5, 3, 5) }
    };
    
    // Camera tour variables
    private Coroutine cameraTourCoroutine;
    private bool isTourActive = false;
    private Transform originalCameraTarget;
    private float originalDistance;
    private float originalDownwardAngle;
    private bool originalAutoOrbit;

    // Properties to access the current values
    public bool ToggleValue => booleanToggle != null ? booleanToggle.isOn : false;
    public int SelectedDropdownIndex => comboBoxDropdown != null ? comboBoxDropdown.value : 0;
    public string SelectedDropdownOption => comboBoxDropdown != null && comboBoxDropdown.options.Count > 0 
        ? comboBoxDropdown.options[comboBoxDropdown.value].text : "";

    private void Start()
    {
        StoreCameraDefaults();
        InitializeComponents();
        
    }

    

    private void StoreCameraDefaults()
    {
        if (orbitCameraTopDown != null)
        {
            originalCameraTarget = GameObject.Find("Cube").transform;
            originalDistance = 150;
            originalDownwardAngle = 60;
            originalAutoOrbit = true;
        }
    }

    private void InitializeComponents()
    {
        // Initialize confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
        }

        // Initialize toggle
        if (booleanToggle != null)
        {
            booleanToggle.onValueChanged.AddListener(OnToggleChanged);
            // Set default value if needed
            booleanToggle.isOn = false;
        }

        // Initialize dropdown
        if (comboBoxDropdown != null)
        {
            SetupDropdown();
            comboBoxDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        // Optionally, set a default seed in the input field
        if (seedInputField != null && wfcController != null)
        {
            // You can set a default seed here if needed
            // seedInputField.text = wfcController.GetSeed().ToString();
        }
    }

    private void SetupDropdown()
    {
        if (comboBoxDropdown == null) return;

        // Clear existing options
        comboBoxDropdown.ClearOptions();

        // Add the dropdown options
        comboBoxDropdown.AddOptions(dropdownOptions);

        // Set default selection
        comboBoxDropdown.value = 0;
        comboBoxDropdown.RefreshShownValue();
    }

    private void OnConfirm()
    {
        wfcController.ClearChests();
        wfcController.ClearMeshes();
        batchGenerator.ClearExistingStructures();
        
        if (seedInputField != null && wfcController != null && batchGenerator != null)
        {

            if (ToggleValue && int.TryParse(seedInputField.text, out int batchSeed))
            {
                batchGenerator.setSeed(batchSeed);
                batchGenerator.GenerateStructures();
                
                // Start camera tour after generation
                StartCoroutine(WaitAndStartTour());
            }
            
            else if (!ToggleValue && int.TryParse(seedInputField.text, out int newSeed))
            {
                wfcController.SetSeed(newSeed);
                wfcController.setSize(GetSelectedSize());
                wfcController.TestWithChests();
                
                // Set camera to orbit the single generated structure
                RestoreCameraDefaults();
                
                Debug.Log($"New seed applied: {newSeed}");
            }
            else if (!ToggleValue)
            {
                int seed = wfcController.GetSeed();
                wfcController.SetSeed(++seed);
                
                seedInputField.text = seed.ToString();
                
                wfcController.TestWithChests();
                
                StoreCameraDefaults();
            }

            // Log the current state of toggle and dropdown
            Debug.Log($"Toggle Value: {ToggleValue}");
            Debug.Log($"Selected Dropdown Option: {SelectedDropdownOption} (Index: {SelectedDropdownIndex})");
        }
    }

    private IEnumerator WaitAndStartTour()
    {
        // Wait a frame to ensure structures are generated
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f); // Additional wait to ensure everything is ready
        
        if (ToggleValue)
        {
            StartCameraTour();
        }
    }

    private void OnToggleChanged(bool value)
    {
        Debug.Log($"Toggle changed to: {value}");
        
        if (value)
        {
            
            // If batch generator has children, start the tour
            if (batchGenerator != null && batchGenerator.transform.childCount > 0)
            {
                StartCameraTour();
            }
        }
        else
        {
            // Toggle is OFF - stop tour and restore normal camera behavior
            StopCameraTour();
            RestoreCameraDefaults();
        }
    }

    private void StartCameraTour()
    {
        if (batchGenerator == null || orbitCameraTopDown == null)
        {
            Debug.LogWarning("BatchGenerator or OrbitCamera reference is missing!");
            return;
        }

        // Stop any existing tour
        StopCameraTour();
        
        // Start the new tour
        cameraTourCoroutine = StartCoroutine(CameraTourCoroutine());
    }

    private void StopCameraTour()
    {
        if (cameraTourCoroutine != null)
        {
            StopCoroutine(cameraTourCoroutine);
            cameraTourCoroutine = null;
        }
        isTourActive = false;
        
        // Clean up any structure target GameObjects
        CleanupStructureTargets();
    }

    private void CleanupStructureTargets()
    {
        // Find and destroy all structure target GameObjects
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("StructureTarget_") || 
                obj.name == "TempCameraTarget" || 
                obj.name == "SingleStructureTarget")
            {
                Destroy(obj);
            }
        }
    }

    private IEnumerator SetCameraToSingleStructure()
    {
        // Wait a moment for the structure to be generated
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
        // Find the generated structure (assuming it's a child of wfcController)
        Transform generatedStructure = FindSingleGeneratedStructure();
        
        if (generatedStructure != null)
        {
            Vector3 structureCenter = CalculateStructureCenter(generatedStructure);
            
            // Create a target GameObject at the structure center
            GameObject singleTarget = new GameObject("SingleStructureTarget");
            singleTarget.transform.position = structureCenter;
            singleTarget.transform.SetParent(generatedStructure);
            
            // Set camera to orbit this target with manual control
            orbitCameraTopDown.target = singleTarget.transform;
            orbitCameraTopDown.autoOrbit = false; // Manual control for single structure
            
            Debug.Log($"Camera set to orbit single structure: {generatedStructure.name} at center: {structureCenter}");
        }
        else
        {
            Debug.LogWarning("Could not find generated structure for camera targeting");
        }
    }

    private Transform FindSingleGeneratedStructure()
    {
        // First try to find structures under the wfcController
        if (wfcController != null)
        {
            // Look for the most recently added child that likely contains the generated structure
            for (int i = wfcController.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = wfcController.transform.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    // Check if this child has renderers (indicating it's a structure)
                    Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        return child;
                    }
                }
            }
        }
        
        // Alternative: Look for any GameObject with specific naming patterns or components
        // You might need to adjust this based on how your WFCController creates structures
        return null;
    }

    private IEnumerator CameraTourCoroutine()
    {
        isTourActive = true;
        
        while (isTourActive && ToggleValue)
        {
            Transform[] children = GetBatchGeneratorChildren();
            
            if (children.Length == 0)
            {
                Debug.Log("No structures found for camera tour. Waiting...");
                yield return new WaitForSeconds(1f);
                continue;
            }

            for (int i = 0; i < children.Length && isTourActive && ToggleValue; i++)
            {
                Transform targetChild = children[i];
                if (targetChild == null) continue;

                Debug.Log($"Camera touring structure {i + 1}/{children.Length}: {targetChild.name}");

                // Calculate the center of the structure and move camera to focus on it
                Vector3 structureCenter = CalculateStructureCenter(targetChild);
                yield return StartCoroutine(MoveCameraToTarget(targetChild, structureCenter));

                // Stay at this structure for the specified duration
                float elapsedTime = 0f;
                while (elapsedTime < tourDuration && isTourActive && ToggleValue)
                {
                    // Keep the camera focused on the structure center
                    // (No need to update target as it's already set to the correct position)
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
        }
        
        isTourActive = false;
    }

    private Transform[] GetBatchGeneratorChildren()
    {
        if (batchGenerator == null) return new Transform[0];

        List<Transform> children = new List<Transform>();
        for (int i = 0; i < batchGenerator.transform.childCount; i++)
        {
            Transform child = batchGenerator.transform.GetChild(i);
            if (child.gameObject.activeInHierarchy)
            {
                children.Add(child);
            }
        }
        
        return children.ToArray();
    }

    private Vector3 CalculateStructureCenter(Transform structure)
    {
        // Get all renderers in the structure and its children
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // If no renderers found, use the transform position
            Debug.LogWarning($"No renderers found in structure {structure.name}, using transform position");
            return structure.position;
        }

        // Calculate the combined bounds of all renderers
        Bounds combinedBounds = renderers[0].bounds;
        
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        Vector3 center = combinedBounds.center;
        Debug.Log($"Structure {structure.name} center calculated at: {center}");
        return center;
    }

    private IEnumerator MoveCameraToTarget(Transform targetTransform, Vector3 targetCenter)
    {
        if (targetTransform == null) yield break;
        
        // Get the current target position
        Vector3 startPosition = (orbitCameraTopDown.target != null) ? 
            orbitCameraTopDown.target.position : orbitCameraTopDown.transform.position;
        
        float journey = 0f;
        
        // Create a temporary GameObject to serve as the camera target during transition
        GameObject tempTarget = new GameObject("TempCameraTarget");
        tempTarget.transform.position = startPosition;
        
        // Set camera to follow the temporary target
        orbitCameraTopDown.target = tempTarget.transform;
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * transitionSpeed;
            tempTarget.transform.position = Vector3.Lerp(startPosition, targetCenter, journey);
            yield return null;
        }
        
        // Create a permanent target GameObject at the structure center for orbiting
        GameObject structureTarget = new GameObject($"StructureTarget_{targetTransform.name}");
        structureTarget.transform.position = targetCenter;
        structureTarget.transform.SetParent(targetTransform); // Parent it to the structure
        
        // Set final target and cleanup temporary target
        orbitCameraTopDown.target = structureTarget.transform;
        Destroy(tempTarget);
    }

    private void RestoreCameraDefaults()
    {
        if (orbitCameraTopDown != null)
        {
            orbitCameraTopDown.target = originalCameraTarget;
            orbitCameraTopDown.distance = originalDistance;
            orbitCameraTopDown.downwardAngle = originalDownwardAngle;
            orbitCameraTopDown.autoOrbit = originalAutoOrbit;
            
            Debug.Log("Camera defaults restored");
        }
    }

    private void OnDropdownChanged(int index)
    {
        string selectedOption = SelectedDropdownOption;
        Debug.Log($"Dropdown changed to: {selectedOption} (Index: {index})");
    }
    
    public Vector3Int GetSelectedSize()
    {
        if (dropdownOptionsDict.TryGetValue(SelectedDropdownIndex, out Vector3Int size))
        {
            Debug.Log($"Selected size: {size}");
            return size;
        }
        return Vector3Int.zero; // Default value if not found
    }
    
    public bool GetToggleValue()
    {
        return ToggleValue;
    }

    public string GetSelectedDropdownOption()
    {
        return SelectedDropdownOption;
    }

    public int GetSelectedDropdownIndex()
    {
        return SelectedDropdownIndex;
    }

    // Public method to programmatically set dropdown options
    public void SetDropdownOptions(List<string> newOptions)
    {
        if (newOptions == null || newOptions.Count == 0) return;

        dropdownOptions = new List<string>(newOptions);
        SetupDropdown();
    }

    // Public method to programmatically set toggle value
    public void SetToggleValue(bool value)
    {
        if (booleanToggle != null)
        {
            booleanToggle.isOn = value;
        }
    }

    // Public method to programmatically set dropdown selection
    public void SetDropdownSelection(int index)
    {
        if (comboBoxDropdown != null && index >= 0 && index < comboBoxDropdown.options.Count)
        {
            comboBoxDropdown.value = index;
            comboBoxDropdown.RefreshShownValue();
        }
    }

    // Public methods to control the camera tour
    public void SetTourDuration(float duration)
    {
        tourDuration = Mathf.Max(0.1f, duration);
    }

    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0.1f, speed);
    }

    public bool IsTourActive()
    {
        return isTourActive;
    }

    private void OnDestroy()
    {
        // Stop camera tour
        StopCameraTour();
        
        // Remove all listeners to prevent memory leaks
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
        }

        if (booleanToggle != null)
        {
            booleanToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        if (comboBoxDropdown != null)
        {
            comboBoxDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }
    }
}