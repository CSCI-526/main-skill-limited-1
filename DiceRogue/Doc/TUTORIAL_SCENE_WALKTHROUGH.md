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

### 4.3: Create "Next" Button
1. Under `TutorialPromptPanel`:
   - Right-click **TutorialPromptPanel** > **UI > Button - TextMeshPro**
   - Name it: `ContinueButton`
   - Position: Bottom center (anchor to lower middle)
   - Text: "Next"
   - Size: Around 200x60 works well

### 4.4: (Optional) Skip Tutorial Button
If you want to let players skip the tutorial later, you can add a button for it. The default script hides it, so feel free to skip this for now.
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

## Step 6: Hook Up `TutorialController`

The script at `Assets/Scripts/Tutorial/TutorialController.cs` is already action-driven. You only need to wire the references in the TutorialScene Inspector—no code edits required.

### Inspector fields to assign
- **Backpack References**: `backpackManager`, `diceSelectionUI`
- **Dice / Score**: `diceRowParent` (the hand container) and `scoreAnimator`
- **Gameplay Buttons**: `openBackpackButton`, `rollButton`, `submitComboButton`
- **Prompt UI**: `tutorialPromptPanel`, `tutorialText`, `tutorialContinueButton`
  - `skipTutorialButton` can stay empty (it is hidden by default)

Everything else is optional. The controller will automatically fall back to the components already wired on `BattleController` if any of the above are left blank.

### Layout behaviour
- Intro/outro steps keep the prompt in the centre, show the `Next` button on the right side of the text, and use the `introPrompt*` settings.
- Action steps pin the prompt under the Combo Preference area on the left, stay visible while the player performs the action, and use the `actionPrompt*` settings (anchor/pivot/offset/size) for fine tuning.
- You can tweak sizes, offsets, and anchors via the exposed fields on `TutorialController` if you want to move the prompts elsewhere.

### Flow summary
1. **Intro** – prompt in the centre; click `Next` to begin.
2. **Build Hand / Roll / Lock / Submit** – prompt stays on-screen (right side) until each action is completed.
3. **Score Breakdown** – waits for the score animation to finish before continuing.
4. **Tutorial Complete** – final `Next` calls `RunLoader` to load `BattleScene` so the real run begins immediately.

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
   - (Optional) Skip Button → `skipTutorialButton`

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

