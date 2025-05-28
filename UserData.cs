/*
 * Fridge Organization Game - UserData.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: User identification and session management system that handles user ID generation,
 * storage, and session tracking. Manages user preferences and provides persistent data storage
 * for research and analytics purposes.
 * 
 * Key Responsibilities:
 * - User ID generation and storage
 * - Session tracking and management
 * - Data persistence and retrieval
 * - User preference management
 * - Research participant identification
 */

using UnityEngine;
using System;

public class UserData : MonoBehaviour
{
    public static UserData Instance { get; private set; }
    
    // Make these settable from PlayFabManager if needed
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string Platform { get; private set; }
    public string GameVersion { get; private set; }
    public string ParticipantId { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUserData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeUserData()
    {
        // Generate or retrieve persisted user ID
        UserId = GetOrCreateUserId();
        // Generate a new session ID for each game session
        SessionId = Guid.NewGuid().ToString();
        // Get platform information
        #if UNITY_EDITOR
        Platform = "Unity_Editor";
        #else
        Platform = Application.platform.ToString();
        #endif
        // Get game version
        GameVersion = Application.version;
        
        // Initialize participant ID as empty - will be set when user enters it
        ParticipantId = "";

        Debug.Log($"User initialized - ID: {UserId}, Session: {SessionId}, Platform: {Platform}, Version: {GameVersion}");
    }

    private string GetOrCreateUserId()
    {
        string userId;
        try
        {
            #if UNITY_EDITOR
            // Use fixed developer ID in the editor
            userId = "dev_" + SystemInfo.deviceUniqueIdentifier;
            #else
            // Try to get existing user ID from PlayerPrefs
            userId = PlayerPrefs.GetString("UserId", "");
            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("UserId", userId);
                PlayerPrefs.Save();
            }
            #endif
            
            // Validate the user ID
            if (string.IsNullOrEmpty(userId))
            {
                // Fallback if something went wrong
                userId = Guid.NewGuid().ToString();
                Debug.LogWarning($"Generated fallback user ID: {userId}");
            }
        }
        catch (Exception e)
        {
            // Handle any exceptions that might occur
            userId = Guid.NewGuid().ToString();
            Debug.LogError($"Error getting user ID: {e.Message}. Generated fallback ID: {userId}");
        }
        
        return userId;
    }

    public string GetUserInfo()
    {
        string participantInfo = string.IsNullOrEmpty(ParticipantId) ? "Not set" : ParticipantId;
        return $"User: {UserId}\nParticipant ID: {participantInfo}\nPlatform: {Platform}\nVersion: {GameVersion}";
    }
    
    // Save participant ID to PlayerPrefs for persistence
    public void SaveParticipantId(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            ParticipantId = id;
            PlayerPrefs.SetString("ParticipantId", id);
            PlayerPrefs.Save();
            Debug.Log($"Saved participant ID to PlayerPrefs: {id}");
        }
    }
    
    // Load participant ID from PlayerPrefs on startup
    public void LoadParticipantId()
    {
        if (PlayerPrefs.HasKey("ParticipantId"))
        {
            ParticipantId = PlayerPrefs.GetString("ParticipantId");
            Debug.Log($"Loaded participant ID from PlayerPrefs: {ParticipantId}");
        }
    }
} 
