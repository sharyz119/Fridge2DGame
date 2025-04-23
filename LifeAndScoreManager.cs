using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class LifeAndScoreManager : MonoBehaviour
{
    public static LifeAndScoreManager Instance;

    private int currentScore = 0;          
    private int temperatureScore = 0;      // Separate temperature score

    
    private Dictionary<string, bool> itemPlacementResults = new Dictionary<string, bool>();

    public GameObject FinalPanel;
    public TextMeshProUGUI FinalScoreText;

    // 总尝试次数 & 正确次数
    private int totalAttempts = 0;
    private int correctAttempts = 0;

    // Total number of food items in the game
    private int totalFoodItems = 20;

    private int correctlyPlacedItems = 0;

    // 游戏状态追踪
    private DateTime gameStartTime;
    private bool isGameActive = false;
    private float lastScoreChangeTime = 0f;

    // 每个食物放置的尝试次数（防止重复放置时多次失败）
    private Dictionary<string, int> itemPlacementAttempts = new Dictionary<string, int>();

    // 食物温度范围与分数定义
    private Dictionary<string, (int minTemp, int maxTemp, string storageType)>
        foodRequirements = new Dictionary<string, (int, int, string)>
    {
        { "apple",      (4, 7,  "Drawer") },
        { "pepper",     (4, 7,  "DryBox") },
        { "egg",        (1, 4,  "TopDoor") },
        { "butter",     (1, 4,  "TopDoor") },
        { "eggplant",   (4, 7,  "DryBox") },
        { "juice",      (1, 4,  "BottomDoor") },
        { "salmon",     (1, 4,  "BottomShelf") },
        { "mango",      (1, 4,  "DryBox") },
        { "broccoli",   (1, 4,  "Drawer") },
        { "cucumber",   (1, 4,  "DryBox") },
        { "lettuce",    (1, 4,  "Drawer") },
        { "yoghurt",    (1, 4,  "BottomDoor") },
        { "jam",        (1, 4,  "MiddleDoor") },
        { "leftover",   (1, 4,  "MiddleShelf") },
        { "milk",       (1, 4,  "BottomDoor") },
        { "pasta",      (1, 4,  "MiddleShelf") },
        { "sausage",    (1, 4,  "BottomShelf") },
        { "cheese",     (1, 4,  "MiddleShelf") },
        { "pickles",    (1, 4,  "TopShelf") },
        { "mustard",    (1, 4,  "MiddleDoor") }
    };

    // Modified temperature scoring - just track if temperature is in ideal range (1-4°C)
    private bool isTemperatureCorrect = false;
    
    // Track if the last placement was correct
    private bool lastPlacementCorrect = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("LifeAndScoreManager initialized as singleton instance");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Duplicate LifeAndScoreManager destroyed");
        }
    }

    private void Start()
    {
        ResetGameState();
        StartGame();
    }
    
    // 开始游戏，记录时间，通知 Analytics
    public void StartGame()
    {
        gameStartTime = DateTime.Now;
        isGameActive = true;
        lastScoreChangeTime = Time.time;
        
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogGameStart();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log game start: {e.Message}");
            }
        }
    }

    // 核心方法：检查放置是否正确
    public bool CheckPlacement(string foodType, string currentZone, int currentTemp)
    {
        if (string.IsNullOrEmpty(foodType) || string.IsNullOrEmpty(currentZone))
        {
            Debug.LogError("Invalid parameters in CheckPlacement: foodType or currentZone is empty");
            lastPlacementCorrect = false;
            return false;
        }
        
        Debug.Log($"CheckPlacement called: foodType={foodType}, zone={currentZone}, temp={currentTemp}");
        
        // Increment total attempts only once per food type
        if (!itemPlacementAttempts.ContainsKey(foodType))
        {
            itemPlacementAttempts[foodType] = 1;
            totalAttempts++;
        }
        else
        {
            // Update the attempt count but don't increment totalAttempts again
            itemPlacementAttempts[foodType]++;
            Debug.Log($"Additional attempt for {foodType}, total attempts for this item: {itemPlacementAttempts[foodType]}");
        }
        
        bool isCorrect = false;
        bool wasCorrectBefore = itemPlacementResults.ContainsKey(foodType) && itemPlacementResults[foodType];

        if (foodRequirements.TryGetValue(foodType, out var requirements))
        {
            bool correctZone = currentZone == requirements.storageType;
            bool correctTemp = currentTemp >= requirements.minTemp && currentTemp <= requirements.maxTemp;

            Debug.Log($"Food {foodType} requirements: Zone={requirements.storageType}, Temp={requirements.minTemp}-{requirements.maxTemp}");
            Debug.Log($"Current placement: Zone={currentZone}, Temp={currentTemp}");
            Debug.Log($"Correct zone: {correctZone}, Correct temp: {correctTemp}");
            
            // If this food was correct before but is now incorrect, decrement correctAttempts
            if (wasCorrectBefore && !correctZone)
            {
                correctAttempts--;
                correctlyPlacedItems--;
                Debug.Log($"Food {foodType} was correctly placed before but is now incorrect. Adjusting counts.");
            }
            // If this food was incorrect before but is now correct, increment correctAttempts
            else if (!wasCorrectBefore && correctZone && itemPlacementResults.ContainsKey(foodType))
            {
                correctAttempts++;
                correctlyPlacedItems++;
                Debug.Log($"Food {foodType} was incorrectly placed before but is now correct. Adjusting counts.");
            }
            // If this is the first placement and it's correct
            else if (!itemPlacementResults.ContainsKey(foodType) && correctZone)
            {
                correctAttempts++;
                correctlyPlacedItems++;
                Debug.Log($"First placement of {foodType} is correct. Incrementing counts.");
            }
            
            // Store placement result (for final summary)
            itemPlacementResults[foodType] = correctZone;
            isCorrect = correctZone;
            lastPlacementCorrect = correctZone;

            if (correctZone)
            {
                // Zone is correct - award 5 points for each correct placement
                AddScore(5, $"Correct placement of {foodType}");

                // Add to temperature score if temperature is also correct
                AddTemperatureScore(correctTemp);

                // Record completion percentage
                float completionPercentage = (float)correctlyPlacedItems / totalFoodItems * 100f;
                
                Debug.Log($"✅ {foodType} placed correctly in {currentZone}! +5 points");
            }
            else
            {
                // Zone is incorrect
                DeductScore(10, $"Wrong placement of {foodType}");
                
                Debug.Log($"❌ {foodType} placed incorrectly in {currentZone}. -10 points.");
            }
            
            // Debug placement counts
            Debug.Log($"Current placement counts - Total: {totalAttempts}, Correct: {correctAttempts}, Items Placed Correctly: {correctlyPlacedItems}");
            
            // Log to PlayFab regardless of correctness
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsInitialized)
            {
                try
                {
                    Vector3 position = Vector3.zero; // Default value since we don't have the actual position here
                    PlayFabManager.Instance.LogFoodPlacement(foodType, currentZone, position, correctZone, currentTemp);
                    Debug.Log($"Logged food placement to PlayFab: {foodType}, {currentZone}, correct={correctZone}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error logging to PlayFab: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("PlayFabManager not available - food placement not logged to PlayFab");
            }
        }
        else
        {
            Debug.LogError($"Food type {foodType} not found in requirements!");
            lastPlacementCorrect = false;
        }

        UpdateUI();
        return isCorrect;
    }

    // 记录阶段完成情况
    private void LogStageCompletion(float completionPercentage)
    {
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                float elapsedTime = (float)(DateTime.Now - gameStartTime).TotalSeconds;
                analytics.LogStageTime($"Items placed {completionPercentage}%", elapsedTime);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log stage completion: {e.Message}");
            }
        }
    }
    
    // 记录游戏完成情况
    private void LogGameCompletion()
    {
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                float completionTime = (float)(DateTime.Now - gameStartTime).TotalSeconds;
                analytics.LogCompletionTime(completionTime, (correctlyPlacedItems >= totalFoodItems));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log game completion: {e.Message}");
            }
        }
    }

    private void AddScore(int points, string reason)
    {
        currentScore += points;
        UpdateUI();
        
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogScoreChange(points, reason);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log score change: {e.Message}");
            }
        }
    }

    private void DeductScore(int points, string reason)
    {
        int previousScore = currentScore;
        currentScore = Mathf.Max(0, currentScore - points);
        int actualPointsDeducted = previousScore - currentScore;
        
        UpdateUI();
        
        if (actualPointsDeducted > 0)
        {
            GameAnalytics analytics = GameAnalytics.Instance;
            if (analytics != null)
            {
                try
                {
                    analytics.LogScoreChange(-actualPointsDeducted, reason);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to log score deduction: {e.Message}");
                }
            }
        }
    }

    private void DeductLife()
    {
        // Method kept for compatibility but does nothing
        Debug.Log("DeductLife called but lives system removed.");
    }

    private void UpdateUI()
    {
        // ScoreText related code removed
    }

    private void EndGame()
    {
        isGameActive = false;
        TimeSpan gameTime = DateTime.Now - gameStartTime;
        
        // 计算准确率（正确放置次数 / 总放置次数 * 100）
        float accuracy = (totalAttempts > 0)
            ? ((float)correctAttempts / totalAttempts) * 100f
            : 0f;
        
        // 等级
        string grade = CalculateGrade(accuracy);
        
        // 生成总结信息
        string summary = GenerateGameSummary(accuracy, temperatureScore);

        // 上报游戏结束
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogGameEnd(Mathf.RoundToInt(accuracy), accuracy, grade);
                analytics.LogCompletionTime((float)gameTime.TotalSeconds, (totalAttempts >= totalFoodItems));
                
                // 记录详细统计
                Dictionary<string, object> stats = GetGameStats();
                foreach (var stat in stats)
                {
                    Dictionary<string, object> statData = new Dictionary<string, object>
                    {
                        { "stat_name", stat.Key },
                        { "stat_value", stat.Value }
                    };
                    analytics.LogCustomEvent("game_stat", statData);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log game end: {e.Message}");
            }
        }
        
        // 显示最终结果到对应UI
        if (FinalPanel != null)
        {
            FinalPanel.SetActive(true);
            if (FinalScoreText != null)
            {
                FinalScoreText.text = summary;
            }
        }
        else
        {
            Debug.LogError("FinalPanel reference is missing in LifeAndScoreManager");
            // 若没有绑定 FinalPanel，可尝试调用 GameManager 的 ShowFinalPanel
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowFinalPanel();
            }
        }
        
        Debug.Log(summary);
    }
    
    private string GenerateGameSummary(float accuracy, int tempScore)
    {
        // Get the final score (5 points per correctly placed item)
        int finalScore = GetScore(true);
        int correctItems = finalScore / 5; // Calculate how many items were placed correctly
        
        string summary = $"Final Score: {finalScore}/100\n";
        summary += $"Correctly placed items: {correctItems}/20\n";
        
        // 给出当前温度的信息
        if (TemperatureManager.Instance != null)
        {
            int currentTemp = TemperatureManager.Instance.currentTemperature;
            if (currentTemp < 1)
            {
                summary += $"<color=red>Temperature: {currentTemp}°C (Too Low)</color>\n" +
                           "This temperature is too cold for most foods.\n\n";
            }
            else if (currentTemp > 7)
            {
                summary += $"<color=red>Temperature: {currentTemp}°C (Too High)</color>\n" +
                           "This temperature is too warm for safe storage.\n\n";
            }
            else if (currentTemp <= 4)
            {
                summary += $"<color=green>Temperature: {currentTemp}°C (Ideal)</color>\n" +
                           "Excellent temperature for most items.\n\n";
            }
            else
            {
                summary += $"<color=yellow>Temperature: {currentTemp}°C (Acceptable)</color>\n" +
                           "Good for some produce, high for others.\n\n";
            }
        }
        
        // 列出正确与错误放置的食物
        summary += "Placement Summary:\n";
        
        List<string> correctItemsList = new List<string>();
        List<string> incorrectItems = new List<string>();
        
        foreach (var kvp in itemPlacementResults)
        {
            if (kvp.Value) correctItemsList.Add(kvp.Key);
            else incorrectItems.Add(kvp.Key);
        }

        summary += "Correct Items:\n";
        if (correctItemsList.Count > 0)
        {
            foreach (var item in correctItemsList)
            {
                summary += $"• {item}\n";
            }
        }
        else
        {
            summary += "None\n";
        }
        
        summary += "\nIncorrect Items:\n";
        if (incorrectItems.Count > 0)
        {
            foreach (var item in incorrectItems)
            {
                summary += $"• {item}\n";
            }
        }
        else
        {
            summary += "None\n";
        }
        
        return summary;
    }

    private string CalculateGrade(float accuracy)
    {
        if (accuracy >= 90) return "S Perfect!";
        if (accuracy >= 80) return "A Very Good!";
        if (accuracy >= 70) return "B Not Bad";
        if (accuracy >= 60) return "C Need Practice";
        return "D Need Review";
    }

    public void RestartGame()
    {
        // 记录重启事件
        GameAnalytics analytics = GameAnalytics.Instance;
        if (analytics != null)
        {
            try
            {
                analytics.LogRestartButtonClick();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to log restart: {e.Message}");
            }
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // 获取食物温度范围
    public (int minTemp, int maxTemp) GetFoodTemperatureRange(string foodType)
    {
        if (foodRequirements.TryGetValue(foodType, out var requirements))
        {
            return (requirements.minTemp, requirements.maxTemp);
        }
        return (0, 0);
    }

    // 重置状态
    public void ResetGameState()
    {
        currentScore = 0;
        temperatureScore = 0;
        totalAttempts = 0;
        correctAttempts = 0;
        correctlyPlacedItems = 0;
        itemPlacementResults.Clear();
        itemPlacementAttempts.Clear();
        
        UpdateUI();
    }
    
    // 获取当前状态统计信息
    public Dictionary<string, object> GetGameStats()
    {
        float accuracy = (totalAttempts > 0) ? ((float)correctAttempts / totalAttempts) * 100f : 0f;
        string grade = CalculateGrade(accuracy);
        
        var stats = new Dictionary<string, object>
        {
            { "currentScore", currentScore },
            { "totalAttempts", totalAttempts },
            { "correctAttempts", correctAttempts },
            { "accuracy", accuracy },
            { "grade", grade },
            { "correctlyPlacedItems", correctlyPlacedItems },
            { "totalFoodItems", totalFoodItems },
            { "completionPercentage", (float)correctlyPlacedItems / totalFoodItems * 100f },
        };
        
        return stats;
    }
    
    /// <summary>
    /// Get current score. Returns either the raw point score for in-game display
    /// or a score based on 5 points per correctly placed item (max 100) for final results.
    /// </summary>
    /// <param name="finalResults">If true, returns score based on 5 points per correctly placed item.</param>
    public int GetScore(bool finalResults = true)
    {
        if (finalResults)
        {
            // Count correct placements
            int correctPlacements = 0;
            foreach (var placement in itemPlacementResults)
            {
                if (placement.Value) correctPlacements++;
            }
            
            // Each correct placement is worth 5 points
            int finalScore = correctPlacements * 5;
            
            // Return raw score instead of normalized score
            return finalScore;
        }
        else
        {
            // Return raw current score for in-game display
            return currentScore;
        }
    }
    
    public float GetAccuracy()
    {
        return totalAttempts > 0
            ? ((float)correctAttempts / totalAttempts) * 100f
            : 0f;
    }
    
    public int GetTemperatureScore()
    {
        return temperatureScore;
    }
    
    public string GetGrade()
    {
        return CalculateGrade(GetAccuracy());
    }

    // Method to check if temperature is in ideal range
    public bool IsTemperatureCorrect()
    {
        return isTemperatureCorrect;
    }
    
    // Each correctly placed food gets +5 points to temperature score
    private void AddTemperatureScore(bool correctTemp)
    {
        if (correctTemp)
        {
            // Still add points for scoring system
            temperatureScore += 5;
            Debug.Log($"Temperature score +=5. Now: {temperatureScore}");
        }
        
        // Check if current temperature is in the ideal range (1-4°C)
        if (TemperatureManager.Instance != null)
        {
            int currentTemp = TemperatureManager.Instance.currentTemperature;
            isTemperatureCorrect = (currentTemp >= 1 && currentTemp <= 4);
            Debug.Log($"Temperature is {(isTemperatureCorrect ? "correct" : "incorrect")} at {currentTemp}°C");
        }
    }
    
    // Get temperature status string for final panel
    public string GetTemperatureStatus()
    {
        if (TemperatureManager.Instance == null)
            return "Temperature: Unknown";
            
        int currentTemp = TemperatureManager.Instance.currentTemperature;
        
        if (currentTemp >= 1 && currentTemp <= 4)
        {
            return $"<color=green>Temperature: {currentTemp}°C (CORRECT)</color>";
        }
        else
        {
            return $"<color=red>Temperature: {currentTemp}°C (INCORRECT)</color>\nIdeal range is 1-4°C";
        }
    }
    
    // Get correctly placed items for FinalPanel1
    public string GetCorrectlyPlacedItemsList()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Correctly placed items:</b>");
        
        // Create a list of correctly placed items
        List<string> correctItems = new List<string>();
        foreach (var item in itemPlacementResults)
        {
            if (item.Value) // If item was placed correctly
            {
                string foodName = item.Key.ToUpper();
                correctItems.Add($"□ {foodName}");
            }
        }
        
        if (correctItems.Count == 0)
        {
            sb.AppendLine("None");
            return sb.ToString();
        }
        
        // Calculate how many items to put in each column
        int totalItems = correctItems.Count;
        int leftColumnCount = (totalItems + 1) / 2; // Ceiling division
        
        // Create two columns layout with proper spacing
        for (int i = 0; i < leftColumnCount; i++)
        {
            // Left column item
            string leftItem = correctItems[i];
            
            // Check if there's a corresponding right column item
            if (i + leftColumnCount < totalItems)
            {
                string rightItem = correctItems[i + leftColumnCount];
                // Add both items with fixed spacing (25 characters for left column)
                sb.AppendLine($"{leftItem,-25}{rightItem}");
            }
            else
            {
                // Only add left column item if no right item exists
                sb.AppendLine(leftItem);
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Returns the count of correctly placed items
    /// </summary>
    public int GetCorrectPlacementsCount()
    {
        int count = 0;
        foreach (var item in itemPlacementResults)
        {
            if (item.Value) // If item was placed correctly
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Returns the total count of items that were placed
    /// </summary>
    public int GetTotalPlacementsCount()
    {
        return itemPlacementResults.Count;
    }
    
    // Get list of incorrectly placed items with explanations for FinalPanel2
    public string GetIncorrectlyPlacedItemsList()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Incorrectly placed items:</b>");
        
        // Create a list of incorrectly placed items
        List<string> incorrectItems = new List<string>();
        
        foreach (var item in itemPlacementResults)
        {
            if (!item.Value) // If item was placed incorrectly
            {
                string foodName = item.Key.ToUpper();
                
                // Get the correct zone for this item
                string correctZone = "unknown";
                if (foodRequirements.TryGetValue(item.Key, out var requirements))
                {
                    correctZone = requirements.storageType;
                }
                
                incorrectItems.Add($"□ {foodName} should be in {correctZone}");
            }
        }
        
        if (incorrectItems.Count == 0)
        {
            sb.AppendLine("None - Great job!");
            return sb.ToString();
        }
        
        // Calculate how many items to put in each column if needed
        int totalItems = incorrectItems.Count;
        
        // For incorrect items, use a single column as they might be longer
        foreach (string item in incorrectItems)
        {
            sb.AppendLine(item);
        }
        
        return sb.ToString();
    }

    // Returns whether the last placement was correct
    public bool WasLastPlacementCorrect()
    {
        return lastPlacementCorrect;
    }

    // Get current temperature score
    public int GetTempScore()
    {
        return temperatureScore;
    }

    // Get maximum possible temperature score
    public int GetMaxTempScore()
    {
        return totalFoodItems * 5;
    }

    /// <summary>
    /// Checks if placement would be correct without affecting score or lives
    /// </summary>
    /// <param name="foodType">The type of food to check</param>
    /// <param name="currentZone">The zone where the food is placed</param>
    /// <param name="currentTemp">The current refrigerator temperature</param>
    /// <returns>True if the placement would be correct, false otherwise</returns>
    public bool WouldBeCorrectPlacement(string foodType, string currentZone, int currentTemp)
    {
        if (string.IsNullOrEmpty(foodType) || string.IsNullOrEmpty(currentZone))
            return false;
        
        // Get requirements for this food type
        if (foodRequirements.TryGetValue(foodType, out var requirements))
        {
            bool isCorrectZone = (currentZone == requirements.storageType);
            bool isCorrectTemp = (currentTemp >= requirements.minTemp && currentTemp <= requirements.maxTemp);
            
            // Just return if it would be correct, without updating any state
            return isCorrectZone && isCorrectTemp;
        }
        
        // No requirements found for this food type
        return false;
    }

    /// <summary>
    /// Gets the correct zone for a given food type
    /// </summary>
    /// <param name="foodType">The type of food to look up</param>
    /// <returns>The correct zone for the food, or null if not found</returns>
    public string GetFoodZone(string foodType)
    {
        if (string.IsNullOrEmpty(foodType))
        {
            Debug.LogWarning("GetFoodZone: foodType is null or empty");
            return null;
        }
        
        if (foodRequirements.TryGetValue(foodType, out var requirements))
        {
            return requirements.storageType;
        }
        
        Debug.LogWarning($"GetFoodZone: No requirements found for food type '{foodType}'");
        return null;
    }

    /// <summary>
    /// Returns detailed data about each item's placement for analytics
    /// </summary>
    public string GetDetailedItemPlacementData()
    {
        Dictionary<string, object> placementDetails = new Dictionary<string, object>();
        
        try
        {
            // Add each item's placement information
            foreach (var item in itemPlacementResults)
            {
                string foodName = item.Key;
                bool isCorrect = item.Value;
                
                // Get the current zone where the item was placed
                string placedZone = GetItemCurrentZone(foodName);
                
                // Get the correct zone where the item should be placed
                string correctZone = "unknown";
                if (foodRequirements.TryGetValue(foodName, out var requirements))
                {
                    correctZone = requirements.storageType;
                }
                
                // Create detailed placement info
                var itemData = new Dictionary<string, object>
                {
                    { "is_correct", isCorrect },
                    { "placed_zone", placedZone },
                    { "correct_zone", correctZone }
                };
                
                placementDetails.Add(foodName, itemData);
            }
            
            // Convert to JSON
            string json = JsonUtility.ToJson(new Serializable_Dictionary<string, object>(placementDetails));
            return json;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating detailed item placement data: {e.Message}");
            return "{}";
        }
    }
    
    /// <summary>
    /// Gets the current zone where an item is placed
    /// </summary>
    private string GetItemCurrentZone(string itemName)
    {
        // Find the object with this name
        GameObject itemObject = GameObject.Find(itemName);
        if (itemObject == null)
        {
            return "not_found";
        }
        
        // Get the DragSprite2D component
        DragSprite2D dragComponent = itemObject.GetComponent<DragSprite2D>();
        if (dragComponent == null)
        {
            return "no_drag_component";
        }
        
        // Check current position to determine zone
        Vector3 itemPosition = itemObject.transform.position;
        Collider2D[] colliders = Physics2D.OverlapPointAll(itemPosition);
        
        // Check for zone colliders
        foreach (Collider2D collider in colliders)
        {
            // Check for any of the possible zones tags
            if (collider.CompareTag("TopShelf") || collider.CompareTag("MiddleShelf") || 
                collider.CompareTag("BottomShelf") || collider.CompareTag("Drawer") ||
                collider.CompareTag("DryBox") || collider.CompareTag("TopDoor") ||
                collider.CompareTag("MiddleDoor") || collider.CompareTag("BottomDoor"))
            {
                return collider.tag;
            }
        }
        
        return "no_zone";
    }
    
    // Serializable dictionary helper class for JSON conversion
    [System.Serializable]
    public class Serializable_Dictionary<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<string> values = new List<string>();
        
        public Serializable_Dictionary(Dictionary<TKey, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value.ToString());
            }
        }
    }
}
