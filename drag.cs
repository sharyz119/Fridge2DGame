using UnityEngine;
using System.Collections;

public class DragSprite2D : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private bool isInCorrectZone = false;

    public string correctTag; 
    public string foodType; 
    private Vector3 originalPosition;
    private bool isPlaced = false; 

    private LifeAndScoreManager scoreManager;
    private TemperatureManager tempManager;
    private GameAnalytics analytics;
    private GameManager gameManager;
    private bool isInitialized = false;
    private int initAttempts = 0;
    private const int MAX_INIT_ATTEMPTS = 10;

    void Awake()
    {
        originalPosition = transform.position;
        // Start attempting to initialize managers
        InvokeRepeating("TryInitializeManagers", 0.1f, 0.3f);
    }

    void TryInitializeManagers()
    {
        // If already initialized or exceeded max attempts, stop trying
        if (isInitialized || initAttempts >= MAX_INIT_ATTEMPTS)
        {
            CancelInvoke("TryInitializeManagers");
            return;
        }

        initAttempts++;
        
        // First try to get references from ManagerInitializer
        ManagerInitializer initializer = FindObjectOfType<ManagerInitializer>();
        if (initializer != null)
        {
            Debug.Log($"Found ManagerInitializer for {foodType}");
            scoreManager = initializer.scoreManager;
            tempManager = initializer.tempManager;
            analytics = initializer.analytics;
        }
        
        // If not found from initializer, try singleton or direct find
        if (scoreManager == null)
            scoreManager = LifeAndScoreManager.Instance ?? FindObjectOfType<LifeAndScoreManager>();
        
        if (tempManager == null)
            tempManager = TemperatureManager.Instance ?? FindObjectOfType<TemperatureManager>();
            
        if (analytics == null)
            analytics = GameAnalytics.Instance ?? FindObjectOfType<GameAnalytics>();
            
        // Get GameManager
        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();

        // Check if all managers were successfully obtained
        if (scoreManager != null && tempManager != null)
        {
            isInitialized = true;
            Debug.Log($"Successfully initialized managers for {foodType} after {initAttempts} attempts");
            CancelInvoke("TryInitializeManagers");
        }
        else
        {
            Debug.Log($"Attempt {initAttempts}/{MAX_INIT_ATTEMPTS} to initialize managers for {foodType}");
            if (initAttempts == MAX_INIT_ATTEMPTS)
            {
                Debug.LogError($"Failed to initialize managers for {foodType} after {MAX_INIT_ATTEMPTS} attempts");
                Debug.LogError($"Score Manager: {(scoreManager == null ? "NULL" : "OK")}, Temp Manager: {(tempManager == null ? "NULL" : "OK")}, Analytics: {(analytics == null ? "NULL" : "OK")}, Game Manager: {(gameManager == null ? "NULL" : "OK")}");
            }
        }
    }

    void Start()
    {
        Debug.Log($"Initialized {foodType} with target zone: {correctTag}");
    }

    void OnMouseDown()
    {
        if (isPlaced) return;
        
        // If managers aren't initialized, try to initialize again
        if (!isInitialized)
        {
            TryInitializeManagers();
            if (!isInitialized)
            {
                Debug.LogWarning($"Cannot drag {foodType}: managers not initialized");
                return;
            }
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mousePosition.x, mousePosition.y, 0);
        dragging = true;
        
        // Record drag start event
        if (analytics != null)
        {
            try {
                analytics.LogItemDragStart(foodType, transform.position);
            } catch (System.Exception e) {
                Debug.LogWarning($"Failed to log drag start: {e.Message}");
            }
        }
    }

    void OnMouseDrag()
    {
        if (!dragging || isPlaced) return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePosition.x, mousePosition.y, 0) + offset;
    }

    void OnMouseUp()
    {
        // First ensure we're no longer dragging
        dragging = false;
        
        // Do not mark as placed - keep draggable until game over
        // if (isPlaced) 
        // {
        //     Debug.Log($"{foodType} is already placed, ignoring mouse up event");
        //     return;
        // }

        // Check for manager initialization
        if (!isInitialized || scoreManager == null || tempManager == null)
        {
            Debug.LogError($"Required managers not found for {foodType}!");
            return;
        }

        // Get current temperature
        int currentTemp = tempManager.currentTemperature;

        // Check placement zone
        string currentZone = "";
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("TopShelf") || collider.CompareTag("MiddleShelf") || 
                collider.CompareTag("BottomShelf") || collider.CompareTag("Drawer") ||
                collider.CompareTag("DryBox") || collider.CompareTag("TopDoor") ||
                collider.CompareTag("MiddleDoor") || collider.CompareTag("BottomDoor"))
            {
                currentZone = collider.tag;
                break;
            }
        }

        try
        {
            // Log detailed debugging
            Debug.Log($"Food placement: {foodType} in {currentZone} at temp {currentTemp}°C");
            
            // Get correct zone for this food
            string correctZoneForFood = scoreManager != null ? 
                (scoreManager.GetFoodZone(foodType) ?? "unknown") : "unknown";
            Debug.Log($"Correct zone for {foodType} should be: {correctZoneForFood}");
            
            // Check if we found a valid zone
            if (string.IsNullOrEmpty(currentZone))
            {
                Debug.Log($"{foodType} not placed in a valid zone - no action taken");
                return;
            }
            
            // NO LONGER MARK as placed - keep draggable
            // isPlaced = true;
            // Debug.Log($"Marked {foodType} as placed=true");
            
            // Evaluate placement with the LifeAndScoreManager (this actually affects scoring)
            bool isCorrectPlacement = false;
            
            if (scoreManager != null)
            {
                // This will check placement, update score, and record the result
                isCorrectPlacement = scoreManager.CheckPlacement(foodType, currentZone, currentTemp);
                Debug.Log($"CheckPlacement result for {foodType}: {(isCorrectPlacement ? "CORRECT ✓" : "INCORRECT ✗")}");
            }

            // Log placement to analytics
            if (analytics != null)
            {
                if (isCorrectPlacement)
                {
                    analytics.LogCorrectPlacement(foodType, currentZone, transform.position, currentTemp);
                }
                else
                {
                    analytics.LogIncorrectPlacement(foodType, currentZone, correctZoneForFood, transform.position, currentTemp);
                }
            }

            // Log to PlayFab via GameManager, using try-catch to avoid any errors that might freeze the game
            try
            {
                if (gameManager != null)
                {
                    gameManager.LogFoodPlacement(foodType, currentZone, transform.position, isCorrectPlacement, currentTemp);
                    Debug.Log($"Logged to PlayFab via GameManager: {foodType}, zone={currentZone}, correct={isCorrectPlacement}");
                }
                else if (PlayFabManager.Instance != null)
                {
                    // Try direct call to PlayFabManager if GameManager not available
                    PlayFabManager.Instance.LogFoodPlacement(foodType, currentZone, transform.position, isCorrectPlacement, currentTemp);
                    Debug.Log($"Logged to PlayFab directly: {foodType}, zone={currentZone}, correct={isCorrectPlacement}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error logging to PlayFab: {e.Message}");
                // Continue even if PlayFab logging fails
            }
            
            // Final status update
            Debug.Log($"OnMouseUp completed for {foodType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during placement check for {foodType}: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if entered correct zone
        if (other.CompareTag(correctTag))
        {
            isInCorrectZone = true;
            Debug.Log($"✨ {foodType} entered correct zone: {correctTag}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if exited correct zone
        if (other.CompareTag(correctTag))
        {
            isInCorrectZone = false;
            Debug.Log($"💨 {foodType} exited zone: {correctTag}");
        }
    }

    public void ResetFood()
    {
        transform.position = originalPosition;
        isPlaced = false;
        isInCorrectZone = false;
        this.enabled = true;
        dragging = false;
    }
}

