using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;
using UnityEngine.SceneManagement; // for fallback scene load
using DiceRogue.Boot;              // for RunLoader wipe

namespace DiceGame
{
    /// <summary>
    /// Battle scene controller: Orchestrates hand gameplay with cooldown system
    /// Delegates responsibilities to specialized components:
    /// - HandManager: hand state and roll counting
    /// - DiceEffectHandler: special dice effects
    /// - DiceMultiplierCalculator: damage multipliers
    /// - DiceViewFactory: UI view management
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("UI")]
        public Transform diceRowParent;   // Container for DiceView
        public GameObject diceViewPrefab; // Prefab (with DiceView component)
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;  // NEW: Submit current locked combo
        public Button continueButton;     // NEW: Continue to next level after evaluation
        public TMP_Text rollFeedbackText; // Shows dice status only
        public TMP_Text handCounterText;  // NEW: Display hand counter
        public TMP_Text deckStatusText;   // NEW: Display dice pool/deck status
    public Button backpackButton;     // Opens the backpack viewer during a hand
    public SimpleBackpackUI backpackUI; // Lightweight backpack overlay

        [Header("Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system
        public TMP_Text targetScoreText;    // Target score display

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 2;   // Max 2 rolls per hand
        public int baseTargetScore = 300; // Starting target score
    [Tooltip("Scene to load when failing to reach target score")]
    public string endSceneName = "EndScene";

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem; // Reference to cooldown system

        // Core components
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceMultiplierCalculator _multiplierCalculator;
        private DiceViewFactory _viewFactory;
        
        // Score tracking
        private int _totalScore = 0;
        private int _currentLevel = 1;
        private int _currentTargetScore;

        // Current hand state
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
    private readonly List<BaseDice> _selectedSpecialDice = new();
    private readonly List<BaseDice> _allDiceCache = new();
    private readonly List<BaseDice> _availableDiceCache = new();
    private bool _awaitingSelection;

        private void SetGameplayButtonsInteractable(bool enabled)
        {
            if (rollButton != null) rollButton.interactable = enabled;
            if (resetRollButton != null) resetRollButton.interactable = enabled;
            if (submitComboButton != null) submitComboButton.interactable = enabled;
            if (backpackButton != null) backpackButton.interactable = enabled && !_awaitingSelection;
        }

        void Start()
        {
            // Initialize cooldown system if not assigned
            if (cooldownSystem == null)
            {
                cooldownSystem = FindObjectOfType<CooldownSystem>();
                if (cooldownSystem == null)
                {
                    Debug.LogError("[BattleController] CooldownSystem not found! Please assign it in the inspector.");
                    return;
                }
            }

            // Initialize score animator if not assigned
            if (scoreAnimator == null)
            {
                scoreAnimator = FindObjectOfType<ScoreAnimator>();
                if (scoreAnimator == null)
                {
                    Debug.LogWarning("[BattleController] ScoreAnimator not found! Score animations will be disabled.");
                }
                else
                {
                    scoreAnimator.ResetTotalScore();
                }
            }

            // Initialize target score and level
            _currentLevel = 1;
            _currentTargetScore = baseTargetScore;
            UpdateTargetScoreDisplay();

            // Initialize and hide continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
                continueButton.onClick.AddListener(OnContinue);
            }

            // Initialize core components
            _handManager = new HandManager();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            _effectHandler = new DiceEffectHandler();
            _multiplierCalculator = new DiceMultiplierCalculator();
            _viewFactory = new DiceViewFactory(diceViewPrefab, diceRowParent);

            // Subscribe to cooldown system events
            cooldownSystem.OnDicePoolRefresh += OnDicePoolRefresh;
            cooldownSystem.OnHandCounterUpdate += OnHandCounterUpdate;
            cooldownSystem.OnAvailableDiceChanged += OnAvailableDiceChanged;

            // Set up UI
            rollButton.onClick.AddListener(OnRollOnce);
            resetRollButton.onClick.AddListener(ResetForNewHand);
            submitComboButton.onClick.AddListener(OnSubmitCombo);
            if (backpackButton != null)
            {
                backpackButton.onClick.AddListener(OpenBackpackViewer);
            }
            
            // Start first hand
            StartNewHand();
            
            Debug.Log("[BattleController] Battle scene initialized with decoupled components.");
        }

    /// <summary>
    /// Start a new hand by selecting available dice from the pool
    /// </summary>
    private void StartNewHand()
    {
        // Check if hands remain (safety check before pool refresh)
        var (handCount, handRemaining) = cooldownSystem.GetHandCounter();
        if (handRemaining <= 0 && handCount > 0) // Don't block the very first hand
        {
            Debug.LogWarning("[BattleController] Cannot start new hand - no hands remaining. Battle complete!");
            UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nAll hands have been used.\n<color=#AAAAAA>Battle complete! Press Continue to next level.</color>");
            UpdateHandCounter(handCount, handRemaining);
            return;
        }

        // Advance cooldowns before starting new hand (except for the very first hand)
        if (handCount > 0) // Only advance cooldowns if this is not the first hand
        {
            cooldownSystem.AdvanceCooldowns();
        }
        
    // Clear previous dice and views using factory
    _dice.Clear();
    _selectedSpecialDice.Clear();
    _viewFactory.DestroyViews(_views);

        // Display full dice pool with cooldown status
    _allDiceCache.Clear();
    _allDiceCache.AddRange(cooldownSystem.GetAllDice());
    var allDice = _allDiceCache;
        var (currentHand, remainingHands) = cooldownSystem.GetHandCounter();
        Debug.Log($"=== HAND {currentHand + 1} - DICE POOL STATUS ===");
        Debug.Log($"Hand {currentHand + 1}/{currentHand + remainingHands} ({remainingHands} remaining)");
        Debug.Log("Full Dice Pool:");
        foreach (var dice in allDice)
        {
            string status = dice.cooldownRemain > 0 ? $"COOLDOWN({dice.cooldownRemain})" : "AVAILABLE";
            Debug.Log($"  {dice.diceName}: {dice.tier}, cost={dice.cost}, {status}");
        }
        Debug.Log("========================================");

        // Get available dice from cooldown system (after advancing cooldowns)
        _availableDiceCache.Clear();
        _availableDiceCache.AddRange(cooldownSystem.GetAvailableDice());

        // Show selection UI and wait for player
        _awaitingSelection = true;
        OpenSelectionOverlay(currentHand + 1);
    return;
    }

    private void OpenSelectionOverlay(int displayHandNumber)
    {
        if (backpackUI == null)
        {
            Debug.LogWarning("[BattleController] SimpleBackpackUI not assigned. Falling back to automatic selection.");
            AutoSelectFallback("Backpack UI missing");
            return;
        }

        SetGameplayButtonsInteractable(false);

        UpdateFeedback($"<size=110%><b>Select Dice for Hand {displayHandNumber}</b></size>\n\n" +
                       $"Pick up to {diceCount} dice. You can confirm with fewer if needed.");

        backpackUI.OpenForSelection(_allDiceCache, _availableDiceCache, diceCount, OnSelectionConfirmed);
    }

    private void OnSelectionConfirmed(List<BaseDice> selection)
    {
        var sanitized = new List<BaseDice>();
        if (selection != null)
        {
            foreach (var dice in selection)
            {
                if (dice != null && (_availableDiceCache.Contains(dice) || sanitized.Contains(dice)))
                {
                    sanitized.Add(dice);
                }
            }
        }

        if (sanitized.Count > diceCount)
        {
            sanitized = sanitized.Take(diceCount).ToList();
        }

        if (!cooldownSystem.SelectDiceForHand(sanitized))
        {
            Debug.LogWarning("[BattleController] Backpack selection failed validation. Falling back to automatic choice.");
            AutoSelectFallback("Selection validation failed");
            return;
        }

        CompleteHandSetupAfterSelection(sanitized);
        SetGameplayButtonsInteractable(true);
    }

    private void AutoSelectFallback(string reason)
    {
        var fallback = cooldownSystem.GetAvailableDice().Take(diceCount).ToList();
        cooldownSystem.SelectDiceForHand(fallback);
        CompleteHandSetupAfterSelection(fallback);
        SetGameplayButtonsInteractable(true);

        UpdateFeedback($"<size=110%><b>Auto Selection</b></size>\n\n" +
                       "Couldn't open the bag, so we picked for you.");
        Debug.LogWarning($"[BattleController] Auto-selected {fallback.Count} dice. Reason: {reason}");
    }

    private void CompleteHandSetupAfterSelection(List<BaseDice> selectedDice)
    {
        if (selectedDice == null)
        {
            selectedDice = new List<BaseDice>();
        }

        _selectedSpecialDice.Clear();
        _selectedSpecialDice.AddRange(selectedDice);

        if (_awaitingSelection)
        {
            _awaitingSelection = false;
        }

        CompleteHandSetup();
    }

    private void CompleteHandSetup()
    {
        var (currentHand, remainingHands) = cooldownSystem.GetHandCounter();

        // Add selected special dice to hand
        _dice.AddRange(_selectedSpecialDice);

        // Fill remaining slots with normal dice up to diceCount
        int normalDiceNeeded = Mathf.Max(0, diceCount - _dice.Count);
        for (int i = 0; i < normalDiceNeeded; i++)
        {
            var normalDice = new NormalDice { diceName = $"Normal Dice #{i + 1}" };
            _dice.Add(normalDice);
            Debug.Log($"  Added: {normalDice.diceName}");
        }

        Debug.Log($"[BattleController] Final hand composition: {_dice.Count} dice total ({_selectedSpecialDice.Count} special + {normalDiceNeeded} normal)");

        foreach (var dice in _dice)
        {
            dice.ResetLockAndValue();
        }

        var newViews = _viewFactory.CreateViews(_dice, diceCount);
        _views.AddRange(newViews);

        _handManager.StartHand();

        string feedbackMsg = $"<size=110%><b>Hand {currentHand + 1}</b></size>\n\n";
        feedbackMsg += $"<color=#88FF88>Ready! {_dice.Count} dice prepared.</color>\n";
        if (_selectedSpecialDice.Count < diceCount)
        {
            feedbackMsg += $"<color=#AAAAAA>({_selectedSpecialDice.Count} special + {normalDiceNeeded} normal dice)</color>\n";
        }
        feedbackMsg += "\n<b>Instructions:</b>\n  • Roll the dice\n  • Click to lock dice you want to keep\n  • Submit when ready";

        UpdateFeedback(feedbackMsg);
        UpdateHandCounter(currentHand, remainingHands);
        UpdateDeckStatus();

        Debug.Log($"[BattleController] Started hand with {_dice.Count} dice total");
    }

        void OnRollOnce()
        {
            if (_awaitingSelection)
            {
                UpdateFeedback("<color=#FFD070><b>Select your dice from the backpack first!</b></color>");
                return;
            }

            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nRound complete. Evaluating...");
                Debug.LogWarning("[BattleController] Cannot roll - no hands remaining.");
                return;
            }

            // Check if we can roll using HandManager
            if (!_handManager.CanRoll)
            {
                UpdateFeedback($"Already reached maximum rolls per hand ({maxRollsPerHand}). Submit your combo or Reset.");
                Debug.LogWarning("[BattleController] Max rolls reached.");
                return;
            }

            // Increment roll counter
            int rollNumber = _handManager.IncrementRoll();
            Debug.Log($"[BattleController] Rolling dice (Roll {rollNumber}/{maxRollsPerHand})");

            // Roll only unlocked dice (skip placeholder dice)
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (!d.isLocked && d.tier != DiceTier.Filler) // Don't roll placeholder dice
                {
                    // Setup PlusOne dice context before rolling
                    _effectHandler.SetupPlusOneDice(d, i, _dice);

                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} rolled: {result}");
                }
                else if (d.isLocked)
                {
                    Debug.Log($"  - {d.diceName} locked at: {d.lastRollValue}");
                }
            }

            // Apply all special dice effects using effect handler
            _effectHandler.ApplyRollEffects(_dice);

            // Refresh all views using factory
            _viewFactory.RefreshViews(_views);
            
            // Update deck status after roll
            UpdateDeckStatus();

            // Build feedback - ONLY show dice status, no score calculation
            var sb = new StringBuilder();
            sb.AppendLine($"<size=110%><b>Roll {rollNumber}/{maxRollsPerHand}</b></size>\n");
            sb.AppendLine("<b>Dice Status:</b>");
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (d.tier != DiceTier.Filler) // Only show real dice
                {
                    string status = d.isLocked ? "<color=#FFD700>[LOCKED]</color>" : "";
                    sb.AppendLine($"  • <b>{d.diceName}:</b> {d.lastRollValue} {status}");
                }
            }

            sb.AppendLine(); // Empty line
            if (rollNumber < maxRollsPerHand)
                sb.AppendLine("<color=#88FF88>Click dice to lock/unlock, then Roll again or Submit.</color>");
            else
                sb.AppendLine("<color=#FF8888>Max rolls reached! Submit your combo now.</color>");

            UpdateFeedback(sb.ToString());
        }

        void OnSubmitCombo()
        {
            if (_awaitingSelection)
            {
                UpdateFeedback("<color=#FFD070><b>Select dice for the new hand first!</b></color>");
                return;
            }

            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nRound complete. Evaluating...");
                Debug.LogWarning("[BattleController] Cannot submit - no hands remaining.");
                return;
            }

            // Validate using HandManager
            if (!_handManager.CanSubmit(_dice))
            {
                UpdateFeedback("<color=#FF8888><b>No dice are locked!</b></color>\n\nLock some dice before submitting.");
                return;
            }

            // Get submitted dice using HandManager
            var submittedDice = _handManager.GetSubmittedDice(_dice);
            var submittedValues = _handManager.GetSubmittedValues(submittedDice);

            Debug.Log("[BattleController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[BattleController] Rolls used: {_handManager.RollsUsed}/{maxRollsPerHand}");
            Debug.Log($"[BattleController] Submitted {submittedDice.Count} locked dice");
            
            // Update feedback to show submitted dice (DICE STATUS ONLY)
            var sb = new StringBuilder();
            sb.AppendLine("<size=110%><b>COMBO SUBMITTED</b></size>\n");
            sb.AppendLine($"<color=#AAAAAA>Rolls used: {_handManager.RollsUsed}/{maxRollsPerHand}</color>");
            sb.AppendLine($"<color=#AAAAAA>Submitted {submittedDice.Count} dice:</color>\n");
            
            foreach (var dice in submittedDice)
            {
                sb.AppendLine($"  • <b>{dice.diceName}:</b> {dice.lastRollValue} <color=#FFD700>[SUBMITTED]</color>");
                Debug.Log($"  {dice.diceName}: {dice.lastRollValue} [SUBMITTED]");
            }
            
            UpdateFeedback(sb.ToString());

            // Calculate multiplier using multiplier calculator
            float mult = _multiplierCalculator.Calculate(submittedDice, submittedValues);

            // Evaluate combo and trigger animated score display
            if (submittedValues.Count > 0)
            {
                string combo = DiceHandEvaluator.Evaluate(submittedValues, out int finalScore, out float comboMult, mult);
                
                // Calculate breakdown for animation
                int diceSum = submittedValues.Sum();
                int baseScore = CalculateBaseScore(combo);
                
                Debug.Log($"[BattleController] Combo: {combo}, Base: {baseScore}, Sum: {diceSum}, ComboMult: {comboMult}, DiceMult: {mult}, Final: {finalScore}");
                
                // Trigger Balatro-style animated score display
                if (scoreAnimator != null)
                {
                    scoreAnimator.AnimateScore(submittedValues, combo, baseScore, diceSum, comboMult, mult, finalScore);
                }
                
                _totalScore += finalScore;
            }
            else
            {
                UpdateFeedback(sb.ToString() + "\n<color=#FF8888>No dice submitted!</color>");
            }
            
            Debug.Log($"[BattleController] Submitted dice values: [{string.Join(", ", submittedValues)}]");
            Debug.Log("[BattleController] ============================");
            
            // Complete the hand in cooldown system with submitted dice
            // Filter out NormalDice (temporary fillers) - only submit special dice from the pool
            var specialDiceOnly = submittedDice.Where(d => !(d is NormalDice)).ToList();
            cooldownSystem.CompleteHand(specialDiceOnly);
            _handManager.EndHand();
            
            // Update deck status after submitting
            UpdateDeckStatus();

            if (cooldownSystem.GetHandCounter().remaining > 0)
            {
                StartCoroutine(DelayedStartNewHand());
            }
            
            // Check if we can start a new hand
            var (currentHand, handsRemaining) = cooldownSystem.GetHandCounter();
            if (handsRemaining > 0)
            {
                // Start next hand after animation completes (brief delay)
                StartCoroutine(DelayedStartNewHand());
            }
            else
            {
                Debug.Log("[BattleController] All hands completed! Evaluating target score...");
                // Update UI to show battle is complete
                UpdateHandCounter(currentHand, handsRemaining);
                
                // Trigger target score evaluation animation
                StartCoroutine(EvaluateTargetScore());
            }
        }

        /// <summary>
        /// Start a new hand after a brief delay
        /// </summary>
        private System.Collections.IEnumerator DelayedStartNewHand()
        {
            yield return new UnityEngine.WaitForSeconds(2.5f); // Wait for animation to complete
            StartNewHand();
        }

        /// <summary>
        /// Helper method to extract base score from combo name
        /// This matches the values in DiceHandEvaluator
        /// </summary>
        private int CalculateBaseScore(string comboName)
        {
            if (comboName.Contains("Five of a Kind") || comboName.Contains("Yahtzee")) return 180;
            if (comboName.Contains("Four of a Kind")) return 120;
            if (comboName.Contains("Full House")) return 100;
            if (comboName.Contains("Large Straight")) return 90;
            if (comboName.Contains("Small Straight")) return 75;
            if (comboName.Contains("Sum Jackpot")) return 70;
            if (comboName.Contains("Three of a Kind")) return 60;
            if (comboName.Contains("Two Pair")) return 45;
            if (comboName.Contains("All Even") || comboName.Contains("All Odd")) return 35;
            if (comboName.Contains("One Pair")) return 30;
            if (comboName.Contains("Low Roll") || comboName.Contains("High Roll")) return 25;
            return 10; // No Combo (Bust)
        }


        void ResetForNewHand()
        {
            Debug.Log("[BattleController] Resetting for new hand...");
            
            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                // No hands remain - reset everything to level 1 (game over / try again)
                Debug.Log("[BattleController] No hands remaining - resetting to Level 1...");
                
                // Hide continue button if visible
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(false);
                }
                
                // Reset to level 1
                _currentLevel = 1;
                _currentTargetScore = baseTargetScore;
                
                // Reset total score
                _totalScore = 0;
                if (scoreAnimator != null)
                {
                    scoreAnimator.ResetTotalScore();
                }
                
                // Refresh dice pool and hand counter
                cooldownSystem.RefreshDicePool();
                
                // Update displays
                UpdateTargetScoreDisplay();
                UpdateFeedback("<color=#88FF88><b>Starting Fresh!</b></color>\n\nReturning to Level 1.\nTarget: " + _currentTargetScore + "\n\n<color=#AAAAAA>Good luck!</color>");
                
                // Start a new hand after refresh
                StartNewHand();
                return;
            }
            
            // Normal reset behavior during active hands
            // Reset hand state using HandManager
            _handManager.Reset();
            
            // Reset dice states
            foreach (var d in _dice) d.ResetLockAndValue();
            
            // Refresh views using factory
            _viewFactory.RefreshViews(_views);
            
            // Start a new hand
            StartNewHand();
            
            Debug.Log("[BattleController] Hand reset complete.");
        }

        void UpdateFeedback(string msg)
        {
            if (rollFeedbackText != null) rollFeedbackText.text = msg;
            else Debug.Log(msg);
        }

        /// <summary>
        /// Update hand counter display
        /// </summary>
        private void UpdateHandCounter(int current, int remaining)
        {
            if (handCounterText != null)
            {
                if (remaining <= 0)
                {
                    handCounterText.text = $"Hands: {current}/{current}";
                }
                else
                {
                    handCounterText.text = $"Hand {current + 1}/{current + remaining} ({remaining} remaining)";
                }
            }
        }

        /// <summary>
        /// Update deck status display showing all dice and their states
        /// </summary>
        private void UpdateDeckStatus()
        {
            if (deckStatusText == null) return;

            int totalDice = _allDiceCache.Count;
            int ready = Mathf.Max(0, _availableDiceCache.Count - _selectedSpecialDice.Count);
            ready = Mathf.Min(ready, Mathf.Max(0, totalDice - _selectedSpecialDice.Count));
            int active = _selectedSpecialDice.Count;
            int onCooldown = Mathf.Max(0, totalDice - ready - active);

            var sb = new StringBuilder();
            sb.AppendLine("<size=95%><b>Backpack Summary</b></size>");
            sb.AppendLine($"Ready: {ready}");
            sb.AppendLine($"Active: {active}");
            sb.AppendLine($"Cooldown: {onCooldown}");
            sb.AppendLine("<size=75%><color=#AAAAAA>Use the Backpack button to pick dice.</color></size>");

            deckStatusText.text = sb.ToString();
        }

        /// <summary>
        /// Update target score display
        /// </summary>
        private void UpdateTargetScoreDisplay()
        {
            if (targetScoreText != null)
            {
                targetScoreText.text = $"<size=70%>Target Score</size>\n<size=150%><b>{_currentTargetScore}</b></size>\n<size=80%><color=#AAAAAA>Level {_currentLevel}</color></size>";
            }
        }

        /// <summary>
        /// Continue to next level - reset game state and increase target score
        /// </summary>
        private void OnContinue()
        {
            Debug.Log($"[BattleController] Continuing to next level from Level {_currentLevel}...");

            // Hide continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            // Increase level
            _currentLevel++;

            // Calculate new target score based on level
            // Progressive increase: +300, +400, +500, +600, +700, ...
            // Formula: increase = 200 + n*100 where n = level number
            // Level 1: 1000 (base)
            // Level 2: 1000 + 300 = 1300
            // Level 3: 1300 + 400 = 1700
            // Level 4: 1700 + 500 = 2200
            // Level 5: 2200 + 600 = 2800
            // Level n: previous + (200 + n*100)
            _currentTargetScore = baseTargetScore;
            for (int i = 0; i < _currentLevel - 1; i++)
            {
                int increase = 300 + i * 100; // 300, 400, 500, 600, 700, ...
                _currentTargetScore += increase;
            }

            Debug.Log($"[BattleController] Level {_currentLevel} - New target score: {_currentTargetScore}");

            // Reset total score
            _totalScore = 0;
            if (scoreAnimator != null)
            {
                scoreAnimator.ResetTotalScore();
            }

            // Reset dice pool and hand counter
            cooldownSystem.RefreshDicePool();

            // Update displays
            UpdateTargetScoreDisplay();
            UpdateFeedback($"<size=120%><b>Level {_currentLevel} Start!</b></size>\n\n<color=#88FF88>New target: {_currentTargetScore}</color>\n\n<color=#AAAAAA>All dice and hands have been reset.\nGood luck!</color>");

            // Start first hand of new level
            StartNewHand();
        }

        /// <summary>
        /// Evaluate if player passed target score with dramatic animation
        /// </summary>
        private System.Collections.IEnumerator EvaluateTargetScore()
        {
            // Wait for the score counting animation to complete to keep the flow fluent
            if (scoreAnimator != null)
            {
                yield return scoreAnimator.WaitForIdle();
            }

            // Small extra pause to let the final value linger
            yield return new UnityEngine.WaitForSeconds(0.6f);

            int finalScore = scoreAnimator != null ? scoreAnimator.GetTotalScore() : _totalScore;
            bool passed = finalScore >= _currentTargetScore;

            Debug.Log($"[BattleController] Target Evaluation - Target: {_currentTargetScore}, Final: {finalScore}, Passed: {passed}");

            if (!passed)
            {
                if (scoreAnimator != null)
                {
                    scoreAnimator.AnimateTargetEvaluation(finalScore, _currentTargetScore, false);
                    yield return scoreAnimator.WaitForIdle();
                }
                else
                {
                    UpdateFeedback("<color=#FF5555><b>Failed to reach target.</b></color>\n\nPreparing results...");
                    yield return new UnityEngine.WaitForSeconds(2.5f);
                }

                // Transition to EndScene with wipe after showing failure animation.
                EndSceneData.Set(finalScore, _currentTargetScore, false);

                if (Time.timeScale == 0f)
                {
                    Debug.LogWarning("[BattleController] Time.timeScale was 0. Resetting to 1 before loading EndScene.");
                    Time.timeScale = 1f;
                }

                if (!Application.CanStreamedLevelBeLoaded(endSceneName))
                {
                    Debug.LogError($"[BattleController] EndScene '{endSceneName}' not found in Build Settings. Add it via File > Build Settings.");
                }

                var loader = RunLoader.Instance;
                if (loader != null)
                {
                    Debug.Log("[BattleController] FAIL → Using RunLoader wipe to EndScene.");
                    loader.GoToScene(endSceneName);
                }
                else
                {
                    Debug.Log("[BattleController] FAIL → RunLoader not found; using direct SceneManager.LoadScene.");
                    SceneManager.LoadScene(endSceneName);
                }
                yield break;
            }

            // PASS: keep your existing battle-side animation and then show Continue
            if (scoreAnimator != null)
            {
                scoreAnimator.AnimateTargetEvaluation(finalScore, _currentTargetScore, true);
            }
            else
            {
                UpdateFeedback("<color=#FFD700><b>TARGET PASSED!</b></color>\n\n" +
                               $"Final Score: {finalScore}\nTarget: {_currentTargetScore}");
            }

            // After success animation, reveal Continue
            yield return new UnityEngine.WaitForSeconds(2.5f);
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        #region CooldownSystem Event Handlers

        /// <summary>
        /// Called when dice pool refreshes (manual refresh via Reset button)
        /// </summary>
        private void OnDicePoolRefresh()
        {
            Debug.Log("[BattleController] Dice pool refreshed - starting new battle cycle!");
            UpdateFeedback("<color=#88FF88><b>Dice Pool Refreshed!</b></color>\n\nAll dice are now available again.\nStarting new battle cycle...");
            UpdateDeckStatus(); // Update deck display
            
            // Start a new hand with refreshed dice
            StartNewHand();
        }

        /// <summary>
        /// Called when hand counter updates
        /// </summary>
        private void OnHandCounterUpdate(int current, int remaining)
        {
            Debug.Log($"[BattleController] Hand counter updated: {current}/{current + remaining}");
            UpdateHandCounter(current, remaining);
        }

        /// <summary>
        /// Called when available dice list changes
        /// </summary>
        private void OnAvailableDiceChanged(List<BaseDice> availableDice)
        {
            Debug.Log($"[BattleController] Available dice changed: {availableDice.Count} dice available");

            _availableDiceCache.Clear();
            if (availableDice != null)
            {
                _availableDiceCache.AddRange(availableDice);
            }

            _allDiceCache.Clear();
            _allDiceCache.AddRange(cooldownSystem.GetAllDice());
            
            // Update deck status display
            UpdateDeckStatus();
            
            // Log details
            var sb = new StringBuilder();
            sb.AppendLine($"Available dice: {availableDice.Count}/8");
            sb.AppendLine("Dice pool:");
            foreach (var dice in availableDice)
            {
                sb.AppendLine($"  - {dice.diceName} ({dice.tier}, cost: {dice.cost})");
            }
            
            if (handCounterText != null)
            {
                var (current, remaining) = cooldownSystem.GetHandCounter();
                sb.AppendLine($"\nHands: {current + 1}/{current + remaining} ({remaining} remaining)");
            }
            
            Debug.Log(sb.ToString());
        }

        #endregion

        private void OpenBackpackViewer()
        {
            if (_awaitingSelection)
            {
                Debug.Log("[BattleController] Backpack already open for selection.");
                return;
            }

            if (backpackUI == null)
            {
                Debug.LogWarning("[BattleController] SimpleBackpackUI not assigned. Cannot open viewer.");
                return;
            }

            _allDiceCache.Clear();
            _allDiceCache.AddRange(cooldownSystem.GetAllDice());
            _availableDiceCache.Clear();
            _availableDiceCache.AddRange(cooldownSystem.GetAvailableDice());

            backpackUI.OpenViewer(_allDiceCache, _availableDiceCache);
        }

        /// <summary>
        /// Clean up event subscriptions
        /// </summary>
        void OnDestroy()
        {
            if (cooldownSystem != null)
            {
                cooldownSystem.OnDicePoolRefresh -= OnDicePoolRefresh;
                cooldownSystem.OnHandCounterUpdate -= OnHandCounterUpdate;
                cooldownSystem.OnAvailableDiceChanged -= OnAvailableDiceChanged;
            }
        }
    }
}
