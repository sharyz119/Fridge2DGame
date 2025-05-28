/*
 * Fridge Organization Game - TooltipSystem.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Core tooltip system that manages the display, positioning, and content of tooltips
 * throughout the game. Works with TooltipController to provide a comprehensive help system
 * that enhances the educational experience.
 * 
 * Key Responsibilities:
 * - Tooltip display management and visibility control
 * - Dynamic positioning and screen boundary handling
 * - Content formatting and presentation
 * - Tooltip lifecycle management (show/hide/update)
 * - Integration with hoverable items and UI elements
 */

using UnityEngine;

/// <summary>
/// Add this component to your game scene to enable tooltip functionality
/// </summary>
public class TooltipSystem : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Automatically add hoverable components to food items")]
    public bool autoAddToFoodItems = true;
    
    [Tooltip("The tooltip panel prefab (optional)")]
    public GameObject tooltipPanelPrefab;
    
    private TooltipController tooltipController;
    
    void Awake()
    {
        Debug.Log("TooltipSystem initializing...");
        
        // Create tooltip controller if it doesn't exist
        tooltipController = FindObjectOfType<TooltipController>();
        
        if (tooltipController == null)
        {
            // Look for an existing game object first
            GameObject tooltipObj = GameObject.Find("TooltipController");
            
            if (tooltipObj != null)
            {
                tooltipController = tooltipObj.GetComponent<TooltipController>();
                if (tooltipController == null)
                {
                    tooltipController = tooltipObj.AddComponent<TooltipController>();
                    Debug.Log("Added TooltipController component to existing GameObject");
                }
            }
            else
            {
                // Create new controller
                tooltipObj = new GameObject("TooltipController");
                tooltipController = tooltipObj.AddComponent<TooltipController>();
                Debug.Log("Created new TooltipController GameObject");
            }
            
            // If we have a prefab, use it
            if (tooltipPanelPrefab != null)
            {
                GameObject tooltipPanel = Instantiate(tooltipPanelPrefab, tooltipObj.transform);
                tooltipPanel.name = "TooltipPanel";
                tooltipController.tooltipPanel = tooltipPanel;
                
                // Find references in the prefab
                try
                {
                    // Look for specific named objects first
                    Transform titleTransform = tooltipPanel.transform.Find("Content/TitleText");
                    Transform descTransform = tooltipPanel.transform.Find("Content/DescriptionText");
                    
                    if (titleTransform != null)
                    {
                        tooltipController.titleText = titleTransform.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                    
                    if (descTransform != null)
                    {
                        tooltipController.descriptionText = descTransform.GetComponent<TMPro.TextMeshProUGUI>();
                    }
                    
                    // If we didn't find them by path, search by component
                    if (tooltipController.titleText == null || tooltipController.descriptionText == null)
                    {
                        // Look for text components
                        TMPro.TextMeshProUGUI[] texts = tooltipPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                        
                        if (texts.Length > 0)
                        {
                            if (tooltipController.titleText == null && texts.Length > 0)
                                tooltipController.titleText = texts[0];
                                
                            if (tooltipController.descriptionText == null && texts.Length > 1)
                                tooltipController.descriptionText = texts[1];
                        }
                    }
                    
                    Debug.Log($"Tooltip references found - Title: {tooltipController.titleText != null}, Description: {tooltipController.descriptionText != null}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error setting up tooltip panel: {e.Message}");
                }
            }
        }
        
        // Make sure tooltip controller is initialized
        if (tooltipController != null)
        {
            tooltipController.gameObject.SetActive(true);
        }
    }
    
    void Start()
    {
        // Make sure a tooltip controller exists
        if (tooltipController == null)
        {
            tooltipController = TooltipController.Instance;
            
            if (tooltipController == null)
            {
                Debug.LogWarning("No TooltipController found, creating one");
                GameObject tooltipObj = new GameObject("TooltipController");
                tooltipController = tooltipObj.AddComponent<TooltipController>();
            }
        }
        
        if (autoAddToFoodItems)
        {
            AddHoverableToFoodItems();
        }
    }
    
    // Add hoverable components to food items
    private void AddHoverableToFoodItems()
    {
        // Find all food items (assuming they have DragSprite2D component)
        DragSprite2D[] foodItems = FindObjectsOfType<DragSprite2D>();
        
        if (foodItems.Length == 0)
        {
            Debug.LogWarning("No DragSprite2D components found in the scene!");
            return;
        }
        
        int addedCount = 0;
        foreach (DragSprite2D foodItem in foodItems)
        {
            if (foodItem == null)
                continue;
                
            // Skip if it already has a hoverable component
            if (foodItem.GetComponent<HoverableItem>() != null)
                continue;
                
            // Add hoverable component
            HoverableItem hoverable = foodItem.gameObject.AddComponent<HoverableItem>();
            
            // Set appropriate name (using the object name if available)
            string foodName = foodItem.gameObject.name;
            if (string.IsNullOrEmpty(foodName) || foodName.Contains("Clone"))
            {
                // Try to get a better name from the sprite
                SpriteRenderer spriteRenderer = foodItem.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    foodName = spriteRenderer.sprite.name;
                }
            }
            
            // Clean up the name for display
            foodName = CleanupFoodName(foodName);
            hoverable.itemName = foodName;
            
            // Add a description about proper storage temperature
            SetFoodDescription(hoverable, foodName);
            
            addedCount++;
        }
        
        Debug.Log($"Added hoverable tooltips to {addedCount} food items");
    }
    
    // Helper to clean up food names
    private string CleanupFoodName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Food Item";
            
        // Remove common prefixes/suffixes
        name = name.Replace("Prefab", "").Replace("(Clone)", "").Replace("_", " ");
        
        // Capitalize first letter of each word
        string[] words = name.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + 
                          (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
            }
        }
        
        return string.Join(" ", words).Trim();
    }
    
    // Set description based on food name
    private void SetFoodDescription(HoverableItem item, string foodName)
    {
        if (item == null)
            return;
            
        // Default description
        string description = "Check packaging for proper storage instructions.";
        string idealTemp = "1-4°C";
        
        // Try to identify food type based on name
        foodName = foodName.ToLower();
        
        // Meats (1-4°C)
        if (foodName.Contains("meat") || foodName.Contains("beef") || 
            foodName.Contains("chicken") || foodName.Contains("pork") || 
            foodName.Contains("fish") || foodName.Contains("seafood"))
        {
            description = "Keep refrigerated at 1-4°C.\nStore on bottom shelf away from other foods to prevent cross-contamination.";
            idealTemp = "1-4°C";
        }
        // Dairy (1-4°C)
        else if (foodName.Contains("milk") || foodName.Contains("yogurt") || 
                 foodName.Contains("cheese") || foodName.Contains("butter") ||
                 foodName.Contains("cream") || foodName.Contains("dairy"))
        {
            description = "Keep refrigerated at 1-4°C.\nStore away from strong-smelling foods as dairy easily absorbs odors.";
            idealTemp = "1-4°C";
        }
        // Fruits (4-7°C for most refrigerated fruits)
        else if (foodName.Contains("apple") || foodName.Contains("grape") || 
                 foodName.Contains("berry") || foodName.Contains("fruit") ||
                 foodName.Contains("orange") || foodName.Contains("citrus"))
        {
            description = "Most fruits should be stored at 4-7°C in the crisper drawer.\nSome fruits like bananas are better kept outside the refrigerator.";
            idealTemp = "4-7°C";
        }
        // Vegetables (4-7°C for most)
        else if (foodName.Contains("vegetable") || foodName.Contains("veg") || 
                 foodName.Contains("lettuce") || foodName.Contains("salad") ||
                 foodName.Contains("carrot") || foodName.Contains("broccoli") ||
                 foodName.Contains("pepper") || foodName.Contains("tomato"))
        {
            description = "Most vegetables should be stored at 4-7°C in the crisper drawer.\nKeep vegetables away from ethylene-producing fruits.";
            idealTemp = "4-7°C";
        }
        // Leftovers/prepared foods (1-4°C)
        else if (foodName.Contains("leftover") || foodName.Contains("cooked") || 
                 foodName.Contains("prepared") || foodName.Contains("meal"))
        {
            description = "Store at 1-4°C and consume within 3-4 days.\nKeep in airtight containers to maintain freshness.";
            idealTemp = "1-4°C";
        }
        
        // Set final description with temperature info included
        item.itemDescription = description + $"\n\nIdeal temperature: {idealTemp}";
    }
} 
