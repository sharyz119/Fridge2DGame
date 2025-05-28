/*
 * Fridge Organization Game - PlayFabManager.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Comprehensive PlayFab integration system that handles all data collection, user authentication,
 * and analytics logging for research purposes. This is the primary data pipeline that captures detailed
 * player behavior and game metrics for educational research analysis.
 * 
 * Key Responsibilities:
 * - User authentication and session management
 * - Detailed event logging (placements, temperature changes, scores)
 * - Statistics tracking and updating with retry logic
 * - Error handling and network resilience
 * - Data export and research analytics support
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }
    
    [Header("PlayFab Settings")]
    [Tooltip("Your PlayFab Title ID from the PlayFab Developer Portal")]
    [SerializeField] private string titleId = "1E510B"; // Make this inspectable in the Unity Editor
    
    [Space]
    [Header("Debug Settings")]
    [Tooltip("Enable verbose debugging to see detailed PlayFab operations")]
    [SerializeField] private bool verboseDebug = false;
    
    private bool isInitialized = false;
    private string playFabId;
    private string sessionId;
    private UserData userData;
    private int loginRetryCount = 0;
    private const int MAX_LOGIN_RETRIES = 3;
    private DateTime sessionStartTime; // Track when the session started
    
    // Statistic update type
    private enum StatisticUpdateType { Set, Increment }
    
    // Flag to track if statistics are available
    private bool statisticsApiEnabled = true;
    
    // Public properties for accessing private variables safely
    public bool IsInitialized => isInitialized;
    public string PlayFabId => playFabId;
    public string SessionId => sessionId;
    public string UserId => userData?.UserId;
    public bool IsStatisticsEnabled => statisticsApiEnabled;
    
    // Reference to the data exporter
    private PlayFabDataExporter dataExporter;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find or create data exporter
            dataExporter = FindObjectOfType<PlayFabDataExporter>();
            if (dataExporter == null && Application.isEditor)
            {
                // Only auto-create in editor for debugging
                GameObject exporterObj = new GameObject("PlayFabDataExporter");
                dataExporter = exporterObj.AddComponent<PlayFabDataExporter>();
                DontDestroyOnLoad(exporterObj);
            }
            
            // Set debug logging level for PlayFab
            if (verboseDebug)
            {
                Debug.Log("Enabling verbose PlayFab debug logging");
                // Different approaches depending on PlayFab SDK version
                try
                {
                    // Try using reflection to set the appropriate debug level
                    // This handles different SDK versions that might use different enum names
                    var playFabSettingsType = typeof(PlayFabSettings);
                    
                    // First try using PlayFabSettings.DebugLevel property
                    var debugLevelProperty = playFabSettingsType.GetProperty("DebugLevel");
                    if (debugLevelProperty != null)
                    {
                        // Find the "verbose" or highest level in the enum
                        Type enumType = debugLevelProperty.PropertyType;
                        object highestValue = null;
                        
                        // Look for a value named "Verbose" or "All" in the enum
                        foreach (var value in Enum.GetValues(enumType))
                        {
                            string valueName = Enum.GetName(enumType, value);
                            if (valueName == "Verbose" || valueName == "All")
                            {
                                highestValue = value;
                                break;
                            }
                            // If we don't find a specific name, just use the highest numeric value
                            highestValue = value;
                        }
                        
                        if (highestValue != null)
                        {
                            debugLevelProperty.SetValue(null, highestValue);
                            Debug.Log($"Set PlayFab debug level to: {highestValue}");
                        }
                    }
                    // Try using PlayFabSettings.LogLevel field
                    else
                    {
                        var logLevelField = playFabSettingsType.GetField("LogLevel");
                        if (logLevelField != null)
                        {
                            // Similar approach for field
                            Type enumType = logLevelField.FieldType;
                            object highestValue = null;
                            
                            foreach (var value in Enum.GetValues(enumType))
                            {
                                string valueName = Enum.GetName(enumType, value);
                                if (valueName == "Verbose" || valueName == "All")
                                {
                                    highestValue = value;
                                    break;
                                }
                                // If we don't find a specific name, just use the highest numeric value
                                highestValue = value;
                            }
                            
                            if (highestValue != null)
                            {
                                logLevelField.SetValue(null, highestValue);
                                Debug.Log($"Set PlayFab log level to: {highestValue}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Could not find debug level property or field in PlayFabSettings");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not set PlayFab debug level: {e.Message}");
                }
            }
            
            InitializePlayFab();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePlayFab()
    {
        Debug.Log("Initializing PlayFab...");
        
        // Verify title ID
        if (string.IsNullOrEmpty(titleId) || titleId == "1E510B" /* default value */)
        {
            Debug.LogWarning("PlayFab TitleId is empty or using the default value. Please set your actual PlayFab TitleId in the inspector!");
            Debug.Log("Continuing with default TitleId, but PlayFab operations may fail.");
        }
        
        // Set title ID
        PlayFabSettings.TitleId = titleId;
        
        // Get UserData instance
        userData = FindObjectOfType<UserData>();
        if (userData == null)
        {
            Debug.LogWarning("UserData not found, creating new instance");
            GameObject userDataObj = new GameObject("UserData");
            userData = userDataObj.AddComponent<UserData>();
            DontDestroyOnLoad(userDataObj); // Ensure it persists
        }
        
        // Make sure we have a valid session ID
        if (string.IsNullOrEmpty(userData.SessionId))
        {
            userData.SessionId = Guid.NewGuid().ToString();
            Debug.Log($"Generated new SessionId: {userData.SessionId}");
        }
        sessionId = userData.SessionId;
        
        // Ensure we have a valid user ID
        if (string.IsNullOrEmpty(userData.UserId))
        {
            userData.UserId = Guid.NewGuid().ToString();
            Debug.Log($"Generated new UserId: {userData.UserId}");
        }
        
        // Debug logging to verify data
        Debug.Log($"Connecting to PlayFab with TitleId: {titleId}");
        Debug.Log($"Using UserId: {userData.UserId}");
        
        // Login with existing user ID
        LoginWithCustomID();
    }
    
    private void LoginWithCustomID()
    {
        try
        {
            // Ensure we have a valid user ID
            if (string.IsNullOrEmpty(userData.UserId))
            {
                userData.UserId = Guid.NewGuid().ToString();
                Debug.Log($"Generated new UserId before login: {userData.UserId}");
            }
            
            var request = new LoginWithCustomIDRequest
            {
                CustomId = userData.UserId,
                CreateAccount = true, // Create account if it doesn't exist
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };
            
            Debug.Log($"Attempting PlayFab login with CustomId: {userData.UserId}");
            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during PlayFab login setup: {e.Message}");
        }
    }
    
    private void OnLoginSuccess(LoginResult result)
    {
        loginRetryCount = 0; // Reset retry counter on success
        playFabId = result.PlayFabId;
        isInitialized = true;
        Debug.Log($"PlayFab login successful! PlayFabId: {playFabId}");
        
        // Initialize placement statistics if they don't exist
        InitializeStatistics();
        
        // Record session start
        LogSessionStart();
        
        // Update player display name with default naming
        UpdatePlayerDisplayName();
    }
    
    // Initialize placement statistics for new users
    private void InitializeStatistics()
    {
        try
        {
            // Get existing statistics first
            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest(),
                result => {
                    // Check which statistics need to be created
                    bool hasCorrectPlacements = false;
                    bool hasIncorrectPlacements = false;
                    
                    foreach (var stat in result.Statistics)
                    {
                        if (stat.StatisticName == "CorrectPlacements")
                            hasCorrectPlacements = true;
                        else if (stat.StatisticName == "IncorrectPlacements")
                            hasIncorrectPlacements = true;
                    }
                    
                    // Create a list of statistics to initialize if needed
                    var statsToCreate = new List<StatisticUpdate>();
                    
                    if (!hasCorrectPlacements)
                        statsToCreate.Add(new StatisticUpdate { StatisticName = "CorrectPlacements", Value = 0 });
                        
                    if (!hasIncorrectPlacements)
                        statsToCreate.Add(new StatisticUpdate { StatisticName = "IncorrectPlacements", Value = 0 });
                    
                    // Only call API if we need to create statistics
                    if (statsToCreate.Count > 0)
                    {
                        Debug.Log($"Creating {statsToCreate.Count} missing player statistics");
                        
                        PlayFabClientAPI.UpdatePlayerStatistics(
                            new UpdatePlayerStatisticsRequest { Statistics = statsToCreate },
                            updateResult => Debug.Log("Successfully initialized missing player statistics"),
                            error => Debug.LogError($"Failed to initialize player statistics: {error.ErrorMessage}")
                        );
                    }
                },
                error => {
                    Debug.LogError($"Failed to get player statistics: {error.ErrorMessage}");
                    
                    // If we can't get statistics, try to create both from scratch
                    var statsToCreate = new List<StatisticUpdate>
                    {
                        new StatisticUpdate { StatisticName = "CorrectPlacements", Value = 0 },
                        new StatisticUpdate { StatisticName = "IncorrectPlacements", Value = 0 }
                    };
                    
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        new UpdatePlayerStatisticsRequest { Statistics = statsToCreate },
                        updateResult => Debug.Log("Successfully created initial player statistics"),
                        createError => Debug.LogError($"Failed to create initial statistics: {createError.ErrorMessage}")
                    );
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing statistics: {e.Message}");
        }
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        loginRetryCount++;
        
        Debug.LogError($"PlayFab login failed: {error.ErrorMessage}. Error code: {error.Error}, HTTP status: {error.HttpStatus}");
        
        if (loginRetryCount < MAX_LOGIN_RETRIES)
        {
            Debug.Log($"Retrying PlayFab login (Attempt {loginRetryCount+1}/{MAX_LOGIN_RETRIES})...");
            
            // Generate a new user ID if needed
            if (error.Error == PlayFabErrorCode.InvalidParams)
            {
                userData.UserId = Guid.NewGuid().ToString();
                Debug.Log($"Generated new UserId after error: {userData.UserId}");
            }
            
            // Wait longer between retries
            float retryDelay = 2f * loginRetryCount;
            Debug.Log($"Will retry in {retryDelay} seconds");
            InvokeWithDelay(LoginWithCustomID, retryDelay);
        }
        else
        {
            Debug.LogWarning($"Failed to connect to PlayFab after {MAX_LOGIN_RETRIES} attempts. Analytics will be limited.");
            // Continue game without PlayFab if needed
        }
    }
    
    // Update the player's display name
    private void UpdatePlayerDisplayName(string customName = "")
    {
        if (!isInitialized) return;
        
        // Generate a display name for the player
        string displayName;
        if (!string.IsNullOrEmpty(customName))
        {
            displayName = customName;
        }
        else
        {
            // Use a combination of platform and a random number for the default name
            string platform = userData?.Platform ?? "Unknown";
            string randomSuffix = UnityEngine.Random.Range(1000, 9999).ToString();
            displayName = $"{platform}_{randomSuffix}";
        }
        
        try
        {
            // Update the player's display name
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = displayName
            };
            
            PlayFabClientAPI.UpdateUserTitleDisplayName(request,
                result => Debug.Log($"Display name updated to: {result.DisplayName}"),
                error => {
                    Debug.LogWarning($"Failed to update display name: {error.ErrorMessage}");
                    
                    // Try with a simpler name if failed
                    if (!string.IsNullOrEmpty(customName))
                    {
                        // Don't retry with the same custom name
                        Debug.LogWarning("Custom name update failed, not retrying.");
                        return;
                    }
                    
                    var fallbackRequest = new UpdateUserTitleDisplayNameRequest
                    {
                        DisplayName = $"Player_{UnityEngine.Random.Range(10000, 99999)}"
                    };
                    
                    PlayFabClientAPI.UpdateUserTitleDisplayName(fallbackRequest,
                        fallbackResult => Debug.Log($"Fallback display name updated to: {fallbackResult.DisplayName}"),
                        fallbackError => Debug.LogError($"Failed to update fallback display name: {fallbackError.ErrorMessage}")
                    );
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating display name: {e.Message}");
        }
    }
    
    // Record session start
    public void LogSessionStart()
    {
        if (!isInitialized) return;
        
        try
        {
            // Initialize session start time
            sessionStartTime = DateTime.UtcNow;
            
            var eventData = new Dictionary<string, object>
            {
                { "device_model", SystemInfo.deviceModel },
                { "operating_system", SystemInfo.operatingSystem },
                { "platform", userData.Platform },
                { "game_version", userData.GameVersion }
            };
            
            LogEvent("session_start", eventData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging session start: {e.Message}");
        }
    }
    
    // Record session end
    public void LogSessionEnd(int finalScore, float accuracy, int attempts, int correctPlacements)
    {
        if (!isInitialized) return;
        
        var eventData = new Dictionary<string, object>
        {
            { "final_score", finalScore },
            { "accuracy", accuracy },
            { "attempts", attempts },
            { "correct_placements", correctPlacements },
            { "session_duration", Time.realtimeSinceStartup }
        };
        
        LogEvent("session_end", eventData);
        
        // Only update statistics if the API is enabled
        if (statisticsApiEnabled)
        {
            // Update player statistics
            UpdatePlayerStatistics("HighScore", finalScore);
            UpdatePlayerStatistics("TotalGames", 1, StatisticUpdateType.Increment);
        }
    }
    
    // Record food placement
    public void LogFoodPlacement(string foodType, string targetZone, Vector3 position, bool isCorrect, float temperature)
    {
        if (!isInitialized) return;
        
        try {
            Debug.Log($"LogFoodPlacement: {foodType} in {targetZone}, Correct={isCorrect}, Temp={temperature}");
            
            // Create a simple event with basic data
            var eventData = new Dictionary<string, object>
            {
                { "food_type", foodType },
                { "target_zone", targetZone },
                { "is_correct", isCorrect ? "true" : "false" }, // Use string instead of bool
                { "temperature", temperature.ToString("F1") } // Use string instead of float
            };
            
            // Log directly with the custom event API - but don't block game flow
            LogCustomEvent("food_placement", eventData);
            
            // Start a background process to update statistics
            StartPlacementStatisticsUpdateAsync(foodType, targetZone, isCorrect, temperature);
            
            // Log that we're continuing game flow
            Debug.Log($"Continuing game flow after logging placement of {foodType} in {targetZone}");
        }
        catch (Exception e) {
            Debug.LogError($"Error in LogFoodPlacement: {e.Message}");
        }
    }
    
    // Process statistics update in background
    private void StartPlacementStatisticsUpdateAsync(string foodType, string targetZone, bool isCorrect, float temperature)
    {
        // Update the appropriate statistic directly
        string statName = isCorrect ? "CorrectPlacements" : "IncorrectPlacements";
        
        Debug.Log($"Updating placement statistic: {statName} for {foodType}");
        
        // Add direct event for tracking (always works even if statistics fail)
        LogCustomEvent(isCorrect ? "CorrectPlacement" : "IncorrectPlacement", new Dictionary<string, object> {
            { "food_type", foodType },
            { "zone", targetZone },
            { "temperature", temperature.ToString("F1") }
        });
        
        // Store event data in PlayStream instead of modifying statistics directly
        // This avoids conflicts when multiple updates happen in quick succession
        LogCustomEvent("placement_stat", new Dictionary<string, object> {
            { "statistic_name", statName },
            { "food_type", foodType },
            { "zone", targetZone },
            { "temperature", temperature.ToString("F1") },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        });
        
        // Use a unique ID for this game session + timestamp to avoid conflicts
        string sessionSpecificStatName = $"{statName}_{DateTime.UtcNow.Ticks}";
        
        // Create a session-specific statistic that won't conflict with others
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = sessionSpecificStatName,
                        Value = 1
                    }
                }
            },
            sessionResult => {
                Debug.Log($"Successfully created session-specific statistic: {sessionSpecificStatName}");
                // Now try to update the main statistics with retry logic
                UpdateStatisticWithRetry(statName);
            },
            sessionError => {
                Debug.LogWarning($"Failed to create session-specific stat {sessionSpecificStatName}: {sessionError.ErrorMessage}");
                // Still try to update the main statistics
                UpdateStatisticWithRetry(statName);
            }
        );
    }
    
    // Retry logic for updating statistics to handle conflicts
    private void UpdateStatisticWithRetry(string statName, int retryCount = 0, int maxRetries = 3)
    {
        if (retryCount >= maxRetries)
        {
            Debug.LogWarning($"Failed to update {statName} after {maxRetries} attempts - will try again later");
            
            // Instead of giving up entirely, schedule another attempt with a longer delay
            // This helps handle high-traffic scenarios where conflicts persist
            if (retryCount == maxRetries)
            {
                float longDelay = 5.0f;
                Debug.Log($"Scheduling final attempt to update {statName} after {longDelay} seconds");
                InvokeWithDelay(() => UpdateStatisticWithRetry(statName, 0, maxRetries), longDelay);
            }
            return;
        }
        
        // Get current statistic value first
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            result => {
                int currentValue = 0;
                bool found = false;
                
                // Find the current value
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == statName)
                    {
                        currentValue = stat.Value;
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    // Create with initial value of 1
                    Debug.Log($"Statistic {statName} not found, creating with initial value 1");
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        new UpdatePlayerStatisticsRequest
                        {
                            Statistics = new List<StatisticUpdate>
                            {
                                new StatisticUpdate
                                {
                                    StatisticName = statName,
                                    Value = 1
                                }
                            }
                        },
                        createResult => Debug.Log($"Created {statName} with value 1"),
                        createError => {
                            // Handle error by checking Error type and HTTP code separately
                            if (createError.Error == PlayFabErrorCode.ServiceUnavailable ||
                                createError.HttpCode.Equals("409"))
                            {
                                // Use exponential backoff for retries
                                float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                                Debug.Log($"Conflict when creating {statName}, retrying in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                                InvokeWithDelay(() => UpdateStatisticWithRetry(statName, retryCount + 1, maxRetries), backoffDelay);
                            }
                            else
                            {
                                Debug.LogError($"Failed to create {statName}: {createError.ErrorMessage}");
                            }
                        }
                    );
                }
                else
                {
                    // Increment the value
                    int newValue = currentValue + 1;
                    Debug.Log($"Updating {statName} from {currentValue} to {newValue} (attempt {retryCount+1}/{maxRetries})");
                    
                    var request = new UpdatePlayerStatisticsRequest
                    {
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate
                            {
                                StatisticName = statName,
                                Value = newValue
                            }
                        }
                    };
                    
                    // Update the statistic
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        request,
                        updateResult => Debug.Log($"Successfully updated {statName} from {currentValue} to {newValue}"),
                        error => {
                            // Handle error by checking Error type and HTTP code separately
                            if (error.Error == PlayFabErrorCode.ServiceUnavailable ||
                                error.HttpCode.Equals("409"))
                            {
                                // Use exponential backoff for retries
                                float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                                Debug.Log($"Conflict when updating {statName}, retrying in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                                InvokeWithDelay(() => UpdateStatisticWithRetry(statName, retryCount + 1, maxRetries), backoffDelay);
                            }
                            else
                            {
                                Debug.LogError($"Failed to update {statName}: {error.ErrorMessage}");
                            }
                        }
                    );
                }
            },
            error => {
                Debug.LogError($"Failed to get statistics: {error.ErrorMessage}");
                
                // If we can't get the current value, retry after a delay with exponential backoff
                float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                Debug.Log($"Retrying get statistics for {statName} in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                InvokeWithDelay(() => UpdateStatisticWithRetry(statName, retryCount + 1, maxRetries), backoffDelay);
            }
        );
    }
    
    // Helper method for Invoke with parameters
    private void InvokeWithDelay(Action action, float delay)
    {
        StartCoroutine(InvokeRoutine(action, delay));
    }
    
    private IEnumerator InvokeRoutine(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action();
    }
    
    // Record score change
    public void LogScoreChange(int points, string reason)
    {
        if (!isInitialized) return;
        
        var eventData = new Dictionary<string, object>
        {
            { "points", points },
            { "reason", reason }
        };
        
        LogEvent("score_change", eventData);
    }
    
    // Record temperature change
    public void LogTemperatureChange(float temperature)
    {
        if (!isInitialized) return;
        
        try
        {
            Debug.Log($"LogTemperatureChange: {temperature}°C");
            
            // Get current timestamp for this temperature change
            string timestamp = DateTime.UtcNow.ToString("o");
            
            // Create event data with temperature value and timestamp
            var eventData = new Dictionary<string, object>
            {
                { "temperature", temperature.ToString("F1") },
                { "timestamp", timestamp },
                { "session_id", sessionId }
            };
            
            // Log as event with more detailed data
            LogCustomEvent("temperature_change", eventData);
            
            // Create a unique temperature statistic for this session that won't conflict
            string sessionTemp = $"Temp_{DateTime.UtcNow.Ticks}";
            int tempValue = Mathf.RoundToInt(temperature * 10); // Store with precision (15.5 becomes 155)
            
            PlayFabClientAPI.UpdatePlayerStatistics(
                new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate
                        {
                            StatisticName = sessionTemp,
                            Value = tempValue
                        }
                    }
                },
                result => Debug.Log($"Created temperature record {sessionTemp} with value {tempValue}"),
                error => Debug.LogWarning($"Failed to record temperature: {error.ErrorMessage}")
            );
            
            // Also update LastTemperature statistic using retry logic
            UpdateLastTemperature(Mathf.RoundToInt(temperature));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in LogTemperatureChange: {e.Message}");
        }
    }
    
    // Update LastTemperature statistic with retry logic
    private void UpdateLastTemperature(int temperature, int retryCount = 0, int maxRetries = 3)
    {
        if (retryCount >= maxRetries)
        {
            Debug.LogError($"Failed to update LastTemperature after {maxRetries} attempts");
            return;
        }
        
        PlayFabClientAPI.UpdatePlayerStatistics(
            new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = "LastTemperature",
                        Value = temperature
                    }
                }
            },
            result => Debug.Log($"Updated LastTemperature statistic to {temperature}"),
            error => {
                // Handle error by checking Error type and HTTP code separately
                if (error.Error == PlayFabErrorCode.ServiceUnavailable ||
                    error.HttpCode.Equals("409"))
                {
                    // Use exponential backoff for retries
                    float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                    Debug.Log($"Conflict when updating LastTemperature, retrying in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                    InvokeWithDelay(() => UpdateLastTemperature(temperature, retryCount + 1, maxRetries), backoffDelay);
                }
            }
        );
    }
    
    // Record game start
    public void LogGameStart()
    {
        if (!isInitialized) return;
        LogEvent("game_start", null);
    }
    
    /// <summary>
    /// Logs the end of a game session with comprehensive gameplay data
    /// </summary>
    public void LogGameEnd(int score, float accuracy, string grade, int temperatureScore = 0)
    {
        if (!IsInitialized) return;

        try
        {
            // Create unique identifier for this specific game session
            string gameCompletionId = $"Game_{sessionId}_{DateTime.UtcNow.Ticks}";
            string timestamp = DateTime.UtcNow.ToString("o");
            float gameDuration = (float)(DateTime.UtcNow - sessionStartTime).TotalSeconds;
            
            // Get detailed game metrics
            int correctItems = 0;
            int incorrectItems = 0;
            int temperature = 0;
            string itemPlacementDetails = "";
            
            // Get data from LifeAndScoreManager if available
            if (LifeAndScoreManager.Instance != null)
            {
                correctItems = LifeAndScoreManager.Instance.GetCorrectPlacementsCount();
                incorrectItems = LifeAndScoreManager.Instance.GetTotalPlacementsCount() - correctItems;
                itemPlacementDetails = LifeAndScoreManager.Instance.GetDetailedItemPlacementData();
            }
            
            // Get temperature from TemperatureManager if available
            if (TemperatureManager.Instance != null)
            {
                temperature = TemperatureManager.Instance.currentTemperature;
            }
            
            Debug.Log($"Logging game completion: Score={score}, CorrectItems={correctItems}, IncorrectItems={incorrectItems}, Temp={temperature}°C");
            
            // Build game session statistics with all required metrics
            Dictionary<string, object> gameData = new Dictionary<string, object>
            {
                // Core metrics
                { "score", score },
                { "accuracy", accuracy },
                { "grade", grade },
                { "correct_items_count", correctItems },
                { "incorrect_items_count", incorrectItems },
                { "temperature_setting", temperature },
                
                // Session context
                { "session_id", sessionId },
                { "game_id", gameCompletionId },
                { "timestamp", timestamp },
                { "game_duration", gameDuration },
                
                // User context
                { "participant_id", userData?.ParticipantId ?? "unknown" }
            };
            
            // Log full game completion event
            LogCustomEvent("game_completed", gameData);
            
            // Record the item placement details as a separate event to avoid size limitations
            Dictionary<string, object> placementData = new Dictionary<string, object>
            {
                { "game_id", gameCompletionId },
                { "session_id", sessionId },
                { "timestamp", timestamp },
                { "participant_id", userData?.ParticipantId ?? "unknown" },
                { "item_placement_details", itemPlacementDetails }
            };
            
            LogCustomEvent("item_placements", placementData);
            
            // Increment TotalGames statistic - with retry logic
            UpdateTotalGamesWithRetry();
            
            // Store this game session in player data for persistence
            StoreGameSessionData(gameCompletionId, gameData);
            
            // Update high score only if current score is higher - with retry logic
            UpdateHighScoreWithRetry(score);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in LogGameEnd: {e.Message}");
        }
    }
    
    /// <summary>
    /// Stores a complete game session data record in user's PlayFab data
    /// </summary>
    private void StoreGameSessionData(string gameId, Dictionary<string, object> gameData)
    {
        try
        {
            // Convert game data to JSON
            string jsonData = JsonUtility.ToJson(new Serializable_Dictionary<string, object>(gameData));
            
            // Create a request to store this specific game session
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { gameId, jsonData }
                },
                Permission = UserDataPermission.Public
            };
            
            PlayFabClientAPI.UpdateUserData(
                dataRequest,
                result => Debug.Log($"Successfully stored game session data for {gameId}"),
                error => Debug.LogError($"Failed to store game session data: {error.ErrorMessage}")
            );
            
            // Also record a statistic for this game session
            PlayFabClientAPI.UpdatePlayerStatistics(
                new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate
                        {
                            StatisticName = gameId,
                            Value = (int)gameData["score"]
                        },
                        new StatisticUpdate
                        {
                            StatisticName = "CorrectItemsLast",
                            Value = (int)gameData["correct_items_count"]
                        },
                        new StatisticUpdate
                        {
                            StatisticName = "IncorrectItemsLast",
                            Value = (int)gameData["incorrect_items_count"]
                        },
                        new StatisticUpdate
                        {
                            StatisticName = "LastTemperature",
                            Value = (int)gameData["temperature_setting"]
                        }
                    }
                },
                result => Debug.Log($"Successfully recorded statistics for game {gameId}"),
                error => Debug.LogWarning($"Failed to record game statistics: {error.ErrorMessage}")
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error storing game session data: {e.Message}");
        }
    }
    
    // Update high score with retry logic
    private void UpdateHighScoreWithRetry(int score, int retryCount = 0, int maxRetries = 3)
    {
        if (retryCount >= maxRetries)
        {
            Debug.LogError($"Failed to update HighScore after {maxRetries} attempts");
            return;
        }
        
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            result => {
                int currentHighScore = 0;
                bool found = false;
                
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == "HighScore")
                    {
                        currentHighScore = stat.Value;
                        found = true;
                        break;
                    }
                }
                
                // If no high score found or new score is higher
                if (!found || score > currentHighScore)
                {
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        new UpdatePlayerStatisticsRequest
                        {
                            Statistics = new List<StatisticUpdate>
                            {
                                new StatisticUpdate
                                {
                                    StatisticName = "HighScore",
                                    Value = score
                                }
                            }
                        },
                        updateResult => Debug.Log($"Updated HighScore to {score}"),
                        updateError => {
                            // Handle error by checking Error type and HTTP code separately
                            if (updateError.Error == PlayFabErrorCode.ServiceUnavailable ||
                                updateError.HttpCode.Equals("409"))
                            {
                                // Use exponential backoff for retries
                                float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                                Debug.Log($"Conflict when updating HighScore, retrying in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                                InvokeWithDelay(() => UpdateHighScoreWithRetry(score, retryCount + 1, maxRetries), backoffDelay);
                            }
                        }
                    );
                }
                else
                {
                    Debug.Log($"Current high score ({currentHighScore}) is higher than new score ({score}). Not updating.");
                }
            },
            error => {
                Debug.LogError($"Failed to get HighScore: {error.ErrorMessage}");
                
                // If we can't get the current value, retry after a short delay
                if (retryCount < maxRetries - 1)
                {
                    Debug.Log($"Retrying get HighScore in 0.5s...");
                    InvokeWithDelay(() => UpdateHighScoreWithRetry(score, retryCount + 1, maxRetries), 0.5f);
                }
                else
                {
                    // Last try - just attempt to set the high score
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        new UpdatePlayerStatisticsRequest
                        {
                            Statistics = new List<StatisticUpdate>
                            {
                                new StatisticUpdate
                                {
                                    StatisticName = "HighScore",
                                    Value = score
                                }
                            }
                        },
                        createResult => Debug.Log($"Set HighScore to {score} (fallback)"),
                        createError => Debug.LogError($"Failed to set HighScore: {createError.ErrorMessage}")
                    );
                }
            }
        );
    }
    
    // Update TotalGames count with retry logic
    private void UpdateTotalGamesWithRetry(int retryCount = 0, int maxRetries = 3)
    {
        if (retryCount >= maxRetries)
        {
            Debug.LogError($"Failed to update TotalGames after {maxRetries} attempts");
            return;
        }
        
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            result => {
                int currentGamesPlayed = 0;
                bool found = false;
                
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == "TotalGames")
                    {
                        currentGamesPlayed = stat.Value;
                        found = true;
                        break;
                    }
                }
                
                int newGamesPlayed = currentGamesPlayed + 1;
                
                var request = new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate
                        {
                            StatisticName = "TotalGames",
                            Value = newGamesPlayed
                        }
                    }
                };
                
                // Update the statistic
                PlayFabClientAPI.UpdatePlayerStatistics(
                    request,
                    updateResult => Debug.Log($"Updated TotalGames from {currentGamesPlayed} to {newGamesPlayed}"),
                    updateError => {
                        // Handle error by checking Error type and HTTP code separately
                        if (updateError.Error == PlayFabErrorCode.ServiceUnavailable ||
                            updateError.HttpCode.Equals("409"))
                        {
                            // Use exponential backoff for retries
                            float backoffDelay = Mathf.Pow(2, retryCount) * 0.5f; // 0.5, 1, 2 seconds...
                            Debug.Log($"Conflict when updating TotalGames, retrying in {backoffDelay}s (attempt {retryCount+1}/{maxRetries})");
                            InvokeWithDelay(() => UpdateTotalGamesWithRetry(retryCount + 1, maxRetries), backoffDelay);
                        }
                        else
                        {
                            Debug.LogError($"Failed to update TotalGames: {updateError.ErrorMessage}");
                        }
                    }
                );
            },
            error => {
                Debug.LogError($"Failed to get TotalGames: {error.ErrorMessage}");
                
                // If we can't get the current value, retry after a short delay
                if (retryCount < maxRetries - 1)
                {
                    Debug.Log($"Retrying get TotalGames in 0.5s...");
                    InvokeWithDelay(() => UpdateTotalGamesWithRetry(retryCount + 1, maxRetries), 0.5f);
                }
                else
                {
                    // Last try - just attempt to set total games to 1
                    PlayFabClientAPI.UpdatePlayerStatistics(
                        new UpdatePlayerStatisticsRequest
                        {
                            Statistics = new List<StatisticUpdate>
                            {
                                new StatisticUpdate
                                {
                                    StatisticName = "TotalGames",
                                    Value = 1
                                }
                            }
                        },
                        createResult => Debug.Log("Set TotalGames to 1 (fallback)"),
                        createError => Debug.LogError($"Failed to set TotalGames: {createError.ErrorMessage}")
                    );
                }
            }
        );
    }
    
    // Generic event logging method
    private void LogEvent(string eventName, Dictionary<string, object> eventData)
    {
        if (!isInitialized) 
        {
            Debug.LogWarning($"Cannot log event '{eventName}' - PlayFab is not initialized");
            return;
        }
        
        // Check if we have a valid PlayFab ID (meaning we're logged in)
        if (string.IsNullOrEmpty(playFabId))
        {
            Debug.LogWarning($"Cannot log event '{eventName}' - Not logged in to PlayFab");
            return;
        }
        
        try
        {
            // Validate event data to avoid WebGL errors
            if (eventData != null)
            {
                ValidateEventData(eventData);
            }
            
            // Log via the standardized method
            LogCustomEvent(eventName, eventData);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error logging event '{eventName}': {e.Message}");
            
            // On WebGL, store failed events locally if supported
            #if UNITY_WEBGL
            if (dataExporter != null && Application.isEditor)
            {
                try
                {
                    var exportData = new Dictionary<string, object>
                    {
                        { "eventName", eventName },
                        { "eventData", eventData },
                        { "timestamp", DateTime.UtcNow.ToString("o") }
                    };
                    dataExporter.SaveToFile(exportData, $"FailedEvent_{eventName}_{DateTime.UtcNow.Ticks}.json");
                }
                catch { } // Suppress any errors during fallback
            }
            #endif
        }
    }
    
    // Update player statistics with a dictionary of statistic name/value pairs
    private void UpdatePlayerStatistics(Dictionary<string, int> statistics)
    {
        if (!isInitialized || !statisticsApiEnabled) return;
        
        if (statistics == null || statistics.Count == 0)
        {
            Debug.LogWarning("UpdatePlayerStatistics called with empty statistics dictionary");
            return;
        }
        
        // Create a list for all statistics updates
        var statisticUpdates = new List<StatisticUpdate>();
        
        foreach (var stat in statistics)
        {
            statisticUpdates.Add(new StatisticUpdate
            {
                StatisticName = stat.Key,
                Value = stat.Value
            });
        }
        
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = statisticUpdates
        };
        
        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result => Debug.Log($"Successfully updated {statisticUpdates.Count} statistics"),
            error => {
                // Check if this is a permissions error
                if (error.Error == PlayFabErrorCode.APINotEnabledForGameClientAccess)
                {
                    HandleStatisticsApiDisabled(error.ErrorMessage);
                }
                else
                {
                    Debug.LogError($"Failed to update statistics: {error.ErrorMessage}");
                }
            }
        );
    }
    
    // Update player statistics
    private void UpdatePlayerStatistics(string statisticName, int value, StatisticUpdateType updateType = StatisticUpdateType.Set)
    {
        if (!isInitialized) return;
        
        // Skip if we already know statistics API is not enabled
        if (!statisticsApiEnabled)
        {
            if (verboseDebug)
                Debug.LogWarning($"Skipping statistic update for '{statisticName}' because the API is not enabled on PlayFab.");
            return;
        }
        
        // For incremental updates, get current value first
        if (updateType == StatisticUpdateType.Increment)
        {
            PlayFabClientAPI.GetPlayerStatistics(
                new GetPlayerStatisticsRequest(),
                result => {
                    int currentValue = 0;
                    foreach (var stat in result.Statistics)
                    {
                        if (stat.StatisticName == statisticName)
                        {
                            currentValue = stat.Value;
                            break;
                        }
                    }
                    
                    // Update with incremental value
                    SendStatisticsUpdate(statisticName, currentValue + value);
                },
                error => {
                    // Check if this is a permissions error
                    if (error.Error == PlayFabErrorCode.APINotEnabledForGameClientAccess)
                    {
                        HandleStatisticsApiDisabled(error.ErrorMessage);
                    }
                    else
                    {
                        Debug.LogError($"Failed to get player statistics: {error.ErrorMessage}");
                        // If unable to get current statistics, send incremental value directly
                        SendStatisticsUpdate(statisticName, value);
                    }
                }
            );
        }
        else
        {
            // Direct value update
            SendStatisticsUpdate(statisticName, value);
        }
    }
    
    private void SendStatisticsUpdate(string statisticName, int value)
    {
        // Skip if we already know statistics API is not enabled
        if (!statisticsApiEnabled)
        {
            if (verboseDebug)
                Debug.LogWarning($"Skipping sending statistic update for '{statisticName}' because the API is not enabled on PlayFab.");
            return;
        }
        
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = statisticName,
                    Value = value
                }
            }
        };
        
        PlayFabClientAPI.UpdatePlayerStatistics(
            request,
            result => Debug.Log($"Successfully updated statistic: {statisticName} = {value}"),
            error => {
                // Check if this is a permissions error
                if (error.Error == PlayFabErrorCode.APINotEnabledForGameClientAccess)
                {
                    HandleStatisticsApiDisabled(error.ErrorMessage);
                }
                else
                {
                    Debug.LogError($"Failed to update statistic: {error.ErrorMessage}");
                }
            }
        );
    }
    
    // Handle the case where Statistics API is not enabled
    private void HandleStatisticsApiDisabled(string errorMessage)
    {
        // Only log this once
        if (statisticsApiEnabled)
        {
            statisticsApiEnabled = false;
            Debug.LogWarning(
                "PlayFab Statistics API is not enabled for client access. To use statistics, you need to:\n" +
                "1. Log into your PlayFab Game Manager dashboard: https://developer.playfab.com\n" +
                "2. Go to your title's settings\n" +
                "3. Select 'API Features'\n" +
                "4. Enable 'Client Player Statistics API'\n" +
                "Game will continue without statistics tracking."
            );
            Debug.LogError($"Statistics API error: {errorMessage}");
        }
    }
    
    private void OnApplicationQuit()
    {
        // Only try to log if properly initialized AND logged in with a valid session
        if (isInitialized && !string.IsNullOrEmpty(playFabId) && PlayFabClientAPI.IsClientLoggedIn())
        {
            try
            {
                Debug.Log("Application quitting, attempting to log final analytics");
                
                // Record app quit event
                var eventData = new Dictionary<string, object>
                {
                    { "session_duration", Time.realtimeSinceStartup }
                };
                
                // Send event synchronously but don't block with Thread.Sleep on WebGL
                // Use direct method instead of LogCustomEvent to avoid potential login check issues
                WriteEventDirectly("app_quit", eventData);
                
                #if !UNITY_WEBGL
                // Give event some time to send, but don't do this on WebGL as it doesn't support threading
                System.Threading.Thread.Sleep(500);
                #endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error during app quit event logging: {e.Message}");
                // No need to rethrow, we're quitting anyway
            }
        }
        else
        {
            Debug.Log("Application quitting, but PlayFab not initialized or not logged in, skipping analytics");
        }
    }
    
    // A simplified direct write method that bypasses some checks for shutdown scenarios
    private void WriteEventDirectly(string eventName, Dictionary<string, object> eventData)
    {
        try
        {
            // Additional safety check
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                Debug.LogWarning($"Skipping event {eventName} - client not logged in during shutdown");
                return;
            }
            
            var eventBody = new Dictionary<string, object>();
            
            // Add basic metadata
            eventBody["session_id"] = sessionId ?? "unknown_session";
            eventBody["client_time"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            // Convert all event data to strings
            if (eventData != null)
            {
                foreach (var kvp in eventData)
                {
                    if (string.IsNullOrEmpty(kvp.Key)) continue;
                    eventBody[kvp.Key] = kvp.Value?.ToString() ?? "null";
                }
            }
            
            // Create and send request
            var request = new WriteClientPlayerEventRequest
            {
                EventName = eventName,
                Body = eventBody
            };
            
            PlayFabClientAPI.WritePlayerEvent(
                request,
                result => Debug.Log($"Successfully logged final {eventName} event"),
                error => Debug.LogWarning($"Failed to log final event: {error.ErrorMessage}")
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error in WriteEventDirectly: {e.Message}");
        }
    }
    
    // Additional WebGL-friendly methods
    
    // Handle graceful shutdown for WebGL
    private void OnApplicationPause(bool pauseStatus)
    {
        // In WebGL, OnApplicationQuit may not be called reliably
        // Handle the case where the game is being paused (browser tab switched, etc.)
        #if UNITY_WEBGL
        if (pauseStatus && isInitialized)
        {
            // Record pause event which may be the last chance in WebGL
            var eventData = new Dictionary<string, object>
            {
                { "session_duration", Time.realtimeSinceStartup },
                { "reason", "application_pause" }
            };
            
            LogEvent("app_pause", eventData);
        }
        #endif
    }
    
    // Validate data to prevent assertion errors
    private bool ValidateEventData(Dictionary<string, object> eventData)
    {
        if (eventData == null) return true; // Null is valid, we handle it elsewhere
        
        foreach (var pair in eventData)
        {
            // Check for problematic values that could cause WebGL issues
            if (pair.Value is Vector3)
            {
                Vector3 vec = (Vector3)pair.Value;
                if (float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z) ||
                    float.IsInfinity(vec.x) || float.IsInfinity(vec.y) || float.IsInfinity(vec.z))
                {
                    Debug.LogWarning($"Invalid Vector3 value for key '{pair.Key}': {vec}. Replacing with zero vector.");
                    eventData[pair.Key] = Vector3.zero;
                }
            }
            else if (pair.Value is float)
            {
                float val = (float)pair.Value;
                if (float.IsNaN(val) || float.IsInfinity(val))
                {
                    Debug.LogWarning($"Invalid float value for key '{pair.Key}': {val}. Replacing with 0.");
                    eventData[pair.Key] = 0f;
                }
            }
        }
        
        return true;
    }
    
    // Public method to export data on demand
    public void ExportAnalyticsData()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Cannot export analytics data - PlayFab is not initialized");
            return;
        }
        
        if (dataExporter != null)
        {
            dataExporter.ExportAllData();
            Debug.Log("Manually triggered PlayFab data export");
        }
        else
        {
            Debug.LogWarning("PlayFabDataExporter not found - cannot export data");
        }
    }
    
    // Export specific data for debugging
    public void ExportDebugData()
    {
        if (!isInitialized) return;
        
        // Create a debug data collection
        var debugData = new Dictionary<string, object>
        {
            { "PlayFabId", playFabId },
            { "UserId", userData?.UserId },
            { "SessionId", sessionId },
            { "IsInitialized", isInitialized },
            { "StatisticsApiEnabled", statisticsApiEnabled },
            { "SessionDuration", Time.realtimeSinceStartup },
            { "TitleId", PlayFabSettings.TitleId },
            { "Platform", userData?.Platform },
            { "GameVersion", userData?.GameVersion }
        };
        
        string json = JsonUtility.ToJson(new Serializable_Dictionary<string, object>(debugData), true);
        Debug.Log($"PlayFab Debug Data: {json}");
        
        // If data exporter exists, save it there too
        if (dataExporter != null)
        {
            dataExporter.SaveToFile(debugData, "PlayFabDebugData.json");
        }
    }
    
    // Simple serializable dictionary to help with JSON conversion of debug data
    [Serializable]
    private class Serializable_Dictionary<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<TValue> values = new List<TValue>();
        
        public Serializable_Dictionary(Dictionary<TKey, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
    }
    
    /// <summary>
    /// Saves the user's entered ID to PlayFab using the UserData API
    /// </summary>
    /// <param name="userId">The user-entered ID to save</param>
    public void SaveUserIdToPlayFab(string userId)
    {
        if (!isInitialized)
        {
            Debug.LogError("Cannot record user ID: PlayFab not initialized");
            return;
        }
        
        try
        {
            // Sanitize input - remove any special characters that could cause JSON issues
            string sanitizedUserId = System.Text.RegularExpressions.Regex.Replace(userId, "[^a-zA-Z0-9_]", "").Trim();
            
            if (string.IsNullOrEmpty(sanitizedUserId))
            {
                Debug.LogError("User ID is empty after sanitization. Please enter alphanumeric characters only.");
                return;
            }
            
            Debug.Log($"Saving user ID to PlayFab: {sanitizedUserId}");
            
            // 1. First log as a custom event - this is more reliable
            var userIdEvent = new Dictionary<string, object>
            {
                { "participant_id", sanitizedUserId }
            };
            
            LogCustomEvent("participant_id_entered", userIdEvent);
            Debug.Log($"Logged participant_id_entered event with ID: {sanitizedUserId}");
            
            // 2. Save as player title data - this is accessible via Game Manager
            // Ensure it's valid JSON by wrapping in quotes
            var dataRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "ParticipantID", JsonUtility.ToJson(sanitizedUserId) }
                },
                Permission = UserDataPermission.Public
            };
            
            PlayFabClientAPI.UpdateUserData(
                dataRequest,
                result => {
                    Debug.Log($"Successfully stored ParticipantID in user data: {sanitizedUserId}");
                    
                    // If successful, also update the player's display name with more uniqueness
                    UpdateParticipantDisplayName(sanitizedUserId);
                },
                error => {
                    Debug.LogError($"Failed to store user data: {error.ErrorMessage}");
                    
                    // Try with an alternative format if JSON conversion was the issue
                    if (error.ErrorMessage.Contains("JSON") || (error.ErrorDetails != null && error.ErrorDetails.ContainsKey("Data")))
                    {
                        Debug.Log("Trying alternative JSON format for participant ID...");
                        
                        var wrapper = new StringWrapper { Value = sanitizedUserId };
                        string jsonString = JsonUtility.ToJson(wrapper);
                        
                        var retryRequest = new UpdateUserDataRequest
                        {
                            Data = new Dictionary<string, string>
                            {
                                { "ParticipantID", jsonString }
                            },
                            Permission = UserDataPermission.Public
                        };
                        
                        PlayFabClientAPI.UpdateUserData(
                            retryRequest,
                            retryResult => {
                                Debug.Log($"Successfully stored ParticipantID with alternative format: {sanitizedUserId}");
                                UpdateParticipantDisplayName(sanitizedUserId);
                            },
                            retryError => {
                                Debug.LogError($"Failed retry to store user data: {retryError.ErrorMessage}");
                                
                                // Log the participant ID using a custom event instead as ultimate fallback
                                LogCustomEvent("participant_id_fallback", 
                                    new Dictionary<string, object> { 
                                        { "id", sanitizedUserId },
                                        { "method", "custom_event_only" }
                                    });
                                
                                UpdateParticipantDisplayName(sanitizedUserId);
                            }
                        );
                    }
                    else
                    {
                        // Try updating display name anyway as a fallback
                        UpdateParticipantDisplayName(sanitizedUserId);
                    }
                }
            );
            
            // 3. Also record in internal data model
            if (userData != null)
            {
                userData.ParticipantId = sanitizedUserId;
                Debug.Log($"Saved participant ID to local UserData: {sanitizedUserId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error recording user ID: {e.Message}");
        }
    }
    
    // Helper class for JSON serialization
    [Serializable]
    private class StringWrapper
    {
        public string Value;
    }
    
    // Backup method to try a different API call
    private void TryUpdatePlayerTitleData(string participantId)
    {
        try 
        {
            Debug.Log("Attempting alternative method to save participant ID...");
            
            // Generate a unique name that's unlikely to conflict
            string uniqueName = $"P{UnityEngine.Random.Range(100000, 999999)}";
            
            // Try a different API call as fallback with more uniqueness
            var displayRequest = new UpdateUserTitleDisplayNameRequest { 
                DisplayName = uniqueName 
            };
            
            PlayFabClientAPI.UpdateUserTitleDisplayName(
                displayRequest,
                result => {
                    Debug.Log($"Successfully updated display name as fallback: {result.DisplayName}");
                    
                    // Now that we have a display name, try to store the participant ID as a stat
                    try {
                        string statName = $"ParticipantID_{DateTime.UtcNow.Ticks}";
                        int idAsNumber;
                        
                        // Try to convert the ID to a number if possible (for statistics)
                        if (int.TryParse(participantId, out idAsNumber)) {
                            PlayFabClientAPI.UpdatePlayerStatistics(
                                new UpdatePlayerStatisticsRequest {
                                    Statistics = new List<StatisticUpdate> {
                                        new StatisticUpdate {
                                            StatisticName = statName,
                                            Value = idAsNumber
                                        }
                                    }
                                },
                                statResult => Debug.Log($"Saved participant ID as statistic: {statName}"),
                                statError => Debug.LogWarning($"Could not save ID as statistic: {statError.ErrorMessage}")
                            );
                        }
                    }
                    catch (Exception e) {
                        Debug.LogWarning($"Error attempting to save ID as statistic: {e.Message}");
                    }
                },
                error => {
                    // If we get a conflict here too, just log the ID to events
                    Debug.LogError($"Failed to update display name fallback: {error.ErrorMessage}");
                    
                    // Last resort - log as event only
                    LogCustomEvent("participant_id_last_resort", 
                        new Dictionary<string, object> { 
                            { "id", participantId },
                            { "timestamp", DateTime.UtcNow.Ticks.ToString() }
                        });
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in fallback method: {e.Message}");
        }
    }
    
    // Helper method to update display name with participant ID
    private void UpdateParticipantDisplayName(string participantId)
    {
        try
        {
            // Create a unique display name with timestamp AND random number to ensure uniqueness
            string uniqueId = DateTime.UtcNow.Ticks.ToString() + UnityEngine.Random.Range(1000, 9999);
            string safeDisplayName = $"P{uniqueId}_{participantId}";
            
            // Make sure display name follows required format (3-25 alphanumeric characters)
            if (safeDisplayName.Length < 3)
            {
                safeDisplayName = safeDisplayName.PadRight(3, '0');
            }
            else if (safeDisplayName.Length > 25)
            {
                safeDisplayName = safeDisplayName.Substring(0, 25);
            }
            
            Debug.Log($"Attempting to update display name to: {safeDisplayName}");
            
            // Update DisplayName with retry mechanism
            UpdateDisplayNameWithRetry(safeDisplayName, 0, 3);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in UpdateParticipantDisplayName: {e.Message}");
        }
    }
    
    // Retry mechanism for updating display name
    private void UpdateDisplayNameWithRetry(string displayName, int retryCount, int maxRetries)
    {
        if (retryCount >= maxRetries)
        {
            Debug.LogWarning($"Failed to update display name after {maxRetries} attempts");
            return;
        }
        
        // Create a unique name if this is a retry
        string nameToUse = displayName;
        if (retryCount > 0)
        {
            // Add more randomness on each retry
            string suffix = UnityEngine.Random.Range(10000, 99999).ToString();
            nameToUse = $"P{suffix}";
            
            if (nameToUse.Length > 25)
            {
                nameToUse = nameToUse.Substring(0, 25);
            }
            
            Debug.Log($"Retry attempt {retryCount} with new name: {nameToUse}");
        }
        
        var displayNameRequest = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = nameToUse
        };
        
        PlayFabClientAPI.UpdateUserTitleDisplayName(
            displayNameRequest,
            nameResult => Debug.Log($"Successfully updated display name to: {nameResult.DisplayName}"),
            nameError => {
                if (nameError.HttpCode != null && nameError.HttpCode.Equals("409"))
                {
                    Debug.LogWarning($"Display name conflict detected for: {nameToUse}. Retrying...");
                    
                    // Wait a moment before retrying (increasing delay with each retry)
                    float delay = 0.5f * (retryCount + 1);
                    InvokeWithDelay(() => UpdateDisplayNameWithRetry(displayName, retryCount + 1, maxRetries), delay);
                }
                else
                {
                    Debug.LogError($"Failed to update display name: {nameError.ErrorMessage}");
                }
            }
        );
    }
    
    // Standardized method to log custom events to PlayFab
    public void LogCustomEvent(string eventName, Dictionary<string, object> eventData)
    {
        // First check if PlayFab is properly initialized and logged in
        if (!isInitialized)
        {
            Debug.LogWarning($"Cannot log event '{eventName}' - PlayFab is not initialized");
            return;
        }
        
        // Enhanced login check using PlayFab's IsClientLoggedIn method
        if (string.IsNullOrEmpty(playFabId) || !PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning($"Cannot log event '{eventName}' - Not logged in to PlayFab");
            return;
        }
        
        try
        {
            Debug.Log($"Logging custom event: {eventName}");
            
            // Prepare a clean event body with only string values
            var eventBody = new Dictionary<string, object>();
            
            // Add basic metadata to all events - using our own prefix to avoid reserved names
            eventBody["session_id"] = sessionId ?? "unknown_session";
            eventBody["client_time"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"); // Changed from client_timestamp
            
            // Convert all event data to strings to avoid serialization issues
            if (eventData != null)
            {
                foreach (var kvp in eventData)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        continue;
                    
                    // Skip reserved field names (timestamp is reserved by PlayFab)
                    if (kvp.Key.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning($"Skipping reserved field name 'timestamp' in event {eventName}");
                        continue;
                    }
                    
                    // Convert value to string, handling nulls and special types
                    string strValue = "null";
                    if (kvp.Value != null)
                    {
                        // Handle Vector3 specially
                        if (kvp.Value is Vector3 vector3Value)
                        {
                            strValue = $"{vector3Value.x},{vector3Value.y},{vector3Value.z}";
                        }
                        else if (kvp.Value is bool boolValue)
                        {
                            strValue = boolValue ? "true" : "false";
                        }
                        else
                        {
                            strValue = kvp.Value.ToString();
                        }
                    }
                    
                    eventBody[kvp.Key] = strValue;
                }
            }
            
            // Create the request
            var request = new WriteClientPlayerEventRequest
            {
                EventName = eventName,
                Body = eventBody
            };
            
            // Send the event to PlayFab - note this is asynchronous and does not block
            Debug.Log($"Sending event '{eventName}' to PlayFab with {eventBody.Count} properties");
            
            // Don't await this operation, let it run in the background
            PlayFabClientAPI.WritePlayerEvent(
                request,
                result => {
                    Debug.Log($"Successfully logged event '{eventName}' to PlayFab");
                    
                    // If this is verbose debug, also log the event data
                    if (verboseDebug)
                    {
                        string eventDataStr = string.Join(", ", eventBody.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                        Debug.Log($"Event data: {eventDataStr}");
                    }
                },
                error => {
                    Debug.LogError($"Failed to log event '{eventName}' to PlayFab: {error.ErrorMessage}");
                    
                    // If it failed due to connectivity issues, try to save locally for later upload
                    if (dataExporter != null && Application.isEditor)
                    {
                        var exportData = new Dictionary<string, object>
                        {
                            { "eventName", eventName },
                            { "eventData", eventBody },
                            { "event_time", DateTime.UtcNow.ToString("o") } // Changed from timestamp
                        };
                        dataExporter.SaveToFile(exportData, $"FailedEvent_{eventName}_{DateTime.UtcNow.Ticks}.json");
                        Debug.Log($"Saved failed event to local file for later processing");
                    }
                }
            );
            
            // Log that we're proceeding without waiting for PlayFab
            Debug.Log($"Continuing without waiting for PlayFab event response: {eventName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in LogCustomEvent: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
    
    // Record restart button clicks
    public void LogRestartButtonClick()
    {
        // Check if we're properly initialized and logged in
        if (!isInitialized) 
        {
            Debug.LogWarning("Cannot log restart click - PlayFab is not initialized");
            return;
        }
        
        // Check if we have a valid PlayFab ID (meaning we're logged in)
        if (string.IsNullOrEmpty(playFabId))
        {
            Debug.LogWarning("Cannot log restart click - Not logged in to PlayFab");
            return;
        }
        
        try
        {
            Debug.Log("=== Restart button clicked, logging to PlayFab ===");
            
            // Log event with non-reserved field names
            var eventData = new Dictionary<string, object>
            {
                { "game_time", Time.realtimeSinceStartup },
                { "event_time", DateTime.UtcNow.ToString("o") } // Changed from timestamp
            };
            
            // Log the restart event, but don't wait for PlayFab
            LogEvent("restart_button_click", eventData);
            
            // Increment the restart counter in the background, don't block game restart
            IncrementRestartStatisticAsync();
            
            // Continue with game restart immediately, don't wait for PlayFab
            Debug.Log("=== Continuing with game restart, PlayFab operations running in background ===");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in LogRestartButtonClick: {e.Message}");
            // Still continue with game restart even if logging failed
            Debug.Log("=== Continuing with game restart despite error ===");
        }
    }
    
    // Separate method to increment restart statistic without blocking
    private void IncrementRestartStatisticAsync()
    {
        string statName = "RestartCount";
        
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            result => {
                int currentValue = 0;
                bool found = false;
                
                foreach (var stat in result.Statistics)
                {
                    if (stat.StatisticName == statName)
                    {
                        currentValue = stat.Value;
                        found = true;
                        Debug.Log($"Current {statName} value: {currentValue}");
                        break;
                    }
                }
                
                int newValue = currentValue + 1;
                Debug.Log($"Updating {statName} to {newValue}");
                
                PlayFabClientAPI.UpdatePlayerStatistics(
                    new UpdatePlayerStatisticsRequest
                    {
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate
                            {
                                StatisticName = statName,
                                Value = newValue
                            }
                        }
                    },
                    updateResult => Debug.Log($"Successfully updated {statName} to {newValue}"),
                    updateError => Debug.LogError($"Failed to update {statName}: {updateError.ErrorMessage}")
                );
            },
            error => {
                Debug.LogError($"Failed to get {statName}: {error.ErrorMessage}");
                
                // If we can't get the current value, try to increment with a value of 1
                PlayFabClientAPI.UpdatePlayerStatistics(
                    new UpdatePlayerStatisticsRequest
                    {
                        Statistics = new List<StatisticUpdate>
                        {
                            new StatisticUpdate
                            {
                                StatisticName = statName,
                                Value = 1
                            }
                        }
                    },
                    createResult => Debug.Log($"Created new statistic {statName} with value 1"),
                    createError => Debug.LogError($"Failed to create {statName}: {createError.ErrorMessage}")
                );
            }
        );
    }
} 
