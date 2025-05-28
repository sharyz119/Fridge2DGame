/*
 * Fridge Organization Game - GameAnalytics.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Local analytics and engagement tracking system that monitors player behavior,
 * session duration, and user engagement metrics. Works alongside PlayFab to provide comprehensive
 * data collection for educational research and game improvement.
 * 
 * Key Responsibilities:
 * - Session duration tracking and engagement ratio calculation
 * - User engagement metrics and interaction patterns
 * - Local event queuing and data persistence
 * - Completion time analysis and performance metrics
 * - Application lifecycle handling and data cleanup
 */

using UnityEngine;
using System.Collections.Generic;
using System;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;

public class GameAnalytics : MonoBehaviour
{
    public static GameAnalytics Instance { get; private set; }
    private bool isInitialized = false;
    private UserData userData;
    private PlayFabManager playFabManager;
    
    // Event queue for batch processing
    private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
    private string sessionId;
    private string userId;
    private DateTime lastEventSendTime;
    private float eventSendInterval = 30f; // Send events every 30 seconds or when explicitly called
    
    // Internal event structure
    private class AnalyticsEvent
    {
        public string EventName;
        public Dictionary<string, object> EventData;
    }
    
    // game duration tracking
    private DateTime gameStartTime;
    private bool isGameActive = false;
    
    // User behavior tracking
    private int restartButtonClickCount = 0;
    private int tutorialStepsCompleted = 0;
    private int totalTutorialSteps = 6; // Assume tutorial has 6 steps
    private List<int> scoreChanges = new List<int>();

    // Custom event tracking
    private Dictionary<string, int> customEventCounts = new Dictionary<string, int>();
    
    // A/B testing support
    private string abTestGroup = null;
    private Dictionary<string, string> abTestVariants = new Dictionary<string, string>();
    
    // Engagement metrics
    private Dictionary<string, float> engagementMetrics = new Dictionary<string, float>
    {
        { "session_time", 0f },
        { "active_time", 0f },
        { "interaction_count", 0 }
    };
    
    // Session start time (for calculating total duration)
    private DateTime sessionStartTime;
    private DateTime lastActiveTime;
    private float inactiveThreshold = 30f; // 30 seconds of inactivity considered inactive
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAnalytics();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAnalytics()
    {
        try
        {
            Debug.Log("初始化游戏分析系统...");
            
            // get UserData instance
            userData = FindObjectOfType<UserData>();
            if (userData == null)
            {
                Debug.Log("创建新的UserData对象...");
                GameObject userDataObj = new GameObject("UserData");
                userData = userDataObj.AddComponent<UserData>();
            }

            // Set session and user IDs
            sessionId = userData.SessionId;
            userId = userData.UserId;
            
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                userData.SessionId = sessionId;
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                userData.UserId = userId;
            }

            // get PlayFabManager instance
            playFabManager = FindObjectOfType<PlayFabManager>();
            if (playFabManager == null)
            {
                Debug.LogWarning("PlayFabManager not found, creating new instance");
                GameObject playFabObj = new GameObject("PlayFabManager");
                playFabManager = playFabObj.AddComponent<PlayFabManager>();
            }
            
            isInitialized = true;
            lastEventSendTime = DateTime.UtcNow;
            Debug.Log("Game analytics system initialized successfully!");
            
            // log session start after initialization
            LogSessionStart();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Game analytics system initialization error: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    public void LogSessionStart()
    {
        if (!isInitialized) return;

        // record game start time
        gameStartTime = DateTime.UtcNow;
        sessionStartTime = DateTime.UtcNow;
        lastActiveTime = DateTime.UtcNow;
        isGameActive = true;
        
        // use PlayFab to record session start
        if (playFabManager != null)
        {
            playFabManager.LogSessionStart();
        }
        
        Debug.Log($"Session started: {userData.SessionId}");
    }

    /// <summary>
    /// Log end of game with detailed stats
    /// </summary>
    public void LogGameEnd(int score, float accuracy, string grade, float elapsedTime = 0)
    {
        try
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>
            {
                { "action", "game_end" },
                { "score", score },
                { "accuracy", accuracy },
                { "grade", grade },
                { "elapsed_time", elapsedTime },
                { "timestamp", GetCurrentTimestamp() },
                { "session_id", sessionId },
                { "user_id", userId }
            };
            
            // Add to queue
            eventQueue.Enqueue(new AnalyticsEvent
            {
                EventName = "game_end",
                EventData = eventData
            });
            
            // Try to send immediately
            SendEvents();
            
            Debug.Log($"Logged game end: Score={score}, Accuracy={accuracy:F1}%, Grade={grade}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to log game end: {e.Message}");
        }
    }

