using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("UI/Hoverable Item")]
public class HoverableItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("The name/title that will appear in the tooltip")]
    public string itemName;
    
    [Tooltip("The description that will appear in the tooltip")]
    [TextArea(3, 5)]
    public string itemDescription;
    
    [Tooltip("Offset for tooltip position")]
    public Vector2 tooltipOffset = new Vector2(15, 15);
    
    private TooltipController tooltipController;
    private bool tooltipInitialized = false;
    
    private void Start()
    {
        InitializeTooltip();
    }
    
    private void InitializeTooltip()
    {
        // Find tooltip controller or create one if it doesn't exist
        if (tooltipController == null)
        {
            tooltipController = TooltipController.Instance;
            
            if (tooltipController == null)
            {
                GameObject tooltipObj = null;
                
                // Look for an existing tooltip controller object first
                var existingObj = GameObject.Find("TooltipController");
                if (existingObj != null)
                {
                    tooltipObj = existingObj;
                    tooltipController = existingObj.GetComponent<TooltipController>();
                    
                    if (tooltipController == null)
                    {
                        tooltipController = existingObj.AddComponent<TooltipController>();
                    }
                }
                else
                {
                    // Create a new tooltip controller
                    tooltipObj = new GameObject("TooltipController");
                    tooltipController = tooltipObj.AddComponent<TooltipController>();
                    Debug.Log("Created new TooltipController");
                }
            }
            
            tooltipInitialized = true;
        }
    }
    
    // Called whenever the cursor/pointer enters this UI element or collider
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!tooltipInitialized)
        {
            InitializeTooltip();
        }
        
        // Double check we have a controller
        if (tooltipController == null)
        {
            Debug.LogWarning("No TooltipController available - tooltip won't be shown");
            return;
        }
        
        try
        {
            tooltipController.ShowTooltip(itemName, itemDescription, Input.mousePosition + (Vector3)tooltipOffset);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing tooltip: {e.Message}");
        }
    }

    // Called whenever the cursor/pointer exits this UI element or collider
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipController != null)
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
    
    // This is called when the cursor is over the GameObject and we click or tap
    public void OnPointerClick(PointerEventData eventData)
    {
        // Optional: implement any click behavior here
    }
}
