# Deck Status Display System

## Overview
A live display showing all dice in your deck/pool with their current status, organized by tier and color-coded for easy reading.

## Features

### Visual Organization
Dice are grouped by tier:
- **★ LEGENDARY ★** (Gold color)
- **◆ RARE ◆** (Purple color)
- **● COMMON ●** (Green color)

### Status Indicators
Each dice shows:
- **Icon**: Visual indicator of status
- **Name**: Dice name
- **Status**: Current state with color coding

### Three States

#### 1. ✓ AVAILABLE (Green)
- Dice is ready to be selected for a hand
- Not currently in use
- No cooldown remaining

#### 2. ▶ SELECTED (Gold)
- Dice is in the current hand
- Being used right now
- Cannot be selected again until hand completes

#### 3. ⏱ COOLDOWN (X) (Red)
- Dice was used and is on cooldown
- Number shows remaining turns
- Will become available after cooldown expires

## Display Example

```
DICE DECK STATUS

★ LEGENDARY ★
  ▶ Golden Dice [SELECTED]
  ⏱ Zombie Dice [COOLDOWN (1)]

◆ RARE ◆
  ✓ High Roller [AVAILABLE]
  ▶ Twin Bond [SELECTED]
  ⏱ Lucky Dice [COOLDOWN (2)]

● COMMON ●
  ✓ Plus One [AVAILABLE]
  ✓ Standard D6 [AVAILABLE]
  ▶ Consistent Dice [SELECTED]

━━━━━━━━━━━━━━━━━━━━━━
Available: 3
Selected: 3
On Cooldown: 2
```

## UI Setup

### 1. Create Text Element
In Unity:
1. Create a new **TextMeshPro - Text** UI element in your Canvas
2. Name it: `DeckStatusText`
3. Configure settings:
   - **Font Size**: 20-24
   - **Alignment**: Left/Top
   - **Enable Rich Text**: ✓ (IMPORTANT!)
   - **Overflow**: Overflow (vertical)
   - **Word Wrapping**: Enabled
   - **Color**: White

### 2. Assign to BattleController
1. Select your `BattleController` GameObject
2. In the Inspector, find the **UI** section
3. Drag `DeckStatusText` to the **Deck Status Text** field

### 3. Layout Recommendation

**Suggested placement:**
```
┌─────────────────────────────────────────┐
│  Total Score (Top Center)               │
├──────────────┬──────────────────────────┤
│              │                          │
│  DECK STATUS │   Dice Feedback         │
│  (Left Side) │   (Center)              │
│              │                          │
│  • Legendary │   • Current roll        │
│  • Rare      │   • Lock status         │
│  • Common    │                          │
│              ├──────────────────────────┤
│  Summary:    │   Combo Score           │
│  • Available │   (Right)               │
│  • Selected  │   • Breakdown           │
│  • Cooldown  │   • Multipliers         │
└──────────────┴──────────────────────────┘
```

## Update Timing

The deck status automatically updates when:
1. **Starting a new hand** - Shows newly selected dice
2. **After rolling** - Maintains current selection
3. **After submitting** - Shows dice entering cooldown
4. **Cooldown advances** - Shows updated cooldown numbers
5. **Pool refreshes** - Shows all dice available again

## Color Scheme

### Tier Colors
- **Legendary**: `#FFD700` (Gold)
- **Rare**: `#9370DB` (Medium Purple)
- **Common**: `#90EE90` (Light Green)

### Status Colors
- **Available**: `#88FF88` (Light Green)
- **Selected**: `#FFD700` (Gold)
- **Cooldown**: `#FF6666` (Soft Red)
- **Separator**: `#AAAAAA` (Gray)

## Implementation Details

### Method: `UpdateDeckStatus()`
- Located in: `BattleController.cs` (lines 436-494)
- Called automatically at key game moments
- Builds formatted text with Rich Text markup
- Queries cooldown system for dice states

