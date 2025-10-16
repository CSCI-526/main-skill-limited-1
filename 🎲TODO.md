> # 🎲 Dice Roguelike Prototype (48h Build)
>
> ## ⚡ Project Goal
> Deliver a **fully playable prototype** in 48 hours that demonstrates the complete dice-rolling core loop:  
> `Select Dice → Roll (3x) → Detect Combo → Score → Cooldown → Reward`.
>
> **Deliverables**
> - ✅ Playable prototype (Unity, minimal UI / text-based OK)  
> - ✅ Three functional scenes:  
>   1. **Main Menu Scene**  
>   2. **Battle Scene**  
>   3. **Reward Scene**  
> - ✅ One-page GDD (done)
>
> ---
>
> ## 🎮 Game Flow Overview
>
> 1. **Main Scene:** start the run → initialize player data (budget, dice pool).  
> 2. **Battle Scene:**  
>    - Player selects up to 5 dice (cost ≤ budget).  
>    - Roll up to 3 times (lock/unlock between rolls).  
>    - Detect combo → calculate score → apply cooldown.  
>    - Repeat for 4–5 hands.  
> 3. **Reward Scene:**  
>    - Display total score & stage result.  
>    - Offer one of three rewards (new die / relic / +budget).  
>    - After choosing, return to Main Scene or next battle.
>
> ---
>
> ## 🧩 Core Systems
>
> | System                  | Function                                        | Notes                             |
> | ----------------------- | ----------------------------------------------- | --------------------------------- |
> | 🎲 **Dice System**       | Data structure with tier, cost, cooldown, faces | Serialized in Unity or JSON       |
> | 💰 **Budget System**     | Limit hand cost ≤ budget                        | Update on reward selection        |
> | ❄️ **Cooldown System**   | Used dice unavailable for 1 hand                | Auto refresh when all on cooldown |
> | 🧮 **Combo Recognition** | Detect poker hands                              | FullHouse, Straight, etc.         |
> | 📊 **Scoring System**    | `(Base + Sum) × Mult`                           | Refer to GDD table                |
> | 🔁 **Hand Flow**         | 5 hands per round                               | Decrement, update cooldown        |
> | 🎯 **Win / Lose Check**  | Target score reached?                           | Output message                    |
> | 🪄 **Reward System**     | Choose 1 of 3 upgrades                          | Modify player stats/dice          |
> | 🧠 **State Manager**     | Handles scene transitions                       | Between Main, Battle, Reward      |
>
> ---
>
> ## 🗺️ Scene Breakdown
>
> ### 1️⃣ **Main Scene**
> **Purpose:** Start / restart the run.  
> **Elements:**
> - “Start Game” button (loads Battle Scene)
> - Player status summary (budget, relics, best score)
> - Placeholder background only  
>
> **Responsibilities:**
> - Load/save basic player data
> - Initialize deck (8 dice)
> - Scene switch to BattleScene
>
> ---
>
> ### 2️⃣ **Battle Scene**
> **Purpose:** Core gameplay loop (selection, roll, score).  
> **Elements (can be console or simple text UI):**
> - Dice list with name, cost, cooldown
> - Selection system (choose ≤5 within budget)
> - Roll button (simulate up to 3 rolls)
> - Output combo type + score
> - Show total score and remaining hands
>
> **Responsibilities:**
> - Implement dice selection + budget validation  
> - Execute rolling / locking logic  
> - Combo recognition + scoring  
> - Apply cooldown to used dice  
> - Trigger transition to Reward Scene when round ends
>
> ---
>
> ### 3️⃣ **Reward Scene**
> **Purpose:** Offer run progression and replay.  
> **Elements:**
> - Display final score  
> - Present 3 random reward options (text only)  
>   - e.g. “+1 Budget”, “Add Rare Die”, “Relic: ×1.2 Mult”  
> - Player selects one → apply effect  
> - Button: “Next Stage” → load BattleScene again  
> - Button: “Quit” → back to MainScene  
>
> **Responsibilities:**
> - Randomize and display reward choices  
> - Apply reward effects  
> - Manage scene transition
>
> ---
>
> ## 👥 Team Roles & Detailed Tasks
>
> | Role                                            | Name | Scene Focus      | Tasks                                                        | Deadline    |
> | ----------------------------------------------- | ---- | ---------------- | ------------------------------------------------------------ | ----------- |
> | 🎯 **Lead Designer**                             | —    | All scenes       | - Maintain GDD & data tables<br>- Define Base/Mult/Cost values<br>- Manage task sync & version control<br>- Integrate reward balance | Day 1 AM    |
> | 💻 **Programmer 1 – Dice & Rolling System**      | —    | **Battle Scene** | - Implement `Dice` class (cost, tier, cooldown, faces)<br>- Rolling (random 1–6)<br>- Lock/unlock logic<br>- Text feedback for rolls | Day 1 PM    |
> | 💻 **Programmer 2 – Combo & Scoring**            | —    | **Battle Scene** | - Write poker-hand recognition (5/4/3 of a kind, full house, straight)<br>- Score formula `(Base+Sum)×Mult`<br>- Print result summary | Day 1 Night |
> | ⚙️ **Programmer 3 – Hand & Cooldown Flow**       | —    | **Battle Scene** | - Manage 8-dice pool rotation<br>- Apply 1-turn cooldown<br>- Hand counter (5 hands)<br>- Auto refresh when all used | Day 2 AM    |
> | 🧩 **Programmer 4 – State Manager & Scene Flow** | —    | **All Scenes**   | - Scene transitions: Main → Battle → Reward<br>- Pass data between scenes (budget, dice)<br>- Handle run reset | Day 2 Noon  |
> | 💎 **Programmer 5 – Reward System**              | —    | **Reward Scene** | - Generate 3 reward options (random from pool)<br>- Apply effects (budget+, relic, new die)<br>- Integrate back to next battle | Day 2 PM    |
> | 🔍 **Tester / Debugger**                         | —    | All              | - Verify combo logic correctness<br>- Simulate multiple rounds<br>- Log bugs & balance issues<br>- Record short demo video | Day 2 Night |
>
> ---
>
> ## 🧱 Functional Priorities
>
> | Priority | Feature                             | Target Scene | Description                    |
> | -------- | ----------------------------------- | ------------ | ------------------------------ |
> | 🟩 P0     | Dice structure & roll randomization | Battle       | Must be functional early       |
> | 🟩 P0     | Budget & selection system           | Battle       | Blocker for all testing        |
> | 🟩 P0     | Combo detection & scoring           | Battle       | Must output text result        |
> | 🟩 P0     | Cooldown & dice rotation            | Battle       | Complete the loop              |
> | 🟨 P1     | Reward selection & apply effect     | Reward       | Minimal logic demo             |
> | 🟨 P1     | Scene transitions                   | All          | Basic flow: Main→Battle→Reward |
> | 🟧 P2     | Data persistence (score, relics)    | Main         | Optional polish                |
> | 🟧 P2     | Multiple levels / difficulty ramp   | All          | Stretch goal                   |
>
> ---
>
> ## 📅 Timeline
>
> | Time                     | Task                                  | Owner    |
> | ------------------------ | ------------------------------------- | -------- |
> | **Day 1 AM (0–4h)**      | GDD finalization + repo setup         | Designer |
> | **Day 1 PM (4–10h)**     | Dice + Rolling + Combo draft          | Prog 1–2 |
> | **Day 1 Night (10–14h)** | Score + Budget + Cooldown logic       | Prog 3   |
> | **Day 2 AM (14–24h)**    | Integrate battle loop + win condition | Prog 4   |
> | **Day 2 PM (24–36h)**    | Reward scene & flow link              | Prog 5   |
> | **Day 2 Night (36–48h)** | Test & record demo                    | Tester   |
>
> ---
>
> ## ✅ Completion Checklist
>
> - [ ] Main Scene loads and starts run  
> - [ ] Battle Scene: choose ≤5 dice (within budget)  
> - [ ] Rolling and lock/unlock works  
> - [ ] Combo detection outputs correct combo  
> - [ ] Score calculation correct  
> - [ ] Cooldown applies correctly  
> - [ ] Reward Scene triggers at end  
> - [ ] Selecting reward modifies player data  
> - [ ] Back to MainScene loop works  
> - [ ] Console or minimal UI prints all states clearly  
>
> ---
>
> **Core Identity:**  
> > *“8-dice pool + 6 budget + 1-turn cooldown = every hand a new puzzle.”*  
> > Short, readable, and expandable — perfect for a 48-hour proof-of-concept.