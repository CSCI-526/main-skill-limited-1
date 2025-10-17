# Testing Dice Mechanics - Quick Reference

## How to Test Each Special Dice

### Interactive Dice (Require Multiple Dice)

#### PlusOne (Rare)
**Test Setup**: Have PlusOne as the 2nd or later dice in hand
1. Roll all dice
2. Check Unity Console - should see previous dice value being passed
3. PlusOne should roll previous value + 1 about 70% of the time

**Expected Log**: `"Plus One"` should reference the previous dice's value

#### TwinBond (Rare)
**Test Setup**: Have TwinBond + at least one other dice
1. Roll all dice
2. Check Unity Console for: `"Twin Bond copied value X from [DiceName]"`
3. TwinBond's value should match another dice in hand

**Expected Behavior**: TwinBond copies a random dice each roll

#### ZombieDice (Legendary)
**Test Setup**: Have Zombie dice with neighbors (not in first/last position)
1. Roll multiple times (20% chance to trigger)
2. When infection triggers, you'll see:
   - `"Zombie is infecting neighbors with value X!"`
   - `"Infected [LeftDice] (left)"`
   - `"Infected [RightDice] (right)"`
3. Neighbor dice values should match Zombie's value

**Expected Behavior**: 
- 20% chance to infect (test multiple times)
- Only unlocked neighbors get infected
- Locked dice are immune

#### GoldenDice (Legendary)
**Test Setup**: Have GoldenDice + other dice
1. Roll all dice
2. Check Unity Console for: `"Golden Dice is adding +1 to all other dice!"`
3. See value changes: `"[DiceName]: X -> Y"` where Y = X + 1
4. All other dice (except GoldenDice itself) should have +1 added

**Expected Behavior**:
- All dice get +1 (capped at 6)
- GoldenDice itself doesn't get the bonus
- Happens AFTER zombie infection and twin bond

### Multiplier Dice (Check on Submit)

#### CollectorDice (Rare)
**Test Setup**: Have CollectorDice
1. Roll once - note the value
2. Lock CollectorDice
3. Reset hand, start new hand with CollectorDice
4. Roll again - if you match previous hand's value, you'll see:
   - `"Multiplier Breakdown:"`
   - `"Collector Dice: x1.5 (matched previous roll)"`

**Note**: The "previous roll" refers to the previous roll within the same hand, not the previous hand

#### D8 (Legendary)
**Test Setup**: Have D8 dice
1. Roll dice - D8 rolls 1-8
2. Only rolls ONCE per hand (subsequent rolls won't change value)
3. On submit, if rolled 7 or 8:
   - `"D8: x5 (rolled 7)"` or `"D8: x10 (rolled 8)"`

**Expected Multipliers**:
- Value 7 = 5x damage
- Value 8 = 10x damage
- Other values = 1x (no bonus)

#### LuckySix (Rare)
**Test Setup**: Have LuckySix
1. Roll until you get a 6
2. Lock and submit
3. Should see: `"Lucky Six: x1.5 (rolled 6)"`

**Expected Behavior**: Only gives multiplier when value is exactly 6

#### SevenSevenSeven (Rare)
**Test Setup**: Have 777 + ability to make three-of-a-kind
1. Roll dice to get three of the same value (including 777)
2. Lock all three dice with same value
3. Submit combo
4. Should see: `"777: x2 (three-of-a-kind)"`

**Expected Behavior**: 
- Only triggers when 777 is part of a three-of-a-kind
- Works with any three matching values (doesn't have to be 7s)

### Multiplier Stacking Test

**Test Setup**: Have multiple multiplier dice (e.g., D8 + LuckySix + 777)
1. Roll to trigger multiple multipliers
2. Lock and submit
3. Check console for multiplier breakdown

**Expected Behavior**:
- Multipliers multiply together: 1.5 × 2.0 = 3.0x
- See: `"Total Multiplier: x3.00"`
- Final score reflects multiplied damage

## Common Dice (Self-Contained)

These don't need special testing beyond verifying their roll probabilities:

| Dice | Effect | Test Method |
|------|--------|-------------|
| BigOne | 25% to roll 1 | Roll many times, should get 1 about 25% |
| BigSix | 25% to roll 6 | Roll many times, should get 6 about 25% |
| CounterDice | Faces [1,2,2,5,5,6] | Should never see 3 or 4 |
| EvenDice | Only even (2,4,6) | Should never see odd numbers |
| OddDice | Only odd (1,3,5) | Should never see even numbers |
| HeavyDice | 70% high (4-6) | Should roll high numbers more often |
| LightDice | 70% low (1-3) | Should roll low numbers more often |
| MirrorDice | Value = 7 - last | First roll random, then alternates (1↔6, 2↔5, 3↔4) |
| WeightedEdge | Only 3 or 6 | Should only see 3 or 6 |

## Test Scenarios

### Scenario 1: Zombie + GoldenDice
1. Roll with both in hand
2. If Zombie infects, neighbors get Zombie's value
3. Then GoldenDice adds +1 to ALL dice (including infected ones)
4. Result: Infected dice get +1 on top of infection

### Scenario 2: TwinBond + Zombie
1. TwinBond copies BEFORE Zombie infects
2. TwinBond might copy Zombie's value
3. Then Zombie might infect TwinBond if they're neighbors

### Scenario 3: Multiple Multipliers + Combo
1. Get D8 to roll 7 (x5)
2. Get LuckySix to roll 6 (x1.5)
3. Make a Full House (base score ~100)
4. Final score = (100 + sum) × 5 × 1.5 = 7.5x base damage!

## Debugging Tips

### Enable Detailed Logs
Check Unity Console with filter: `BattleController`
- All dice rolls are logged
- Special effects are logged
- Multiplier breakdown is logged

### Common Issues
1. **No special effect?** 
   - Check dice is unlocked
   - Check dice is not Filler tier
   - Some effects are chance-based (Zombie = 20%)

2. **Multiplier not applying?**
   - Must SUBMIT the combo (not just roll)
   - Dice must be LOCKED when submitted
   - Check console for multiplier breakdown

3. **GoldenDice not adding +1?**
   - Effect applies AFTER rolling
   - Refresh views to see updated values
   - Check dice has valid value (> 0)

## Quick Console Command Reference

Look for these key log messages:

```
[BattleController] Rolling dice (Roll X/3)
  - [DiceName] rolled: X
  - Plus One: setting previous value
  - Twin Bond copied value X from [DiceName]
  - Zombie is infecting neighbors with value X!
    - Infected [DiceName] (left/right)
  - Golden Dice is adding +1 to all other dice!
    - [DiceName]: X -> Y

Multiplier Breakdown:
  [DiceName]: xY (reason)
Total Multiplier: xZ.ZZ
```

## Performance Testing

Test with all 5 dice having special abilities:
1. PlusOne + TwinBond + Zombie + GoldenDice + D8
2. Roll multiple times
3. Verify order of operations:
   - Roll (PlusOne gets context)
   - TwinBond copies
   - Zombie infects
   - GoldenDice adds +1
4. Check all effects apply correctly

This tests the most complex interaction case!

