# Normal Dice Filler System

## Overview
The battle system now automatically fills empty hand slots with Normal Dice when there aren't enough special dice available from the cooldown system, ensuring players always have exactly 5 dice to play with.

## Problem Solved
Previously, if only 2-3 special dice were available from the cooldown pool, the hand would only have 2-3 dice, making it harder to form combos. Now, the system ensures all 5 slots are always filled.

## Implementation

### 1. Normal Dice as Fillers
**File:** `Assets/Scripts/Dice/NormalDice.cs`

Normal Dice properties:
- **Tier**: `DiceTier.Common` (changed from Filler to make them rollable)
- **Cost**: `0` (free dice, don't consume hand budget)
- **Cooldown**: `0` (temporary dice, no cooldown needed)
- **Faces**: Standard 1-6 values

### 2. Automatic Slot Filling
**File:** `Assets/Scripts/Battle/BattleController.cs` - `StartNewHand()` method

The hand selection process now:
1. Selects special dice from the cooldown pool (up to 5)
2. Calculates remaining slots: `normalDiceNeeded = 5 - selectedDice.Count`
3. Creates Normal Dice instances to fill remaining slots
4. Always ensures exactly 5 dice in every hand

Example:
```csharp
// If only 2 special dice are available:
_dice.AddRange(selectedDice);  // Add 2 special dice

int normalDiceNeeded = diceCount - _dice.Count;  // 5 - 2 = 3
for (int i = 0; i < normalDiceNeeded; i++)
{
    var normalDice = new NormalDice();
    normalDice.diceName = $"Normal Dice #{i + 1}";
    _dice.Add(normalDice);
}
// Result: Hand has 5 dice total (2 special + 3 normal)
```

### 3. Normal Dice Filtering on Submit
**File:** `Assets/Scripts/Battle/BattleController.cs` - `OnSubmitCombo()` method

When submitting combos:
- All locked dice (including Normal Dice) contribute to the combo score
- Only special dice are passed to the cooldown system
- Normal Dice are filtered out before cooldown application

```csharp
var specialDiceOnly = submittedDice.Where(d => !(d is NormalDice)).ToList();
cooldownSystem.CompleteHand(specialDiceOnly);
```

This prevents temporary Normal Dice from being added to the cooldown pool.

## UI Feedback

The feedback text now shows the composition:
```
Ready! 5 dice prepared.
(2 special + 3 normal dice)

Instructions:
  • Roll the dice
  • Click to lock dice you want to keep
  • Submit when ready
```

## Debug Logging

Enhanced logging shows the dice selection process:
```
[BattleController] Selected 2 special dice from pool:
  Selected: Lucky Dice
  Selected: High Roller
[BattleController] Filling 3 remaining slots with Normal Dice
  Added: Normal Dice #1
  Added: Normal Dice #2
  Added: Normal Dice #3
[BattleController] Final hand composition: 5 dice total (2 special + 3 normal)
```

## Gameplay Impact

### Benefits:
1. **Consistent Gameplay**: Every hand has exactly 5 dice
2. **Better Combos**: More dice = more combo opportunities
3. **Fair Distribution**: Players can still form good combos even when many special dice are on cooldown
4. **Smooth Difficulty Curve**: Early rounds feel better when few special dice are available

### Strategy:
- Normal Dice provide baseline combo potential
- Special dice add unique effects and multipliers
- Players must balance using special dice vs saving them for later hands
- Even with all special dice on cooldown, players can still submit combos with normal dice

## Technical Details

### Dice Tier System
- **Filler**: Used for placeholder/empty slots only (not rollable)
- **Common**: Basic playable dice (Normal Dice, standard special dice)
- **Rare**: Higher-tier special dice
- **Legendary**: Top-tier special dice

### Normal Dice Behavior
- ✅ **Rollable**: Will be rolled like any other dice
- ✅ **Lockable**: Players can lock them for combos
- ✅ **Submittable**: Count toward combo scoring
- ❌ **Not in Pool**: Don't enter cooldown system
- ❌ **Temporary**: Discarded after hand completes

### Key Checks in Code
```csharp
// Skip only actual placeholder/empty slots
if (d.tier != DiceTier.Filler)  // Normal Dice (Common tier) pass this check

// Filter out temporary fillers when submitting to cooldown
if (!(d is NormalDice))  // Only special dice pass this check
```

## Files Modified

1. **NormalDice.cs**
   - Changed tier from `DiceTier.Filler` to `DiceTier.Common`
   - Changed cooldown from 1 to 0
   - Added clarifying comments

2. **BattleController.cs**
   - Added automatic Normal Dice creation in `StartNewHand()`
   - Added filtering in `OnSubmitCombo()` to exclude Normal Dice from cooldown
   - Enhanced UI feedback to show dice composition
   - Improved debug logging

## Testing Checklist

- [x] Hand always has exactly 5 dice
- [x] Normal Dice can be rolled
- [x] Normal Dice can be locked
- [x] Normal Dice contribute to combos
- [x] Normal Dice don't enter cooldown pool
- [x] UI shows correct dice composition
- [x] Score calculation works with mixed dice
- [x] Cooldown system only affects special dice
- [x] Multiple hands work correctly
- [x] Edge case: All special dice on cooldown (5 normal dice work)

## Future Enhancements

Possible improvements:
1. Different Normal Dice variants (weighted dice, etc.)
2. Upgradeable Normal Dice system
3. Visual distinction for Normal vs Special dice in UI
4. Option to disable Normal Dice fillers (hard mode)
5. Normal Dice could have small random bonuses

