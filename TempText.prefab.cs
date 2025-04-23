using UnityEngine;
using TMPro;

/// <summary>
/// Helper component to display the current temperature from the TemperatureManager
/// </summary>
public class TempTextController : MonoBehaviour
{
    [Tooltip("The text component to display the temperature")]
    public TextMeshProUGUI temperatureText;
    
    [Tooltip("Optional - format string for temperature display (use {0} for the temperature value)")]
    public string format = "{0}°C";
    
    [Tooltip("How often to update the temperature display (in seconds)")]
    public float updateInterval = 0.5f;
    
    private float timer = 0f;
    
    private void Start()
    {
        // If text component not assigned, try to get it from this GameObject
        if (temperatureText == null)
        {
            temperatureText = GetComponent<TextMeshProUGUI>();
            if (temperatureText == null)
            {
                Debug.LogError("TempTextController: No TextMeshProUGUI component found!");
                enabled = false;
                return;
            }
        }
        
        // Update immediately on start
        UpdateTemperatureDisplay();
    }
    
    private void Update()
    {
        // Update the display periodically
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdateTemperatureDisplay();
            timer = 0f;
        }
    }
    
    public void UpdateTemperatureDisplay()
    {
        if (temperatureText == null) return;
        
        if (TemperatureManager.Instance != null)
        {
            int currentTemp = TemperatureManager.Instance.currentTemperature;
            temperatureText.text = string.Format(format, currentTemp);
        }
        else
        {
            temperatureText.text = string.Format(format, 0);
        }
    }
    
    // Call this method when the temperature is changed through the slider
    public void OnTemperatureChanged(float value)
    {
        UpdateTemperatureDisplay();
    }
} 