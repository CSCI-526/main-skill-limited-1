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

## 🕒 CooldownSystem

Manages 8-dice pool rotation with cooldown mechanics and hand counter.

**Key Features:**
- 8-dice pool sourced from player's backpack/inventory
- Fills remaining slots with normal dice if backpack has < 8 dice
- 1-turn cooldown only on submitted dice (locked dice that were submitted)
- 5-hand counter with auto-refresh
- Event-driven architecture for UI updates

**Core Methods:**
```csharp
// Get available dice (not on cooldown)
List<BaseDice> GetAvailableDice()

// Select dice for current hand (up to 5)
bool SelectDiceForHand(List<BaseDice> selectedDice)

// Complete hand and apply cooldowns to submitted dice only
void CompleteHand(List<BaseDice> submittedDice = null)

// Get hand counter info
(int current, int remaining) GetHandCounter()

// Check if dice are available
bool HasAvailableDice()

// Get selected dice cost
int GetSelectedDiceCost()

// Set dice from player's backpack/inventory
void SetPlayerBackpackDice(List<BaseDice> backpackDice)
```

**Events:**
```csharp
// Triggered when dice pool refreshes (all hands used)
System.Action OnDicePoolRefresh

// Triggered when hand counter updates
System.Action<int, int> OnHandCounterUpdate

// Triggered when available dice changes
System.Action<List<BaseDice>> OnAvailableDiceChanged
```

**Usage Example:**
```csharp
// Subscribe to events
cooldownSystem.OnDicePoolRefresh += OnPoolRefresh;
cooldownSystem.OnHandCounterUpdate += OnCounterUpdate;

// Set dice from player's backpack (from inventory system)
var playerDice = GetPlayerInventoryDice(); // Your inventory system
cooldownSystem.SetPlayerBackpackDice(playerDice);

// Get available dice and select for hand
var available = cooldownSystem.GetAvailableDice();
var selected = available.Take(5).ToList();
cooldownSystem.SelectDiceForHand(selected);

// After hand completion (applies 1-turn cooldown to submitted dice only)
var submittedDice = GetLockedAndSubmittedDice(); // Your logic to get submitted dice
cooldownSystem.CompleteHand(submittedDice);
```

---

## 🎮 BattleController

Manages the rolling phase UI and flow. Now integrated with CooldownSystem.

**Public References:**
```csharp
// UI Buttons (assign in Inspector)
Button rollButton;
Button resetRollButton;
Button submitComboButton;
TMP_Text rollFeedbackText;
TMP_Text handCounterText;  // NEW: Hand counter display

// Config
int maxRollsPerHand = 3;
int diceCount = 5;

// Cooldown System Integration
CooldownSystem cooldownSystem;  // Reference to cooldown system
```

**New Integration Features:**
```csharp
// BattleController now automatically:
// 1. Gets available dice from CooldownSystem
// 2. Selects up to 5 dice for each hand
// 3. Completes hands and applies cooldowns
// 4. Handles auto-refresh when all hands used
// 5. Updates hand counter display

// For combo detection/scoring (unchanged):
void OnSubmitCombo()
{
    var values = new List<int>();
    foreach (var d in _dice)
        if (d.lastRollValue > 0)
            values.Add(d.lastRollValue);
    
    // TODO: Call ComboDetector.Detect(values)
    // TODO: Call ScoringSystem.Calculate(combo, values)
    
    // Hand completion is now automatic via CooldownSystem
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

### For **Cooldown System** (Programmer 3) - ✅ IMPLEMENTED:
```csharp
// CooldownSystem manages 8-dice pool with 1-turn cooldown
// BattleController now integrates with CooldownSystem automatically

// Access cooldown system:
CooldownSystem cooldownSystem = FindObjectOfType<CooldownSystem>();

// Get available dice:
List<BaseDice> available = cooldownSystem.GetAvailableDice();

// Select dice for hand:
bool success = cooldownSystem.SelectDiceForHand(selectedDice);

// Complete hand (applies cooldowns):
cooldownSystem.CompleteHand();

// Check hand counter:
var (current, remaining) = cooldownSystem.GetHandCounter();
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

- **No budget validation** (cost calculation exists, validation needed)
- **No combo detection** (placeholder in submit)
- **No scoring** (placeholder in submit)
- **Limited dice selection UI** (auto-selects first 5 available dice)

---

## ✅ What's Ready

- ✅ Dice data structure with tier/cost/cooldown
- ✅ Normal & Weighted dice implementations
- ✅ 3-roll limit per hand
- ✅ Lock/unlock between rolls
- ✅ Early submission (player choice)
- ✅ Debug logging for all actions
- ✅ UI components (DiceView with lock indicator)
- ✅ **8-dice pool management** (CooldownSystem)
- ✅ **1-turn cooldown system** (automatic after hand completion)
- ✅ **5-hand counter** (tracks hands used/remaining)
- ✅ **Auto-refresh** (when all hands used)
- ✅ **Event-driven UI updates** (hand counter, available dice)

---

## 🔗 Next Integration Steps

1. **Combo Detection** → Hook into `OnSubmitCombo()`, read `_dice` values
2. **Scoring** → Use combo result + dice values + tier modifiers
3. **Budget Validation** → Use `GetSelectedDiceCost()` to validate before allowing roll
4. **Dice Selection UI** → Allow player to choose from available dice pool
5. **Advanced Pool Management** → Add dice purchasing/upgrading system

---

**Questions?** Check existing code in:
- `DiceRogue/Assets/Scripts/Dice/BaseDice.cs`
- `DiceRogue/Assets/Scripts/Battle/BattleController.cs`
- `DiceRogue/Assets/Scripts/Battle/DiceView.cs`
- `DiceRogue/Assets/Scripts/Battle/CooldownSystem.cs` (NEW)

