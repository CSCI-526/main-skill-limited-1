# Random Dice Pool System

## Overview
The game now features a **random dice pool system** that selects 8 dice from all available dice types at the start of each battle, providing variety and replayability.

## How It Works

### 1. DicePoolFactory
**Location**: `Assets/Scripts/Battle/DicePoolFactory.cs`

Creates a random pool of 8 dice from all 17 available dice types:

#### Common Dice (8 types)
- BigOne - 25% chance to roll 1
- BigSix - 25% chance to roll 6
- CounterDice - Faces [1,2,2,5,5,6]
- EvenDice - Only rolls even numbers
- OddDice - Only rolls odd numbers
- HeavyDice - 70% chance to roll 4-6
- LightDice - 70% chance to roll 1-3
- MirrorDice - Roll = 7 - last roll

#### Rare Dice (6 types)
- CollectorDice - x1.5 if matches previous roll
- LuckySix - x1.5 if rolls 6
- PlusOne - 70% chance to roll previous+1
- SevenSevenSeven - x2 if part of three-of-a-kind
- TwinBond - Copy random dice
- WeightedEdge - Only rolls 3 or 6

#### Legendary Dice (3 types)
- D8 - Rolls 1-8, x5 for 7, x10 for 8
- GoldenDice - All dice +1
- ZombieDice - 20% chance to infect neighbors

### 2. Pool Selection Process

```
Start of Battle
      ↓
DicePoolFactory.CreateRandomPool(8)
      ↓
Randomly select 8 dice from 17 types
      ↓
CooldownSystem manages pool
      ↓
Each hand: Pick 5 from 8 available
```

### 3. Hand Selection

**BattleController** picks 5 dice each hand:
- Gets available dice from CooldownSystem (not on cooldown)
- Randomly shuffles available dice
- Selects first 5 (or all if less than 5 available)
- Displays selected dice + empty placeholders

## Code Flow

### Initialization
```csharp
// CooldownSystem.cs - Line 99
private List<BaseDice> GetPlayerBackpackDice()
{
    // Creates random pool of 8 dice
    var backpackDice = DicePoolFactory.CreateRandomPool(maxDicePool, cooldownTurns);
    return backpackDice;
}
```

### Pool Creation
```csharp
// DicePoolFactory.cs
public static List<BaseDice> CreateRandomPool(int poolSize = 8, int cooldownTurns = 1)
{
    // Get all 17 dice types
    var allDiceTypes = GetAllDiceTypes();
    
    // Shuffle randomly
    var shuffled = allDiceTypes.OrderBy(x => Random.value).ToList();
    
    // Take first 8
    var selectedTypes = shuffled.Take(poolSize).ToList();
    
    // Create instances
    foreach (var diceType in selectedTypes)
    {
        var dice = CreateDiceInstance(diceType, cooldownTurns);
        pool.Add(dice);
    }
    
    return pool;
}
```

### Hand Selection
```csharp
// BattleController.cs - Line 119-128
// Get available dice (not on cooldown)
var availableDice = cooldownSystem.GetAvailableDice();

// Select up to 5 dice
int diceToSelect = Mathf.Min(5, availableDice.Count);

// Randomly shuffle for variety
var shuffledDice = availableDice.OrderBy(x => Random.value).ToList();

// Take first 5
for (int i = 0; i < diceToSelect; i++)
{
    selectedDice.Add(shuffledDice[i]);
}
```

## Key Features

### ✅ Randomization
- **Pool**: 8 random dice from 17 types at battle start
- **Hand**: 5 random dice from 8 available each turn
- Different pool each battle = high replayability

### ✅ Cooldown System
- After using dice, they go on cooldown
- Cooldown advances each hand
- Only available dice can be selected

### ✅ Tier Distribution
The 17 dice types include:
- **8 Common** (47%)
- **6 Rare** (35%)
- **3 Legendary** (18%)

Random selection means each pool has different tier distributions.

## Example Battle Flow

### Battle Start
```
[DicePoolFactory] Creating random pool...
Selected 8 dice:
  1. BigSix (Common, cost: 1)
  2. HeavyDice (Common, cost: 1)
  3. CollectorDice (Rare, cost: 2)
  4. TwinBond (Rare, cost: 2)
  5. SevenSevenSeven (Rare, cost: 2)
  6. WeightedEdge (Rare, cost: 2)
  7. D8 (Legendary, cost: 3)
  8. ZombieDice (Legendary, cost: 3)
```