    // determine grade based on score
    private string DetermineGrade(int finalScore)
    {
        if (finalScore >= 90) return "A";
        if (finalScore >= 80) return "B";
        if (finalScore >= 70) return "C";
        if (finalScore >= 60) return "D";
        return "F";
    }

    // reset session statistics
    private void ResetSessionStats()
    {
        tutorialStepsCompleted = 0;
        scoreChanges.Clear();
    }

    public void LogPlacement(string foodType, string targetZone, Vector3 position, bool isCorrect, float temperature)
    {
        if (!isInitialized) return;

        // use PlayFab to record food placement
        if (playFabManager != null)
        {
            playFabManager.LogFoodPlacement(foodType, targetZone, position, isCorrect, temperature);
        }
    }

    public void LogTemperature(float temperature)
    {
        if (!isInitialized) return;

        // use PlayFab to record temperature changey
        if (playFabManager != null)
        {
            playFabManager.LogTemperatureChange(temperature);
        }
    }

    public void LogScoreChange(int points, string reason)
    {
        if (!isInitialized) return;

        // add to score change history
        scoreChanges.Add(points);

        // use PlayFab to record score change
        if (playFabManager != null)
        {
            playFabManager.LogScoreChange(points, reason);
        }
        
        Debug.Log($"Score changed: {points} points, reason: {reason}");
    }

    // record game start
    public void LogGameStart()
    {
        // use PlayFab to record game start
        if (playFabManager != null)
        {
            playFabManager.LogGameStart();
        }
        Debug.Log("Game started");
    }

    // record game end
    public void LogGameEnd(int finalScore, float accuracy, string grade)
    {
        // use PlayFab to record game end
        if (playFabManager != null)
        {
            playFabManager.LogGameEnd(finalScore, accuracy, grade);
        }
        Debug.Log($"Game ended - Score: {finalScore}, Accuracy: {accuracy}%, Grade: {grade}");
    }

    // record score change
    public void LogScore(int score)
    {
        Debug.Log($"Score changed to {score}");
    }

    // record temperature change
    public void LogTemperature(int temperature)
    {
        Debug.Log($"Temperature changed to {temperature}°C");
    }

    // record food placement
    public void LogPlacement(string foodType, string zone, Vector3 position, bool isCorrect, int temperature)
    {
        Debug.Log($"{foodType} placed in {zone} (correct: {isCorrect})");
    }

    // record restart game
    public void LogRestartButtonClick()
    {
        if (!isInitialized) return;

        restartButtonClickCount++;
        
        // calculate current game duration
        TimeSpan currentDuration = DateTime.UtcNow - gameStartTime;

        Debug.Log($"Game restarted ({restartButtonClickCount} times, this session)");
    
        gameStartTime = DateTime.UtcNow;
    }

    // record tutorial progress
    public void LogTutorialProgress(int stepNumber, string stepName)
    {
        if (!isInitialized) return;

        if (stepNumber > tutorialStepsCompleted)
        {
            tutorialStepsCompleted = stepNumber;
        }
        
        Debug.Log($"Tutorial progress: step {stepNumber}/{totalTutorialSteps} ({stepName})");
    }

    // record error
    public void LogError(string errorType, string message)
    {
        Debug.LogError($"Error: {errorType} - {message}");
    }

    // record drag start
    public void LogItemDragStart(string foodType, Vector3 position)
    {
        Debug.Log($"Start dragging {foodType} from position ({position.x}, {position.y})");
    }

