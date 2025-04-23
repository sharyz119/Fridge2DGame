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
    
    // Analytics queue system
    private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
    private string sessionId;
    private string userId;
    private DateTime lastEventSendTime;
    private float eventSendInterval = 30f; // Send events every 30 seconds or when explicitly called
    
    // Event class for the queue
    private class AnalyticsEvent
    {
        public string EventName;
        public Dictionary<string, object> EventData;
    }
    
    // 游戏时长追踪
    private DateTime gameStartTime;
    private bool isGameActive = false;
    
    // 统计数据
    private int restartButtonClickCount = 0;
    private int tutorialStepsCompleted = 0;
    private int totalTutorialSteps = 6; // 假设教程有6个步骤
    private List<int> scoreChanges = new List<int>();

    // 自定义事件跟踪
    private Dictionary<string, int> customEventCounts = new Dictionary<string, int>();
    
    // A/B测试分组
    private string abTestGroup = null;
    private Dictionary<string, string> abTestVariants = new Dictionary<string, string>();
    
    // 用户参与度指标
    private Dictionary<string, float> engagementMetrics = new Dictionary<string, float>
    {
        { "session_time", 0f },
        { "active_time", 0f },
        { "interaction_count", 0 }
    };
    
    // 会话开始时间（用于计算总时长）
    private DateTime sessionStartTime;
    private DateTime lastActiveTime;
    private float inactiveThreshold = 30f; // 30秒无操作视为不活跃
    
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
            
            // 获取UserData实例
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

            // 获取PlayFabManager实例
            playFabManager = FindObjectOfType<PlayFabManager>();
            if (playFabManager == null)
            {
                Debug.LogWarning("未找到PlayFabManager，创建新实例");
                GameObject playFabObj = new GameObject("PlayFabManager");
                playFabManager = playFabObj.AddComponent<PlayFabManager>();
            }
            
            isInitialized = true;
            lastEventSendTime = DateTime.UtcNow;
            Debug.Log("游戏分析系统初始化成功!");
            
            // 初始化成功后立即记录会话开始
            LogSessionStart();
        }
        catch (Exception ex)
        {
            Debug.LogError($"游戏分析系统初始化错误: {ex.Message}\n堆栈: {ex.StackTrace}");
        }
    }

    public void LogSessionStart()
    {
        if (!isInitialized) return;

        // 记录游戏开始时间
        gameStartTime = DateTime.UtcNow;
        sessionStartTime = DateTime.UtcNow;
        lastActiveTime = DateTime.UtcNow;
        isGameActive = true;
        
        // 使用PlayFab记录会话开始
        if (playFabManager != null)
        {
            playFabManager.LogSessionStart();
        }
        
        Debug.Log($"会话开始: {userData.SessionId}");
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

    // 根据分数确定等级
    private string DetermineGrade(int finalScore)
    {
        if (finalScore >= 90) return "A";
        if (finalScore >= 80) return "B";
        if (finalScore >= 70) return "C";
        if (finalScore >= 60) return "D";
        return "F";
    }

    // 重置会话统计数据
    private void ResetSessionStats()
    {
        tutorialStepsCompleted = 0;
        scoreChanges.Clear();
    }

    public void LogPlacement(string foodType, string targetZone, Vector3 position, bool isCorrect, float temperature)
    {
        if (!isInitialized) return;

        // 使用PlayFab记录食物放置
        if (playFabManager != null)
        {
            playFabManager.LogFoodPlacement(foodType, targetZone, position, isCorrect, temperature);
        }
    }

    public void LogTemperature(float temperature)
    {
        if (!isInitialized) return;

        // 使用PlayFab记录温度变化
        if (playFabManager != null)
        {
            playFabManager.LogTemperatureChange(temperature);
        }
    }

    public void LogScoreChange(int points, string reason)
    {
        if (!isInitialized) return;

        // 添加到分数变化历史
        scoreChanges.Add(points);

        // 使用PlayFab记录分数变化
        if (playFabManager != null)
        {
            playFabManager.LogScoreChange(points, reason);
        }
        
        Debug.Log($"分数变化: {points} 分, 原因: {reason}");
    }

    // 记录游戏开始
    public void LogGameStart()
    {
        // 使用PlayFab记录游戏开始
        if (playFabManager != null)
        {
            playFabManager.LogGameStart();
        }
        Debug.Log("分析: 游戏开始");
    }

    // 记录游戏结束
    public void LogGameEnd(int finalScore, float accuracy, string grade)
    {
        // 使用PlayFab记录游戏结束
        if (playFabManager != null)
        {
            playFabManager.LogGameEnd(finalScore, accuracy, grade);
        }
        Debug.Log($"分析: 游戏结束 - 得分: {finalScore}, 准确度: {accuracy}%, 等级: {grade}");
    }

    // 记录分数变化
    public void LogScore(int score)
    {
        Debug.Log($"分析: 分数变更为 {score}");
    }

    // 记录温度变化
    public void LogTemperature(int temperature)
    {
        Debug.Log($"分析: 温度变更为 {temperature}°C");
    }

    // 记录食物放置
    public void LogPlacement(string foodType, string zone, Vector3 position, bool isCorrect, int temperature)
    {
        Debug.Log($"分析: {foodType} 放置在 {zone} (正确: {isCorrect})");
    }

    // 记录重启游戏
    public void LogRestartButtonClick()
    {
        if (!isInitialized) return;

        restartButtonClickCount++;
        
        // 计算当前游戏时长
        TimeSpan currentDuration = DateTime.UtcNow - gameStartTime;

        Debug.Log($"游戏重新开始 ({restartButtonClickCount} 次，本次会话)");
        
        // 重置游戏开始时间，因为游戏重新开始了
        gameStartTime = DateTime.UtcNow;
    }

    // 记录教程进度
    public void LogTutorialProgress(int stepNumber, string stepName)
    {
        if (!isInitialized) return;

        if (stepNumber > tutorialStepsCompleted)
        {
            tutorialStepsCompleted = stepNumber;
        }
        
        Debug.Log($"教程进度: 步骤 {stepNumber}/{totalTutorialSteps} ({stepName})");
    }

    // 记录错误
    public void LogError(string errorType, string message)
    {
        Debug.LogError($"分析错误: {errorType} - {message}");
    }

    // 记录拖动开始
    public void LogItemDragStart(string foodType, Vector3 position)
    {
        Debug.Log($"分析: 开始拖动 {foodType} 从位置 ({position.x}, {position.y})");
    }

    // 记录正确放置
    public void LogCorrectPlacement(string foodType, string zone, Vector3 position, int temperature)
    {
        // 使用已有的LogPlacement方法
        LogPlacement(foodType, zone, position, true, temperature);
        
        Debug.Log($"分析: {foodType} 正确放置在 {zone}，温度 {temperature}°C");
    }

    // 记录错误放置
    public void LogIncorrectPlacement(string foodType, string currentZone, string correctZone, Vector3 position, int temperature)
    {
        // 使用已有的LogPlacement方法
        LogPlacement(foodType, currentZone, position, false, temperature);
        
        Debug.Log($"分析: {foodType} 错误放置在 {currentZone} (应该是 {correctZone})，温度 {temperature}°C");
    }

    // 记录无效放置（未在有效区域内）
    public void LogInvalidPlacement(string foodType, Vector3 position, string reason, int temperature)
    {
        Debug.Log($"分析: {foodType} 放置在无效区域 ({position.x}, {position.y})。原因: {reason}");
    }

    // 获取当前游戏时长（秒）
    public float GetCurrentGameTime()
    {
        if (!isGameActive) return 0;
        return (float)(DateTime.UtcNow - gameStartTime).TotalSeconds;
    }
    
    // 获取重新开始次数
    public int GetRestartCount()
    {
        return restartButtonClickCount;
    }

    // 更新用户参与度计时器
    private void Update()
    {
        if (isInitialized && isGameActive)
        {
            // 更新会话时间
            engagementMetrics["session_time"] = (float)(DateTime.UtcNow - sessionStartTime).TotalSeconds;
            
            // 检查用户活跃状态
            if (Input.anyKey || Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                // 如果用户处于不活跃状态后变为活跃
                if ((DateTime.UtcNow - lastActiveTime).TotalSeconds > inactiveThreshold)
                {
                    // 记录用户重新变为活跃
                    LogCustomEvent("user_reengaged");
                }
                
                lastActiveTime = DateTime.UtcNow;
                engagementMetrics["interaction_count"] += 1;
            }
            
            // 更新活跃时间
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

    // 记录自定义事件
    public void LogCustomEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!isInitialized) return;
        
        // 更新事件计数
        if (!customEventCounts.ContainsKey(eventName))
            customEventCounts[eventName] = 1;
        else
            customEventCounts[eventName]++;
        
        Debug.Log($"自定义事件记录: {eventName}");
    }
    
    // 设置A/B测试组
    public void SetABTestGroup(string groupName)
    {
        abTestGroup = groupName;
        
        Debug.Log($"用户分配到A/B测试组: {groupName}");
    }
    
    // 记录A/B测试变体
    public void SetABTestVariant(string testName, string variantName)
    {
        abTestVariants[testName] = variantName;
        
        Debug.Log($"A/B测试 '{testName}' 变体设置为: {variantName}");
    }
    
    // 记录用户参与度指标
    public void LogEngagementMetrics()
    {
        if (!isInitialized) return;
        
        Debug.Log($"参与度指标记录 - 活跃: {engagementMetrics["active_time"]}秒, 会话: {engagementMetrics["session_time"]}秒");
    }
    
    // 记录界面交互事件
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
    
    // 获取当前界面名称
    private string GetCurrentScreenName()
    {
        // 这里可以实现获取当前UI界面的逻辑
        // 简单实现，可以根据需要完善
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    // 记录功能使用频率
    public void LogFeatureUsage(string featureName)
    {
        Dictionary<string, object> parameters = new Dictionary<string, object>
        {
            { "feature_name", featureName },
            { "game_time", GetCurrentGameTime() }
        };
        
        LogCustomEvent("feature_usage", parameters);
    }
    
    // 记录学习曲线数据
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
    
    // 获取用户参与度指标
    public Dictionary<string, float> GetEngagementMetrics()
    {
        return new Dictionary<string, float>(engagementMetrics);
    }
    
    // 获取A/B测试组
    public string GetABTestGroup()
    {
        return abTestGroup;
    }
    
    // 获取A/B测试变体
    public string GetABTestVariant(string testName)
    {
        if (abTestVariants.ContainsKey(testName))
            return abTestVariants[testName];
        return null;
    }
    
    // 重置会话时，同时重置参与度指标
    public void ResetSession()
    {
        // 记录之前会话的参与度数据
        LogEngagementMetrics();
        
        // 重置指标
        sessionStartTime = DateTime.UtcNow;
        lastActiveTime = DateTime.UtcNow;
        engagementMetrics["session_time"] = 0f;
        engagementMetrics["active_time"] = 0f;
        engagementMetrics["interaction_count"] = 0;
        
        Debug.Log("会话和参与度指标已重置");
    }
    
    // 应用退出前记录数据
    private void OnApplicationQuit()
    {
        if (isInitialized && isGameActive)
        {
            LogEngagementMetrics();
            Debug.Log("应用退出时记录最终参与度指标");
        }
    }

    // 记录游戏完成时间
    public void LogCompletionTime(float timeInSeconds, bool isFullCompletion)
    {
        if (!isInitialized) return;
        
        Debug.Log($"游戏完成时间记录: {timeInSeconds} 秒, 完全完成: {isFullCompletion}");
    }

    // 记录玩家在特定阶段花费的时间
    public void LogStageTime(string stageName, float timeInSeconds)
    {
        if (!isInitialized) return;
        
        Debug.Log($"阶段时间记录: {stageName} - {timeInSeconds} 秒");
    }

    // 记录用户遇到的困难
    public void LogUserDifficulty(string difficultyType, string description, int attemptCount)
    {
        if (!isInitialized) return;
        
        Debug.Log($"用户困难记录: {difficultyType} - {description} (尝试次数: {attemptCount})");
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


