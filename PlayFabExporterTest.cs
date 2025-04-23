using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test script to verify PlayFabDataExporter functionality.
/// Attach this to a GameObject with a Button component to test exporting.
/// </summary>
public class PlayFabExporterTest : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button testButton;
    [SerializeField] private Text statusText;
    
    [Header("Test Settings")]
    [SerializeField] private bool exportOnStart = false;
    [SerializeField] private bool printExportPath = true;
    
    private PlayFabManager playFabManager;
    private PlayFabDataExporter dataExporter;
    
    void Start()
    {
        // Find references
        playFabManager = FindObjectOfType<PlayFabManager>();
        dataExporter = FindObjectOfType<PlayFabDataExporter>();
        
        // Set up button
        if (testButton != null)
        {
            testButton.onClick.AddListener(TestExport);
        }
        
        // Show export path
        if (printExportPath && dataExporter != null)
        {
            Debug.Log($"PlayFab export path: {dataExporter.exportPath}");
            UpdateStatus($"Export path: {dataExporter.exportPath}");
        }
        
        // Auto export if enabled
        if (exportOnStart)
        {
            Invoke("TestExport", 3f); // Wait 3 seconds to ensure PlayFab is initialized
        }
    }
    
    /// <summary>
    /// Test the export functionality
    /// </summary>
    public void TestExport()
    {
        if (playFabManager == null || dataExporter == null)
        {
            Debug.LogError("Cannot test export - PlayFabManager or PlayFabDataExporter not found");
            UpdateStatus("Export failed - components missing");
            return;
        }
        
        // Check if PlayFab is initialized
        if (!playFabManager.IsInitialized)
        {
            Debug.LogWarning("PlayFab not initialized yet - waiting 2 seconds and trying again");
            UpdateStatus("Waiting for PlayFab initialization...");
            Invoke("TestExport", 2f);
            return;
        }
        
        // Try export debug data directly
        try
        {
            playFabManager.ExportDebugData();
            dataExporter.ExportAllData();
            Debug.Log("Test export triggered successfully");
            UpdateStatus("Export successful! Check logs for path.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during test export: {e.Message}");
            UpdateStatus($"Export error: {e.Message}");
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    /// <summary>
    /// Create a sample dictionary to test serialization
    /// </summary>
    private void TestSerializationOnly()
    {
        if (dataExporter == null) return;
        
        var testData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "TestString", "Hello World" },
            { "TestNumber", 42 },
            { "TestBool", true },
            { "TestNull", null }
        };
        
        dataExporter.SaveToFile(testData, "TestSerializationOnly.json");
    }
} 