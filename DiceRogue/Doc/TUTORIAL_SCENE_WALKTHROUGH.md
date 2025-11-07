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
2. **Build Your Hand** – select up to five dice in the backpack (prompt on the left).
3. **Roll Your Dice** – press `Roll` (left prompt).
4. **Lock Dice** – click dice to keep them (left prompt).
5. **Check Combo Preference** – prompt under the dice; click `Next` after reviewing the combo panel.
6. **Roll Again** – left prompt explains locked dice stay fixed; waits for a second roll.
7. **Submit Your Hand** – left prompt waits for `Submit`.
8. **Score Breakdown** – centred prompt waits for the score animation to finish.
9. **Tutorial Complete** – centred prompt; `Next` jumps to `BattleScene`.

That’s it—place the controller, ensure the panel/text/button exist, and the tutorial will run end-to-end. 🎲