    // record correct placement
    public void LogCorrectPlacement(string foodType, string zone, Vector3 position, int temperature)
    {
        // use existing LogPlacement method
        LogPlacement(foodType, zone, position, true, temperature);
        
        Debug.Log($"{foodType} correctly placed in {zone}, temperature {temperature}°C");
    }

    // record incorrect placement
    public void LogIncorrectPlacement(string foodType, string currentZone, string correctZone, Vector3 position, int temperature)
    {
        // use existing LogPlacement method
        LogPlacement(foodType, currentZone, position, false, temperature);
        
        Debug.Log($"{foodType} incorrectly placed in {currentZone} (should be {correctZone}), temperature {temperature}°C");
    }

    // record invalid placement (not in valid area)
    public void LogInvalidPlacement(string foodType, Vector3 position, string reason, int temperature)
    {
        Debug.Log($"{foodType} placed in invalid area ({position.x}, {position.y}). Reason: {reason}");
    }

    // get current game time (seconds)
    public float GetCurrentGameTime()
    {
        if (!isGameActive) return 0;
        return (float)(DateTime.UtcNow - gameStartTime).TotalSeconds;
    }
    
    // get restart count
    public int GetRestartCount()
    {
        return restartButtonClickCount;
    }

    // update user engagement timer
    private void Update()
    {
        if (isInitialized && isGameActive)
        {
            // 更新会话时间
            engagementMetrics["session_time"] = (float)(DateTime.UtcNow - sessionStartTime).TotalSeconds;
            
            // Check user activity status
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // User is active
                if ((DateTime.UtcNow - lastActiveTime).TotalSeconds > inactiveThreshold)
                {
                    
                    LogCustomEvent("user_reengaged");
                }
                
                lastActiveTime = DateTime.UtcNow;
                engagementMetrics["interaction_count"] += 1;
            }
            
            // Update active time
            if ((DateTime.UtcNow - lastActiveTime).TotalSeconds <= inactiveThreshold)
            {
                engagementMetrics["active_time"] += Time.deltaTime;
            }
        }
        
