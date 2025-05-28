/*
 * Fridge Organization Game - TooltipController.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Interactive help system that provides hover-based information display for food items
 * and game elements. Delivers educational content and guidance to enhance the learning experience
 * through contextual tooltips and dynamic positioning.
 * 
 * Key Responsibilities:
 * - Hover-based information display and tooltip management
 * - Food-specific guidance and educational content delivery
 * - Dynamic tooltip positioning and layout management
 * - Interactive help system coordination
 * - Educational content presentation
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }
    
    [Header("References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Settings")]
    public float fadeSpeed = 7f;
    public Vector2 screenPadding = new Vector2(10f, 10f);
    public Vector2 minSize = new Vector2(100f, 50f);
    
    private RectTransform panelRectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTooltip();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeTooltip()
    {
        // If tooltip panel doesn't exist, create it
        if (tooltipPanel == null)
        {
            CreateTooltipPanel();
        }
        
        panelRectTransform = tooltipPanel.GetComponent<RectTransform>();
        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            
        // Initially hide the tooltip
        canvasGroup.alpha = 0f;
        tooltipPanel.SetActive(false);
        
        isInitialized = true;
    }
    
    private void CreateTooltipPanel()
    {
        try
        {
            Debug.Log("Creating tooltip panel");
            
            // Find canvas or create one
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TooltipCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("Created new canvas for tooltips");
            }
            parentCanvas = canvas;
            
            // Create tooltip panel
            tooltipPanel = new GameObject("TooltipPanel");
            tooltipPanel.transform.SetParent(canvas.transform, false);
            
            // Add required components
            panelRectTransform = tooltipPanel.AddComponent<RectTransform>();
            panelRectTransform.sizeDelta = new Vector2(200, 100);
            
            Image panelImage = tooltipPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // Add content container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(tooltipPanel.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(5, 5);
            contentRect.offsetMax = new Vector2(-5, -5);
            
            // Add title text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(contentObj.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(0, 20);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.color = Color.black;
            
            // Add description text
            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(contentObj.transform, false);
            descriptionText = descObj.AddComponent<TextMeshProUGUI>();
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.offsetMin = new Vector2(0, 5);
            descRect.offsetMax = new Vector2(0, -25);
            descriptionText.fontSize = 12;
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
            descriptionText.color = Color.black;
            
            // Add canvas group for fading
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            
            tooltipPanel.SetActive(false);
            
            // Verify all components are set
            Debug.Log($"Tooltip created - Panel: {tooltipPanel != null}, Title: {titleText != null}, Description: {descriptionText != null}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating tooltip panel: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public void ShowTooltip(string title, string description = "", Vector3 position = default)
    {
        // If instance is null, don't try to show tooltip
        if (this == null)
        {
            Debug.LogError("TooltipController is null - can't show tooltip");
            return;
        }
        
        // Make sure we're initialized
        if (!isInitialized)
        {
            try
            {
                InitializeTooltip();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize tooltip: {e.Message}");
                return;
            }
        }
            
        // Safety check - if tooltipPanel is still null after initialization, exit
        if (tooltipPanel == null)
        {
            Debug.LogError("TooltipPanel is null after initialization - can't show tooltip");
            return;
        }
        
        // Set text carefully with null checks
        if (titleText != null) 
        {
            titleText.text = title ?? "No Title";
            titleText.color = Color.black; // Ensure text is black at runtime
        }
        else
            Debug.LogWarning("TitleText component is null in ShowTooltip");
            
        if (descriptionText != null)
        {
            descriptionText.text = description ?? "";
            descriptionText.color = Color.black; // Ensure text is black at runtime
        }
        else
            Debug.LogWarning("DescriptionText component is null in ShowTooltip");
        
        // Position the tooltip with null check on RectTransform
        if (panelRectTransform != null)
            UpdatePosition(position);
        else
            Debug.LogWarning("Panel RectTransform is null in ShowTooltip");
        
        // Make sure we have a CanvasGroup for fade effect
        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        
        // Set initial alpha without coroutine
        canvasGroup.alpha = 0f;
        
        // Show the tooltip - IMPORTANT: Activate before starting coroutine
        tooltipPanel.SetActive(true);
        
        // Handle fade effect
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }
    
    public void HideTooltip()
    {
        if (!isInitialized) return;
        
        // If the panel is already inactive, do nothing
        if (tooltipPanel == null || !tooltipPanel.activeInHierarchy)
        {
            return;
        }
        
        StopAllCoroutines();
        
        // Check if we can run a coroutine
        if (gameObject.activeInHierarchy && tooltipPanel.activeInHierarchy)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            // If we can't run a coroutine, just hide immediately
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private System.Collections.IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        tooltipPanel.SetActive(false);
    }
    
    private void Update()
    {
        if (tooltipPanel.activeInHierarchy)
        {
            // Update position to follow mouse if needed
            UpdatePosition();
        }
    }
    
    private void UpdatePosition(Vector3 position = default)
    {
        if (panelRectTransform == null) return;
        
        // Ensure we have a canvas reference
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError("No Canvas found in the scene for tooltip positioning!");
                return;
            }
        }
        
        // If no position specified, use mouse position
        if (position == default)
            position = Input.mousePosition;
            
        Vector2 localPoint;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        
        if (canvasRect == null)
        {
            Debug.LogError("Canvas has no RectTransform component!");
            return;
        }
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            position,
            parentCanvas.worldCamera,
            out localPoint))
        {
            Debug.LogWarning("Could not convert screen position to canvas local point");
            // Use fallback position near the mouse
            localPoint = new Vector2(position.x, position.y);
        }
        
        // Content size fitting
        if (panelRectTransform != null)
        {
            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRectTransform);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error rebuilding layout: {e.Message}");
            }
        }
        
        // Keep tooltip on screen
        Vector2 pivot = new Vector2(0, 1); // Default: top-left
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        
        // Adjust pivot based on screen edges
        if (position.x + panelRectTransform.sizeDelta.x > screenSize.x - screenPadding.x)
            pivot.x = 1; // Right side of tooltip
        if (position.y - panelRectTransform.sizeDelta.y < screenPadding.y)
            pivot.y = 0; // Bottom of tooltip
            
        panelRectTransform.pivot = pivot;
        panelRectTransform.anchoredPosition = localPoint;
    }
}
