# 🎲 Dice Roguelike Prototype (48h Build)

### ⚡ Project Goal
A 48-hour prototype of a *Balatro*-style roguelike dice game.  
Players build a dice pool, select up to 5 dice each hand, roll to form combinations, and score points under **budget** and **cooldown** constraints.

**Delivery Objectives:**
- ✅  GDD (mechanics + data tables)
- ✅ Playable prototype (no UI, console-based or minimal Unity scene)
- ✅ Demonstrates the full core loop:
  `Select Dice → Roll (3x) → Detect Combo → Score → Apply Cooldown → Next Hand`

---

## 🎯 Core Game Loop

1. **Goal:** Reach a target score (e.g., 500) within 4–5 hands.  
2. **Select Dice:** Choose up to 5 dice from an 8-dice pool.  
3. **Budget Check:** Total cost ≤ Hand Budget (default = 6).  
4. **Roll Phase:** Roll up to 3 times, locking dice between rolls.  
5. **Combo Recognition:** Detect poker-style combinations.  
6. **Score Calculation:** `(Base + Sum) × Multiplier`.  
7. **Cooldown:** All used dice go on cooldown for 1 hand.  
8. **Repeat:** Continue until out of hands or target reached.

---

## 🧩 Core Systems to Implement

| System                  | Description                                                  | Priority   |
| ----------------------- | ------------------------------------------------------------ | ---------- |
| 🎲 **Dice Data**         | Class with `tier`, `cost`, `cooldown`, and `faces`           | 🟩 Must     |
| 🎲 **Rolling**           | Random face generation (1–6), support dice locking           | 🟩 Must     |
| 💰 **Budget Check**      | Validate hand cost ≤ budget                                  | 🟩 Must     |
| ❄️ **Cooldown**          | Dice used go on cooldown (1 hand)                            | 🟩 Must     |
| 🧮 **Combo Recognition** | Detect: Pair, Two Pair, Three, Full House, Straight, Four, Five | 🟩 Must     |
| 📊 **Scoring Formula**   | `(Base + Sum) × Mult` using combo table                      | 🟩 Must     |
| 🔁 **Turn Flow**         | Manage 4–5 hands, total score, end condition                 | 🟩 Must     |
| 🎯 **Win Condition**     | Display “Target Reached” if cumulative ≥ goal                | 🟩 Must     |
| 🧱 **Auto Fill Dice**    | Fill to 5 dice with basic (cost=0) dice                      | 🟨 Optional |
| 💎 **Relic Example**     | One relic effect (+1 Budget / ×1.2 Multiplier)               | 🟨 Optional |

---

## 💰 Default Parameters

| Parameter            | Value                         | Note                     |
| -------------------- | ----------------------------- | ------------------------ |
| **Deck Size**        | 8                             | Auto-filled if fewer     |
| **Dice per Hand**    | 5                             | Max selection            |
| **Hand Budget (HB)** | 6                             | Total dice cost per hand |
| **Dice Cost**        | Common=1, Rare=2, Legendary=3 |                          |
| **Cooldown**         | 1 hand (all dice)             | Simple rotation          |
| **Hands per Round**  | 5                             | Limited turns            |
| **Target Score**     | 500                           | Stage goal               |

---

## 👥 Team Roles & Tasks (6 Members)

| Role                                           | Responsibilities                                             | Deadline    |
| ---------------------------------------------- | ------------------------------------------------------------ | ----------- |
| 🎯 **Lead Designer**                            | - Finalize GDD and data tables<br>- Define scoring & cost balance<br>- Coordinate team tasks | Day 1 AM    |
| 💻 **Programmer 1 – Core Dice Logic**           | - Implement `Dice` class<br>- Rolling + Locking system       | Day 1 PM    |
| 💻 **Programmer 2 – Combo & Scoring**           | - Poker combo detection<br>- `(Base + Sum) × Mult` formula   | Day 1 Night |
| ⚙️ **Programmer 3 – Game Flow**                 | - Hand system (4–5 turns)<br>- Budget check & cooldown rotation | Day 2 AM    |
| 🧩 **Programmer 4 – Integration / Interaction** | - Integrate modules<br>- Add simple console I/O (keyboard input) | Day 2 Noon  |
| 🔍 **Tester / Recorder**                        | - Verify logic correctness<br>- Record sample runs / demo video<br>- Prepare submission notes | Day 2 PM    |

> Focus on **functional prototype** — no UI, no animation, no sound.

---

## ✅ Completion Checklist

- [ ] Dice pool of 8 displayed in console (name, cost, cooldown)  
- [ ] Player can select ≤5 dice within budget  
- [ ] Rolling & lock system (3 rolls) works  
- [ ] Combo detection outputs correct type  
- [ ] Score calculation matches GDD table  
- [ ] Used dice cooldown correctly applied  
- [ ] Cumulative score & hand count displayed  
- [ ] Target goal triggers win message  

---

## 💡 Tips

- Keep everything **text-based** — print results and states in console.  
- No physics or animation; just random numbers.  
- Cooldown logic is simple: used dice `cd=1`, skip next hand, auto-reset.  
- Make all numbers (Base, Mult, Budget) adjustable constants.  
- Stop adding features after 36h; focus on stability.

---

**Core Identity:**  
> “8-dice pool + 6-point budget + 1-turn cooldown → every hand is a new strategic puzzle.”  