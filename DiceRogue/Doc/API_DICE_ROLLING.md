# 🎲 Dice & Rolling System API Documentation

**Module Owner:** Programmer 1 (Dice & Rolling System)  
**Status:** ✅ Complete  
**Namespace:** `DiceGame`

---

## Overview
Handles dice data structure, rolling mechanics (up to 3 rolls/hand), and lock/unlock logic.

---

## 📦 Core Classes

### `BaseDice` (abstract)
Base class for all dice types.

**Key Properties:**
```csharp
string diceName           // Display name
DiceTier tier            // Filler=0, Common=1, Rare=2, Legendary=3
int cost                 // Budget cost (0-3)
int cooldownAfterUse     // Default = 1 (for cooldown system)
bool isLocked            // Lock state (prevents re-roll)
int lastRollValue        // Current face value (0 = not rolled)
int cooldownRemain       // Remaining cooldown turns (for integration)
```

**Key Methods:**
```csharp
abstract int Roll()              // Returns 1-6, respects isLocked
void ToggleLock()                // Lock/unlock dice
void ResetLockAndValue()         // Reset for new hand
```

---

### `NormalDice : BaseDice`
Standard D6 with uniform distribution.

**Usage:**
```csharp
var dice = new NormalDice {
    diceName = "D6_1",
    tier = DiceTier.Common,
    cost = 1,
    cooldownAfterUse = 1
};
```

---

### `WeightedDice : BaseDice`
D6 with weighted probabilities.

**Additional Fields:**
```csharp
int[] faces = {1,2,3,4,5,6}      // Face values
float[] weights = {1,1,1,1,1,2}  // Probabilities (not normalized)
```

**Example (30% chance for 6):**
```csharp
var rareDice = new WeightedDice {
    diceName = "Lucky Six",
    tier = DiceTier.Rare,
    cost = 2,
    weights = new float[] { 1, 1, 1, 1, 1, 2 }  // 2/7 ≈ 28.6%
};
```

---

## 🎮 BattleController

Manages the rolling phase UI and flow.

**Public References:**
```csharp
// UI Buttons (assign in Inspector)
Button rollButton;
Button resetRollButton;
Button submitComboButton;

// Config
int maxRollsPerHand = 3;
int diceCount = 5;
```

**For Integration:**
```csharp
// Access current dice data
List<BaseDice> _dice  // All dice in current hand

// To integrate combo detection/scoring:
void OnSubmitCombo()
{
    var values = new List<int>();
    foreach (var d in _dice)
        if (d.lastRollValue > 0)
            values.Add(d.lastRollValue);
    
    // TODO: Call ComboDetector.Detect(values)
    // TODO: Call ScoringSystem.Calculate(combo, values)
}
```

---

## 🔌 Integration Points

### For **Combo Detection** (Programmer 2):
```csharp
// Get all rolled values
List<int> GetDiceValues()
{
    var values = new List<int>();
    foreach (var dice in _dice)
        if (dice.lastRollValue > 0)
            values.Add(dice.lastRollValue);
    return values;
}

// Call your system:
// ComboType combo = ComboDetector.Detect(GetDiceValues());
```

### For **Cooldown System** (Programmer 3):
```csharp
// After hand completes:
foreach (var dice in _dice)
{
    dice.cooldownRemain = dice.cooldownAfterUse;  // Set to 1
}

// Before next hand (check availability):
bool IsAvailable(BaseDice dice) => dice.cooldownRemain == 0;
```

### For **Budget/Selection** (needs implementation):
```csharp
// Calculate total cost:
int totalCost = _dice.Sum(d => d.cost);

// Validate against budget:
bool IsWithinBudget(List<BaseDice> selected, int budget) 
    => selected.Sum(d => d.cost) <= budget;
```

---

## 🐛 Debug Logging

All actions log to Unity Console with `[BattleController]` or `[DiceView]` prefixes:
- Roll events (which dice rolled/locked)
- Lock/unlock actions
- Combo submission with full state
- Hand reset events

**Example Output:**
```
[BattleController] Rolling dice (Roll 1/3)
  - D6_1 rolled: 4
  - D6_2 rolled: 6
[DiceView] D6_2 is now LOCKED (value: 6)
[BattleController] ====== COMBO SUBMITTED ======
  D6_1: 4 (Locked: False)
  D6_2: 6 (Locked: True)
```

---

## 📋 Current Limitations

- **No budget validation** (5 dice hardcoded)
- **No dice pool selection** (need 8-dice pool → select 5)
- **No cooldown enforcement** (fields exist, logic needed)
- **No combo detection** (placeholder in submit)
- **No scoring** (placeholder in submit)

---

## ✅ What's Ready

- ✅ Dice data structure with tier/cost/cooldown
- ✅ Normal & Weighted dice implementations
- ✅ 3-roll limit per hand
- ✅ Lock/unlock between rolls
- ✅ Early submission (player choice)
- ✅ Debug logging for all actions
- ✅ UI components (DiceView with lock indicator)

---

## 🔗 Next Integration Steps

1. **Combo Detection** → Hook into `OnSubmitCombo()`, read `_dice` values
2. **Scoring** → Use combo result + dice values + tier modifiers
3. **Cooldown** → Set `cooldownRemain` after hand, filter available dice
4. **Budget** → Validate selection cost before allowing roll
5. **Dice Pool** → Expand to 8 dice, add selection UI

---

**Questions?** Check existing code in:
- `DiceRogue/Assets/Scripts/Dice/BaseDice.cs`
- `DiceRogue/Assets/Scripts/Battle/BattleController.cs`
- `DiceRogue/Assets/Scripts/Battle/DiceView.cs`