### Helper: `AppendDiceStatus()`
- Located in: `BattleController.cs` (lines 499-525)
- Formats individual dice status lines
- Determines icon and color based on state
- Creates consistent formatting

### Data Flow
```
CooldownSystem.GetAllDice()
    ↓
Group by DiceTier
    ↓
Check each dice state:
  - In _dice list? → SELECTED
  - cooldownRemain > 0? → COOLDOWN (X)
  - Otherwise → AVAILABLE
    ↓
Format with colors and icons
    ↓
Display in deckStatusText
```

## Benefits

### For Players
- 🔍 **At-a-glance overview**: See entire deck status instantly
- 📊 **Strategic planning**: Know what's available for next hand
- ⏱️ **Cooldown tracking**: See when dice will be available again
- 🎯 **Clear organization**: Tier-based grouping for easy scanning

### For Developers
- 🔄 **Automatic updates**: No manual refresh needed
- 🎨 **Highly customizable**: Easy to modify colors and format
- 📝 **Rich information**: Shows everything about deck state
- 🐛 **Debug friendly**: Clear visualization of system state

## Customization

### Change Colors
Edit the color hex codes in `UpdateDeckStatus()` or `AppendDiceStatus()`:
```csharp
// Tier headers
sb.AppendLine("<color=#FFD700><b>★ LEGENDARY ★</b></color>");

// Status colors
statusColor = "#88FF88"; // Available (Green)
statusColor = "#FFD700"; // Selected (Gold)
statusColor = "#FF6666"; // Cooldown (Red)
```

### Change Icons
Modify the icon characters in `AppendDiceStatus()`:
```csharp
statusIcon = "✓";  // Available
statusIcon = "▶";  // Selected
statusIcon = "⏱";  // Cooldown
```

### Change Format
Adjust the formatting in `UpdateDeckStatus()`:
```csharp
// Current format:
sb.AppendLine($"  <color={statusColor}>{statusIcon}</color> <b>{diceName}</b> <color={statusColor}>[{statusText}]</color>");

// Alternative compact format:
sb.Append($"{statusIcon}{diceName} ");

// Alternative verbose format:
sb.AppendLine($"{statusIcon} {diceName} - {statusText} (Cost: {dice.cost})");
```

## Testing

1. **Initial State**: All dice show as AVAILABLE
2. **After Hand Start**: Selected dice show as SELECTED
3. **After Submit**: Used dice show COOLDOWN (2)
4. **After Next Hand**: Cooldown numbers decrease
5. **After Pool Refresh**: All dice return to AVAILABLE

## Files Modified

- **BattleController.cs**
  - Added `deckStatusText` field
  - Added `UpdateDeckStatus()` method
  - Added `AppendDiceStatus()` helper
  - Added calls to update deck status at appropriate times
  - Updated event handlers to refresh deck display

## Troubleshooting

### Deck Status Not Showing
- Check `deckStatusText` is assigned in Inspector
- Verify TextMeshPro element exists in Canvas
- Check "Rich Text" is enabled on TMP component

### Colors Not Displaying
- Ensure "Rich Text" is enabled (most common issue!)
- Check TMP component is not overriding with material color
- Verify color tags use proper format: `<color=#RRGGBB>`

### Icons Not Showing
- Use standard Unicode characters (✓, ▶, ⏱)
- Ensure font supports these characters
- Try different font if icons appear as boxes

### Layout Issues
- Adjust panel size to fit content
- Enable vertical overflow on TMP
- Set appropriate font size (20-24 recommended)
- Check word wrapping is enabled

## Future Enhancements

Possible improvements:
1. Click dice in deck to view details
2. Animate status changes (fade/pulse)
3. Sort options (by name, tier, status)
4. Collapsible tier sections
5. Search/filter functionality
6. Show dice effects/abilities
7. Deck statistics (usage rates, etc.)
8. Comparison view (before/after hand)

