# Tutorial Scene Overview

The tutorial scene reuses the existing `BattleScene` gameplay setup and layers a guided prompt system on top via `TutorialController.cs`.

## Scene Structure
- Keep the original `BattleController` GameObject and UI from `BattleScene` (dice row, buttons, backpack, score animator, etc.).
- Add the `TutorialController` component (on the same GameObject or a sibling). It automatically resolves references from `BattleController`, so only override fields in the inspector if you want custom values.
- UI elements needed: `TutorialPromptPanel` (panel with a dark background), `TutorialText` (TextMeshPro child), and `NextButton` (TextMeshPro button inside the panel). The optional `SkipTutorialButton` can be left empty.

## Controller Behaviour
- `TutorialController` drives a sequence of steps. Each step may display a prompt, wait for a gameplay action, or both.
- Prompts remain visible and move to different anchors depending on the step:
  - Intro/outro: centred with `Next` on the right edge of the panel.
  - Action steps: panel anchored near `Combo Preference` on the left, no `Next` button.
  - Combo preference reminder: panel centred under the dice.
  - Score breakdown: panel centred below the dice.
- The layout values are configurable via inspector fields (`introPrompt*`, `actionPrompt*`, `comboPrompt*`, etc.).
- Gameplay events are detected by piggybacking on existing buttons (`rollButton`, `submitComboButton`, backpack `Confirm`, dice lock buttons, score animation state). No changes to `BattleController` are required.
- When the tutorial ends, `RunLoader.StartRun()` loads `BattleScene` so the player immediately starts their real run.

## Tutorial Flow
1. **Welcome** – centred prompt; click `Next` to begin.
2. **Build Your Hand** – select up to five dice from the backpack (left prompt, waits for confirmation).
3. **Lock Dice** – click dice to keep them locked; locked dice won't change when rolling (left prompt, waits for dice lock action).
4. **Roll Your Dice** – press `Roll` to throw your selected dice (left prompt, waits for roll action).
5. **Check Combo Preference** – prompt centred below the dice; review the combo panel and click `Next` to continue.
6. **Roll Again** – left prompt explains that you can roll again and that locked dice stay fixed; the number of rolls and casts left can be seen on the right panel (waits for second roll action).
7. **Submit Your Hand** – lock the dice you want to use and press `Cast` to score the hand (left prompt, waits for cast action).
8. **Score Breakdown** – prompt centred below the dice; watch how the combo is scored (basic combo + dice effect + relic effect). Waits for score animation to complete, then displays for 3 seconds so players can read the message.
9. **Cooldown System** – left prompt explains that dice selected and casted need one round to cooldown before reuse; backpack is shown with dice in cooldown. Click `Next` to continue.
10. **Tutorial Complete** – centred prompt; click `Next` to start your first run in `BattleScene`.

That’s it—place the controller, ensure the panel/text/button exist, and the tutorial will run end-to-end. 🎲

