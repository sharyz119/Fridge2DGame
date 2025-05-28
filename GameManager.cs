/*
 * Fridge Organization Game - GameManager.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Central game controller that manages game state, coordinates between different systems,
 * handles scoring, and manages analytics event logging. This is the main orchestrator for the entire
 * game flow from start to finish.
 * 
 * Key Responsibilities:
 * - Game state management (start, end, restart)
 * - Score tracking and coordination
 * - Analytics event coordination
 * - Manager initialization and reference management
 */

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int score = 0;
    // Lives system removed - just play once

    [Header("UI Elements")]
    // ScoreText removed
    public GameObject gameOverPanel;
    public TextMeshProUGUI FinalScoreText;

    // Analytics related fields
    private bool isInitialized = false;
    private string userId;
    private string sessionId;
    private PlayFabManager playFabManager;
    private GameAnalytics gameAnalytics;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // ScoreText related code removed
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void InitializeManagers()
    {
        userId = System.Guid.NewGuid().ToString();
        sessionId = System.Guid.NewGuid().ToString();
        
        // Get PlayFabManager instance
        playFabManager = FindObjectOfType<PlayFabManager>();
        if (playFabManager == null)
        {
            Debug.LogWarning("PlayFabManager not found. Creating a new instance.");
            GameObject pfmObj = new GameObject("PlayFabManager");
            playFabManager = pfmObj.AddComponent<PlayFabManager>();
        }
        
        // Get GameAnalytics instance
        gameAnalytics = FindObjectOfType<GameAnalytics>();
        if (gameAnalytics == null)
        {
            Debug.LogWarning("GameAnalytics not found. Analytics may be limited.");
        }
        
        isInitialized = true;
    }

    /// <summary>
    /// Called when starting the actual game round.
    /// </summary>
    public void StartGame()
    {
        // No lives system - just play once
        score = 0;
        // ScoreText related code removed

        // Log game start with analytics
        if (playFabManager != null)
        {
            playFabManager.LogGameStart();
        }
        if (gameAnalytics != null)
        {
            gameAnalytics.LogGameStart();
        }
    }

    /// <summary>
    /// Add points to the score (e.g. on correct placement).
    /// </summary>
    public void AddScore(int points)
    {
        score += points;
        // ScoreText related code removed
        Debug.Log("✅ Correct! Score: " + score);
        
        // Log score change with analytics
        if (playFabManager != null)
        {
            playFabManager.LogScoreChange(points, "Correct placement");
        }
        if (gameAnalytics != null)
        {
            gameAnalytics.LogScoreChange(points, "Correct placement");
        }
    }

    /// <summary>
    /// Deduct points from the score (e.g. on incorrect placement).
    /// </summary>
    public void DeductPoints(int points)
    {
        score = Mathf.Max(0, score - points);
        // ScoreText related code removed
        Debug.Log("❌ Points deducted. Score: " + score);
        
        // Log score change with analytics
        if (playFabManager != null)
        {
            playFabManager.LogScoreChange(-points, "Incorrect placement");
        }
        if (gameAnalytics != null)
        {
            gameAnalytics.LogScoreChange(-points, "Incorrect placement");
        }
    }

    public void LoseLife()
    {
        // No lives system - do nothing
        // Lives functionality removed
    }

    // UpdateScoreText method removed if it exists

    /// <summary>
    /// Shows the final panel with the player's score
    /// </summary>
    public void ShowFinalPanel()
    {
        // Get the UIManager instance to show the final panel
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            // Get the LifeAndScoreManager instance to retrieve scores
            LifeAndScoreManager scoreManager = FindObjectOfType<LifeAndScoreManager>();
            if (scoreManager != null)
            {
                uiManager.ShowFinalPanel(
                    scoreManager.GetScore(true), // Get the normalized percentage score (0-100)
                    0,  // Passing 0 for tempScore (no longer used)
                    0   // Passing 0 for maxTempScore (no longer used)
                );
            }
            else
            {
                Debug.LogError("LifeAndScoreManager not found when showing final panel");
            }
        }
        else
        {
            Debug.LogError("UIManager not found when showing final panel");
        }
    }
    
    // Keep the GameOver method for backward compatibility, but make it call ShowFinalPanel
    private void GameOver()
    {
        ShowFinalPanel();
    }
    
    // Example grade calculation (legacy):
    private string DetermineGrade(int finalScore)
    {
        if (finalScore >= 90) return "A";
        if (finalScore >= 80) return "B";
        if (finalScore >= 70) return "C";
        if (finalScore >= 60) return "D";
        return "F";
    }

    // Analytics bridging methods - improved to prevent blocking
    public void LogFoodPlacement(string foodType, string targetZone, Vector3 position, bool isCorrect, float temperature)
    {
        try
        {
            // Log to PlayFab asynchronously
            if (playFabManager != null)
            {
                playFabManager.LogFoodPlacement(foodType, targetZone, position, isCorrect, temperature);
            }
            
            // Log to GameAnalytics asynchronously
            if (gameAnalytics != null)
            {
                try
                {
                    int tempInt = Mathf.RoundToInt(temperature);
                    gameAnalytics.LogPlacement(foodType, targetZone, position, isCorrect, tempInt);
                }
                catch (Exception e)
                {
                    // Just log the error but don't let it block game flow
                    Debug.LogError($"Error in GameAnalytics.LogPlacement: {e.Message}");
                }
            }
            
            // Log successful completion
            Debug.Log($"GameManager.LogFoodPlacement completed for {foodType} in {targetZone}");
        }
        catch (Exception e)
        {
            // Log error but don't block game flow
            Debug.LogError($"Error in GameManager.LogFoodPlacement: {e.Message}");
        }
    }
    
    public void LogTemperatureChange(float temperature)
    {
        if (playFabManager != null)
        {
            playFabManager.LogTemperatureChange(temperature);
        }
        
        if (gameAnalytics != null)
        {
            gameAnalytics.LogTemperature(temperature);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        
        // Use UIManager's restart method instead of reloading scene
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestartGame();
        }
        else
        {
            // Fallback to reloading the scene if UIManager is not available
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Finalizes the game and calculates the final score when player clicks "I'm done"
    /// </summary>
    public void FinalizeGameAndCalculateScore()
    {
        // Stop the gameplay timer if analytics is available
        if (gameAnalytics != null)
        {
            gameAnalytics.StopGameplayTimer();
        }
        
        // Evaluate all food placements
        EvaluateAllFoodPlacements();
        
        // Show the final panel
        ShowFinalPanel();
    }
    
    /// <summary>
    /// Evaluates the placement of all draggable food items
    /// </summary>
    private void EvaluateAllFoodPlacements()
    {
        // Get current temperature
        int currentTemp = 0;
        if (TemperatureManager.Instance != null)
        {
            currentTemp = TemperatureManager.Instance.currentTemperature;
        }
        
        // First check traditional DragSprite2D components
        DragSprite2D[] dragItems = FindObjectsOfType<DragSprite2D>();    
        foreach (DragSprite2D foodItem in dragItems)
        {
            // Skip inactive foods
            if (!foodItem.gameObject.activeSelf)
                continue;
            
            string foodType = foodItem.foodType;
            
            // Get the zone using OverlapPoint since DragSprite2D doesn't track zones itself
            string currentZone = GetZoneAtPosition(foodItem.transform.position);
            
            // Skip if no zone detected
            if (string.IsNullOrEmpty(currentZone))
            {
                Debug.Log($"No zone detected for {foodType} during final evaluation.");
                continue;
            }
            
            // Evaluate the placement and add to score
            bool isCorrect = LifeAndScoreManager.Instance.CheckPlacement(foodType, currentZone, currentTemp);
            
            // Log the placement for analytics
            LogFoodPlacement(foodType, currentZone, foodItem.transform.position, isCorrect, currentTemp);
        }
        
        // Then check FoodTooltip components (used for tooltips and zone tracking)
        FoodTooltip[] foodItems = FindObjectsOfType<FoodTooltip>();    
        foreach (FoodTooltip foodItem in foodItems)
        {
            // Skip inactive foods
            if (!foodItem.gameObject.activeSelf)
                continue;
            
            string foodType = foodItem.GetFoodType();
            string currentZone = foodItem.GetCurrentZone();
            
            // Skip if already evaluated as DragSprite2D or no zone detected
            if (foodItem.GetComponent<DragSprite2D>() != null || string.IsNullOrEmpty(currentZone))
                continue;
            
            // Evaluate the placement and add to score
            bool isCorrect = LifeAndScoreManager.Instance.CheckPlacement(foodType, currentZone, currentTemp);
            
            // Log the placement for analytics
            LogFoodPlacement(foodType, currentZone, foodItem.transform.position, isCorrect, currentTemp);
        }
    }

    /// <summary>
    /// Gets the zone tag (f1-f6) at the given position
    /// </summary>
    public string GetZoneAtPosition(Vector2 position)
    {
        try 
        {
            // First, check for raycast hits with valid zone tags
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero);
            Debug.Log($"Raycast at position {position} found {hits.Length} colliders");
            
            foreach (RaycastHit2D hit in hits)
            {
                Debug.Log($"Checking object: {hit.collider.gameObject.name}, Tags: {hit.collider.gameObject.tag}");
                
                // Check for predefined zone tags (f1-f6)
                string tag = hit.collider.gameObject.tag;
                if (tag.StartsWith("f") && tag.Length == 2 && char.IsDigit(tag[1]))
                {
                    Debug.Log($"Found zone with tag: {tag}");
                    return tag;
                }
                
                // Check if it contains "zone" in the name
                if (hit.collider.gameObject.name.ToLower().Contains("zone"))
                {
                    Debug.Log($"Found zone by name: {hit.collider.gameObject.name}");
                    return hit.collider.gameObject.name;
                }
            }
            
            // If we didn't find anything, log all objects hit
            if (hits.Length > 0)
            {
                Debug.LogWarning("No zone tags found at position. Objects found:");
                foreach (RaycastHit2D hit in hits)
                {
                    Debug.LogWarning($"- Name: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                }
            }
            else
            {
                Debug.LogWarning($"No objects found at position {position}");
            }
            
            return "unknown";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in GetZoneAtPosition: {e.Message}\n{e.StackTrace}");
            return "error";
        }
    }
}
