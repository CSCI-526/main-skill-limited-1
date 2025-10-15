# 🎲 Dice Roguelike – Core Game Design Document 

## 1. Overview

A roguelike score-chasing dice game inspired by *Balatro*.
 Players build a set of dice with different rarities and costs, then roll combinations to earn points.
 Each hand consumes limited budget and triggers cooldowns, forcing strategic rotation instead of static “best five” plays.

------

## 2. Core Loop

1. **Goal:** reach a target score (e.g., 500) before running out of hands.
2. **Select Dice:** choose up to **5 dice** from a pool of **8**.
3. **Budget Check:** total cost ≤ **Hand Budget (HB)** (default = 6).
4. **Roll Phase:** roll up to **3 times**, locking dice between rolls.
5. **Score Calculation:** evaluate the final combination.
6. **Cooldown:** all dice used enter **1-hand cooldown**.
7. **Repeat** until all dice have cooled; continue until out of hands.
8. **Victory:** cumulative score ≥ target → gain rewards (new die / relic / budget increase).

------

## 3. Dice System

| Type          | Cost | Description                                                  |
| ------------- | ---- | ------------------------------------------------------------ |
| **Common**    | 1    | Balanced; fills empty slots.                                 |
| **Rare**      | 2    | Weighted rolls or minor bonuses.                             |
| **Legendary** | 3    | High variance or special effects (e.g., D20).                |
| **Filler**    | 0    | Auto-spawned to reach 5 dice; optional –10 % multiplier penalty. |

**Cooldown:** all dice = 1 hand.

------

## 4. Scoring Formula

```
Score = (BaseValue + SumOfDiceFaces) × ComboMultiplier × AllModifiers
```

| Combination     | Base | Mult |
| --------------- | ---- | ---- |
| Five of a Kind  | 100  | ×4.0 |
| Four of a Kind  | 80   | ×2.5 |
| Full House      | 70   | ×2.0 |
| Straight        | 60   | ×1.8 |
| Three of a Kind | 50   | ×1.5 |
| Two Pair        | 35   | ×1.2 |
| One Pair        | 20   | ×1.0 |
| High Card       | Sum  | ×0.8 |

------

## 5. Resources & Limits

| Parameter            | Default      | Notes                      |
| -------------------- | ------------ | -------------------------- |
| **Hand Budget (HB)** | 6            | Total cost ≤ HB each hand. |
| **Deck Size**        | 8            | Auto-filled to 8 dice.     |
| **Dice per Hand**    | 5            | Max 5; fillers auto-added. |
| **Hands per Round**  | 5            | Modified by relics ±1.     |
| **Cooldown**         | 1 hand (all) | Simple rotation rule.      |

------

## 6. Relic Examples

| Category        | Example         | Effect                                     |
| --------------- | --------------- | ------------------------------------------ |
| **Budget**      | *Light Belt*    | +2 Hand Budget.                            |
| **Cost**        | *Royal Seal*    | Legendary dice cost –1.                    |
| **Cooldown**    | *Temporal Core* | Cooldown –1 (min 0).                       |
| **Risk/Reward** | *Cursed Cup*    | Roll a 1 → ×0.5 mult / Roll a 6 → ×3 mult. |
| **Score Boost** | *Lucky Coin*    | +10 Base Value per hand.                   |

------

## 7. Roguelike Flow

1. **Enter Stage → Target Score Set.**
2. **Play Hands (4 – 5 turns):** score accumulates.
3. **Meet Target → Choose Reward:** new die / relic / budget upgrade.
4. **Proceed to Next Stage** with higher score threshold and harder budget conditions.

------

## 8. Key Design Goals

- **Simple Math, Deep Strategy:** only two tuning knobs — budget and cooldown.
- **Forced Rotation:** 1-turn cooldown prevents repetitive “strong five” plays.
- **Tight Pacing:** short rounds (4 – 5 hands) keep sessions fast.
- **Expandable:** easy hooks for relics affecting cost, budget, multipliers, or combos.

------

### ✅ Core Identity

> **“8 dice pool + 6 budget + 1-turn cooldown = dynamic decision every hand.”**
>  Players must balance power vs consistency to build toward massive combo scores within limited hands.

### Possible Boss fight:

1. Limitation like balatro(e.g. can not play 3 of a kind)
2. boss that can copy your dice 
3. boss that have hp and will debuff you when hp lower than 50%