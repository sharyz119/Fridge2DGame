using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayFabDebugUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Text statusText;
    [SerializeField] private Button exportButton;
    [SerializeField] private Toggle logToggle;
    
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F10;
    [SerializeField] private bool showInBuild = false;
    
    private PlayFabManager playFabManager;
    private PlayFabDataExporter dataExporter;
    private bool isVisible = false;
    
    private void Start()
    {
        // Find required components
        playFabManager = FindObjectOfType<PlayFabManager>();
        dataExporter = FindObjectOfType<PlayFabDataExporter>();
        
        // Set up UI elements if they exist
        if (exportButton != null)
        {
            exportButton.onClick.AddListener(ExportData);
        }
        
        if (logToggle != null)
        {
            logToggle.onValueChanged.AddListener(ToggleVerboseLogging);
        }
        
        // Hide debug UI by default
        SetDebugUIVisible(false);
        
        // Don't show in non-development builds unless explicitly requested
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (!showInBuild)
        {
            gameObject.SetActive(false);
        }
        #endif
    }
    
    private void Update()
    {
        // Toggle debug panel with key press
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugUI();
        }
        
        // Update status text if visible
        if (isVisible && statusText != null && playFabManager != null)
        {
            UpdateStatusText();
        }
    }
    
    private void UpdateStatusText()
    {
        string status = "PlayFab Status:\n";
        status += $"Initialized: {playFabManager.IsInitialized}\n";
        status += $"Statistics API: {(playFabManager.IsStatisticsEnabled ? "Enabled" : "Disabled")}\n";
        status += $"Session ID: {playFabManager.SessionId}\n";
        status += $"Session Duration: {Time.realtimeSinceStartup:F1}s\n";
        
        if (statusText != null)
        {
            statusText.text = status;
        }
    }
    
    public void ToggleDebugUI()
    {
        SetDebugUIVisible(!isVisible);
    }
    
    private void SetDebugUIVisible(bool visible)
    {
        isVisible = visible;
        if (debugPanel != null)
        {
            debugPanel.SetActive(visible);
        }
    }
    
    public void ExportData()
    {
        if (playFabManager != null)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // Use WebGL-specific export for browser
            if (dataExporter != null)
            {
                dataExporter.ExportForWebGL("statistics");
            }
            else
            {
                Debug.LogWarning("PlayFabDataExporter not found!");
            }
            #else
            // Use normal file export for desktop/editor
            playFabManager.ExportDebugData();
            if (dataExporter != null)
            {
                dataExporter.ExportAllData();
            }
            #endif
            
            // Show success message
            if (statusText != null)
            {
                statusText.text = "Data export triggered!\nCheck logs for details.";
            }
        }
        else
        {
            Debug.LogWarning("PlayFabManager not found!");
        }
    }
    
    public void ToggleVerboseLogging(bool enabled)
    {
        // This could be expanded to change PlayFabManager's verboseDebug setting
        // For now, it just logs the state change
        Debug.Log($"Verbose logging set to: {enabled}");
    }
} 