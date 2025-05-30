# Fridge2DGame

please visit online game link: https://sharyz119.itch.io/fridge2dgame


## Project Overview

The **Fridge Organization Game** is an educational Unity-based game designed to teach players about proper food storage, temperature management, and food safety principles. The game combines interactive gameplay with comprehensive data collection for research and educational analysis. Notice: for further develop this game, please combine with Fridge2dGame.unitypackage to work together.


## Main Features and Objectives

### Core Gameplay Features
- **Drag-and-Drop Mechanics**: Interactive food placement system
- **Temperature Management**: Dynamic temperature control with visual feedback
- **Scoring System**: Points-based evaluation (5 points per correct placement)
- **Multiple Storage Zones**: 8 different storage areas with specific requirements
- **Real-time Feedback**: Immediate visual and audio feedback for player actions

### Educational Objectives
- Teach proper food storage principles
- Demonstrate temperature importance in food preservation
- Reinforce food safety concepts through gameplay
- Provide immediate feedback for learning reinforcement


## Project Architecture

```
Fridge Organization Game
├── Core Game Systems
│   ├── Game Management (GameManager.cs)
│   ├── UI Management (UIManager.cs)
│   ├── Score & Life Management (LifeAndScoreManager.cs)
│   └── Temperature Control (TemperatureManager.cs)
├── Interaction Systems
│   ├── Drag & Drop (drag.cs)
│   ├── Tooltip System (TooltipController.cs, TooltipSystem.cs)
│   └── Hoverable Items (HoverableItem.cs)
├── Analytics & Data Collection
│   ├── PlayFab Integration (PlayFabManager.cs)
│   ├── Game Analytics (GameAnalytics.cs)
│   ├── Data Export (PlayFabDataExporter.cs)
│   └── User Data Management (UserData.cs)
├── Utility Systems
│   ├── Manager Initialization (ManagerInitializer.cs)
│   ├── Button Management (ButtonFixer.cs)
│   ├── Debug Tools (InputDebugLogger.cs, WebGLErrorHandler.cs)
│   └── Platform Optimization (BurstDisable.cs)
└── Web Integration
    ├── HTML Interface (index.html)
    └── PlayFab Debug UI (PlayFabDebugUI.cs)
```

## Code Structure and File Explanations

### Core Game Management

#### `GameManager.cs` 
**Purpose**: Central game controller and coordinator
**Main Functions**:
- Game state management (start, end, restart)
- Score tracking and coordination
- Analytics event coordination
- Manager initialization and reference management

**Key Methods**:
- `StartGame()`: Initializes a new game session
- `AddScore(int points)`: Awards points for correct placements
- `ShowFinalPanel()`: Displays end-game results
- `LogFoodPlacement()`: Coordinates analytics logging

#### `UIManager.cs`
**Purpose**: Comprehensive UI control and panel management
**Main Functions**:
- Panel visibility management (tutorial, game, final panels)
- Button interaction handling
- Temperature display coordination
- Final score presentation

**Key Features**:
- Tutorial panel sequence management
- Temperature slider integration
- Final panel with detailed results
- Button state management and interaction prevention

#### `LifeAndScoreManager.cs`
**Purpose**: Core scoring logic and food placement validation
**Main Functions**:
- Food placement correctness evaluation
- Score calculation (5 points per correct item)
- Food-zone mapping and validation
- Detailed placement data collection

**Key Methods**:
- `CheckPlacement()`: Validates food placement correctness
- `GetScore()`: Calculates final score based on correct placements
- `GetDetailedItemPlacementData()`: Provides comprehensive placement analytics

#### `TemperatureManager.cs`
**Purpose**: Temperature control and feedback system
**Main Functions**:
- Temperature slider management
- Visual warning system (flashing panels)
- Temperature validation for food safety
- Temperature change logging

**Key Features**:
- Optimal temperature range: 1-4°C
- Acceptable range: 1-7°C
- Visual feedback for temperature violations
- Integration with scoring system

### Interaction Systems

#### `drag.cs`
**Purpose**: Drag-and-drop mechanics for food items
**Main Functions**:
- Mouse-based dragging implementation
- Zone detection and validation
- Manager reference initialization
- Placement event triggering

