/*
 * Fridge Organization Game - ManagerInitializer.cs
 * 
 * Author: Zixuan Wang
 * 
 * Description: Manager initialization system that ensures proper setup and coordination of all
 * game managers. Prevents initialization errors and provides centralized reference management
 * for the complex multi-manager architecture.
 * 
 * Key Responsibilities:
 * - Manager instance creation and validation
 * - Reference distribution to game objects
 * - Initialization order management and dependency resolution
 * - Error prevention and debugging support
 * - Centralized manager coordination
 */

using UnityEngine;

/// <summary>
/// Ensures all managers are correctly initialized at the start of the game
/// This script should be attached to a GameObject named "Managers" in the scene
/// This GameObject should contain all necessary manager components
/// </summary>
public class ManagerInitializer : MonoBehaviour
{
    [Header("Manager References")]
    public LifeAndScoreManager scoreManager;
    public TemperatureManager tempManager;
    public GameAnalytics analytics;
    public PlayFabManager playFabManager;

    void Awake()
    {
        Debug.Log("ManagerInitializer: Starting initialization of managers...");
        
        // Ensure the object is not destroyed
        DontDestroyOnLoad(this.gameObject);
        
        // Check and initialize LifeAndScoreManager
        if (scoreManager == null)
        {
            scoreManager = GetComponentInChildren<LifeAndScoreManager>();
            if (scoreManager == null)
            {
                GameObject scoreObj = new GameObject("LifeAndScoreManager");
                scoreObj.transform.SetParent(this.transform);
                scoreManager = scoreObj.AddComponent<LifeAndScoreManager>();
                Debug.Log("ManagerInitializer: Created LifeAndScoreManager");
            }
        }
        
        // Check and initialize TemperatureManager
        if (tempManager == null)
        {
            tempManager = GetComponentInChildren<TemperatureManager>();
            if (tempManager == null)
            {
                GameObject tempObj = new GameObject("TemperatureManager");
                tempObj.transform.SetParent(this.transform);
                tempManager = tempObj.AddComponent<TemperatureManager>();
                Debug.Log("ManagerInitializer: Created TemperatureManager");
            }
        }
        
        // Check and initialize GameAnalytics
        if (analytics == null)
        {
            analytics = GetComponentInChildren<GameAnalytics>();
            if (analytics == null)
            {
                GameObject analyticsObj = new GameObject("GameAnalytics");
                analyticsObj.transform.SetParent(this.transform);
                analytics = analyticsObj.AddComponent<GameAnalytics>();
                Debug.Log("ManagerInitializer: Created GameAnalytics");
            }
        }
        
        // Check and initialize PlayFabManager
        if (playFabManager == null)
        {
            playFabManager = GetComponentInChildren<PlayFabManager>();
            if (playFabManager == null)
            {
                GameObject playFabObj = new GameObject("PlayFabManager");
                playFabObj.transform.SetParent(this.transform);
                playFabManager = playFabObj.AddComponent<PlayFabManager>();
                Debug.Log("ManagerInitializer: Created PlayFabManager");
            }
        }
        
        Debug.Log("ManagerInitializer: All managers initialized successfully");
    }
    
    void Start()
    {
        VerifyAllManagers();
    }
    
    /// <summary>
    /// Verify all managers are correctly initialized
    /// </summary>
    private void VerifyAllManagers()
    {
        bool allValid = true;
        
        if (LifeAndScoreManager.Instance == null)
        {
            Debug.LogError("ManagerInitializer: LifeAndScoreManager.Instance is null!");
            allValid = false;
        }
        
        if (TemperatureManager.Instance == null)
        {
            Debug.LogError("ManagerInitializer: TemperatureManager.Instance is null!");
            allValid = false;
        }
        
        if (GameAnalytics.Instance == null)
        {
            Debug.LogError("ManagerInitializer: GameAnalytics.Instance is null!");
            allValid = false;
        }
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogError("ManagerInitializer: PlayFabManager.Instance is null!");
            allValid = false;
        }
        
        if (allValid)
        {
            Debug.Log("ManagerInitializer: All manager instances are valid and ready to use");
        }
        else
        {
            Debug.LogError("ManagerInitializer: Some manager instances are not valid!");
        }
    }
    
    // Editor menu functionality is disabled in builds
    // This functionality only works in the Unity Editor
#if UNITY_EDITOR
    /// <summary>
    /// Create Managers GameObject in Unity Editor
    /// </summary>
    [UnityEditor.MenuItem("Setup/Create Managers")]
    private static void CreateManagers()
    {
        // Create Managers parent object
        GameObject managers = new GameObject("Managers");
        managers.AddComponent<ManagerInitializer>();
        
        // Create child manager objects
        GameObject scoreManager = new GameObject("LifeAndScoreManager");
        scoreManager.transform.SetParent(managers.transform);
        scoreManager.AddComponent<LifeAndScoreManager>();
        
        GameObject tempManager = new GameObject("TemperatureManager");
        tempManager.transform.SetParent(managers.transform);
        tempManager.AddComponent<TemperatureManager>();
        
        GameObject analytics = new GameObject("GameAnalytics");
        analytics.transform.SetParent(managers.transform);
        analytics.AddComponent<GameAnalytics>();
        
        GameObject playFabManager = new GameObject("PlayFabManager");
        playFabManager.transform.SetParent(managers.transform);
        playFabManager.AddComponent<PlayFabManager>();
        
        Debug.Log("Created Managers hierarchy with all necessary components");
    }
#endif
} 
