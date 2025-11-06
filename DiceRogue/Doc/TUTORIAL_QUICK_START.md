# Tutorial Scene - Quick Start Guide

## ✅ What's Been Done

### Code Changes (Minimal)
1. **RunLoader.cs** - Added `tutorialSceneName` field and `StartTutorial()` method
2. **MainMenuController.cs** - Added `OnClickTutorial()` method
3. **TutorialController.cs** - New tutorial controller script (template)

### Files Created
- `Assets/Scripts/Tutorial/TutorialController.cs` - Main tutorial controller
- `Doc/TUTORIAL_SCENE_WALKTHROUGH.md` - Detailed walkthrough
- `Doc/TUTORIAL_QUICK_START.md` - This file

---

## 🎯 Next Steps in Unity Editor

### Step 1: Create TutorialScene
1. Right-click in `Assets/Scenes/` → **Create > Scene**
2. Name it: `TutorialScene`
3. Save it

### Step 2: Copy BattleScene Setup
1. Open `BattleScene.unity`
2. Select All GameObjects (Ctrl+A)
3. Copy (Ctrl+C)
4. Open `TutorialScene.unity`
5. Paste (Ctrl+V)
6. Rename `BattleController` GameObject to `TutorialController` (optional)

### Step 3: Add TutorialController Component
1. Select `TutorialController` GameObject (or create new empty GameObject)
2. Add Component → `TutorialController` script
3. Assign all references (same as BattleController):
   - UI elements (buttons, text)
   - CooldownSystem
   - BackpackManager
   - ScoreAnimator
   - RelicDisplay
   - etc.

### Step 4: Create Tutorial UI
1. Under Canvas, create:
   - **Panel** → Name: `TutorialPromptPanel` (semi-transparent overlay)
   - **TextMeshPro Text** → Name: `TutorialText` (instruction text)
   - **Button** → Name: `ContinueButton` (for continuing steps)
   - **Button** → Name: `SkipTutorialButton` (top-right corner)

2. Assign to TutorialController:
   - Tutorial Prompt Panel → `tutorialPromptPanel`
   - Tutorial Text → `tutorialText`
   - Continue Button → `tutorialContinueButton`
   - Skip Button → `skipTutorialButton`

3. Connect Continue Button:
   - Select `ContinueButton`
   - In Button component, add OnClick event
   - Drag `TutorialController` → Select `OnTutorialContinue()`

### Step 5: Add Tutorial Button to MainScene
1. Open `MainScene.unity`
2. Find existing "Start" button
3. Duplicate it (Ctrl+D)
4. Rename to "Tutorial Button"
5. Change text to "Tutorial"
6. In Button's OnClick:
   - Add event → Find `MainMenuController` → Select `OnClickTutorial()`

### Step 6: Complete TutorialController Implementation
The `TutorialController.cs` is a template. You need to:
1. Copy initialization logic from `BattleController.Start()`
2. Implement action hooks (connect to your game events)
3. Add visual highlighting system
4. Test each tutorial step

---

## 📋 Checklist

- [ ] TutorialScene created
- [ ] BattleScene setup copied to TutorialScene
- [ ] TutorialController component added and configured
- [ ] Tutorial UI created (Panel, Text, Buttons)
- [ ] UI references assigned in TutorialController
- [ ] Tutorial button added to MainScene
- [ ] TutorialController initialization logic copied from BattleController
- [ ] Action hooks connected to game events
- [ ] Test tutorial flow end-to-end

---

## 🔧 Key Implementation Notes

### TutorialController Structure
- Reuses BattleController game logic
- Adds tutorial step system on top
- Shows prompts at each step
- Tracks tutorial completion via PlayerPrefs

### Tutorial Steps
Currently defined steps:
1. Welcome message
2. Select dice (open backpack)
3. Roll dice
4. Select dice to keep
5. Submit hand
6. Score explanation
7. Completion message

### Action Hooks
You need to call these from your game actions:
- `OnBackpackOpened()` - When backpack opens
- `OnRollButtonClicked()` - When roll button clicked
- `OnDiceSelected()` - When dice selected
- `OnHandSubmitted()` - When hand submitted

### PlayerPrefs
Tutorial completion is saved as:
- Key: `"HasCompletedTutorial"`
- Value: `1` (completed) or `0` (not completed)

You can check this in MainScene to show/hide tutorial button:
```csharp
bool hasCompleted = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
```

---

## 📚 Documentation

- **Full Walkthrough**: See `TUTORIAL_SCENE_WALKTHROUGH.md` for detailed steps
- **Code Reference**: See `TutorialController.cs` for implementation details

---

## 🎮 Testing

1. Start game from MainScene
2. Click "Tutorial" button
3. Verify TutorialScene loads
4. Check tutorial prompts appear
5. Test each step progression
6. Verify "Skip Tutorial" works
7. Check tutorial completion saves

---

## 💡 Tips

- Start simple: Get basic tutorial flow working first
- Add polish later: Highlighting, animations, etc.
- Test frequently: Make sure each step works before moving to next
- Keep BattleController unchanged: All game logic should work the same

