# Tutorial Scene Creation Walkthrough

## Overview
This guide walks you through creating a separate TutorialScene that reuses BattleScene components with tutorial prompts.

---

## Step 1: Create the TutorialScene Unity File

### In Unity Editor:
1. **Right-click** in the Project window under `Assets/Scenes/`
2. Select **Create > Scene**
3. Name it: `TutorialScene`
4. **Save** the scene (Ctrl+S or File > Save)

---

## Step 2: Duplicate BattleScene Setup (Easiest Method)

### Option A: Copy-Paste Method
1. **Open** `BattleScene.unity`
2. **Select All** GameObjects in hierarchy (Ctrl+A)
3. **Copy** (Ctrl+C)
4. **Open** `TutorialScene.unity`
5. **Paste** (Ctrl+V) - This copies all GameObjects
6. **Rename** the `BattleController` GameObject to `TutorialController` (or keep it for now)

### Option B: Manual Setup (If you want minimal setup)
1. Copy essential GameObjects from BattleScene:
   - Canvas (with all UI)
   - Camera
   - EventSystem
   - CooldownSystem (if separate)
   - Any managers/systems

---

## Step 3: Create TutorialController Script

**Location:** `Assets/Scripts/Tutorial/TutorialController.cs`

This will be similar to BattleController but with tutorial-specific logic.

### Key Features:
- Reuses most BattleController code
- Adds tutorial step system
- Shows tutorial prompts
- Progresses through tutorial steps

---

## Step 4: Create Tutorial UI System

### 4.1: Create Tutorial Prompt Panel
1. In **TutorialScene**, under Canvas:
   - Right-click **Canvas** > **UI > Panel**
   - Name it: `TutorialPromptPanel`
   - Set it to cover the screen (Anchor: Stretch-Stretch)
   - Set **Alpha** to 0.8 (semi-transparent overlay)
   - Set **Color** to dark (e.g., black with 80% opacity)

### 4.2: Create Tutorial Text Element
1. Under `TutorialPromptPanel`:
   - Right-click **TutorialPromptPanel** > **UI > TextMeshPro - Text**
   - Name it: `TutorialText`
   - Position: Center of screen
   - Font Size: 24-32
   - Alignment: Center
   - Color: White
   - Word Wrapping: Enabled

### 4.3: Create Continue/Skip Button
1. Under `TutorialPromptPanel`:
   - Right-click **TutorialPromptPanel** > **UI > Button - TextMeshPro**
   - Name it: `ContinueButton`
   - Position: Bottom center
   - Text: "Continue" or "Next"
   - Size: Appropriate (e.g., 200x50)

### 4.4: Create Skip Tutorial Button
1. Under Canvas (not in TutorialPromptPanel):
   - Right-click **Canvas** > **UI > Button - TextMeshPro**
   - Name it: `SkipTutorialButton`
   - Position: Top-right corner
   - Text: "Skip Tutorial"
   - Make it subtle (smaller, less prominent)

---

## Step 5: Minimal Code Changes

### 5.1: Update RunLoader.cs
**File:** `Assets/Scripts/Main Menu and Transition/RunLoader.cs`

**Add this field:**
```csharp
[Header("Scene Names")]
public string mainSceneName = "MainScene";
public string battleSceneName = "BattleScene";
public string tutorialSceneName = "TutorialScene";  // ADD THIS LINE
```

**Add this method:**
```csharp
public void StartTutorial()
{
    StartCoroutine(LoadSceneWithWipe(tutorialSceneName));
}
```

### 5.2: Update MainMenuController.cs
**File:** `Assets/Scripts/Main Menu and Transition/MainMenuController.cs`

**Add this method:**
```csharp
public void OnClickTutorial()
{
    // Stop both dice animations instantly
    if (animatedDice != null)
    {
        foreach (var d in animatedDice)
            if (d != null) d.Pause();
    }

    // Kick off the tutorial
    DiceRogue.Boot.RunLoader.Instance.StartTutorial();
}
```

**In Unity Editor:**
- Add a "Tutorial" button to MainScene
- Assign `OnClickTutorial()` to the button's OnClick event

---

## Step 6: Create TutorialController Script

**Create new file:** `Assets/Scripts/Tutorial/TutorialController.cs`

### Basic Structure:
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;
using DiceGame.Analytics;
using DiceGame.Relics;
using DiceGame.UI;
using UnityEngine.SceneManagement;

