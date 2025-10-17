using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;

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
        public TMP_Text rollFeedbackText; // Shows dice status only
        public TMP_Text handCounterText;  // NEW: Display hand counter
        public TMP_Text deckStatusText;   // NEW: Display dice pool/deck status

        [Header("Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system
        public TMP_Text targetScoreText;    // Target score display

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 3;   // Max 3 rolls per hand
        public int targetScore = 1000;    // Target score to beat

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem; // Reference to cooldown system

        // Core components
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceMultiplierCalculator _multiplierCalculator;
        private DiceViewFactory _viewFactory;
        
        // Score tracking
        private int _totalScore = 0;

        // Current hand state
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();

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

            // Initialize target score display
            UpdateTargetScoreDisplay();

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
            UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nAll hands have been used.\n<color=#AAAAAA>Battle complete! Press Reset to start new battle cycle (for testing).</color>");
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
        _viewFactory.DestroyViews(_views);

        // Display full dice pool with cooldown status
        var allDice = cooldownSystem.GetAllDice();
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
        var availableDice = cooldownSystem.GetAvailableDice();
        var selectedDice = new List<BaseDice>();
        
        if (availableDice.Count > 0)
        {
            // Select up to 5 dice (or all available if less than 5)
            int diceToSelect = Mathf.Min(diceCount, availableDice.Count);
            
            // Randomly shuffle available dice for variety
            var shuffledDice = availableDice.OrderBy(x => UnityEngine.Random.value).ToList();
            
            for (int i = 0; i < diceToSelect; i++)
            {
                selectedDice.Add(shuffledDice[i]);
            }

            Debug.Log($"[BattleController] Selected {selectedDice.Count} special dice from pool:");
            foreach (var dice in selectedDice)
            {
                Debug.Log($"  Selected: {dice.diceName}");
            }

            // Register selection with cooldown system
            if (!cooldownSystem.SelectDiceForHand(selectedDice))
            {
                Debug.LogError("[BattleController] Failed to select dice for hand!");
                return;
            }
        }
        else
        {
            Debug.LogWarning("[BattleController] No special dice available from cooldown system!");
        }

        // Add selected special dice to hand
        _dice.AddRange(selectedDice);
        
        // Fill remaining slots with normal dice to reach 5 total
        int normalDiceNeeded = diceCount - _dice.Count;
        if (normalDiceNeeded > 0)
        {
            Debug.Log($"[BattleController] Filling {normalDiceNeeded} remaining slots with Normal Dice");
            for (int i = 0; i < normalDiceNeeded; i++)
            {
                var normalDice = new NormalDice();
                normalDice.diceName = $"Normal Dice #{i + 1}";
                _dice.Add(normalDice);
                Debug.Log($"  Added: {normalDice.diceName}");
            }
        }
        
        Debug.Log($"[BattleController] Final hand composition: {_dice.Count} dice total ({selectedDice.Count} special + {normalDiceNeeded} normal)");
        
        // Reset dice state for new hand
        foreach (var dice in _dice)
        {
            dice.ResetLockAndValue(); // This sets lastRollValue = 0 and isLocked = false
        }

        // Create views using factory (includes placeholders for empty slots)
        var newViews = _viewFactory.CreateViews(_dice, diceCount);
        _views.AddRange(newViews);

        // Start new hand in hand manager
        _handManager.StartHand();
        
        // Build feedback message
        string feedbackMsg = $"<size=110%><b>Hand {currentHand + 1}</b></size>\n\n";
        feedbackMsg += $"<color=#88FF88>Ready! {diceCount} dice prepared.</color>\n";
        if (selectedDice.Count < diceCount)
        {
            feedbackMsg += $"<color=#AAAAAA>({selectedDice.Count} special + {normalDiceNeeded} normal dice)</color>\n";
        }
        feedbackMsg += "\n<b>Instructions:</b>\n  • Roll the dice\n  • Click to lock dice you want to keep\n  • Submit when ready";
        
        UpdateFeedback(feedbackMsg);
        UpdateHandCounter(currentHand, remainingHands);
        UpdateDeckStatus(); // Update deck display
        
        Debug.Log($"[BattleController] Started hand with {diceCount} dice total");
    }

        void OnRollOnce()
        {
            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nAll hands have been used.\n<color=#AAAAAA>Battle complete! Press Reset to start new battle cycle (for testing).</color>");
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
            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("<color=#FF8888><b>No Hands Remaining!</b></color>\n\nAll hands have been used.\n<color=#AAAAAA>Battle complete! Press Reset to start new battle cycle (for testing).</color>");
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
            if (specialDiceOnly.Count > 0)
            {
                Debug.Log($"[BattleController] Passing {specialDiceOnly.Count} special dice to cooldown system");
                cooldownSystem.CompleteHand(specialDiceOnly);
            }
            else
            {
                Debug.Log("[BattleController] No special dice submitted, only normal dice used");
                cooldownSystem.CompleteHand(new List<BaseDice>()); // Complete hand without cooldown
            }
            _handManager.EndHand();
            
            // Update deck status after submitting
            UpdateDeckStatus();
            
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
                // Allow reset when no hands remain - this refreshes the dice pool for testing
                Debug.Log("[BattleController] No hands remaining - refreshing dice pool for testing...");
                cooldownSystem.RefreshDicePool();
                
                UpdateFeedback("<color=#88FF88><b>Dice Pool Refreshed!</b></color>\n\nAll dice are now available again.\nStarting new battle cycle...");
                
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
                    handCounterText.text = $"<color=#FF8888><b>No Hands Remaining!</b></color>\nHands: {current}/{current}\n<size=90%>(Battle complete - Press Reset to test again)</size>";
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

            var sb = new StringBuilder();
            sb.AppendLine("<b>DICE DECK</b>\n");

            var allDice = cooldownSystem.GetAllDice();
            var selectedDiceNames = _dice.Where(d => !(d is NormalDice)).Select(d => d.diceName).ToHashSet();

            // Display all dice in simple list with colored names by rarity
            foreach (var dice in allDice)
            {
                AppendDiceStatus(sb, dice, selectedDiceNames);
            }

            // Compact summary
            int available = allDice.Count(d => d.cooldownRemain == 0 && !selectedDiceNames.Contains(d.diceName));
            int selected = selectedDiceNames.Count;
            int onCooldown = allDice.Count(d => d.cooldownRemain > 0);

            sb.AppendLine($"\n<size=90%>Ready: {available} | Active: {selected} | CD: {onCooldown}</size>");

            deckStatusText.text = sb.ToString();
        }

        /// <summary>
        /// Helper method to append dice status line
        /// </summary>
        private void AppendDiceStatus(StringBuilder sb, BaseDice dice, HashSet<string> selectedDiceNames)
        {
            // Determine rarity color for dice name
            string rarityColor;
            switch (dice.tier)
            {
                case DiceTier.Legendary:
                    rarityColor = "#FFD700"; // Gold
                    break;
                case DiceTier.Rare:
                    rarityColor = "#9370DB"; // Purple
                    break;
                case DiceTier.Common:
                    rarityColor = "#90EE90"; // Light Green
                    break;
                default:
                    rarityColor = "#FFFFFF"; // White
                    break;
            }

            // Determine status
            string statusText;
            string statusColor;

            if (selectedDiceNames.Contains(dice.diceName))
            {
                statusText = "ACTIVE";
                statusColor = "#FFD700"; // Gold
            }
            else if (dice.cooldownRemain > 0)
            {
                statusText = $"CD({dice.cooldownRemain})";
                statusColor = "#FF6666"; // Red
            }
            else
            {
                statusText = "READY";
                statusColor = "#88FF88"; // Green
            }

            // Compact format: [Status] Dice Name
            sb.AppendLine($"<color={statusColor}>[{statusText}]</color> <color={rarityColor}>{dice.diceName}</color>");
        }

        /// <summary>
        /// Update target score display
        /// </summary>
        private void UpdateTargetScoreDisplay()
        {
            if (targetScoreText != null)
            {
                targetScoreText.text = $"<size=80%>Target Score</size>\n<size=150%><b>{targetScore}</b></size>";
            }
        }

        /// <summary>
        /// Evaluate if player passed target score with dramatic animation
        /// </summary>
        private System.Collections.IEnumerator EvaluateTargetScore()
        {
            // Wait for score animation to finish
            yield return new UnityEngine.WaitForSeconds(3f);

            int finalScore = scoreAnimator != null ? scoreAnimator.GetTotalScore() : _totalScore;
            bool passed = finalScore >= targetScore;

            Debug.Log($"[BattleController] Target Evaluation - Target: {targetScore}, Final: {finalScore}, Passed: {passed}");

            // Trigger pass/fail animation in ScoreAnimator
            if (scoreAnimator != null)
            {
                scoreAnimator.AnimateTargetEvaluation(finalScore, targetScore, passed);
            }
            else
            {
                // Fallback if no animator
                string resultMsg = passed 
                    ? "<color=#FFD700><b>TARGET PASSED!</b></color>\n\n" 
                    : "<color=#FF6666><b>TARGET FAILED</b></color>\n\n";
                resultMsg += $"Final Score: {finalScore}\nTarget: {targetScore}\n\n";
                resultMsg += "<color=#AAAAAA>Press Reset to start new battle cycle.</color>";
                UpdateFeedback(resultMsg);
            }
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
