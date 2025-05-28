/*
 * Fridge Organization Game - WebGLErrorHandler.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: WebGL-specific error handling and debugging system that manages browser-specific
 * issues, JavaScript integration errors, and web deployment challenges. Ensures robust
 * performance across different web browsers and platforms.
 * 
 * Key Responsibilities:
 * - WebGL error detection and handling
 * - Browser compatibility issue management
 * - JavaScript-Unity communication error handling
 * - Web deployment debugging support
 * - Cross-platform web performance monitoring
 */

using UnityEngine;
using System;
using System.Collections.Generic;

public class WebGLErrorHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private int maxErrors = 10;

    [Header("UI References")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private UnityEngine.UI.Text errorText;

    private Queue<string> errorMessages = new Queue<string>();
    private bool isActive = false;

    private void Awake()
    {
        // Only enable in WebGL builds or if explicitly testing in editor
        #if !UNITY_WEBGL && !UNITY_EDITOR
        enabled = false;
        return;
        #endif

        // Listen for unhandled exceptions
        Application.logMessageReceived += HandleLog;

        // Set up UI
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Clean up event handler
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            // Format error message
            string errorMsg = $"[{DateTime.Now.ToString("HH:mm:ss")}] {type}: {logString}";
            
            // Store in our queue
            errorMessages.Enqueue(errorMsg);
            
            // Trim queue if too large
            while (errorMessages.Count > maxErrors)
            {
                errorMessages.Dequeue();
            }
            
            // Update UI
            if (showOnScreen)
            {
                UpdateErrorUI();
            }
            
            // Log WebGL-friendly message
            if (logToConsole)
            {
                PrintWebGLError(errorMsg, stackTrace);
            }
        }
    }
    
    private void UpdateErrorUI()
    {
        // Make sure panel is showing
        if (errorPanel != null && !errorPanel.activeSelf)
        {
            errorPanel.SetActive(true);
        }
        
        // Update error text
        if (errorText != null)
        {
            string allErrors = string.Join("\n", errorMessages.ToArray());
            errorText.text = allErrors;
        }
    }
    
    public void ClearErrors()
    {
        errorMessages.Clear();
        if (errorText != null)
        {
            errorText.text = "";
        }
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }
    
    public void ToggleErrorPanel()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(!errorPanel.activeSelf);
        }
    }
    
    // Special method that outputs error info in a way WebGL console can read
    private void PrintWebGLError(string errorMsg, string stackTrace)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Use simpler logging for WebGL to avoid circular error references
        Debug.Log("WebGLError: " + errorMsg.Replace("\n", " "));
        
        // Split stack trace into lines to avoid giant console dumps
        if (!string.IsNullOrEmpty(stackTrace))
        {
            string[] lines = stackTrace.Split('\n');
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    Debug.Log("Stack: " + line.Trim());
                }
            }
        }
        #else
        // In editor, use standard verbose logging
        Debug.LogError($"{errorMsg}\n{stackTrace}");
        #endif
    }
    
    // Add PlayFab specific debugging info if we detect PlayFab errors
    public void ReportPlayFabState()
    {
        try
        {
            PlayFabManager playFabMgr = FindObjectOfType<PlayFabManager>();
            if (playFabMgr != null)
            {
                string message = "PlayFab Debug State:";
                message += $"\nInitialized: {playFabMgr.IsInitialized}";
                message += $"\nSession ID: {playFabMgr.SessionId}";
                message += $"\nUser ID: {playFabMgr.UserId}";
                Debug.Log(message);
                
                // Trigger debug data export if available
                playFabMgr.ExportDebugData();
            }
            else
            {
                Debug.Log("PlayFabManager not found for error reporting");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error while reporting PlayFab state: {e.Message}");
        }
    }
} 