namespace DiceGame.Tutorial
{
    public class TutorialController : MonoBehaviour
    {
        // Copy all the same references from BattleController
        [Header("UI")]
        public Transform diceRowParent;
        public GameObject diceViewPrefab;
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;
        public Button continueButton;
        public Button openBackpackButton;
        public TMP_Text rollFeedbackText;
        public TMP_Text handCounterText;
        
        [Header("Tutorial UI")]
        public GameObject tutorialPromptPanel;  // The overlay panel
        public TMP_Text tutorialText;           // Instruction text
        public Button tutorialContinueButton;   // Continue button
        public Button skipTutorialButton;       // Skip button
        
        [Header("Backpack")]
        public BackpackManager backpackManager;
        
        [Header("Score Display")]
        public ScoreAnimator scoreAnimator;
        public TMP_Text targetScoreText;
        
        [Header("Relic Display")]
        public RelicDisplay relicDisplay;
        
        [Header("Config")]
        public int diceCount = 5;
        public int maxRollsPerHand = 2;
        public int baseTargetScore = 300;
        
        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem;
        
        // Tutorial state
        private int currentTutorialStep = 0;
        private bool isTutorialActive = true;
        
        // Core components (same as BattleController)
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceViewFactory _viewFactory;
        private RelicManager _relicManager;
        private ScoreCalculator _scoreCalculator;
        private ProgressionManager _progressionManager;
        private BattleUIPresenter _uiPresenter;
        private HandCompositionService _compositionService;
        
        // Current hand state
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private bool _isSelectionMode = false;
        
        // Tutorial steps
        private readonly List<TutorialStep> tutorialSteps = new();
        
        void Start()
        {
            InitializeTutorial();
            InitializeGameSystems();
            StartTutorialStep(0);
        }
        
        void InitializeTutorial()
        {
            // Hide tutorial UI initially
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            // Set up tutorial steps
            SetupTutorialSteps();
            
            // Set up skip button
            if (skipTutorialButton != null)
                skipTutorialButton.onClick.AddListener(OnSkipTutorial);
        }
        
        void InitializeGameSystems()
        {
            // Copy initialization from BattleController
            // This reuses all the same game logic
            // ... (same as BattleController.Start())
        }
        
        void SetupTutorialSteps()
        {
            tutorialSteps.Add(new TutorialStep
            {
                title = "Welcome to Dice Roguelike!",
                message = "This tutorial will teach you the basics of the game.",
                highlightElement = null, // No specific element to highlight
                waitForAction = false
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Select Your Dice",
                message = "Click on dice from your backpack to build your hand. You need 5 dice per hand.",
                highlightElement = openBackpackButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.OpenBackpack
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Your Dice",
                message = "Once you've selected 5 dice, click the Roll button to roll them.",
                highlightElement = rollButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.Roll
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Select Dice to Keep",
                message = "Click on dice you want to keep. Selected dice will be locked for your final score.",
                highlightElement = null,
                waitForAction = true,
                requiredAction = TutorialAction.SelectDice
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Submit Your Hand",
                message = "Click Submit to lock in your dice and calculate your score.",
                highlightElement = submitComboButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.Submit
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Score Combinations",
                message = "Your score is based on combinations like Three of a Kind, Full House, etc.",
                highlightElement = scoreAnimator?.gameObject,
                waitForAction = false
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "You're ready to play! This tutorial will be marked as completed.",
                highlightElement = null,
                waitForAction = false
            });
        }
        
        void StartTutorialStep(int stepIndex)
        {
            if (stepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            currentTutorialStep = stepIndex;
            var step = tutorialSteps[stepIndex];
            
            // Show tutorial prompt
            ShowTutorialPrompt(step.title, step.message, step.highlightElement);
            
            // If step waits for action, disable other interactions
            if (step.waitForAction)
            {
                DisableNonTutorialInteractions(step.requiredAction);
            }
        }
        
        void ShowTutorialPrompt(string title, string message, GameObject highlightElement)
        {
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(true);
            
            if (tutorialText != null)
                tutorialText.text = $"<b>{title}</b>\n\n{message}";
            
            // Highlight specific element if provided
            if (highlightElement != null)
            {
                // Add highlight effect (e.g., outline, glow, or pointer)
                HighlightElement(highlightElement);
            }
        }
        
        void HighlightElement(GameObject element)
        {
            // Simple highlight: add outline component or change color
            // You can implement this based on your UI system
        }
        
        void DisableNonTutorialInteractions(TutorialAction requiredAction)
        {
            // Disable buttons that aren't part of current step
            // Enable only the button/element needed for current step
        }
        
        void OnTutorialContinue()
        {
            // Hide prompt
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            // Move to next step
            StartTutorialStep(currentTutorialStep + 1);
        }
        
        void OnSkipTutorial()
        {
            CompleteTutorial();
        }
        
        void CompleteTutorial()
        {
            // Mark tutorial as completed
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();
            
            // Hide tutorial UI
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            isTutorialActive = false;
            
            // Return to main menu or start a normal game
            // Option 1: Return to main menu
            DiceRogue.Boot.RunLoader.Instance.StartCoroutine(
                DiceRogue.Boot.RunLoader.Instance.LoadSceneWithWipe("MainScene")
            );
            
            // Option 2: Start a normal game
            // DiceRogue.Boot.RunLoader.Instance.StartRun();
        }
        
        // Hook into game actions to track tutorial progress
        public void OnBackpackOpened()
        {
            if (isTutorialActive && currentTutorialStep == 1) // Step for opening backpack
            {
                OnTutorialContinue();
            }
        }
        
        public void OnRollButtonClicked()
        {
            if (isTutorialActive && currentTutorialStep == 2) // Step for rolling
            {
                OnTutorialContinue();
            }
            // Also call actual roll logic
        }
        
        // ... (other action hooks)
    }
    