**Key Features**:
- Robust manager initialization with retry logic
- Zone collision detection
- Analytics event triggering
- Visual feedback coordination

#### `TooltipController.cs` & `TooltipSystem.cs`
**Purpose**: Interactive help system
**Main Functions**:
- Hover-based information display
- Food-specific guidance
- Dynamic tooltip positioning
- Educational content delivery

### Analytics and Data Collection

#### `PlayFabManager.cs`
**Purpose**: Comprehensive PlayFab integration for data collection
**Main Functions**:
- User authentication and session management
- Detailed event logging (placements, temperature changes, scores)
- Statistics tracking and updating
- Error handling and retry logic

**Data Collected**:
- Final scores for each game session
- Correct/incorrect item counts
- Temperature settings and changes
- Individual item placements and correctness
- User IDs and session information
- Game duration and completion metrics

**Key Methods**:
- `LogGameEnd()`: Records comprehensive session data
- `LogFoodPlacement()`: Tracks individual placement events
- `UpdateStatisticWithRetry()`: Robust statistic updating with error handling

#### `GameAnalytics.cs`
**Purpose**: Local analytics and engagement tracking
**Main Functions**:
- Session duration tracking
- User engagement metrics
- Local event queuing
- Completion time analysis

**Key Features**:
- Engagement ratio calculation
- Session data persistence
- Event queue management
- Application lifecycle handling

#### `PlayFabDataExporter.cs` (324 lines)
**Purpose**: Data export and analysis tools
**Main Functions**:
- Data retrieval from PlayFab
- CSV export functionality
- Data formatting and processing
- Batch data operations

### Utility and Support Systems

#### `ManagerInitializer.cs`
**Purpose**: Ensures proper initialization of all game managers
**Main Functions**:
- Manager instance creation and validation
- Reference distribution to game objects
- Initialization order management
- Error prevention and debugging

#### `ButtonFixer.cs`
**Purpose**: UI interaction management and button state control
**Main Functions**:
- Button interactivity management
- Click event handling
- UI state synchronization
- Interaction prevention during transitions

#### `UserData.cs`
**Purpose**: User identification and session management
**Main Functions**:
- User ID generation and storage
- Session tracking
- Data persistence
- User preference management

### Web Integration

#### `index.html`
**Purpose**: Web deployment interface with external user input
**Main Functions**:
- HTML-based user ID input (replacing Unity InputField)
- Unity WebGL integration
- JavaScript-Unity communication
- Improved web compatibility

**Key Features**:
- External HTML input field for better web performance
- JavaScript function to pass user ID to Unity
- Responsive design for web deployment

## Data Collection and Analytics

### Comprehensive Data Tracking

The game collects the following data through PlayFab:

1. **Game Session Data**:
   - Final score for each game played
   - Number of correct and incorrect placements
   - Game duration and completion status
   - Temperature settings throughout the game

2. **Individual Item Tracking**:
   - Which items were placed in which zones
   - Correctness of each placement
   - Timestamp of each placement action
   - Temperature at time of placement

3. **User Behavior Analytics**:
   - User ID (entered before game start)
   - Session identifiers and timestamps
   - Interaction patterns and timing
   - Error patterns and learning progression

4. **Performance Metrics**:
   - Total games played per user
   - Improvement over multiple sessions
   - Common mistake patterns
   - Temperature management effectiveness

### Data Structure Example
```json
{
  "userId": "user123",
  "sessionId": "session456",
  "gameData": {
    "finalScore": 25,
    "correctPlacements": 5,
    "incorrectPlacements": 0,
    "temperatureSettings": [2, 3, 2],
    "itemPlacements": {
      "apple": {"zone": "Drawer", "correct": true, "temperature": 2},
      "milk": {"zone": "MiddleDoor", "correct": true, "temperature": 3}
    }
  }
}
```

## How to Use - For Players

### Getting Started
1. **Access the Game**: Visit the game URL (itch.io or GitHub Pages)
2. **Enter User ID**: Input your participant ID in the text field
3. **Click "Start Game"**: Begin the tutorial and gameplay

### Gameplay Instructions
1. **Tutorial Phase**:
   - Follow the guided tutorial panels
   - Learn about different storage zones
   - Practice with temperature controls

