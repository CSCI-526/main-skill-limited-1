# Balatro-Style Score Animation System

## Overview
This system separates dice status feedback from score calculation display, featuring Balatro-style animated score breakdowns with smooth number counting and step-by-step calculation reveals.

## New Components

### 1. `ScoreAnimator.cs`
A MonoBehaviour component that handles animated score displays with the following features:
- **Step-by-step calculation reveal**: Shows base score, dice sum, multipliers
- **Smooth number counting**: Numbers count up with easing animation
- **Pulse effects**: Visual emphasis on multipliers
- **Total score tracking**: Accumulates scores across hands
- **Color-coded display**: Different colors for different calculation steps

## Unity Setup Instructions

### Step 1: Create UI Elements

1. **Combo Score Panel** (for animated score breakdown):
   - Create a new `Panel` in your Canvas
   - Add a `TextMeshPro - Text` component
   - Name it: `ComboScoreText`
   - Recommended settings:
     - Font Size: 24-30
     - Alignment: Left/Top
     - Enable Rich Text
     - Vertical Overflow: Overflow
     - Horizontal Overflow: Wrap

2. **Total Score Panel** (for running total):
   - Create another `Panel` in your Canvas
   - Add a `TextMeshPro - Text` component
   - Name it: `TotalScoreText`
   - Recommended settings:
     - Font Size: 36-48
     - Alignment: Center
     - Enable Rich Text
     - Font Style: Bold

3. **Dice Feedback Text** (now only shows dice status):
   - Update your existing `rollFeedbackText` to show only dice status
   - No changes needed - it's automatically handled by the updated code

### Step 2: Add ScoreAnimator Component

1. Create a new GameObject in your scene
2. Name it: `ScoreAnimator`
3. Add the `ScoreAnimator` component to it
4. In the Inspector, assign:
   - **Combo Score Text**: Drag your `ComboScoreText` here
   - **Total Score Text**: Drag your `TotalScoreText` here
5. Adjust animation settings if desired:
   - **Count Duration**: 0.5s (default) - how fast numbers count up
   - **Step Delay**: 0.3s (default) - delay between calculation steps
   - **Multiplier Pulse Scale**: 1.2 (default) - scale factor for emphasis
   - **Highlight Color**: Yellow (default) - color during animation
   - **Normal Color**: White (default) - color when idle

### Step 3: Connect to BattleController

1. Select your `BattleController` GameObject
2. In the Inspector, find the new **Score Display** section
3. Drag your `ScoreAnimator` GameObject to the **Score Animator** field

### Step 4: Layout Recommendations

**Suggested UI Layout:**
```
┌─────────────────────────────────────────┐
│  TOTAL SCORE PANEL (Top Center)        │
│        [Large number display]           │
├─────────────────────────────────────────┤
│                                         │
│  DICE STATUS (Left)    COMBO SCORE     │
│  • Dice rolls          (Right)          │
│  • Lock status         • Breakdown      │
│  • Instructions        • Multipliers    │
│                        • Animation      │
│                                         │
└─────────────────────────────────────────┘
```

## Features

### Animated Score Breakdown
When a combo is submitted, the score calculation is revealed step-by-step:

1. **Combo Name & Dice**: Shows the identified combo and dice values
2. **Base Score**: The base score for the combo type
3. **Dice Sum**: The sum of all dice values added
4. **Combo Multiplier**: Applied with visual pulse effect
5. **Dice Multiplier**: Special dice multipliers (if applicable)
6. **Final Score**: Large, emphasized final result
7. **Total Score Count Up**: Smoothly counts up to new total

### Color Coding
- **Combo Name**: Large, bold (120% size)
- **Base/Sum Labels**: Light green (#88FF88)
- **Intermediate Results**: Orange (#FFAA44)
- **Multipliers**: Pink/Magenta (#FF88FF)
- **Final Score**: Gold (#FFD700, 150% size)
- **Dice Values**: Gray (#AAAAAA)

### Separated Displays

**Dice Feedback Text** now shows ONLY:
- Current roll number
- Dice names and values
- Lock status (shown in gold)
- Instructions/hints

**Combo Score Panel** shows:
- Animated calculation breakdown
- All score components
- Step-by-step reveals

**Total Score Panel** shows:
- Cumulative score across hands
- Animated counting up
- Large, prominent display

## Animation Timing

The complete animation sequence takes approximately **2-2.5 seconds**:
- Each step delay: 0.3s
- Number counting: 0.5s
- Pulse effects: 0.4s total (0.2s up, 0.2s down)
- New hand starts after animation completes

## Customization

### Adjust Animation Speed
In `ScoreAnimator` Inspector:
- **Faster**: Reduce `Count Duration` to 0.3s, `Step Delay` to 0.2s
- **Slower**: Increase `Count Duration` to 0.8s, `Step Delay` to 0.5s

### Change Colors
Modify these in `ScoreAnimator.cs`:
```csharp
public Color highlightColor = Color.yellow;
public Color normalColor = Color.white;
```

Or adjust inline colors in the animation text:
```csharp
// In AnimateScoreCoroutine method
display += $"<color=#YOUR_COLOR>Label:</color> <b>{value}</b>\n";
```

### Disable Animations
If `ScoreAnimator` is not assigned in `BattleController`, the system gracefully falls back to instant score display (debug logs only).

## Testing

1. **Play the scene**
2. **Roll dice** and lock some
3. **Submit combo** - watch the animation:
   - Score breakdown should appear step-by-step
   - Multipliers should pulse
   - Total score should count up smoothly
4. **Play multiple hands** - verify total score accumulates

## Troubleshooting

### No Animation Appears
- Check `ScoreAnimator` is assigned in `BattleController`
- Verify `ComboScoreText` and `TotalScoreText` are assigned in `ScoreAnimator`
- Check UI elements are active and visible

### Animation Too Fast/Slow
- Adjust `Count Duration` and `Step Delay` in `ScoreAnimator` Inspector

### Text Overlaps/Formatting Issues
- Increase panel size
- Enable text overflow
- Adjust font size
- Check Rich Text is enabled

### Total Score Not Updating
- Verify `TotalScoreText` is assigned
- Check for errors in console
- Ensure `ScoreAnimator.ResetTotalScore()` is called on scene start

## Files Modified/Created

**New Files:**
- `Assets/Scripts/Battle/ScoreAnimator.cs`
- `Assets/Scripts/Battle/ScoreAnimator.cs.meta`
- `Doc/SCORE_ANIMATION_SETUP.md`

**Modified Files:**
- `Assets/Scripts/Battle/BattleController.cs`
  - Added `ScoreAnimator` reference
  - Added `_totalScore` tracking
  - Separated feedback display from score calculation
  - Updated `OnSubmitCombo()` to use animated scores
  - Updated `OnRollOnce()` to show only dice status
  - Added `CalculateBaseScore()` helper method

## Design Philosophy

This system follows **Balatro's** core design principles:
- **Clear information hierarchy**: Separate status from scoring
- **Satisfying feedback**: Step-by-step reveals with animations
- **Visual emphasis**: Pulse effects and color coding for important numbers
- **Smooth transitions**: Eased counting animations, not instant jumps
- **Readable breakdown**: Every calculation component is visible

The player always knows:
1. What dice they submitted
2. How the score was calculated
3. What their total score is
4. How much they gained from each hand