    // Tutorial step data structure
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        public string message;
        public GameObject highlightElement;
        public bool waitForAction;
        public TutorialAction requiredAction;
    }
    
    public enum TutorialAction
    {
        None,
        OpenBackpack,
        SelectDice,
        Roll,
        Submit,
        Continue
    }
}
```

---

## Step 7: Connect TutorialController in Unity

1. **Select** the `TutorialController` GameObject in TutorialScene
2. **Assign all references** (same as BattleController):
   - UI elements (buttons, text)
   - CooldownSystem
   - BackpackManager
   - ScoreAnimator
   - RelicDisplay
   - etc.

3. **Assign Tutorial UI references:**
   - Tutorial Prompt Panel → `tutorialPromptPanel`
   - Tutorial Text → `tutorialText`
   - Continue Button → `tutorialContinueButton`
   - Skip Button → `skipTutorialButton`

4. **Connect Continue Button:**
   - In TutorialController Inspector, find `tutorialContinueButton`
   - In Button component, add OnClick event
   - Drag TutorialController to the object field
   - Select `TutorialController.OnTutorialContinue`

---

## Step 8: Add Tutorial Button to MainScene

1. **Open** `MainScene.unity`
2. **Find** the existing "Start" button
3. **Duplicate** it (Ctrl+D)
4. **Rename** to "Tutorial Button"
5. **Change text** to "Tutorial"
6. **Position** it next to Start button
7. **In Button's OnClick:**
   - Add new event
   - Find `MainMenuController` in scene
   - Select `OnClickTutorial()` method

---

## Step 9: Test the Flow

1. **Play** the game from MainScene
2. **Click** "Tutorial" button
3. **Verify** TutorialScene loads
4. **Check** tutorial prompts appear
5. **Test** each step progression
6. **Verify** "Skip Tutorial" works
7. **Check** tutorial completion saves to PlayerPrefs

---

## Step 10: Optional Enhancements

### Highlight System
- Add outline/glow effect to highlighted elements
- Use Unity's Animation system for pulsing effects

### Tutorial Progress Indicator
- Add progress bar showing "Step 2 of 7"
- Add step counter text

### Tutorial Completion Reward
- Show "Tutorial Complete!" screen
- Option to go straight to game

### Save Tutorial State
- Allow players to resume tutorial if they quit mid-way
- Save current step to PlayerPrefs

---

## File Structure Summary

```
Assets/
├── Scenes/
│   ├── MainScene.unity
│   ├── BattleScene.unity
│   └── TutorialScene.unity          ← NEW
├── Scripts/
│   ├── Main Menu and Transition/
│   │   ├── MainMenuController.cs    ← MODIFIED (1 method added)
│   │   └── RunLoader.cs             ← MODIFIED (1 field + 1 method)
│   └── Tutorial/
│       └── TutorialController.cs    ← NEW
```

---

## Changes Summary

**Modified Files:** 2
- `RunLoader.cs` - Added `tutorialSceneName` field and `StartTutorial()` method
- `MainMenuController.cs` - Added `OnClickTutorial()` method

**New Files:** 2
- `TutorialScene.unity` - New scene
- `TutorialController.cs` - New script

**Total Lines Added:** ~50 lines across 2 files

---

## Next Steps

1. Implement the actual tutorial step logic
2. Connect action hooks to game events
3. Polish UI/UX for tutorial prompts
4. Add visual effects (highlighting, animations)
5. Test full tutorial flow

