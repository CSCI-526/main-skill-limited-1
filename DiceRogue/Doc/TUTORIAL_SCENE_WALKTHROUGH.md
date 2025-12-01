# Tutorial Scene Overview

The tutorial scene reuses the existing `BattleScene` gameplay setup and layers a guided prompt system on top via `TutorialController.cs`.

## Scene Structure
- Keep the original `BattleController` GameObject and UI from `BattleScene` (dice row, buttons, backpack, score animator, relic display panel, right info panel, etc.).
- Add the `TutorialController` component (on the same GameObject or a sibling). It automatically resolves references from `BattleController`, including:
  - Gameplay references: `BattleController`, `BackpackManager`, `DiceSelectionUI`, `DiceRowParent`, `ScoreAnimator`, buttons
  - UI panel references: `RelicDisplayPanel` (for highlighting relics), `RightInfoPanel` (for highlighting right panel info)
  - Only override fields in the inspector if you want custom values or if auto-resolution fails.
- UI elements needed in **both** `BattleScene` and `ShopScene`: `TutorialPromptPanel` (panel with a dark background), `TutorialText` (TextMeshPro child), and `NextButton` (TextMeshPro button inside the panel). The optional `SkipTutorialButton` can be left empty.
- `TutorialController` now uses `DontDestroyOnLoad` and listens to `SceneManager.sceneLoaded`, so the same instance persists when the player moves from the battle tutorial into the shop tutorial step.

## Controller Behaviour
- `TutorialController` drives a sequence of steps. Each step may display a prompt, wait for a gameplay action, or both.
- Prompts remain visible and move to different anchors depending on the step:
  - Intro/outro: centred with `Next` on the right edge of the panel.
  - Action steps: panel anchored near `Combo Preference` on the left, no `Next` button (shop tutorial also uses this position, but with an enlarged panel).
  - Combo preference reminder: panel centred under the dice.
  - Score breakdown: panel centred below the dice.
- The layout values are configurable via inspector fields (`introPrompt*`, `actionPrompt*`, `comboPrompt*`, etc.).
- Gameplay events are detected by piggybacking on existing buttons (`rollButton`, `submitComboButton`, backpack `Confirm`, dice lock buttons, score animation state, shop item buttons, combo rules button). No changes to `BattleController` or `ShopManager` are required.
- **Button Management**: The controller manages button interactability per step. For example, during the "Relics" step, all buttons except `Next` are disabled (including dice locking) to focus attention on the relic display.
- **Dynamic Highlighting**: Some steps use dynamic highlighting that changes based on player actions. For example, the "Check Combo Rules" step highlights the combo rules button initially, then switches to highlighting the `Next` button after the player clicks the combo rules button.
- After the score breakdown step completes, the controller keeps tutorial mode active, loads `ShopScene`, and resumes the tutorial there. Once the player buys the free shop item, the controller finishes the tutorial, saves completion, and loads Level 1 in `BattleScene`.

## Shop Tutorial Step
- Ensure `ShopScene` contains the same `TutorialPromptPanel / TutorialText / NextButton` hierarchy. These GameObjects can be disabled by default; the controller re-enables them when it arrives in the scene.
- The controller automatically finds `ShopManager`, hooks each `ShopItemUI` buy button, and highlights only the item that displays `FREE` in its price label.
- **Only the free dice button is clickable** during this step; all other shop item buttons are disabled to guide the player.
- When the free dice purchase succeeds, the tutorial step completes and the final "Tutorial Complete!" prompt appears before returning to `BattleScene`.

## Tutorial Flow
1. **Welcome** – centred prompt; click `Next` to begin.
2. **Build Your Hand** – select up to five dice from the backpack (left prompt, waits for confirmation).
3. **Relics** – left prompt introduces relics in the top right corner. All other buttons are disabled except `Next`. Hover over relics to see their effects (waits for `Next` click).
4. **Lock Dice** – click dice to keep them locked; locked dice won't change when rolling (left prompt, waits for dice lock action).
5. **Right Panel Info** – left prompt introduces the right panel showing combos, cast count, and rolls remaining. Click `Next` to continue.
6. **Roll Your Dice** – press `Roll` to throw your selected dice (left prompt, waits for roll action).
7. **Check Combo Rules** – prompt centred below the dice. The combo rules button is highlighted; click it to open the combo panel. After clicking, the highlight moves to the `Next` button (which appears), then click `Next` to continue.
8. **Roll Again** – left prompt explains that you can roll again and that locked dice stay fixed; the number of rolls and casts left can be seen on the right panel (waits for second roll action).
9. **Submit Your Hand** – lock the dice you want to use and press `Cast` to score the hand (left prompt, waits for cast action).
10. **Score Breakdown** – prompt centred below the dice; watch how the combo is scored (basic combo + dice effect + relic effect). Waits for score animation to complete, then displays for 1 second so players can read the message.
11. **Shop Tutorial** – after loading `ShopScene`, the left prompt explains the shop. The free dice is highlighted; buying it completes the step.
12. **Tutorial Complete** – centred prompt; click `Next` to return to Level 1 in `BattleScene` and start your real run.

That’s it—place the controller, ensure the panel/text/button exist in both scenes, and the tutorial will run end-to-end. 🎲

