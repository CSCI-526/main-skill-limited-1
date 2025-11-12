# UI Right Panel Redesign Plan

## Overview
Redesign the right-side information panel in BattleScene to match the scoreboard-style UI shown in the reference image. This plan focuses on features 1-5 (highest priority), with features 6-8 (lower priority) marked for UI-only implementation.

**Design Direction**: Sharp, Minimal, Light Color Style
- All UI elements use sharp corners (no rounded rectangles)
- Clean, minimal design with generous spacing
- Light color palette (light backgrounds, dark text)

---

## 🎨 Design Style Guide: Sharp, Minimal, Light

### Core Principles

1. **Sharp Corners**
   - Use default Unity UI sprites (they have sharp corners)
   - Avoid rounded rectangle sprites
   - Clean, geometric shapes only

2. **Minimal Design**
   - Generous spacing between elements (8-16px)
   - No unnecessary decorations or gradients
   - Clean typography with clear hierarchy
   - Consistent padding (8-12px inside boxes)

3. **Light Color Palette**
   - **Backgrounds**: Light grays and whites (#F5F5F5, #E8E8E8, #E0E0E0)
   - **Text**: Dark grays and black (#2A2A2A, #1A1A1A, #000000)
   - **Borders**: Subtle light gray (#CCCCCC, #999999)
   - **Shadows**: Very subtle, low opacity (#00000020 or #00000010)

### Color Reference

| Element | Color Hex | RGB | Usage |
|---------|-----------|-----|-------|
| Main Panel Background | #F5F5F5 | (245, 245, 245) | Right panel background |
| Box Backgrounds | #E0E0E0 | (224, 224, 224) | All info boxes |
| Primary Text | #2A2A2A | (42, 42, 42) | Main text content |
| Secondary Text | #1A1A1A | (26, 26, 26) | Labels, smaller text |
| Border/Accent | #CCCCCC | (204, 204, 204) | Subtle borders |
| Shadow | #00000020 | (0, 0, 0, 32) | Very subtle depth |

### Typography Tips

- **Font Sizes**: 
  - Headers/Labels: 18-24pt
  - Body Text: 14-16pt
  - Small Text: 12-14pt
- **Font Weight**: Use bold for important numbers/labels
- **Alignment**: Center-align text in boxes for clean look
- **Spacing**: Add padding inside boxes (8-12px all sides)

### Spacing Guidelines

- **Between boxes**: 8-12px vertical spacing
- **Inside boxes**: 8-12px padding on all sides
- **Between side-by-side elements**: 8-12px horizontal spacing
- **Panel margins**: 8-12px from screen edges

### Unity Implementation Tips

1. **Creating Sharp-Cornered Boxes**:
   - Use default Unity UI sprite (UISprite) - it's already sharp
   - Don't use rounded rectangle sprites
   - Set Image Type to "Simple" for solid colors

2. **Adding Subtle Depth**:
   - Use Shadow component with very low opacity (alpha: 32-64)
   - Effect Distance: (1, -1) or (2, -2) for subtle offset
   - Avoid heavy shadows - keep it minimal

3. **Consistent Styling**:
   - Create a prefab for info boxes to ensure consistency
   - Use same colors across all boxes
   - Maintain consistent font sizes and spacing

4. **Layout Management**:
   - Use Vertical Layout Group for main panel (spacing: 8-12px)
   - Use Horizontal Layout Group for side-by-side elements
   - Set "Child Controls Size" to maintain consistent box sizes

### Visual Hierarchy

1. **Level Info** (Top) - Most prominent, larger font
2. **Target Score** - Important, medium font
3. **Total Score** - Important, medium font
4. **Combo Preview** - Dynamic content, medium font
5. **Counters** (Roll/Submit) - Secondary info, smaller font
6. **Money/Buttons** (Bottom) - Secondary, smaller font

---

## Current State Analysis

### Existing UI Elements
1. **`handCounterText`** (TMP_Text) - Currently displays hand counter, will be replaced
2. **`targetScoreText`** (TMP_Text) - Displays target score, already exists
3. **`totalScoreText`** (in ScoreAnimator) - Displays total score, already exists
4. **`rollFeedbackText`** (TMP_Text) - Shows dice status/feedback (not part of right panel)
5. **Combo Dropdown Button** - Exists via `ComboDropdownController`, needs to be moved

### Data Sources Available
- **Level**: `_progressionManager.CurrentLevel`
- **Target Score**: `_progressionManager.TargetScore`
- **Total Score**: `scoreAnimator.GetTotalScore()` or `_progressionManager.TotalScore`
- **Roll Count**: `_handManager.TotalRollsUsed` / `maxRollsPerHand`
- **Submit Count**: Currently not tracked (needs new counter)
- **Combo Preview**: Need to calculate from currently locked dice values

---

## Feature Breakdown

### ✅ Feature 1: Scoreboard-Style Background (Sharp, Minimal, Light)
**Priority**: HIGH  
**Status**: Unity Editor Work Required

**Design Style**: Sharp, Minimal, Light Color Palette
- **Sharp corners**: No rounded rectangles - use straight edges
- **Minimal design**: Clean lines, generous spacing, no unnecessary decoration
- **Light colors**: Light backgrounds with dark text (inverted from typical dark UI)

**Color Palette Recommendations**:
- **Panel Background**: Light gray/white (#F5F5F5, #FFFFFF, or #E8E8E8)
- **Box Backgrounds**: Slightly darker gray (#E0E0E0 or #D5D5D5) for contrast
- **Text**: Dark gray/black (#2A2A2A, #1A1A1A, or #000000)
- **Accents**: Subtle borders using very light gray (#CCCCCC) or medium gray (#999999)
- **Shadows**: Very subtle, light gray shadows (#00000020 or #00000010) for depth

**Requirements**:
- Create a light-colored vertical panel on the right side of the screen
- Sharp corners (no rounding)
- Minimal shadow for subtle depth
- Should span the full height of the screen
- Width: approximately 200-300px (adjust based on content)

**Unity Implementation Steps**:
1. Create a new `GameObject` → `UI` → `Image` named `RightInfoPanel`
2. Set anchor to right edge (Anchor: Right-Stretch)
3. Apply light gray/white color:
   - Recommended: #F5F5F5 (light gray) or #FFFFFF (white)
   - In Unity Color Picker: R: 245, G: 245, B: 245, A: 255
4. Add subtle `Shadow` component for depth:
   - Add Component → UI → Shadow
   - Effect Color: Black with low alpha (#00000020 or R:0, G:0, B:0, A:32)
   - Effect Distance: (2, -2) or (1, -1) for subtle depth
   - Use Spread: 0-2 for soft shadow
5. Optional: Add thin border using `Outline` component:
   - Add Component → UI → Outline
   - Effect Color: Light gray (#CCCCCC or R:204, G:204, B:204)
   - Effect Distance: (1, -1) for thin border
   - Use only if you want defined edges
6. Adjust `RectTransform` size and position:
   - Width: 200-300px
   - Position: Right edge of screen
   - Ensure sharp corners (default Unity UI sprites have sharp corners)

---

### ✅ Feature 2: LevelInfo Box (NEW)
**Priority**: HIGH  
**Status**: New UI Element + Code Integration

**Requirements**:
- Display current level number
- Format: "Level X" or just "X"
- Position: Top of the right panel
- Style: Sharp-cornered light gray box with dark text (minimal design)

**Unity Implementation Steps**:
1. Create `GameObject` → `UI` → `Image` named `LevelInfoBox`
   - Parent: `RightInfoPanel`
   - Style: Sharp-cornered box (default Unity sprite - no rounding)
   - Color: Light gray (#E0E0E0 or R:224, G:224, B:224)
   - Position: Top of panel
   - Add padding: Use RectTransform or add empty child for spacing
2. Create `GameObject` → `UI` → `Text - TextMeshPro` named `LevelInfoText`
   - Parent: `LevelInfoBox`
   - Text: "Level 1" (will be updated by code)
   - Style: Dark gray/black (#2A2A2A or #000000), bold, centered
   - Font size: 18-24pt (adjust for readability)
3. Optional: Add subtle border/shadow:
   - Add `Outline` component to LevelInfoBox Image
   - Color: Very light gray (#CCCCCC)
   - Distance: (1, -1) for thin border

**Code Changes**:
- Add to `BattleController.cs`:
  ```csharp
  [Header("Right Panel UI")]
  public TMP_Text levelInfoText;  // NEW: Level display
  ```
- Create method `UpdateLevelInfo()`:
  ```csharp
  private void UpdateLevelInfo()
  {
      if (levelInfoText != null)
      {
          levelInfoText.text = $"Level {_progressionManager.CurrentLevel}";
      }
  }
  ```
- Call `UpdateLevelInfo()` when:
  - Level changes (`OnContinue()`)
  - Scene starts (`Start()`)
  - Level resets (`ResetForNewHand()`)

---

### ✅ Feature 3: Target Score & Total Score Display
**Priority**: HIGH  
**Status**: Reposition Existing Elements

**Requirements**:
- Target Score: Already exists (`targetScoreText`), needs repositioning
- Total Score: Already exists (`totalScoreText` in ScoreAnimator), needs repositioning
- Position: Below LevelInfo box
- Style: Sharp-cornered light gray boxes with dark text (minimal design)

**Unity Implementation Steps**:
1. **Target Score**:
   - Move `targetScoreText` GameObject to be child of `RightInfoPanel`
   - Reposition below `LevelInfoBox`
   - Wrap in sharp-cornered light gray box container if not already:
     - Create `Image` child with color #E0E0E0
     - Ensure sharp corners (default Unity sprite)
   - Update text color to dark gray/black (#2A2A2A)
   - Update formatting via `BattleUIPresenter.FormatTargetScore()` (already exists)

2. **Total Score**:
   - Move `totalScoreText` GameObject (from ScoreAnimator) to be child of `RightInfoPanel`
   - Reposition below `targetScoreText`
   - Wrap in sharp-cornered light gray box container if not already:
     - Create `Image` child with color #E0E0E0
     - Ensure sharp corners
   - Update text color to dark gray/black (#2A2A2A)
   - Update formatting in `ScoreAnimator.UpdateTotalScore()` if needed

**Code Changes**:
- No code changes needed (elements already exist)
- May need to adjust formatting in `BattleUIPresenter.FormatTargetScore()` for new layout

---

### ✅ Feature 4: Combo Preview Box (NEW)
**Priority**: HIGH  
**Status**: New UI Element + Code Integration

**Requirements**:
- Display combo name for currently locked dice
- Display base score × multiplier (DEFAULT values only, no dice/relic effects)
- Update dynamically when dice are locked/unlocked
- Format: 
  ```
  ComboName
  Base × Multiplier
  ```
- Position: Below Total Score

**Unity Implementation Steps**:
1. Create `GameObject` → `UI` → `Image` named `ComboPreviewBox`
   - Parent: `RightInfoPanel`
   - Style: Sharp-cornered light gray box (#E0E0E0)
   - Position: Below Total Score
   - Add padding for text spacing
2. Create `GameObject` → `UI` → `Text - TextMeshPro` named `ComboNameText`
   - Parent: `ComboPreviewBox`
   - Text: "No Combo" (default)
   - Style: Dark gray/black (#2A2A2A), centered, bold
   - Font size: 16-18pt
3. Create `GameObject` → `UI` → `Text - TextMeshPro` named `ComboBaseText`
   - Parent: `ComboPreviewBox`
   - Text: "Base: 0"
   - Style: Dark gray (#2A2A2A), smaller font (12-14pt)
   - Position: Left side or centered
4. Create `GameObject` → `UI` → `Text - TextMeshPro` named `ComboMultiplierText`
   - Parent: `ComboPreviewBox`
   - Text: "×1.0"
   - Style: Dark gray (#2A2A2A), smaller font (12-14pt)
   - Position: Right side or centered
5. Add visual separator (× symbol) between Base and Multiplier:
   - Use a simple "×" text element or layout spacing
   - Style: Dark gray (#2A2A2A), medium size

**Code Changes**:
- Add to `BattleController.cs`:
  ```csharp
  [Header("Right Panel UI")]
  public TMP_Text comboNameText;      // NEW: Combo name preview
  public TMP_Text comboBaseText;     // NEW: Base score preview
  public TMP_Text comboMultiplierText; // NEW: Multiplier preview
  ```
- Create method `UpdateComboPreview()`:
  ```csharp
  private void UpdateComboPreview()
  {
      // Get locked dice values
      var lockedValues = _dice
          .Where(d => d.isLocked && d.lastRollValue > 0 && d.tier != DiceTier.Filler)
          .Select(d => d.lastRollValue)
          .ToList();
      
      if (lockedValues.Count == 0)
      {
          // No dice locked
          if (comboNameText != null) comboNameText.text = "No Combo";
          if (comboBaseText != null) comboBaseText.text = "Base: 0";
          if (comboMultiplierText != null) comboMultiplierText.text = "×1.0";
          return;
      }
      
      // Calculate combo using ScoreCalculator's EvaluateCombo method
      // Need to extract EvaluateCombo as public or create a preview method
      string comboName;
      int baseScore;
      float multiplier;
      
      // Use ScoreCalculator to evaluate combo (default values only)
      var tempResult = _scoreCalculator.CalculateScore(
          _dice.Where(d => d.isLocked && d.lastRollValue > 0).ToList(),
          lockedValues,
          null,  // No relics for preview
          null   // No context for preview
      );
      
      comboName = tempResult.comboName;
      baseScore = tempResult.comboBaseScore;
      multiplier = tempResult.comboMultiplier;
      
      // Update UI
      if (comboNameText != null) comboNameText.text = comboName;
      if (comboBaseText != null) comboBaseText.text = $"Base: {baseScore}";
      if (comboMultiplierText != null) comboMultiplierText.text = $"×{multiplier:F1}";
  }
  ```
- Call `UpdateComboPreview()` when:
  - Dice are locked/unlocked (in `DiceView.ToggleLock()` or via event)
  - After rolling (`OnRollOnce()`)
  - After dice selection (`OnDiceSelectedFromBackpack()`)

**Note**: May need to make `ScoreCalculator.EvaluateCombo()` public or create a preview-only evaluation method.

---

### ✅ Feature 5: Roll Count & Submit Count (Replace Hand Counter)
**Priority**: HIGH  
**Status**: New UI Elements + Code Integration

**Requirements**:
- Replace current `handCounterText` with two separate boxes:
  - **Roll Count**: "Roll cnt: X/Y" (X = used, Y = max)
  - **Submit Count**: "Submit cnt: X" (X = number of submissions)
- Position: Below Combo Preview
- Style: Two sharp-cornered light gray boxes side-by-side (minimal design)

**Unity Implementation Steps**:
1. **Remove/Repurpose `handCounterText`**:
   - Option A: Remove from scene (if not needed elsewhere)
   - Option B: Keep but hide (for backward compatibility)

2. **Create Roll Count Box**:
   - Create `GameObject` → `UI` → `Image` named `RollCountBox`
     - Parent: `RightInfoPanel`
     - Style: Sharp-cornered light gray box (#E0E0E0)
     - Size: Approximately half panel width minus spacing
   - Create `GameObject` → `UI` → `Text - TextMeshPro` named `RollCountText`
     - Parent: `RollCountBox`
     - Text: "Roll cnt: 0/5"
     - Style: Dark gray/black (#2A2A2A), centered
     - Font size: 14-16pt

3. **Create Submit Count Box**:
   - Create `GameObject` → `UI` → `Image` named `SubmitCountBox`
     - Parent: `RightInfoPanel`
     - Style: Sharp-cornered light gray box (#E0E0E0)
     - Size: Approximately half panel width minus spacing
   - Create `GameObject` → `UI` → `Text - TextMeshPro` named `SubmitCountText`
     - Parent: `SubmitCountBox`
     - Text: "Submit cnt: 0"
     - Style: Dark gray/black (#2A2A2A), centered
     - Font size: 14-16pt

4. **Layout**: Use `Horizontal Layout Group` or manual positioning:
   - Create parent `GameObject` → `UI` → `Image` (or empty) named `CountersContainer`
   - Parent: `RightInfoPanel`
   - Add `Horizontal Layout Group` component:
     - Spacing: 8-12px between boxes
     - Child Alignment: Middle Center
     - Child Controls Size: Width and Height
   - Make `RollCountBox` and `SubmitCountBox` children of `CountersContainer`

**Code Changes**:
- Add to `BattleController.cs`:
  ```csharp
  [Header("Right Panel UI")]
  public TMP_Text rollCountText;     // NEW: Roll counter
  public TMP_Text submitCountText;   // NEW: Submit counter
  
  private int _submitCount = 0;      // NEW: Track submit count
  ```
- Update `UpdateHandCounter()` method:
  ```csharp
  private void UpdateRollAndSubmitCount()
  {
      // Update roll count
      if (rollCountText != null)
      {
          rollCountText.text = $"Roll cnt: {_handManager.TotalRollsUsed}/{maxRollsPerHand}";
      }
      
      // Update submit count
      if (submitCountText != null)
      {
          submitCountText.text = $"Submit cnt: {_submitCount}";
      }
  }
  ```
- Increment `_submitCount` in `OnSubmitCombo()`:
  ```csharp
  void OnSubmitCombo()
  {
      // ... existing code ...
      _submitCount++;  // NEW: Increment submit count
      UpdateRollAndSubmitCount();  // NEW: Update display
      // ... rest of existing code ...
  }
  ```
- Call `UpdateRollAndSubmitCount()` when:
  - After rolling (`OnRollOnce()`)
  - After submitting (`OnSubmitCombo()`)
  - On hand reset (`ResetForNewHand()` - reset submit count?)
  - On level start (`OnContinue()` - reset submit count?)

**Note**: Decide if submit count should reset per hand, per level, or persist for entire battle.

---

### ⚠️ Feature 6: Money Display Box (UI Only)
**Priority**: LOW (Skip for now, UI placeholder only)

**Requirements**:
- Display player's money
- Position: Below Roll/Submit counters
- Style: Sharp-cornered light gray box with dark text (minimal design)
- **No functionality** - just display "Money: 0" or placeholder

**Unity Implementation Steps**:
1. Create `GameObject` → `UI` → `Image` named `MoneyBox`
   - Parent: `RightInfoPanel`
   - Style: Sharp-cornered light gray box (#E0E0E0)
2. Create `GameObject` → `UI` → `Text - TextMeshPro` named `MoneyText`
   - Parent: `MoneyBox`
   - Text: "Money: 0"
   - Style: Dark gray/black (#2A2A2A), centered
   - Font size: 14-16pt

**Code Changes**:
- None (placeholder only)

---

### ⚠️ Feature 7: Move Combo Preference Button
**Priority**: LOW (Skip for now, just reposition)

**Requirements**:
- Move existing combo preference button to right panel
- Position: Below Money box (or at bottom if Money skipped)
- Style: Sharp-cornered light gray button with dark text (minimal design)

**Unity Implementation Steps**:
1. Find existing combo preference button in scene
2. Move to be child of `RightInfoPanel`
3. Reposition to bottom of panel
4. Update button styling:
   - Button Image color: Light gray (#E0E0E0)
   - Button Text color: Dark gray/black (#2A2A2A)
   - Ensure sharp corners (default Unity sprite)
   - Optional: Add subtle border/shadow for definition
5. No code changes needed (button functionality remains the same)

**Code Changes**:
- None (just reposition in Unity Editor)

---

### ⚠️ Feature 8: Setting Panel (UI Only)
**Priority**: LOW (Skip for now, UI placeholder only)

**Requirements**:
- Create a settings button
- Position: Bottom of right panel
- Style: Sharp-cornered light gray button with dark text (minimal design)
- **No functionality** - just a button placeholder

**Unity Implementation Steps**:
1. Create `GameObject` → `UI` → `Button` named `SettingButton`
   - Parent: `RightInfoPanel`
   - Style: Sharp-cornered light gray box (#E0E0E0)
   - Button Text: "Setting"
   - Text color: Dark gray/black (#2A2A2A)
   - Font size: 14-16pt
   - Position: Bottom of panel
   - Ensure sharp corners (default Unity sprite)

**Code Changes**:
- None (placeholder only)

---

## Implementation Order

### Phase 1: Core Structure (Features 1-3)
1. ✅ Create right panel background (Feature 1)
2. ✅ Create LevelInfo box (Feature 2)
3. ✅ Reposition Target Score & Total Score (Feature 3)

### Phase 2: Dynamic Content (Features 4-5)
4. ✅ Create Combo Preview box (Feature 4)
5. ✅ Create Roll/Submit Count boxes (Feature 5)

### Phase 3: Placeholders (Features 6-8) - Optional
6. ⚠️ Create Money box (Feature 6)
7. ⚠️ Move Combo Preference button (Feature 7)
8. ⚠️ Create Setting button (Feature 8)

---

## Code Integration Checklist

### New Fields in BattleController.cs
- [ ] `public TMP_Text levelInfoText;`
- [ ] `public TMP_Text comboNameText;`
- [ ] `public TMP_Text comboBaseText;`
- [ ] `public TMP_Text comboMultiplierText;`
- [ ] `public TMP_Text rollCountText;`
- [ ] `public TMP_Text submitCountText;`
- [ ] `private int _submitCount = 0;`

### New Methods in BattleController.cs
- [ ] `UpdateLevelInfo()`
- [ ] `UpdateComboPreview()`
- [ ] `UpdateRollAndSubmitCount()` (replaces `UpdateHandCounter()`)

### Modified Methods in BattleController.cs
- [ ] `Start()` - Initialize new UI elements
- [ ] `OnRollOnce()` - Call `UpdateComboPreview()` and `UpdateRollAndSubmitCount()`
- [ ] `OnSubmitCombo()` - Increment `_submitCount`, call `UpdateRollAndSubmitCount()`
- [ ] `OnDiceSelectedFromBackpack()` - Call `UpdateComboPreview()`
- [ ] `OnContinue()` - Call `UpdateLevelInfo()`, reset `_submitCount`
- [ ] `ResetForNewHand()` - Call `UpdateLevelInfo()`, optionally reset `_submitCount`

### ScoreCalculator.cs Changes
- [ ] Make `EvaluateCombo()` public OR create `PreviewCombo()` method for UI preview

### DiceView.cs Changes (if needed)
- [ ] Add event/callback when dice is locked/unlocked to trigger `UpdateComboPreview()`

---

## Unity Editor Work Checklist

### Scene Setup
- [ ] Create `RightInfoPanel` GameObject with dark gray background
- [ ] Set anchor to right edge, full height
- [ ] Add shadow/outline for pseudo-3D effect

### LevelInfo (Feature 2)
- [ ] Create `LevelInfoBox` Image
- [ ] Create `LevelInfoText` TMP_Text
- [ ] Position at top of panel

### Target/Total Score (Feature 3)
- [ ] Move `targetScoreText` to `RightInfoPanel`
- [ ] Move `totalScoreText` (from ScoreAnimator) to `RightInfoPanel`
- [ ] Reposition below LevelInfo

### Combo Preview (Feature 4)
- [ ] Create `ComboPreviewBox` Image
- [ ] Create `ComboNameText` TMP_Text
- [ ] Create `ComboBaseText` TMP_Text
- [ ] Create `ComboMultiplierText` TMP_Text
- [ ] Add × symbol separator
- [ ] Position below Total Score

### Roll/Submit Count (Feature 5)
- [ ] Create `RollCountBox` Image
- [ ] Create `RollCountText` TMP_Text
- [ ] Create `SubmitCountBox` Image
- [ ] Create `SubmitCountText` TMP_Text
- [ ] Position side-by-side below Combo Preview

### Optional Placeholders
- [ ] Create `MoneyBox` and `MoneyText` (Feature 6)
- [ ] Move combo preference button (Feature 7)
- [ ] Create `SettingButton` (Feature 8)

### Inspector Assignments
- [ ] Assign all new TMP_Text references in BattleController
- [ ] Assign `totalScoreText` reference in ScoreAnimator (if moved)
- [ ] Test all UI elements are properly linked

---

## Testing Checklist

- [ ] Level number updates correctly on level change
- [ ] Target score displays correctly
- [ ] Total score updates after combo submission
- [ ] Combo preview updates when dice are locked/unlocked
- [ ] Combo preview shows correct base × multiplier (default values only)
- [ ] Roll count updates after each roll
- [ ] Submit count increments after each submission
- [ ] All UI elements are visible and properly positioned
- [ ] Right panel background has pseudo-3D effect
- [ ] UI scales correctly on different screen sizes

---

## Notes & Considerations

1. **Combo Preview Calculation**: The preview should show DEFAULT combo values only (no dice/relic effects). This may require creating a separate preview method in `ScoreCalculator` that skips dice/relic multipliers.

2. **Submit Count Reset**: Decide when to reset submit count:
   - Per hand? (Reset when new hand starts)
   - Per level? (Reset when level advances)
   - Per battle? (Never reset, cumulative)

3. **Dice Lock Events**: Need a way to detect when dice are locked/unlocked to update combo preview. Options:
   - Add event to `DiceView.ToggleLock()`
   - Poll dice state in `UpdateComboPreview()` (called periodically)
   - Call `UpdateComboPreview()` after each roll

4. **UI Layout**: Consider using Unity's Layout Groups for automatic spacing:
   - `Vertical Layout Group` for main panel
   - `Horizontal Layout Group` for Roll/Submit counters

5. **Performance**: Combo preview calculation should be lightweight. Consider caching results if calculation becomes expensive.

6. **Backward Compatibility**: Keep `handCounterText` reference but hide it, or remove entirely if not needed elsewhere.

---

## Next Steps

1. **Review this plan** with team
2. **Start with Phase 1** (Features 1-3) - Core structure
3. **Test Phase 1** before moving to Phase 2
4. **Implement Phase 2** (Features 4-5) - Dynamic content
5. **Test Phase 2** thoroughly
6. **Optionally implement Phase 3** (Features 6-8) - Placeholders

---

## Questions for Discussion

1. Should submit count reset per hand, per level, or persist?
2. Should combo preview update in real-time as dice are locked, or only after rolling?
3. What exact styling/colors should be used for the pseudo-3D effect?
4. Should the right panel be collapsible/hideable?
5. What screen resolution/size should we optimize for?

