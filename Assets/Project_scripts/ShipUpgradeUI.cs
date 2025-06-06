using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System;

public class ShipUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas existingCanvas; // Drag your existing Canvas here
    public Button openUpgradeButton; // Drag your button that should open upgrades
    public Button closeButton;
    private GameObject upgradePanel; // Will be created procedurally
    public Transform contentParent;
    
    [Header("Upgrade Settings")]
    public int playerCurrency = 1000; // Starting currency
    public TextMeshProUGUI currencyText;
    
    // Auto-detected upgradeable stats
    private Dictionary<string, UpgradeableStatInfo> upgradeableStats = new Dictionary<string, UpgradeableStatInfo>();
    private ShipController shipController;
    private bool isUIOpen = false;
    
    [System.Serializable]
    public class UpgradeableStatInfo
    {
        public string displayName;
        public FieldInfo fieldInfo;
        public float baseValue;
        public float currentValue;
        public int upgradeLevel;
        public float upgradeMultiplier;
        public int baseCost;
        public int currentCost;
        public float minValue;
        public float maxValue;
        public string description;
    }
    
    void Start()
    {
        shipController = FindObjectOfType<ShipController>();
        if (shipController == null)
        {
            Debug.LogError("ShipController not found! Make sure it exists in the scene.");
            return;
        }
        
        if (existingCanvas == null)
        {
            Debug.LogError("Please assign your existing Canvas to the 'Existing Canvas' field in the inspector!");
            return;
        }
        
        SetupUI();
        DetectUpgradeableStats();
        CreateUpgradeItems();
        UpdateCurrencyDisplay();
        
        // Set up button listeners
        if (openUpgradeButton != null)
            openUpgradeButton.onClick.AddListener(OpenUI);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }
    
    void Update()
    {
        // Removed keyboard toggle - now only uses buttons
    }
    
    void SetupUI()
    {
        // Create upgrade panel as child of existing canvas
        upgradePanel = new GameObject("Ship Upgrade Panel");
        upgradePanel.transform.SetParent(existingCanvas.transform, false);
        
        RectTransform panelRect = upgradePanel.AddComponent<RectTransform>();
        // Calculate proper panel size based on screen dimensions
        float panelWidth = Mathf.Min(800, Screen.width * 0.8f);
        float panelHeight = Mathf.Min(600, Screen.height * 0.8f);
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = (Screen.height - panelHeight) * 0.5f;
        
        panelRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, panelX, panelWidth);
        panelRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, panelY, panelHeight);
        
        Image panelImage = upgradePanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        CreatePanelContent();
        
        // Start with panel closed
        upgradePanel.SetActive(false);
    }
    
    void CreatePanelContent()
    {
        RectTransform panelRect = upgradePanel.GetComponent<RectTransform>();
        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        
        // Fallback if rect isn't available yet
        if (panelWidth <= 0) panelWidth = Mathf.Min(800, Screen.width * 0.8f);
        if (panelHeight <= 0) panelHeight = Mathf.Min(600, Screen.height * 0.8f);
        
        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(upgradePanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 10, 50);
        titleRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, panelWidth - 20);
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Ship Upgrades";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        // Currency display
        GameObject currencyGO = new GameObject("Currency");
        currencyGO.transform.SetParent(upgradePanel.transform, false);
        RectTransform currencyRect = currencyGO.AddComponent<RectTransform>();
        currencyRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 70, 35);
        currencyRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, panelWidth - 20);
        
        currencyText = currencyGO.AddComponent<TextMeshProUGUI>();
        currencyText.fontSize = 18;
        currencyText.alignment = TextAlignmentOptions.Center;
        currencyText.color = Color.yellow;
        
        // Scroll view (takes up most of the space)
        float scrollViewHeight = panelHeight - 170; // Leave space for title, currency, and close button
        GameObject scrollViewGO = new GameObject("Scroll View");
        scrollViewGO.transform.SetParent(upgradePanel.transform, false);
        RectTransform scrollRect = scrollViewGO.AddComponent<RectTransform>();
        scrollRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 115, scrollViewHeight);
        scrollRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, panelWidth - 20);
        
        ScrollRect scrollComponent = scrollViewGO.AddComponent<ScrollRect>();
        Image scrollBG = scrollViewGO.AddComponent<Image>();
        scrollBG.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        
        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        contentParent = contentGO.transform;
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
        contentRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, panelWidth - 40);
        
        VerticalLayoutGroup layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        
        ContentSizeFitter sizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollComponent.content = contentRect;
        scrollComponent.horizontal = false;
        scrollComponent.vertical = true;
        
        // Close button (at bottom)
        GameObject closeButtonGO = new GameObject("Close Button");
        closeButtonGO.transform.SetParent(upgradePanel.transform, false);
        RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
        closeButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 15, 45);
        closeButtonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, panelWidth * 0.25f, panelWidth * 0.5f);
        
        closeButton = closeButtonGO.AddComponent<Button>();
        Image closeButtonImage = closeButtonGO.AddComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.3f, 0.2f, 1f);
        
        GameObject closeButtonTextGO = new GameObject("Text");
        closeButtonTextGO.transform.SetParent(closeButtonGO.transform, false);
        RectTransform closeButtonTextRect = closeButtonTextGO.AddComponent<RectTransform>();
        closeButtonTextRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 45);
        closeButtonTextRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, panelWidth * 0.5f);
        
        TextMeshProUGUI closeButtonText = closeButtonTextGO.AddComponent<TextMeshProUGUI>();
        closeButtonText.text = "Close";
        closeButtonText.fontSize = 16;
        closeButtonText.alignment = TextAlignmentOptions.Center;
        closeButtonText.color = Color.white;
    }
    
    void DetectUpgradeableStats()
    {
        Type shipType = typeof(ShipController);
        FieldInfo[] fields = shipType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            if (ShouldIncludeField(field))
            {
                UpgradeableStatInfo statInfo = new UpgradeableStatInfo
                {
                    displayName = FormatFieldName(field.Name),
                    fieldInfo = field,
                    baseValue = (float)field.GetValue(shipController),
                    currentValue = (float)field.GetValue(shipController),
                    upgradeLevel = 0,
                    upgradeMultiplier = GetUpgradeMultiplier(field.Name),
                    baseCost = GetBaseCost(field.Name),
                    minValue = GetMinValue(field.Name),
                    maxValue = GetMaxValue(field.Name),
                    description = GetDescription(field.Name)
                };
                statInfo.currentCost = statInfo.baseCost;
                
                upgradeableStats[field.Name] = statInfo;
            }
        }
    }
    
    bool ShouldIncludeField(FieldInfo field)
    {
        // Include float fields that represent upgradeable stats
        if (field.FieldType != typeof(float)) return false;
        
        string[] excludedFields = { "currentSpeed", "targetSpeed", "currentTurnRate", "targetTurnRate", "lerpSpeed", "currentAngle" };
        
        foreach (string excluded in excludedFields)
        {
            if (field.Name.Equals(excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return true;
    }
    
    string FormatFieldName(string fieldName)
    {
        // Convert camelCase to readable format
        string result = "";
        for (int i = 0; i < fieldName.Length; i++)
        {
            if (i > 0 && char.IsUpper(fieldName[i]))
            {
                result += " ";
            }
            result += i == 0 ? char.ToUpper(fieldName[i]) : fieldName[i];
        }
        return result;
    }
    
    float GetUpgradeMultiplier(string fieldName)
    {
        // Different multipliers for different types of stats
        switch (fieldName.ToLower())
        {
            case "paddlingspeed":
            case "windspeed":
                return 1.2f;
            case "accelerationrate":
            case "decelerationrate":
                return 1.15f;
            case "maxturnrate":
            case "turnacceleration":
            case "turndeceleration":
                return 1.1f;
            case "helmreturnspeed":
                return 1.25f;
            default:
                return 1.2f;
        }
    }
    
    int GetBaseCost(string fieldName)
    {
        // Different base costs for different stats
        switch (fieldName.ToLower())
        {
            case "paddlingspeed":
                return 50;
            case "windspeed":
                return 100;
            case "accelerationrate":
            case "decelerationrate":
                return 75;
            case "maxturnrate":
            case "turnacceleration":
            case "turndeceleration":
                return 60;
            case "helmreturnspeed":
                return 40;
            default:
                return 50;
        }
    }
    
    float GetMinValue(string fieldName)
    {
        return 0.1f; // Minimum value to prevent stats from going to 0
    }
    
    float GetMaxValue(string fieldName)
    {
        // Maximum upgrade levels (base value * multiplier^maxLevel)
        switch (fieldName.ToLower())
        {
            case "paddlingspeed":
            case "windspeed":
                return 20f;
            case "maxturnrate":
                return 50f;
            default:
                return 10f;
        }
    }
    
    string GetDescription(string fieldName)
    {
        switch (fieldName.ToLower())
        {
            case "paddlingspeed":
                return "Speed when using paddles (Gear 1)";
            case "windspeed":
                return "Base sailing speed with wind";
            case "accelerationrate":
                return "How quickly the ship speeds up";
            case "decelerationrate":
                return "How quickly the ship slows down";
            case "maxturnrate":
                return "Maximum turning speed";
            case "turnacceleration":
                return "How quickly turn rate increases";
            case "turndeceleration":
                return "How quickly turn rate decreases";
            case "helmreturnspeed":
                return "Speed helm returns to center";
            default:
                return "Ship performance stat";
        }
    }
    
    void CreateUpgradeItems()
    {
        foreach (var kvp in upgradeableStats)
        {
            CreateUpgradeItem(kvp.Key, kvp.Value);
        }
    }
    
    void CreateUpgradeItem(string statKey, UpgradeableStatInfo statInfo)
    {
        GameObject itemGO = new GameObject("Upgrade_" + statKey);
        itemGO.transform.SetParent(contentParent, false);
        
        RectTransform itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100); // Reduced height
        
        Image itemBG = itemGO.AddComponent<Image>();
        itemBG.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
        
        // Get panel width for calculating item width
        RectTransform panelRect = upgradePanel.GetComponent<RectTransform>();
        float panelWidth = panelRect.rect.width;
        if (panelWidth <= 0) panelWidth = Mathf.Min(800, Screen.width * 0.8f);
        float itemWidth = panelWidth - 60; // Account for scrollview padding
        
        // Stat name
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(itemGO.transform, false);
        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 5, 25);
        nameRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, itemWidth * 0.6f);
        
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = statInfo.displayName;
        nameText.fontSize = 16;
        nameText.color = Color.white;
        
        // Description
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(itemGO.transform, false);
        RectTransform descRect = descGO.AddComponent<RectTransform>();
        descRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 35, 20);
        descRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, itemWidth * 0.6f);
        
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = statInfo.description;
        descText.fontSize = 11;
        descText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
        
        // Current value
        GameObject valueGO = new GameObject("Value");
        valueGO.transform.SetParent(itemGO.transform, false);
        RectTransform valueRect = valueGO.AddComponent<RectTransform>();
        valueRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 60, 35);
        valueRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 10, itemWidth * 0.6f);
        
        TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 13;
        valueText.color = Color.cyan;
        
        // Level display
        GameObject levelGO = new GameObject("Level");
        levelGO.transform.SetParent(itemGO.transform, false);
        RectTransform levelRect = levelGO.AddComponent<RectTransform>();
        levelRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 5, 30);
        levelRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, itemWidth * 0.65f, itemWidth * 0.3f);
        
        TextMeshProUGUI levelText = levelGO.AddComponent<TextMeshProUGUI>();
        levelText.fontSize = 14;
        levelText.color = Color.white;
        levelText.alignment = TextAlignmentOptions.Center;
        
        // Upgrade button
        GameObject buttonGO = new GameObject("Upgrade Button");
        buttonGO.transform.SetParent(itemGO.transform, false);
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 40, 50);
        buttonRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, itemWidth * 0.67f, itemWidth * 0.28f);
        
        Button upgradeButton = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        
        GameObject buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        RectTransform buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
        buttonTextRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 50);
        buttonTextRect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, itemWidth * 0.28f);
        
        TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonText.fontSize = 12;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        // Set up button click
        upgradeButton.onClick.AddListener(() => UpgradeStat(statKey));
        
        UpdateUpgradeItemDisplay(statKey, statInfo, valueText, levelText, buttonText, upgradeButton);
    }
    
    void UpdateUpgradeItemDisplay(string statKey, UpgradeableStatInfo statInfo, TextMeshProUGUI valueText, TextMeshProUGUI levelText, TextMeshProUGUI buttonText, Button upgradeButton)
    {
        valueText.text = "Current: " + statInfo.currentValue.ToString("F2") + "\nBase: " + statInfo.baseValue.ToString("F2");
        levelText.text = "Level " + statInfo.upgradeLevel;
        
        bool canUpgrade = playerCurrency >= statInfo.currentCost && statInfo.currentValue < statInfo.maxValue;
        upgradeButton.interactable = canUpgrade;
        
        if (statInfo.currentValue >= statInfo.maxValue)
        {
            buttonText.text = "MAX";
            upgradeButton.GetComponent<Image>().color = new Color(0.8f, 0.6f, 0.2f, 1f);
        }
        else
        {
            buttonText.text = "Upgrade\n" + statInfo.currentCost + " Gold";
        }
    }
    
    void UpgradeStat(string statKey)
    {
        if (!upgradeableStats.ContainsKey(statKey)) return;
        
        UpgradeableStatInfo statInfo = upgradeableStats[statKey];
        
        if (playerCurrency >= statInfo.currentCost && statInfo.currentValue < statInfo.maxValue)
        {
            // Deduct currency
            playerCurrency -= statInfo.currentCost;
            
            // Upgrade stat
            statInfo.upgradeLevel++;
            statInfo.currentValue = statInfo.baseValue * Mathf.Pow(statInfo.upgradeMultiplier, statInfo.upgradeLevel);
            statInfo.currentValue = Mathf.Min(statInfo.currentValue, statInfo.maxValue);
            
            // Update actual ship stat
            statInfo.fieldInfo.SetValue(shipController, statInfo.currentValue);
            
            // Increase cost for next upgrade
            statInfo.currentCost = Mathf.RoundToInt(statInfo.baseCost * Mathf.Pow(1.5f, statInfo.upgradeLevel));
            
            // Update UI
            RefreshAllUpgradeItems();
            UpdateCurrencyDisplay();
            
            Debug.Log("Upgraded " + statInfo.displayName + " to level " + statInfo.upgradeLevel + " (Value: " + statInfo.currentValue.ToString("F2") + ")");
        }
    }
    
    void RefreshAllUpgradeItems()
    {
        // Destroy existing items and recreate them
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentParent.GetChild(i).gameObject);
        }
        CreateUpgradeItems();
    }
    
    void UpdateCurrencyDisplay()
    {
        if (currencyText != null)
        {
            currencyText.text = "Gold: " + playerCurrency;
        }
    }
    
    void OpenUI()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            isUIOpen = true;
            
            // Optional: Pause game and show cursor
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("Ship Upgrade UI Opened");
        }
    }
    
    void CloseUI()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            isUIOpen = false;
            
            // Resume game
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("Ship Upgrade UI Closed");
        }
    }
    
    void ToggleUI()
    {
        if (isUIOpen)
            CloseUI();
        else
            OpenUI();
    }
    
    // Public method to add currency (call this when player earns money)
    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        UpdateCurrencyDisplay();
    }
    
    void OnDisable()
    {
        // Resume game if UI is destroyed while open
        if (isUIOpen)
        {
            Time.timeScale = 1f;
        }
    }
}