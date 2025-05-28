/*
 * Fridge Organization Game - TemperatureManager.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Temperature control system that manages the refrigerator temperature settings,
 * provides visual feedback for temperature violations, and integrates with the scoring system
 * to teach proper food storage temperature principles.
 */

using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class TemperatureManager : MonoBehaviour
{
    public static TemperatureManager Instance;

    [Header("Temperature Settings")]
    public int currentTemperature = 0;

    [Header("Temperature UI")]
    public TextMeshProUGUI TemperatureText; 
    public Slider temperatureSlider;        

    // temperature range settings
    public int fridgeMinTemp = 1;
    public int fridgeMaxTemp = 4;

    // temperature tracking and analytics
    private List<int> temperatureHistory = new List<int>();
    private float lastTemperatureChangeTime = 0f;
    private int temperatureChangeCount = 0;

    // temperature event tracking for analytics
    private Dictionary<string, int> temperatureEvents = new Dictionary<string, int>
    {
        { "too_high", 0 },
        { "too_low", 0 },
        { "normal", 0 }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("TemperatureManager initialized as singleton instance");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Duplicate TemperatureManager destroyed");
        }
    }

    void Start()
    {
    
        UpdateTemperatureDisplay();    
        InitializeTemperatureSlider();
        RecordTemperature(currentTemperature);
    
        // Make sure this component doesn't block user interaction
        EnsureInteractionNotBlocked();
    }

    /// <summary>
    /// Ensures that the TemperatureManager doesn't block user interaction
    /// </summary>
    private void EnsureInteractionNotBlocked()
    {
    
        if (GetComponent<UnityEngine.UI.Image>() != null)
        {
            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
        }
        
        // make sure our Canvas is properly configured if we're inside one
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Ensure the canvas has a GraphicRaycaster
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }
        
        // if we have a CanvasGroup, make sure it's not blocking interaction
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false; // Don't block raycasts to objects below
        }
        
        Debug.Log("TemperatureManager: Ensured interaction is not blocked");
    }


    private void InitializeTemperatureSlider()
    {
        if (temperatureSlider != null)
        {
            temperatureSlider.minValue = 0;  
            temperatureSlider.maxValue = 8;  
            temperatureSlider.value = currentTemperature;
            temperatureSlider.onValueChanged.AddListener(SetTemperature);
            Debug.Log("Temperature slider initialized.");
        }
        else
        {
            Debug.LogWarning("Temperature slider reference is missing in TemperatureManager.");
        }
    }


    public void SetTemperature(float temp)
    {
        int newTemp = Mathf.RoundToInt(temp);

        // if temperature is not changed, do nothing
        if (newTemp == currentTemperature) return;
        int previousTemp = currentTemperature;
        currentTemperature = newTemp;

        
        RecordTemperature(currentTemperature); // record temperature change
        AnalyzeTemperatureChange(previousTemp, currentTemperature); // analyze temperature change (only for data statistics, no warning display)
        UpdateTemperatureDisplay(); // update UI display (if TemperatureText is available)
        LogTemperatureEventIfNeeded(); // record temperature event (normal / too_high / too_low), no warning display in game

        Debug.Log($"Temperature changed from {previousTemp} to {currentTemperature}°C");

        // report to Analytics
        try
        {
            GameAnalytics analytics = GameAnalytics.Instance;
            if (analytics != null)
            {
                analytics.LogTemperature(currentTemperature);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to log temperature to analytics: {e.Message}");
        }
        
        //  PlayFab
        try
        {
            GameManager gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.LogTemperatureChange(currentTemperature);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to log temperature to PlayFab: {e.Message}");
        }
    }

    
    private void RecordTemperature(int temperature)
    {
        temperatureHistory.Add(temperature);
        temperatureChangeCount++;
        lastTemperatureChangeTime = Time.time;
    }

   
    private void AnalyzeTemperatureChange(int previousTemp, int currentTemp)
    {
        // if the temperature change is more than 2 degrees, it is considered a temperature jump
        if (Mathf.Abs(currentTemp - previousTemp) > 2)
        {
            LogTemperatureJump(previousTemp, currentTemp);
        }

        // if the temperature is changed frequently in a short time, it is considered a "user difficulty"
        // here is an example of 3 times in 3 seconds
        if (temperatureChangeCount >= 3 && Time.time - lastTemperatureChangeTime < 3f)
        {
            LogFrequentTemperatureChanges();
        }
    }

    // record temperature "jump" to Analytics
    private void LogTemperatureJump(int previousTemp, int currentTemp)
    {
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogUserDifficulty(
                    "temperature_jump",
                    $"Large temperature change: {previousTemp} -> {currentTemp}",
                    Mathf.Abs(currentTemp - previousTemp)
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log temperature jump: {e.Message}");
            }
        }
    }

    // record frequent temperature changes
    private void LogFrequentTemperatureChanges()
    {
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogUserDifficulty(
                    "frequent_temperature_changes",
                    $"User changed temperature {temperatureChangeCount} times quickly",
                    temperatureChangeCount
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log frequent temperature changes: {e.Message}");
            }
        }
    }

    // update temperature event statistics (no UI display)
    private void LogTemperatureEventIfNeeded()
    {
        string eventType;

        if (currentTemperature > fridgeMaxTemp)
        {
            eventType = "too_high";
        }
        else if (currentTemperature < fridgeMinTemp)
        {
            eventType = "too_low";
        }
        else
        {
            eventType = "normal";
        }

        if (temperatureEvents.ContainsKey(eventType))
        {
            temperatureEvents[eventType]++;
        }

        
    }

    // update temperature display text (if not needed, can be deleted)
    private void UpdateTemperatureDisplay()
    {
        if (TemperatureText != null)
        {
            TemperatureText.text = $"Temperature: {currentTemperature}°C";
        }
    }

    // check if the current temperature is in the ideal range
    public bool IsTemperatureCorrect()
    {
        return currentTemperature <= fridgeMaxTemp && currentTemperature >= fridgeMinTemp;
    }

    // get all historical temperatures
    public List<int> GetTemperatureHistory()
    {
        return new List<int>(temperatureHistory);
    }

    // get temperature related statistics (for final panel or other purposes)
    public Dictionary<string, object> GetTemperatureStats()
    {
        float avgTemp = 0f;
        if (temperatureHistory.Count > 0)
        {
            int sum = 0;
            foreach (int t in temperatureHistory) sum += t;
            avgTemp = (float)sum / temperatureHistory.Count;
        }

        var stats = new Dictionary<string, object>
        {
            { "currentTemperature", currentTemperature },
            { "averageTemperature", avgTemp },
            { "totalChanges", temperatureChangeCount },
            { "tooHighCount", temperatureEvents["too_high"] },
            { "tooLowCount",  temperatureEvents["too_low"] },
            { "normalCount", temperatureEvents["normal"] }
        };

        return stats;
    }
    
    // Add the missing method that was referenced elsewhere
    public void HideTemperatureWarning()
    {
        // Implementation is empty since warnings are no longer shown
    }
}
