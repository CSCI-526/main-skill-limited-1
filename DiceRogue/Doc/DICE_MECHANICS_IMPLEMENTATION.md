# Dice Mechanics Implementation

## Overview
Special dice mechanics are now handled by a modular, decoupled architecture. BattleController orchestrates specialized components that handle different aspects of dice mechanics. This document describes the implementation details for each special dice type.

## Architecture

### Component Responsibilities
- **BattleController**: Orchestrates gameplay, coordinates components
- **DiceEffectHandler**: Applies special dice effects during rolling
- **DiceMultiplierCalculator**: Calculates damage/score multipliers
- **HandManager**: Manages hand state and roll counting
- **DiceViewFactory**: Creates and manages UI views

## Special Dice Mechanics by Component

### 1. **PlusOne** (Rare) - DiceEffectHandler
- **Effect**: 70% chance to roll last dice number + 1
- **Implementation**: DiceEffectHandler passes the previous dice's value before rolling
- **Location**: `DiceEffectHandler.SetupPlusOneDice()` method
- **Called from**: `BattleController.OnRollOnce()` before rolling each dice
- **How it works**: 
  - Before rolling PlusOne dice, gets the previous dice's `lastRollValue`
  - Calls `SetPreviousDiceValue()` on PlusOne dice
  - PlusOne's Roll() method uses this value for its 70% chance calculation

### 2. **TwinBond** (Rare) - DiceEffectHandler
- **Effect**: Copy a random dice
- **Implementation**: After all dice roll, TwinBond copies a random dice's value
- **Location**: `DiceEffectHandler.HandleTwinBond()` (private)
- **Called from**: `DiceEffectHandler.ApplyRollEffects()`
- **How it works**:
  - After all dice finish rolling, scans for TwinBond dice
  - Collects all other valid dice values (excluding locked and filler dice)
  - Randomly selects one and copies its value using `CopyValue()`

### 3. **ZombieDice** (Legendary) - DiceEffectHandler
- **Effect**: 20% chance to infect neighbor dice (left and right)
- **Implementation**: After rolling, checks for infection and applies to neighbors
- **Location**: `DiceEffectHandler.HandleZombieInfection()` (private)
- **Called from**: `DiceEffectHandler.ApplyRollEffects()`
- **How it works**:
  - After Zombie rolls, calls `ShouldInfectNeighbors()`
  - If infection triggers (20% chance), finds left and right neighbors
  - Uses `InfectDice()` to set neighbors' values to match Zombie's value
  - Only infects unlocked dice

### 4. **GoldenDice** (Legendary) - DiceEffectHandler
- **Effect**: All dice num +1
- **Implementation**: After all dice roll and special effects, applies +1 to all other dice
- **Location**: `DiceEffectHandler.HandleGoldenDice()` (private)
- **Called from**: `DiceEffectHandler.ApplyRollEffects()`
- **How it works**:
  - Checks if GoldenDice is present in the hand
  - If present, loops through all other dice
  - Calls `ApplyBonus()` which adds +1 (capped at 6)
  - Logs each dice's value change

### 5. **Multiplier Dice** - DiceMultiplierCalculator
**Dice**: CollectorDice, D8, LuckySix, SevenSevenSeven
- **Effect**: Various damage/score multipliers
- **Implementation**: Calculates total multiplier from all submitted dice
- **Location**: `DiceMultiplierCalculator.Calculate()` method
- **Called from**: `BattleController.OnSubmitCombo()`
- **How it works**:
  
  #### CollectorDice (Rare)
  - x1.5 if current roll matches previous roll
  - Calls `GetMultiplier()` which checks `HasMatchedPreviousRoll()`
  
  #### D8 (Legendary)
  - x5 if rolled 7, x10 if rolled 8
  - Calls `GetMultiplier()` which returns multiplier based on `lastRollValue`
  - Only rolls once per hand (enforced by D8 class itself)
  
  #### LuckySix (Rare)
  - x1.5 if rolled a 6
  - Calls `GetMultiplier()` which checks if `lastRollValue == 6`
  
  #### SevenSevenSeven (Rare)
  - x2 if part of three-of-a-kind
  - Calls `IsPartOfThreeOfAKind()` with all submitted values
  - Then calls `GetMultiplier(isThreeOfAKind)` to get the multiplier
  
  - All multipliers are **multiplied together** for the final damage calculation
  - Multiplier is passed to `DiceHandEvaluator.Evaluate()` for scoring

## Dice Without Special BattleController Handling

These dice implement their special mechanics entirely within their own `Roll()` method:

### Self-Contained Dice
1. **NormalDice** (Filler) - Standard 1-6 dice
2. **BigOne** (Common) - 25% chance to roll 1
3. **BigSix** (Common) - 25% chance to roll 6
4. **CounterDice** (Common) - Custom faces [1,2,2,5,5,6]
5. **EvenDice** (Common) - Only rolls even numbers [2,4,6]
6. **OddDice** (Common) - Only rolls odd numbers [1,3,5]
7. **HeavyDice** (Common) - 70% chance to roll high (4,5,6)
8. **LightDice** (Common) - 70% chance to roll low (1,2,3)
9. **MirrorDice** (Common) - Roll num = 7 - last num
10. **WeightedEdge** (Rare) - Only rolls 3 or 6

## Execution Order

The special dice mechanics are handled in the following order during `OnRollOnce()`:

1. **Roll Phase**: All unlocked dice roll
   - PlusOne gets previous dice value before rolling
   
2. **Post-Roll Effects** (applied in order):
   - TwinBond copying (`HandleTwinBond()`)
   - Zombie infection (`HandleZombieInfection()`)
   - Golden +1 bonus (`HandleGoldenDice()`)
   
3. **View Refresh**: All dice views update to show final values

4. **Submit Phase** (during `OnSubmitCombo()`):
   - Collect all locked dice
   - Calculate multipliers (`CalculateDiceMultipliers()`)
   - Evaluate combo and score with multipliers

## Testing Notes

### Key Edge Cases Handled
- **Locked dice**: Special effects skip locked dice
- **Filler dice**: Placeholder dice are excluded from all special effects
- **Empty slots**: Game handles fewer than 5 dice correctly
- **Zombie infection**: Only infects unlocked neighbors
- **TwinBond**: Only copies from dice with valid values (> 0)
- **GoldenDice**: Doesn't apply +1 to itself
- **Multipliers**: Multiplies together correctly (e.g., 1.5 × 2 = 3x)

### Debug Logging
All special dice effects log their actions:
- Roll results
- Value changes (infection, +1 bonus, copying)
- Multiplier breakdown
- Final scores

Check Unity Console for detailed logs during gameplay.

## Future Improvements

1. **Visual Effects**: Add animations for special effects (infection spreading, copying, +1 bonus)
2. **UI Indicators**: Show which dice have special effects active
3. **Multiplier Display**: Show multiplier breakdown in the UI (currently only in logs)
4. **Order of Operations**: Consider allowing player choice for effect order
5. **Combo Synergies**: Add achievements/bonuses for specific dice combinations