### Hand 1
```
Available: All 8 dice
Selected for hand: BigSix, CollectorDice, TwinBond, D8, ZombieDice
[Player rolls and submits]
Used: TwinBond, D8, ZombieDice (3 dice on cooldown)
```

### Hand 2
```
Available: BigSix, HeavyDice, CollectorDice, SevenSevenSeven, WeightedEdge (5 dice)
Cooldown: TwinBond(0), D8(0), ZombieDice(0) (3 dice)
Selected for hand: All 5 available dice
[Player rolls and submits]
```

### Hand 3
```
Available: All 8 dice (cooldowns cleared)
Selected for hand: 5 random from 8
...
```

## Benefits

### 🎮 Gameplay
- **Variety**: Different dice each battle
- **Strategy**: Adapt to available dice
- **Replayability**: ~24,000+ possible 8-dice combinations
- **Balance**: Mix of common, rare, and legendary

### 🏗️ Architecture
- **Modular**: DicePoolFactory is standalone
- **Testable**: Easy to test random generation
- **Extensible**: Add new dice by updating factory enum
- **Maintainable**: Clear separation of concerns

### 🐛 Debugging
- Full logging of pool creation
- Shows which dice selected each hand
- Displays cooldown status
- Easy to track dice flow

## Configuration

### Pool Size
Default: 8 dice (can be changed in CooldownSystem)
```csharp
[Header("Dice Pool Settings")]
public int maxDicePool = 8;
```

### Hand Size
Default: 5 dice (can be changed in BattleController)
```csharp
[Header("Config")]
public int diceCount = 5;
```

### Cooldown Duration
Default: 1 turn (can be changed in CooldownSystem)
```csharp
public int cooldownTurns = 1;
```

## Testing

### Quick Test in Unity
1. Enter Play Mode
2. Check Console for pool creation logs
3. Note which 8 dice were selected
4. Each battle should have different dice

### Expected Logs
```
[CooldownSystem] Generating random dice pool from all available dice types...
[DicePoolFactory] Added BigSix (Common, cost: 1)
[DicePoolFactory] Added HeavyDice (Common, cost: 1)
[DicePoolFactory] Added CollectorDice (Rare, cost: 2)
[DicePoolFactory] Added TwinBond (Rare, cost: 2)
[DicePoolFactory] Added SevenSevenSeven (Rare, cost: 2)
[DicePoolFactory] Added WeightedEdge (Rare, cost: 2)
[DicePoolFactory] Added D8 (Legendary, cost: 3)
[DicePoolFactory] Added ZombieDice (Legendary, cost: 3)
[DicePoolFactory] Created pool of 8 dice
[CooldownSystem] Initialized with 8 dice in pool (8 from backpack)
```

## Future Enhancements

### Potential Improvements
1. **Weighted Random**: Favor certain tiers based on difficulty
2. **Guaranteed Dice**: Ensure at least 1 legendary per pool
3. **Player Choice**: Let player pick some dice before battle
4. **Unlockable Dice**: Unlock more types as game progresses
5. **Dice Synergies**: Detect and reward synergistic combinations

### Save System Integration
```csharp
// When implementing save system
var savedPool = SaveSystem.GetDicePool();
cooldownSystem.SetPlayerBackpackDice(savedPool);
```

## Troubleshooting

### Issue: Same dice every battle
- Check if Random.InitState() is being called with fixed seed
- Verify DicePoolFactory is being called at battle start

### Issue: Less than 8 dice in pool
- Check Console for factory logs
- Verify all dice types compile correctly
- Check for null returns in CreateDiceInstance()

### Issue: Compilation error about dice type
- Ensure all dice classes exist in `Assets/Scripts/Dice/`
- Check using statements in DicePoolFactory
- Verify dice constructors work correctly

## Summary

✅ **Fixed**: Removed invalid `WeightedDice` reference  
✅ **Implemented**: Random 8-dice pool from 17 types  
✅ **Working**: 5 dice selected per hand from available pool  
✅ **Tested**: No compilation errors  
✅ **Documented**: Complete system documentation  

The game now has a robust random dice pool system that provides variety and replayability! 🎲