2. **Main Game**:
   - Drag food items from the starting area
   - Drop them into appropriate storage zones
   - Adjust temperature using the slider (optimal: 1-4°C)
   - Aim for correct placement of all items

3. **Scoring**:
   - Earn 5 points for each correctly placed item
   - Maximum possible score: 75 points (15 items × 5 points)
   - Final score displayed at game completion

### Storage Zone Guide
- **TopShelf**: Pickles, condiments
- **MiddleShelf**: Cheese, pasta, leftovers
- **BottomShelf**: Raw meat, fish
- **Drawer**: Fruits and vegetables
- **DryBox**: Produce that doesn't need refrigeration
- **Door Zones**: Dairy, beverages, condiments

### Tips for Success
- Pay attention to temperature warnings (flashing panels)
- Use the tooltip system for item-specific guidance
- Consider food safety principles when placing items
- Maintain temperature between 1-4°C for optimal results

## How to Use - For Developers

### Prerequisites
- Unity 2021.3 or later
- PlayFab Unity SDK
- TextMeshPro package
- Basic understanding of C# and Unity

### Initial Setup

1. **Clone and Open Project**:
   ```bash
   git clone https://github.com/sharyz119/Fridge2DGame.git
   cd Fridge_Organization_Game
   ```
   Open the project in Unity

2. **Install Dependencies**:
   - Import PlayFab SDK from Package Manager
   - Ensure TextMeshPro is installed
   - Import any missing Unity packages (important!!)

3. **Configure PlayFab**:
   - Create a PlayFab account and title
   - Update `PlayFabManager.cs` with your Title ID
   - Configure PlayFab settings in Unity

4. **Scene Setup**:
   - Use the automated setup: `Setup > Create Managers` in Unity menu
   - Or manually create manager hierarchy as described in `SETUP_GUIDE.md`

### Development Workflow

#### Adding New Food Items
1. Create food GameObject with `DragSprite2D` component
2. Set `correctTag` to appropriate zone
3. Set `foodType` to unique identifier
4. Update `LifeAndScoreManager.cs` food-zone mappings

#### Modifying Analytics
1. Add new events in `PlayFabManager.cs`
2. Update data collection methods
3. Test data flow in PlayFab dashboard
4. Update export functionality if needed

#### UI Modifications
1. Update panel references in `UIManager.cs`
2. Modify button interactions in `ButtonFixer.cs`
3. Test panel transitions and state management
4. Update tooltip content in tooltip system

### Building and Testing

#### Local Testing
1. Test in Unity Play Mode
2. Check console for initialization errors
3. Verify manager references are properly set
4. Test analytics data flow

#### WebGL Build Process
1. Configure WebGL settings in Player Settings
2. Set compression to Gzip
3. Build to designated folder
4. Test in local web server before deployment

#### Debugging Tools
- Use `InputDebugLogger.cs` for input debugging
- Check `PlayFabDebugUI.cs` for PlayFab connection status
- Monitor Unity Console for manager initialization messages
- Use browser developer tools for WebGL debugging

### Code Modification Guidelines

#### Adding New Analytics Events
```csharp
// In PlayFabManager.cs
public void LogCustomEvent(string eventName, Dictionary<string, object> eventData)
{
    if (!IsInitialized) return;
    
    var request = new WriteClientPlayerEventRequest
    {
        EventName = eventName,
        Body = eventData
    };
    
    PlayFabClientAPI.WritePlayerEvent(request, OnEventLogged, OnEventError);
}
```

#### Modifying Scoring Logic
```csharp
// In LifeAndScoreManager.cs
public bool CheckPlacement(string foodType, string zone, int temperature)
{
    // Add custom validation logic
    bool isCorrectZone = GetFoodZone(foodType) == zone;
    bool isCorrectTemp = IsTemperatureAppropriate(temperature);
    
    // Update scoring and analytics
    return isCorrectZone && isCorrectTemp;
}
```

### Performance Optimization

#### WebGL Optimization
- Minimize texture sizes
- Use compressed audio formats
- Optimize script execution order
- Implement object pooling for UI elements

#### Analytics Optimization
- Batch analytics events when possible
- Implement retry logic for network failures
- Use async operations to prevent blocking
- Cache data locally before sending to PlayFab

## Deployment and Publishing

