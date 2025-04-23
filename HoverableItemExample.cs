using UnityEngine;

public class HoverableItemExample : MonoBehaviour
{
    // This script demonstrates how to add the HoverableItem component to game objects
    
    void Start()
    {
        // Example: Add hoverable component to all food items
        AddHoverableToFoodItems();
    }
    
    void AddHoverableToFoodItems()
    {
        // Find all food items (assuming they have DragSprite2D component)
        DragSprite2D[] foodItems = FindObjectsOfType<DragSprite2D>();
        
        foreach (DragSprite2D foodItem in foodItems)
        {
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
            
            hoverable.itemName = foodName;
            
            // Set a description based on the food type - customize these
            FoodType foodType = GetFoodTypeForItem(foodItem.gameObject);
            hoverable.itemDescription = GetDescriptionForFoodType(foodType);
        }
        
        Debug.Log("Added hoverable tooltips to " + foodItems.Length + " food items");
    }
    
    // Helper method to determine food type
    FoodType GetFoodTypeForItem(GameObject foodItem)
    {
        // This is just a placeholder - you should implement your own logic
        // to determine the food type based on your game's data structure
        
        string name = foodItem.name.ToLower();
        
        if (name.Contains("meat") || name.Contains("beef") || name.Contains("chicken"))
            return FoodType.Meat;
        else if (name.Contains("veg") || name.Contains("salad") || name.Contains("broccoli"))
            return FoodType.Vegetable;
        else if (name.Contains("dairy") || name.Contains("milk") || name.Contains("cheese"))
            return FoodType.Dairy;
        else if (name.Contains("fruit") || name.Contains("apple") || name.Contains("banana"))
            return FoodType.Fruit;
        else
            return FoodType.Other;
    }
    
    // Helper method to get description based on food type
    string GetDescriptionForFoodType(FoodType type)
    {
        switch (type)
        {
            case FoodType.Meat:
                return "Store meat at 1-4°C.\nKeep away from other foods to avoid cross-contamination.";
                
            case FoodType.Vegetable:
                return "Store vegetables at 4-7°C.\nKeep in crisper drawer for optimal freshness.";
                
            case FoodType.Dairy:
                return "Store dairy at 1-4°C.\nKeep away from strong-smelling foods.";
                
            case FoodType.Fruit:
                return "Store most fruits at 4-7°C.\nSome fruits should be kept outside the refrigerator.";
                
            case FoodType.Other:
            default:
                return "Check packaging for proper storage temperature.";
        }
    }
    
    // Food type enum
    public enum FoodType
    {
        Meat,
        Vegetable,
        Dairy,
        Fruit,
        Other
    }
} 