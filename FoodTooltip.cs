using UnityEngine;

// This class adds hover tooltips and zone tracking to food items
public class FoodTooltip : MonoBehaviour
{
    [Header("Food Information")]
    [Tooltip("Name to display in tooltip")]
    public string displayName;
    [Tooltip("Description to display in tooltip")]
    public string description;
    [Tooltip("The food type identifier")]
    public string foodType;

    [Header("Zone Tracking")]
    // Track the current zone for scoring
    private string currentZone = "";

    // Tooltip controller reference
    private TooltipController tooltipController;
    private bool tooltipInitialized = false;

    void Awake()
    {
        InitializeTooltipController();
    }

    // Try to find the tooltip controller
    private void InitializeTooltipController()
    {
        try
        {
            tooltipController = FindObjectOfType<TooltipController>();
            
            if (tooltipController == null)
            {
                Debug.LogWarning($"TooltipController not found for {gameObject.name} - tooltips will not be shown");
                return;
            }
            
            tooltipInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing tooltip controller: {e.Message}");
        }
    }

    void OnEnable()
    {
        // Try to initialize again if needed
        if (!tooltipInitialized)
        {
            InitializeTooltipController();
        }
    }

    // Get current zone for scoring
    public string GetCurrentZone()
    {
        return currentZone;
    }

    // Get food type for scoring
    public string GetFoodType()
    {
        // First try to use the display name for tooltips
        if (!string.IsNullOrEmpty(displayName))
            return displayName;
            
        // Then try the food type field
        if (!string.IsNullOrEmpty(foodType))
            return foodType;
            
        // Fall back to object name
        return gameObject.name.Replace("(Clone)", "").Trim();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Update current zone for scoring
        if (other.CompareTag("Zone") || 
            other.CompareTag("f1") || other.CompareTag("f2") || 
            other.CompareTag("f3") || other.CompareTag("f4") || 
            other.CompareTag("f5") || other.CompareTag("f6"))
        {
            currentZone = other.tag;
            Debug.Log($"🎯 {GetFoodType()} entered zone: {currentZone}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Update zone tracking when exiting a zone
        if ((other.CompareTag("Zone") || 
            other.CompareTag("f1") || other.CompareTag("f2") || 
            other.CompareTag("f3") || other.CompareTag("f4") || 
            other.CompareTag("f5") || other.CompareTag("f6")) && 
            other.tag == currentZone)
        {
            currentZone = "";
            Debug.Log($"💨 {GetFoodType()} exited zone: {other.tag}");
        }
    }

    // Tooltips on mouse events
    void OnMouseEnter()
    {
        if (!tooltipInitialized)
        {
            InitializeTooltipController();
        }
        
        if (tooltipController != null && tooltipInitialized)
        {
            try
            {
                string name = !string.IsNullOrEmpty(displayName) ? displayName : GetFoodType();
                string desc = !string.IsNullOrEmpty(description) ? description : "";
                
                if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(desc))
                {
                    // Only show tooltip if the game is in play mode
                    if (gameObject.activeInHierarchy && Time.timeScale > 0)
                    {
                        tooltipController.ShowTooltip(name, desc);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing tooltip: {e.Message}");
                tooltipInitialized = false; // Mark for re-initialization
            }
        }
    }
    
    void OnMouseExit()
    {
        if (tooltipController != null && tooltipInitialized)
        {
            try
            {
                tooltipController.HideTooltip();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error hiding tooltip: {e.Message}");
            }
        }
    }

    // Also handle tooltip hiding when object is disabled
    void OnDisable()
    {
        if (tooltipController != null && tooltipInitialized)
        {
            try
            {
                tooltipController.HideTooltip();
            }
            catch (System.Exception e)
            {
                // Just suppress the error
            }
        }
    }
} 