### Web Deployment Options

#### GitHub Pages
1. Create WebGL build in Unity
2. Place build files in `docs` folder of GitHub repository
3. Enable GitHub Pages in repository settings
4. Access game at `https://username.github.io/repository-name`

#### Itch.io Deployment
1. Create WebGL build
2. Compress build folder to ZIP
3. Upload to itch.io project page
4. Configure embed settings and publish

### PlayFab Configuration for Production
1. Set up production PlayFab title
2. Configure data retention policies
3. Set up automated data exports
4. Monitor usage and performance metrics

### Data Collection Compliance
- Implement privacy policy display
- Provide opt-out mechanisms
- Ensure GDPR/COPPA compliance
- Regular data cleanup procedures

## Troubleshooting

### Common Issues and Solutions

#### Manager Initialization Errors
**Problem**: "Required managers not found" errors
**Solution**: 
- Run `Setup > Create Managers` in Unity
- Check that all manager GameObjects exist in scene
- Verify script references are properly assigned

#### PlayFab Connection Issues
**Problem**: Data not being sent to PlayFab
**Solution**:
- Verify Title ID is correctly set
- Check internet connectivity
- Monitor PlayFab dashboard for incoming data
- Check Unity Console for PlayFab error messages

#### WebGL Performance Issues
**Problem**: Game runs slowly in browser
**Solution**:
- Reduce texture quality in build settings
- Enable compression in WebGL settings
- Optimize script execution
- Test in different browsers

#### Analytics Data Missing
**Problem**: Some analytics events not recorded
**Solution**:
- Check manager initialization order
- Verify event logging methods are called
- Monitor network requests in browser dev tools
- Check PlayFab event limits and quotas

### Debug Tools and Logging

#### Unity Console Messages
- Manager initialization status
- PlayFab connection status
- Event logging confirmations
- Error messages and stack traces

#### Browser Developer Tools
- Network requests to PlayFab
- JavaScript errors in WebGL
- Performance profiling
- Local storage inspection

#### PlayFab Dashboard
- Real-time event monitoring
- Player data verification
- Error rate monitoring
- Usage analytics

### Support and Resources


#### External Resources
- [Unity WebGL Documentation](https://docs.unity3d.com/Manual/webgl.html)
- [PlayFab Unity SDK Guide](https://docs.microsoft.com/en-us/gaming/playfab/sdks/unity3d/)
- [Itch.io HTML5 Games Guide](https://itch.io/docs/creators/html5)


## Acknowledgements and Citation Requirements

### 📜 **Code Usage and Attribution**

**⚠️ IMPORTANT NOTICE:** If you use, modify, or distribute any part of this code, you **MUST** provide proper attribution to the original author and project.

#### **Required Citation Format:**

When using this code in academic work, research, or any other project, please cite:

```
Author: Zixuan Wang
Project: Fridge Organization Game - Educational Food Storage Game
GitHub Repository: https://github.com/sharyz119/Fridge2DGame.git
Online Game: https://sharyz119.itch.io/fridge2dgame
Year: 2024-2025
```

#### **For Academic Papers:**
```bibtex
@software{wang2024fridge,
  author = {Zixuan Wang},
  title = {Fridge Organization Game: Educational Food Storage Game},
  year = {2025},
  url = {https://github.com/sharyz119/Fridge2DGame.git},
  note = {Online game available at: https://sharyz119.itch.io/fridge2dgame}
}
```

#### **For Code Comments:**
```csharp
/*
 * Based on Fridge Organization Game by Zixuan Wang
 * Original repository: https://github.com/sharyz119/Fridge2DGame.git
 * Online game: https://sharyz119.itch.io/fridge2dgame
 */
```

#### **For Documentation/README:**
```markdown
## Attribution
This project is based on the Fridge Organization Game by Zixuan Wang.
- GitHub Repository: https://github.com/sharyz119/Fridge2DGame.git
- Online Game: https://sharyz119.itch.io/fridge2dgame
```

### 🎮 **Try the Original Game**

Before modifying or using this code, we encourage you to play the original game to understand its educational purpose and research objectives:

**🔗 Play Online:** [https://sharyz119.itch.io/fridge2dgame](https://sharyz119.itch.io/fridge2dgame)
