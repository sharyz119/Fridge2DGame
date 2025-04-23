using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // Panels and UI Elementsyy
    [Header("Panels")]
    // public GameObject welcomePanel;
    public GameObject StartPanel1, StartPanel2, StartPanel3, StartPanel4;
    public GameObject TemperatureSettingPanel;
    public GameObject FinalPanel1, FinalPanel2, gameOverPanel;

    [Header("Final Panel Text Elements")]
    public TextMeshProUGUI finalScoreText;        // For score in FinalPanel1
    public TextMeshProUGUI temperatureResultText; // For temperature result in FinalPanel1
    public TextMeshProUGUI correctItemsText;      // For correct items in FinalPanel1
    public TextMeshProUGUI incorrectItemsText;    // For incorrect items in FinalPanel2
    public TMP_InputField userIdInputField;       // For user ID input in GameOverPanel

    [Header("Game Elements")]
    public UnityEngine.UI.Slider TemperatureSlider;
    public GameObject TemperatureText;
    public TextMeshProUGUI TempText; // New text element to display current temperature
    public GameObject CheckScoreButton; // Reference to the "I'm done" button
    // public GameObject WarningPanel;

    public GameObject FoodItems;


    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Debug.Log("UIManager Awake - Initializing panel visibility");
        
        // Immediately hide ALL panels at Awake - before anything else can happen
        HideAllPanels();
        
        // Only activate the first panel
        if (StartPanel1 != null)
        {
            StartPanel1.SetActive(true);
            Debug.Log("Activated only StartPanel1 during Awake");
        }
        
        // 设置温度滑块的初始值和范围
        if (TemperatureSlider != null)
        {
            TemperatureSlider.minValue = 0;  // 最低温度
            TemperatureSlider.maxValue = 8;  // 最高温度（设置为8度以测试超温警告）
            TemperatureSlider.value = 0; // Start with no temperature set
            
            // 添加监听器
            TemperatureSlider.onValueChanged.AddListener(OnTemperatureChanged);
        }
    }
    
    /// <summary>
    /// Hide all panels immediately
    /// </summary>
    private void HideAllPanels()
    {
        // Explicitly hide every panel
        if (StartPanel1 != null) StartPanel1.SetActive(false);
        if (StartPanel2 != null) StartPanel2.SetActive(false);
        if (StartPanel3 != null) StartPanel3.SetActive(false);
        if (StartPanel4 != null) StartPanel4.SetActive(false);
        if (TemperatureSettingPanel != null) TemperatureSettingPanel.SetActive(false);
        if (FinalPanel1 != null) FinalPanel1.SetActive(false);
        if (FinalPanel2 != null) FinalPanel2.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Hide game elements too
        if (CheckScoreButton != null) CheckScoreButton.SetActive(false);
        if (FoodItems != null) FoodItems.SetActive(false);
        
        Debug.Log("All panels hidden during initialization");
    }

    // 温度改变时的回调
    private void OnTemperatureChanged(float value)
    {
        if (TemperatureManager.Instance != null)
        {
            TemperatureManager.Instance.SetTemperature(value);
            
            // Update the temperature display text
            UpdateTemperatureText(value);
        }
    }

    // Update the temperature text display
    private void UpdateTemperatureText(float temperature)
    {
        if (TempText != null)
        {
            int tempValue = Mathf.RoundToInt(temperature);
            TempText.text = tempValue.ToString();
            Debug.Log($"Updated temperature display to: {tempValue}");
        }
    }

    void Start()
    {
        Debug.Log("UIManager Start method - setting up UI");
        
        // Set up the UI manager as singleton instance (backup if Awake didn't do it)
        if (Instance == null)
            Instance = this;
            
        // Initialize panels and elements
        EnsureFinalPanelTextElements();
        EnsureGameOverPanelElements();
        
        // Initialize temperature text if it exists
        InitializeTemperatureText();
        
        // Create ButtonFixer if needed
        if (FindObjectOfType<ButtonFixer>() == null)
        {
            GameObject fixerObj = new GameObject("ButtonFixer");
            ButtonFixer fixer = fixerObj.AddComponent<ButtonFixer>();
            Debug.Log("Created ButtonFixer in UIManager Start");
        }
        
        // Ensure Event System exists
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem in UIManager Start");
        }
        
        // DO NOT call ShowWelcomePanel() here - we've already set up panels in Awake
        // Just verify StartPanel1 is the only active panel
        bool startPanel1Active = StartPanel1 != null && StartPanel1.activeSelf;
        if (!startPanel1Active)
        {
            Debug.LogWarning("StartPanel1 was not active at Start - fixing panel visibility");
            HideAllPanels();
            if (StartPanel1 != null) StartPanel1.SetActive(true);
        }
        else
        {
            Debug.Log("StartPanel1 confirmed active at Start");
        }
        
        // Add debug logging for buttons in StartPanel1
        Debug.Log("Checking StartPanel1 buttons in Start method");
        
        if (StartPanel1 != null)
        {
            Button[] buttons = StartPanel1.GetComponentsInChildren<Button>(true);
            Debug.Log($"Found {buttons.Length} buttons in StartPanel1:");
            
            foreach (Button button in buttons)
            {
                // Get the current click handlers
                int handlerCount = button.onClick.GetPersistentEventCount();
                
                // Log button details
                Debug.Log($"Button: {button.name}, Active: {button.gameObject.activeSelf}, Interactable: {button.interactable}, Handlers: {handlerCount}");
                
                // Ensure button is interactable
                button.interactable = true;
                
                // Force raycast target on image
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    Debug.Log($"Ensured {button.name} image is a raycast target");
                }
                
                // Add default handler if none exists
                if (handlerCount == 0)
                {
                    button.onClick.AddListener(() => {
                        Debug.Log($"Button {button.name} clicked in StartPanel1");
                        GoFromStart1ToStart2();
                    });
                    Debug.Log($"Added GoFromStart1ToStart2 handler to {button.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("StartPanel1 is null in Start method!");
        }
        
        // Schedule additional interactivity checks
        StartCoroutine(RunDelayedButtonChecks());
    }
    
    private System.Collections.IEnumerator RunDelayedButtonChecks()
    {
        // Run additional checks after a short delay
        yield return new WaitForSeconds(0.2f);
        
        // Use ButtonFixer for a comprehensive fix
        ButtonFixer fixer = FindObjectOfType<ButtonFixer>();
        if (fixer != null)
        {
            fixer.FixAllButtons();
        }
        
        // Another pass after a longer delay
        yield return new WaitForSeconds(0.5f);
        
        // Final check to ensure buttons work
        if (fixer != null)
        {
            fixer.FixAllButtons();
        }
        else
        {
            // If for some reason ButtonFixer is gone, use our internal method
            EnsureAllButtonsInteractive();
        }
    }

    /// <summary>
    /// Makes sure all buttons in the game are properly interactive
    /// </summary>
    private void EnsureAllButtonsInteractive()
    {
        Debug.Log("Ensuring all buttons are interactive...");
        
        // Find all Canvas components and make sure they're enabled
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            canvas.enabled = true;
            
            // Make sure GraphicRaycaster is present and enabled
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("Added missing GraphicRaycaster to canvas: " + canvas.name);
            }
            raycaster.enabled = true;
        }
        
        // Find all buttons and make sure they're interactive
        UnityEngine.UI.Button[] allButtons = FindObjectsOfType<UnityEngine.UI.Button>();
        foreach (UnityEngine.UI.Button button in allButtons)
        {
            button.interactable = true;
            
            // Check if it's in an inactive parent
            if (!button.gameObject.activeInHierarchy)
            {
                Debug.Log("Button " + button.name + " is in an inactive GameObject hierarchy");
                continue;
            }
            
            // Check if there's a CanvasGroup blocking interaction
            CanvasGroup[] parentCanvasGroups = button.GetComponentsInParent<CanvasGroup>();
            foreach (CanvasGroup cg in parentCanvasGroups)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }
            
            Debug.Log("Ensured button is interactive: " + button.name);
        }
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Show only welcome panel at start.
    /// </summary>
    public void ShowWelcomePanel()
    {
        Debug.Log("ShowWelcomePanel called - hiding all panels and showing only StartPanel1");
        
        // First hide all panels
        HideAllPanels();
        
        // Then activate only StartPanel1
        if (StartPanel1 != null)
        {
            StartPanel1.SetActive(true);
            
            // Make sure its CanvasGroup is interactive if it has one
            CanvasGroup canvasGroup = StartPanel1.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
        
        EnsureAllButtonsInteractive();
    }

    /// <summary>
    /// Start the game: hide all panels, show game elements.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("Game Started!");

        // Hide all panels
        SetAllPanelsExceptGameElements(false);

        // Show game elements
        FoodItems.SetActive(true);
        TemperatureSlider.gameObject.SetActive(true);
        if (TempText != null)
            TempText.gameObject.SetActive(true);
        
        // Explicitly activate the CheckScoreButton
        if (CheckScoreButton != null)
        {
            CheckScoreButton.SetActive(true);
            
            // Make sure button is interactable
            UnityEngine.UI.Button button = CheckScoreButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.interactable = true;
            }
            
            // Make sure CanvasGroup is not blocking interaction
            CanvasGroup canvasGroup = CheckScoreButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
            
            // Ensure raycasting is enabled on parent canvases
            Canvas[] parentCanvases = CheckScoreButton.GetComponentsInParent<Canvas>();
            foreach (Canvas canvas in parentCanvases)
            {
                canvas.enabled = true;
            }
            
            // Force canvas update to refresh UI
            Canvas.ForceUpdateCanvases();
            
            Debug.Log("CheckScoreButton activated and made interactive in StartGame method");
        }
        else
        {
            Debug.LogError("CheckScoreButton is null in StartGame! Make sure it's assigned in the Inspector");
        }

        // Call GameManager to reset values
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null in StartGame method");
        }
    }

    /// <summary>
    /// Like SetAllPanels, but doesn't affect game elements
    /// </summary>
    private void SetAllPanelsExceptGameElements(bool status)
    {
        Debug.Log($"SetAllPanelsExceptGameElements called with status={status}");
        
        // Explicitly set each panel's active state with null checking
        if (StartPanel1 != null) StartPanel1.SetActive(status);
        if (StartPanel2 != null) StartPanel2.SetActive(status);
        if (StartPanel3 != null) StartPanel3.SetActive(status);
        if (StartPanel4 != null) StartPanel4.SetActive(status);
        if (FinalPanel1 != null) FinalPanel1.SetActive(status);
        if (FinalPanel2 != null) FinalPanel2.SetActive(status);
        if (TemperatureSettingPanel != null) TemperatureSettingPanel.SetActive(status);
        if (gameOverPanel != null) gameOverPanel.SetActive(status);
    }

    /// <summary>
    /// Show final result panel.
    /// </summary>
    public void ShowFinalPanel()
    {
        FinalPanel1.SetActive(true);
        FinalPanel2.SetActive(false);
    }

    /// <summary>
    /// Debug method to check all buttons in FinalPanel1 
    /// </summary>
    private void DebugFinalPanelButtons()
    {
        if (FinalPanel1 == null) 
        {
            Debug.LogError("Cannot debug FinalPanel1 buttons - panel is null");
            return;
        }
        
        Debug.Log($"Looking for buttons in {FinalPanel1.name}...");
        
        // Find all buttons in the panel
        UnityEngine.UI.Button[] buttons = FinalPanel1.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning($"No buttons found in {FinalPanel1.name}!");
            return;
        }
        
        Debug.Log($"Found {buttons.Length} buttons in {FinalPanel1.name}:");
        
        foreach (var button in buttons)
        {
            Debug.Log($"- Button: {button.name}, IsActive: {button.gameObject.activeSelf}, IsInteractable: {button.interactable}");
            
            // Check for click handlers
            var clickCount = button.onClick.GetPersistentEventCount();
            if (clickCount > 0)
            {
                for (int i = 0; i < clickCount; i++)
                {
                    var methodName = button.onClick.GetPersistentMethodName(i);
                    var targetObject = button.onClick.GetPersistentTarget(i);
                    Debug.Log($"  * Click handler #{i}: Method={methodName}, Target={targetObject}");
                    
                    // Check if this is our navigation button
                    if (methodName == "GoFromFinal1ToFinal2")
                    {
                        Debug.Log($"  * This is the 'Next' button that should navigate to FinalPanel2");
                        
                        // Enable the button if it's disabled
                        if (!button.interactable)
                        {
                            Debug.Log($"  * Button was not interactable - enabling it now");
                            button.interactable = true;
                        }
                        
                        if (!button.gameObject.activeSelf)
                        {
                            Debug.Log($"  * Button was not active - activating it now");
                            button.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"  * No click handlers attached to button {button.name}");
            }
        }
    }

    /// <summary>
    /// Show final result panel with scores.
    /// </summary>
    /// <param name="score">The player's accuracy score</param>
    /// <param name="tempScore">The temperature score</param>
    /// <param name="maxTempScore">The maximum possible temperature score</param>
    public void ShowFinalPanel(int score, int tempScore, int maxTempScore)
    {
        // Make sure we have the text elements
        EnsureFinalPanelTextElements();
        
        // Hide the CheckScoreButton
        if (CheckScoreButton != null)
        {
            CheckScoreButton.SetActive(false);
        }
        
        // Set score text - showing just the raw score (5 points per correct item)
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}";
        }
        
        // Get the temperature information
        if (temperatureResultText != null && TemperatureManager.Instance != null)
        {
            int currentTemp = TemperatureManager.Instance.currentTemperature;
            string tempStatus;
            
            if (currentTemp < 1)
            {
                tempStatus = $"<color=red>Temperature: {currentTemp}°C (Too Cold)</color>\n" +
                             "Most foods need 4°C for optimal storage.";
            }
            else if (currentTemp > 7)
            {
                tempStatus = $"<color=red>Temperature: {currentTemp}°C (Too Warm)</color>\n" +
                             "Most foods spoil faster above 7°C, need 4°C for optimal storage.";
            }
            else if (currentTemp <= 4)
            {
                tempStatus = $"<color=green>Temperature: {currentTemp}°C (Ideal)</color>\n" +
                             "Perfect for most refrigerated foods.";
            }
            else
            {
                tempStatus = $"<color=yellow>Temperature: {currentTemp}°C (Good)</color>\n" +
                             "Suitable for many foods, though some prefer colder.";
            }
            
            temperatureResultText.text = tempStatus;
        }
        
        // Get correctly placed items list
        if (correctItemsText != null && LifeAndScoreManager.Instance != null)
        {
            correctItemsText.text = LifeAndScoreManager.Instance.GetCorrectlyPlacedItemsList();
        }
        
        // Get incorrectly placed items list
        if (incorrectItemsText != null && LifeAndScoreManager.Instance != null)
        {
            incorrectItemsText.text = LifeAndScoreManager.Instance.GetIncorrectlyPlacedItemsList();
        }
        
        // Show first panel
        FinalPanel1.SetActive(true);
        FinalPanel2.SetActive(false);
        
        // Debug the buttons in FinalPanel1
        DebugFinalPanelButtons();
        
        Debug.Log("FinalPanel1 shown successfully - you should see a 'Next' button to navigate to FinalPanel2");
    }

    /// <summary>
    /// Show Game Over panel.
    /// </summary>
    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
        
        // Ensure user ID input field is ready
        EnsureGameOverPanelElements();
    }

    /// <summary>
    /// Go back to welcome panel (StartPanel1) from any state.
    /// </summary>
    public void GoToWelcome()
    {
        SetAllPanels(false);
        if (StartPanel1 != null) StartPanel1.SetActive(true);
        
        // Ensure buttons are interactive
        ButtonFixer fixer = FindObjectOfType<ButtonFixer>();
        if (fixer != null)
        {
            fixer.FixAllButtons();
            
            // Call FixStartPanel1Buttons specifically
            if (fixer.GetType().GetMethod("FixStartPanel1Buttons") != null)
            {
                fixer.FixStartPanel1Buttons();
            }
        }
        else
        {
            EnsureAllButtonsInteractive();
        }
    }

    public void RestartGame()
    {
        Debug.Log("==== RESTART GAME BEGINNING ====");
        
        // Log restart button click in the background
        if (PlayFabManager.Instance != null)
        {
            // Fire and forget - don't wait for this to complete
            // It will run asynchronously in the background
            PlayFabManager.Instance.LogRestartButtonClick();
            Debug.Log("Restart button click logged to PlayFab (async)");
        }
        
        Debug.Log("==== CONTINUING WITH RESTART IMMEDIATELY ====");
        
        // Reset all food items to initial positions
        DragSprite2D[] allFoodItems = FindObjectsOfType<DragSprite2D>();
        Debug.Log($"Resetting {allFoodItems.Length} food items to initial positions");
        foreach (DragSprite2D food in allFoodItems)
        {
            food.ResetFood();
            food.enabled = true;
        }

        // Reset game state in LifeAndScoreManager
        if (LifeAndScoreManager.Instance != null)
        {
            LifeAndScoreManager.Instance.ResetGameState();
            Debug.Log("LifeAndScoreManager game state reset");
        }
        
        // Hide all panels and game elements
        Debug.Log("Hiding all panels and game elements");
        if (FinalPanel1 != null)
            FinalPanel1.SetActive(false);
        if (FinalPanel2 != null)
            FinalPanel2.SetActive(false);
        
        // Hide the "Check score" button during setup
        if (CheckScoreButton != null)
            CheckScoreButton.SetActive(false);
        
        // Go to temperature setting panel instead of starting the game directly
        Debug.Log("Setting up temperature panel");
        SetAllPanels(false);
        TemperatureSettingPanel.SetActive(true);
        
        // Reset the temperature slider to 0 (or desired default)
        if (TemperatureSlider != null)
        {
            TemperatureSlider.value = 0;
            Debug.Log("Temperature slider reset to 0");
        }
        
        Debug.Log("==== RESTART GAME COMPLETE ====");
    }


    public void GoFromStart1ToStart2()
    {
        Debug.Log("GoFromStart1ToStart2 called - attempting to navigate from StartPanel1 to StartPanel2");
        
        // Check if panels exist before trying to access them
        if (StartPanel1 == null)
        {
            Debug.LogError("StartPanel1 is null! Cannot navigate from it.");
            return;
        }
        
        if (StartPanel2 == null)
        {
            Debug.LogError("StartPanel2 is null! Cannot navigate to it.");
            return;
        }
        
        Debug.Log($"Panel states before transition - StartPanel1: {StartPanel1.activeSelf}, StartPanel2: {StartPanel2.activeSelf}");
        
        // Simple panel transition
        StartPanel1.SetActive(false);
        StartPanel2.SetActive(true);
        
        Debug.Log($"Panel states after transition - StartPanel1: {StartPanel1.activeSelf}, StartPanel2: {StartPanel2.activeSelf}");
        
        // Force canvas update to ensure UI refreshes properly
        Canvas.ForceUpdateCanvases();
        
        // Ensure buttons in StartPanel2 are interactive
        if (StartPanel2 != null)
        {
            Button[] buttons = StartPanel2.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                button.interactable = true;
                Debug.Log($"Made button {button.name} in StartPanel2 interactable");
            }
        }
    }

    

    public void GoFromStart2ToStart3()
    {
        StartPanel2.SetActive(false);
        StartPanel3.SetActive(true);
    }

    public void GoFromStart3ToStart4()
    {
        StartPanel3.SetActive(false);
        StartPanel4.SetActive(true);
    }

    public void GoFromStart4ToStart3()
    {
        StartPanel4.SetActive(false);
        StartPanel3.SetActive(true);
    }

    
    public void GoFromStart4ToTemperatureSetting()
    {
        Debug.Log("Transitioning from Start4 to Temperature Setting Panel");
        StartPanel4.SetActive(false);
        TemperatureSettingPanel.SetActive(true);
        
        // Update the temperature text display
        if (TemperatureManager.Instance != null && TempText != null)
        {
            int currentTemp = TemperatureManager.Instance.currentTemperature;
            TempText.text = currentTemp.ToString();
            Debug.Log($"Updated temperature display to: {currentTemp}");
        }
        
        // Set the slider to the current temperature value
        if (TemperatureSlider != null && TemperatureManager.Instance != null)
        {
            TemperatureSlider.value = TemperatureManager.Instance.currentTemperature;
        }
    }

    public void GoFromStart2ToStart1()
    {
        StartPanel2.SetActive(false);
        StartPanel1.SetActive(true);
    }

    public void GoFromStart3ToStart2()
    {
        StartPanel3.SetActive(false);
        StartPanel2.SetActive(true);
    }

    public void GoFromFinal1ToFinal2()
    {
        Debug.Log("GoFromFinal1ToFinal2 called! Attempting to navigate to FinalPanel2...");
        
        // Check if panels exist before trying to access them
        if (FinalPanel1 == null)
        {
            Debug.LogError("FinalPanel1 is null! Cannot navigate from it.");
            return;
        }
        
        if (FinalPanel2 == null)
        {
            Debug.LogError("FinalPanel2 is null! Cannot navigate to it.");
            return;
        }
        
        // Ensure needed components are set up
        EnsureFinalPanelTextElements();
        
        Debug.Log($"Panel states before transition - FinalPanel1: {FinalPanel1.activeSelf}, FinalPanel2: {FinalPanel2.activeSelf}");
        
        // Hide panel 1 and show panel 2
        FinalPanel1.SetActive(false);
        FinalPanel2.SetActive(true);
        
        Debug.Log($"Panel states after transition - FinalPanel1: {FinalPanel1.activeSelf}, FinalPanel2: {FinalPanel2.activeSelf}");
        
        // Force a layout refresh if needed
        Canvas.ForceUpdateCanvases();
    }

    public void GoFromFinal2ToFinal1()
    {
        Debug.Log("GoFromFinal2ToFinal1 called! Attempting to navigate back to FinalPanel1...");
        
        // Check if panels exist before trying to access them
        if (FinalPanel2 == null)
        {
            Debug.LogError("FinalPanel2 is null! Cannot navigate from it.");
            return;
        }
        
        if (FinalPanel1 == null)
        {
            Debug.LogError("FinalPanel1 is null! Cannot navigate to it.");
            return;
        }
        
        Debug.Log($"Panel states before transition - FinalPanel1: {FinalPanel1.activeSelf}, FinalPanel2: {FinalPanel2.activeSelf}");
        
        // Hide panel 2 and show panel 1
        FinalPanel2.SetActive(false);
        FinalPanel1.SetActive(true);
        
        Debug.Log($"Panel states after transition - FinalPanel1: {FinalPanel1.activeSelf}, FinalPanel2: {FinalPanel2.activeSelf}");
        
        // Force a layout refresh if needed
        Canvas.ForceUpdateCanvases();
    }
    
    public void GoToOverPanel()
    {
        SetAllPanels(false);
        gameOverPanel.SetActive(true);
        
        // Ensure user ID input field is ready
        EnsureGameOverPanelElements();
    }

    public void ContinueGameStart()
    {
        // Hide the temperature setting panel and start the game
        TemperatureSettingPanel.SetActive(false);
        
        // Show game elements
        TemperatureSlider.gameObject.SetActive(true);
        FoodItems.SetActive(true);
        
        // Show the "I'm done" button only during gameplay
        if (CheckScoreButton != null)
        {
            CheckScoreButton.SetActive(true);
            
            // Make sure button is interactable
            UnityEngine.UI.Button button = CheckScoreButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.interactable = true;
            }
            
            // Make sure CanvasGroup is not blocking interaction
            CanvasGroup canvasGroup = CheckScoreButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
            
            // Ensure raycasting is enabled on parent canvases
            Canvas[] parentCanvases = CheckScoreButton.GetComponentsInParent<Canvas>();
            foreach (Canvas canvas in parentCanvases)
            {
                canvas.enabled = true;
            }
            
            // Force canvas update to refresh UI
            Canvas.ForceUpdateCanvases();
            
            Debug.Log("CheckScoreButton activated and made interactive in ContinueGameStart");
        }
        else
        {
            Debug.LogError("CheckScoreButton is null! Make sure it's assigned in the Inspector");
        }
        
        // Start the game logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    /// <summary>
    /// Set the active state of all panels.
    /// </summary>
    private void SetAllPanels(bool status)
    {
        //welcomePanel.SetActive(status);
        StartPanel1.SetActive(status);
        StartPanel2.SetActive(status);
        StartPanel3.SetActive(status);
        StartPanel4.SetActive(status);
        FinalPanel1.SetActive(status);
        FinalPanel2.SetActive(status);
        TemperatureSettingPanel.SetActive(status);
        gameOverPanel.SetActive(status);

        // Always hide the check score button when hiding/showing panels globally
        if (CheckScoreButton != null)
            CheckScoreButton.SetActive(false);

        // In tutorial, keep temperature slider visible
        if (!status)
        {
            TemperatureSlider.gameObject.SetActive(true);
        }
        else
        {
            TemperatureSlider.gameObject.SetActive(status);
        }

        // ScoreText removed
        
        // WarningPanel.SetActive(status);
        FoodItems.SetActive(status);
    }

    /// <summary>
    /// Find existing text elements for final panels
    /// </summary>
    private void EnsureFinalPanelTextElements()
    {
        // Check if panels exist
        if (FinalPanel1 == null || FinalPanel2 == null)
        {
            Debug.LogError("Final panels not assigned!");
            return;
        }
        
        // Find elements for FinalPanel1 if not assigned through inspector
        if (finalScoreText == null)
        {
            finalScoreText = FindTextElementInPanel(FinalPanel1, "FinalScoreText");
        }
        
        if (temperatureResultText == null)
        {
            temperatureResultText = FindTextElementInPanel(FinalPanel1, "TemperatureResultText");
        }
        
        if (correctItemsText == null)
        {
            correctItemsText = FindTextElementInPanel(FinalPanel1, "CorrectItemsText");
            
            // Set alignment settings for consistent display
            if (correctItemsText != null)
            {
                correctItemsText.alignment = TextAlignmentOptions.TopLeft;
                correctItemsText.enableWordWrapping = false;
                correctItemsText.overflowMode = TextOverflowModes.Overflow;
            }
        }
        
        // Find element for FinalPanel2 if not assigned through inspector
        if (incorrectItemsText == null)
        {
            incorrectItemsText = FindTextElementInPanel(FinalPanel2, "IncorrectItemsText");
            
            // Set alignment settings for consistent display
            if (incorrectItemsText != null)
            {
                incorrectItemsText.alignment = TextAlignmentOptions.TopLeft;
                incorrectItemsText.enableWordWrapping = false;
                incorrectItemsText.overflowMode = TextOverflowModes.Overflow;
            }
        }
    }
    
    /// <summary>
    /// Find and set up elements in the Game Over panel
    /// </summary>
    private void EnsureGameOverPanelElements()
    {
        // Check if game over panel exists
        if (gameOverPanel == null)
        {
            Debug.LogError("Game Over panel not assigned! Please assign it in the inspector.");
            return;
        }
        
        Debug.Log($"Setting up GameOverPanel elements: {gameOverPanel.name}");

        // For WebGL builds, we'll use the HTML input field instead
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Call JavaScript to show the user ID input
        RequestUserIdFromWebPage();
        #else
        // Find user ID input field for non-WebGL builds
        if (userIdInputField == null)
        {
            Debug.Log("Looking for UserIdInputField...");
            userIdInputField = FindInputFieldInPanel(gameOverPanel, "UserIdInputField");
            
            // If input field still not found, try to create one or look for any TMP input field
            if (userIdInputField == null)
            {
                Debug.LogWarning("UserIdInputField not found in GameOverPanel. Checking for any TMP_InputField...");
                
                // Find any TMP_InputField
                TMP_InputField[] inputFields = gameOverPanel.GetComponentsInChildren<TMP_InputField>(true);
                if (inputFields != null && inputFields.Length > 0)
                {
                    userIdInputField = inputFields[0];
                    Debug.Log($"Found a TMP_InputField with name '{userIdInputField.name}'. Using this instead.");
                }
                else
                {
                    Debug.LogError("No TMP_InputField found in GameOverPanel. Please add one through the Unity Editor.");
                }
            }
            else
            {
                Debug.Log($"Found UserIdInputField: {userIdInputField.name}");
            }
            
            // Set up the input field if we found one
            if (userIdInputField != null)
            {
                // Set placeholder text to guide the user
                if (userIdInputField.placeholder != null && userIdInputField.placeholder is TextMeshProUGUI placeholderText)
                {
                    placeholderText.text = "Enter your ID number here";
                    Debug.Log("Set placeholder text on input field");
                }
                
                // Make sure the input field is enabled and interactable
                userIdInputField.gameObject.SetActive(true);
                userIdInputField.interactable = true;
                
                // Try to focus on the input field (may not work in certain cases)
                try
                {
                    userIdInputField.Select();
                    Debug.Log("Selected the UserIdInputField");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not select UserIdInputField: {e.Message}");
                }
            }
        }
        #endif
    }

    /// <summary>
    /// Helper method to find TextMeshPro elements by name
    /// </summary>
    private TextMeshProUGUI FindTextElementInPanel(GameObject panel, string elementName)
    {
        // First try to find by exact name
        Transform element = panel.transform.Find(elementName);
        if (element != null)
        {
            TextMeshProUGUI tmp = element.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                return tmp;
            }
        }
        
        // If not found by name, look for any TextMeshProUGUI component in children
        TextMeshProUGUI[] allTextElements = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (allTextElements != null && allTextElements.Length > 0)
        {
            // Log warning that we're using the first text element found
            Debug.LogWarning($"Could not find text element named '{elementName}'. Using first TextMeshProUGUI found in {panel.name}.");
            return allTextElements[0];
        }
        
        Debug.LogError($"No TextMeshProUGUI component found in {panel.name}! Please add a TextMeshProUGUI element to the panel.");
        return null;
    }

    /// <summary>
    /// Helper method to find TMP_InputField elements by name
    /// </summary>
    private TMP_InputField FindInputFieldInPanel(GameObject panel, string elementName)
    {
        if (panel == null)
        {
            Debug.LogError("Panel is null in FindInputFieldInPanel");
            return null;
        }
        
        // Debug to show what we're searching for
        Debug.Log($"Looking for TMP_InputField named '{elementName}' in panel '{panel.name}'");
        
        // First try to find by exact name
        Transform element = panel.transform.Find(elementName);
        if (element != null)
        {
            TMP_InputField inputField = element.GetComponent<TMP_InputField>();
            if (inputField != null)
            {
                Debug.Log($"Found TMP_InputField directly: '{elementName}' in '{panel.name}'");
                return inputField;
            }
        }
        
        // If not found by name at root level, search deeper
        Transform[] allTransforms = panel.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == elementName)
            {
                TMP_InputField inputField = t.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    Debug.Log($"Found TMP_InputField by name in children: '{elementName}' in '{panel.name}'");
                    return inputField;
                }
            }
        }
        
        // If still not found, try any input field
        TMP_InputField[] allInputFields = panel.GetComponentsInChildren<TMP_InputField>(true);
        if (allInputFields != null && allInputFields.Length > 0)
        {
            Debug.LogWarning($"Could not find input field named '{elementName}'. Using first TMP_InputField found in {panel.name}.");
            // Log all found input fields
            for (int i = 0; i < allInputFields.Length; i++)
            {
                Debug.Log($"Input field [{i}]: '{allInputFields[i].name}' (active: {allInputFields[i].gameObject.activeInHierarchy})");
            }
            return allInputFields[0];
        }
        
        Debug.LogError($"No TMP_InputField component found in {panel.name}! Please add a TMP_InputField element to the panel.");
        return null;
    }

    /// <summary>
    /// This method will be called when the player clicks "I'm done! Check my score please" button
    /// </summary>
    public void CheckScoreAndShowResults()
    {
        Debug.Log("CheckScoreAndShowResults called");
        
        // Find GameManager if it's null
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null, trying to find it...");
            GameManager gameManagerObj = FindObjectOfType<GameManager>();
            
            if (gameManagerObj != null)
            {
                Debug.Log("Found GameManager through FindObjectOfType");
                gameManagerObj.FinalizeGameAndCalculateScore();
                return;
            }
            else
            {
                // Create our own way to calculate score if GameManager can't be found
                Debug.LogWarning("Creating fallback score calculation...");
                CalculateScoreFallback();
            }
        }
        else
        {
            GameManager.Instance.FinalizeGameAndCalculateScore();
        }
    }

    /// <summary>
    /// Fallback method to calculate score and show results if GameManager is not available
    /// </summary>
    private void CalculateScoreFallback()
    {
        // Find all food items
        DragSprite2D[] foodItems = FindObjectsOfType<DragSprite2D>();
        
        // Get current temperature
        int currentTemp = 0;
        if (TemperatureManager.Instance != null)
        {
            currentTemp = TemperatureManager.Instance.currentTemperature;
        }
        
        // Calculate score directly using LifeAndScoreManager
        if (LifeAndScoreManager.Instance != null)
        {
            // Check each food item's placement
            foreach (DragSprite2D foodItem in foodItems)
            {
                if (!foodItem.gameObject.activeSelf) continue;
                
                string foodType = foodItem.foodType;
                
                // Get the current zone
                string currentZone = GetZoneAtPosition(foodItem.transform.position);
                
                if (!string.IsNullOrEmpty(currentZone))
                {
                    // Check placement (adds to score inside this method)
                    LifeAndScoreManager.Instance.CheckPlacement(foodType, currentZone, currentTemp);
                }
            }
            
            // Get score and show the final panel
            int score = LifeAndScoreManager.Instance.GetScore(true);
            
            // Show the final panel with only the score parameter
            ShowFinalPanel(score, 0, 0);
        }
        else
        {
            Debug.LogError("Both GameManager and LifeAndScoreManager are unavailable. Cannot calculate score.");
            // Show a simple final panel with error message
            if (finalScoreText != null)
            {
                finalScoreText.text = "Error: Unable to calculate score.\nPlease restart the game.";
            }
            FinalPanel1.SetActive(true);
        }
    }

    /// <summary>
    /// Gets the zone tag at a specific position
    /// </summary>
    private string GetZoneAtPosition(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(position);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("TopShelf") || collider.CompareTag("MiddleShelf") || 
                collider.CompareTag("BottomShelf") || collider.CompareTag("Drawer") ||
                collider.CompareTag("DryBox") || collider.CompareTag("TopDoor") ||
                collider.CompareTag("MiddleDoor") || collider.CompareTag("BottomDoor"))
            {
                return collider.tag;
            }
        }
        return "";
    }

    /// <summary>
    /// Force shows the "Check Score" button even if it was hidden
    /// </summary>
    public void ForceShowCheckScoreButton()
    {
        if (CheckScoreButton != null)
        {
            // Ensure it's active
            CheckScoreButton.SetActive(true);
            
            // Bring to front if needed by setting its sibling index
            CheckScoreButton.transform.SetAsLastSibling();
            
            // Make sure it's not affected by any CanvasGroup
            CanvasGroup cg = CheckScoreButton.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            
            Debug.Log("CheckScoreButton forcefully activated");
        }
        else
        {
            Debug.LogError("CheckScoreButton is null in ForceShowCheckScoreButton! Make sure it's assigned in the Inspector");
        }
    }

    private void UpdateUI()
    {
        // Method is no longer needed - ScoreText removed
    }

    /// <summary>
    /// Handle the "Done" button click in the Game Over panel
    /// </summary>
    public void OnGameOverDoneButtonClicked()
    {
        Debug.Log("Game Over Done button clicked");
        
        // Make sure we have a reference to the input field
        if (userIdInputField == null)
        {
            userIdInputField = FindInputFieldInPanel(gameOverPanel, "UserIdInputField");
        }
        
        string userId = "";
        
        // Get the user ID if the input field exists
        if (userIdInputField != null)
        {
            userId = userIdInputField.text.Trim();
            Debug.Log($"Retrieved User ID from input field: '{userId}'");
        }
        else
        {
            Debug.LogWarning("Could not find UserIdInputField in GameOverPanel");
        }
        
        // Save the ID locally first using UserData if available
        if (!string.IsNullOrEmpty(userId) && UserData.Instance != null)
        {
            UserData.Instance.ParticipantId = userId;
            UserData.Instance.SaveParticipantId(userId);
            Debug.Log($"Saved participant ID to UserData: '{userId}'");
        }
        
        // Save to PlayFab if we have a user ID and PlayFabManager is available
        if (!string.IsNullOrEmpty(userId) && PlayFabManager.Instance != null)
        {
            Debug.Log($"Attempting to save User ID to PlayFab: '{userId}'");
            
            // Make sure we're sanitizing the ID to avoid issues
            string sanitizedUserId = System.Text.RegularExpressions.Regex.Replace(userId, "[^a-zA-Z0-9_]", "").Trim();
            
            if (!string.IsNullOrEmpty(sanitizedUserId))
            {
                // Save to PlayFab - ensure this is an alphanumeric ID
                PlayFabManager.Instance.SaveUserIdToPlayFab(sanitizedUserId);
                
                // Also log the game session completion
                if (LifeAndScoreManager.Instance != null)
                {
                    int score = LifeAndScoreManager.Instance.GetScore(true);
                    
                    // Calculate accuracy using public methods instead of accessing private fields
                    float accuracy = 0;
                    int correctPlacements = LifeAndScoreManager.Instance.GetCorrectPlacementsCount();
                    int totalPlacements = LifeAndScoreManager.Instance.GetTotalPlacementsCount();
                    
                    if (totalPlacements > 0)
                    {
                        accuracy = (float)correctPlacements / totalPlacements * 100f;
                    }
                    
                    int temperatureScore = 0;
                    
                    if (TemperatureManager.Instance != null)
                    {
                        int temp = TemperatureManager.Instance.currentTemperature;
                        if (temp >= 1 && temp <= 4)
                            temperatureScore = 20; // Full temperature score
                        else if (temp >= 5 && temp <= 7)
                            temperatureScore = 10; // Half score
                        else
                            temperatureScore = 0;  // No score
                    }
                    
                    string grade = score >= 80 ? "A" : 
                                   score >= 60 ? "B" : 
                                   score >= 40 ? "C" : 
                                   score >= 20 ? "D" : "F";
                    
                    PlayFabManager.Instance.LogGameEnd(score, accuracy, grade, temperatureScore);
                    Debug.Log($"Logged game end with score={score}, accuracy={accuracy:F1}%, grade={grade}");
                }
            }
            else
            {
                Debug.LogWarning($"User ID '{userId}' became empty after sanitization - not saving to PlayFab");
            }
        }
        else if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("User ID is empty - not saving to PlayFab");
        }
        else if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("PlayFabManager not available - user ID not saved to PlayFab");
        }
        
        // Return to welcome panel or any other desired action
        GoToWelcome();
    }

    // Initialize the temperature text with current temperature
    private void InitializeTemperatureText()
    {
        if (TempText != null)
        {
            // Get the current temperature from TemperatureManager if available
            if (TemperatureManager.Instance != null)
            {
                int currentTemp = TemperatureManager.Instance.currentTemperature;
                TempText.text = currentTemp.ToString();
                Debug.Log($"Initialized temperature display to: {currentTemp}");
            }
            else
            {
                // Default display if TemperatureManager isn't available
                TempText.text = "0";
                Debug.Log("TemperatureManager not found, initialized temperature display to default value");
            }
            
            // Make sure the text is visible
            TempText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("TempText not assigned in UIManager");
        }
    }

    public void SetUserIdFromWebPage(string userId)
    {
        Debug.Log($"Received user ID from web page: {userId}");
        
        // Store the user ID
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.SaveUserIdToPlayFab(userId);
        }
        
        // Hide the GameOverPanel if it's active
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
        }
        
        // If you want to transition to a specific panel or start the game
        // For example, after getting the ID, continue with the game
        GoToWelcome();
    }

    // This method can be called from Unity to prompt for ID again if needed
    public void RequestUserIdFromWebPage()
    {
        // Call JavaScript function to show the ID prompt
        #if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
        Application.ExternalEval("window.showUserIdPromptFromUnity()");
        #else
        Debug.Log("RequestUserIdFromWebPage only works in WebGL builds");
        #endif
    }
}