        if (isInitialized && eventQueue.Count > 0)
        {
            // Send events if it's been more than eventSendInterval since last send
            if ((DateTime.UtcNow - lastEventSendTime).TotalSeconds >= eventSendInterval)
            {
                SendEvents();
            }
        }
    }

    // record custom event
    public void LogCustomEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!isInitialized) return;
        
        // update event count
        if (!customEventCounts.ContainsKey(eventName))
            customEventCounts[eventName] = 1;
        else
            customEventCounts[eventName]++;
        
        Debug.Log($"Custom event recorded: {eventName}");
    }
    
    // set A/B test group
    public void SetABTestGroup(string groupName)
    {
        abTestGroup = groupName;
        
        Debug.Log($"User assigned to A/B test group: {groupName}");
    }
    
    // record A/B test variant
    public void SetABTestVariant(string testName, string variantName)
    {
        abTestVariants[testName] = variantName;
        
        Debug.Log($"A/B test '{testName}' variant set to: {variantName}");
    }
    
    // record user engagement metrics
    public void LogEngagementMetrics()
    {
        if (!isInitialized) return;
        
        Debug.Log($"Engagement metrics recorded - Active: {engagementMetrics["active_time"]}s, Session: {engagementMetrics["session_duration"]}s");
    }
    
    // record UI interaction event
    public void LogUIInteraction(string elementName, string action)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "element_name", elementName },
            { "action", action },
            { "screen_name", GetCurrentScreenName() }
        };
        
        LogCustomEvent("ui_interaction", parameters);
    }
    
    // get current screen name
    private string GetCurrentScreenName()
    {
        // here can implement the logic to get the current UI screen name
        // simple implementation, can be improved as needed
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    // record feature usage frequency
    public void LogFeatureUsage(string featureName)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "feature_name", featureName },
            { "game_time", GetCurrentGameTime() }
        };
        
        LogCustomEvent("feature_usage", parameters);
    }
    
    // record learning curve data
    public void LogLearningProgress(string task, int attemptNumber, bool success, float timeSpent)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "task", task },
            { "attempt_number", attemptNumber },
            { "success", success ? 1 : 0 },
            { "time_spent", timeSpent }
        };
        
        LogCustomEvent("learning_progress", parameters);
    }
    
    // get user engagement metrics
    public Dictionary<string, float> GetEngagementMetrics()
    {
        return new Dictionary<string, float>(engagementMetrics);
    }
    
    // get A/B test group
    public string GetABTestGroup()
    {
        return abTestGroup;
    }
    
    // get A/B test variant
    public string GetABTestVariant(string testName)
    {
        if (abTestVariants.ContainsKey(testName))
            return abTestVariants[testName];
        return null;
    }
    
    // reset session, also reset engagement metrics
    public void ResetSession()
    {
        // record engagement metrics of previous session
        LogEngagementMetrics();
        
        // reset engagement metrics
        sessionStartTime = DateTime.UtcNow;
        lastActiveTime = DateTime.UtcNow;
        engagementMetrics["session_time"] = 0f;
        engagementMetrics["active_time"] = 0f;
        engagementMetrics["interaction_count"] = 0;
        
        Debug.Log("Session and engagement metrics have been reset");
    }
    
    // record engagement metrics before application quit
    private void OnApplicationQuit()
    {
        if (isInitialized && isGameActive)
        {
            LogEngagementMetrics();
            Debug.Log("Record engagement metrics before application quit");
        }
    }

    // record game completion time
    public void LogCompletionTime(float timeInSeconds, bool isFullCompletion)
    {
        if (!isInitialized) return;
        
        Debug.Log($"Game completion time recorded: {timeInSeconds} seconds, fully completed: {isFullCompletion}");
    }

    // record time spent in specific stage
    public void LogStageTime(string stageName, float timeInSeconds)
    {
        if (!isInitialized) return;
        
        Debug.Log($"Stage time recorded: {stageName} - {timeInSeconds} seconds");
    }

    // record user difficulty
    public void LogUserDifficulty(string difficultyType, string description, int attemptCount)
    {
        if (!isInitialized) return;
        
        Debug.Log($"User difficulty recorded: {difficultyType} - {description} (attempt count: {attemptCount})");
    }

    /// <summary>
    /// Get current timestamp in ISO 8601 format
    /// </summary>
    private string GetCurrentTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    
    /// <summary>
    /// Send all events in the queue to the analytics server
    /// </summary>
    private void SendEvents()
    {
        if (!isInitialized || eventQueue.Count == 0)
            return;
            
        // If we have a playFabManager, use it to send events
        if (playFabManager != null)
        {
            try
            {
                while (eventQueue.Count > 0)
                {
                    var evt = eventQueue.Dequeue();
                    if (evt != null && !string.IsNullOrEmpty(evt.EventName))
                    {
                        playFabManager.LogCustomEvent(evt.EventName, evt.EventData);
                    }
                }
                lastEventSendTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send events: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("PlayFabManager not found - cannot send events");
            // Clear the queue anyway to prevent memory buildup
            eventQueue.Clear();
        }
    }

    /// <summary>
    /// Stops the gameplay timer and records the elapsed time
    /// </summary>
    public void StopGameplayTimer()
    {
        if (!isGameActive)
        {
            Debug.LogWarning("Trying to stop gameplay timer, but game is not active");
            return;
        }
        
        // Calculate elapsed time
        TimeSpan elapsedTime = DateTime.UtcNow - gameStartTime;
        
        // Log the duration
        Debug.Log($"Game session duration: {elapsedTime.TotalSeconds:F1} seconds");
        
        // Add to session data
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "action", "game_completed" },
            { "duration_seconds", elapsedTime.TotalSeconds },
            { "timestamp", GetCurrentTimestamp() },
            { "session_id", sessionId },
            { "user_id", userId }
        };
        
        // Add to queue
        eventQueue.Enqueue(new AnalyticsEvent
        {
            EventName = "game_completed",
            EventData = eventData
        });
        
        // Mark game as not active
        isGameActive = false;
        
        // Try to send immediately
        SendEvents();
    }
} 


