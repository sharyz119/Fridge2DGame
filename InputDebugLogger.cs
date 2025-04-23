using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Debug utility to log input events throughout the application
/// </summary>
public class InputDebugLogger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public TMP_InputField inputField;
    public bool logAllMouseClicks = true;
    public bool logAllButtonInteractions = true;
    public bool logRaycastResults = true;

    private GraphicRaycaster[] raycasters;
    private EventSystem eventSystem;

    void Start()
    {
        if (inputField != null)
        {
            inputField.onSelect.AddListener(delegate { Debug.Log("InputField Selected"); });
            inputField.onValueChanged.AddListener((text) => Debug.Log("Input Changed: " + text));
            inputField.onEndEdit.AddListener((text) => Debug.Log("End Edit: " + text));
        }

        // Cache references
        raycasters = FindObjectsOfType<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        // Set up global button click monitoring
        if (logAllButtonInteractions)
        {
            StartCoroutine(SetupButtonMonitoring());
        }

        Debug.Log("InputDebugLogger initialized - will track input events");
    }

    void Update()
    {
        // Track mouse clicks
        if (logAllMouseClicks && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Debug.Log($"Mouse click detected at: {mousePos.x}, {mousePos.y}");

            // If we should log raycast results
            if (logRaycastResults)
            {
                LogRaycastResults(mousePos);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"OnPointerClick event on {gameObject.name}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"OnPointerDown event on {gameObject.name}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"OnPointerUp event on {gameObject.name}");
    }

    void LogRaycastResults(Vector3 position)
    {
        if (eventSystem == null)
        {
            Debug.LogWarning("EventSystem is null. Cannot log raycast results.");
            return;
        }

        // Initialize event data
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = position;

        // Raycast against all graphics raycasters
        foreach (var raycaster in raycasters)
        {
            if (raycaster == null || !raycaster.isActiveAndEnabled) continue;

            var results = new System.Collections.Generic.List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log($"Hit {results.Count} objects through raycaster on {raycaster.gameObject.name}:");
                foreach (var result in results)
                {
                    string buttonInfo = "Not a button";
                    Button button = result.gameObject.GetComponent<Button>();
                    if (button != null)
                    {
                        buttonInfo = $"Button (interactable: {button.interactable})";
                    }

                    Debug.Log($"- Hit {result.gameObject.name} - {buttonInfo} - GameObject active: {result.gameObject.activeInHierarchy}");
                    
                    // Check if it's in a CanvasGroup
                    CanvasGroup[] groups = result.gameObject.GetComponentsInParent<CanvasGroup>();
                    if (groups.Length > 0)
                    {
                        foreach (var group in groups)
                        {
                            Debug.Log($"  * In CanvasGroup: {group.gameObject.name} (interactable: {group.interactable}, blocksRaycasts: {group.blocksRaycasts}, alpha: {group.alpha})");
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"No UI elements hit through raycaster on {raycaster.gameObject.name}");
            }
        }
    }

    IEnumerator SetupButtonMonitoring()
    {
        // Wait a frame to let everything initialize
        yield return null;

        // Find all buttons and add listeners
        Button[] allButtons = FindObjectsOfType<Button>(true);
        Debug.Log($"Adding debug listeners to {allButtons.Length} buttons");

        foreach (var button in allButtons)
        {
            // Add a debug listener
            button.onClick.AddListener(() => {
                Debug.Log($"Button clicked: {button.gameObject.name} in {button.gameObject.transform.parent?.name}");
            });
        }
    }

    // Helper method to call from other scripts
    public static void LogClickAttempt(string objectName)
    {
        Debug.Log($"Click attempted on: {objectName}");
    }
}
