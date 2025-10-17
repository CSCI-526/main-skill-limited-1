using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;

namespace DiceGame
{
    /// <summary>
    /// Battle scene controller: 5 dice, max 3 rolls per hand, text feedback, lock logic
    /// Player can lock dice to form their combo and submit early
    /// Now integrated with CooldownSystem for 8-dice pool management
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("UI")]
        public Transform diceRowParent;   // Container for DiceView
        public GameObject diceViewPrefab; // Prefab (with DiceView component)
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;  // NEW: Submit current locked combo
        public TMP_Text rollFeedbackText;
        public TMP_Text handCounterText;  // NEW: Display hand counter

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 3;   // Max 3 rolls per hand

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem; // Reference to cooldown system

        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private int _rollsUsed = 0;
        private bool _isHandActive = false;

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
        
        Debug.Log("[BattleController] Battle scene initialized with CooldownSystem integration.");
    }

    /// <summary>
    /// Start a new hand by selecting available dice from the pool
    /// </summary>
    private void StartNewHand()
    {
        if (!cooldownSystem.HasAvailableDice())
        {
            UpdateFeedback("No dice available! Waiting for cooldown system refresh...");
            return;
        }

        // Clear previous dice and views
        _dice.Clear();
        foreach (var view in _views)
        {
            if (view != null && view.gameObject != null)
                Destroy(view.gameObject);
        }
        _views.Clear();

        // Get available dice and select first 5 (or all available if less than 5)
        var availableDice = cooldownSystem.GetAvailableDice();
        var selectedDice = new List<BaseDice>();
        
        // Select up to 5 dice (or all available if less than 5)
        int diceToSelect = Mathf.Min(diceCount, availableDice.Count);
        for (int i = 0; i < diceToSelect; i++)
        {
            selectedDice.Add(availableDice[i]);
        }

        // Register selection with cooldown system
        if (!cooldownSystem.SelectDiceForHand(selectedDice))
        {
            Debug.LogError("[BattleController] Failed to select dice for hand!");
            return;
        }

        // Set up dice for this hand
        _dice.AddRange(selectedDice);
        foreach (var dice in _dice)
        {
            var go = Instantiate(diceViewPrefab, diceRowParent);
            var view = go.GetComponent<DiceView>();
            view.Bind(dice);
            _views.Add(view);
        }

        _rollsUsed = 0;
        _isHandActive = true;
        
        var (current, remaining) = cooldownSystem.GetHandCounter();
        UpdateFeedback($"Hand {current + 1}: Ready! {_dice.Count} dice selected. Roll and lock the ones you want to keep!");
        UpdateHandCounter(current, remaining);
        
        Debug.Log($"[BattleController] Started hand with {_dice.Count} dice");
    }

        void OnRollOnce()
        {
            if (_rollsUsed >= maxRollsPerHand)
            {
                UpdateFeedback("Already reached maximum rolls per hand (3). Submit your combo or Reset.");
                Debug.LogWarning("[BattleController] Max rolls reached.");
                return;
            }

            _rollsUsed++;
            Debug.Log($"[BattleController] Rolling dice (Roll {_rollsUsed}/{maxRollsPerHand})");

            // Roll only unlocked dice
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (!d.isLocked)
                {
                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} rolled: {result}");
                }
                else
                {
                    Debug.Log($"  - {d.diceName} locked at: {d.lastRollValue}");
                }

                _views[i].Refresh();
            }

            // Build feedback
            var sb = new StringBuilder();
            sb.AppendLine($"Roll {_rollsUsed}/{maxRollsPerHand}:");
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                sb.AppendLine($"  {d.diceName}: {d.lastRollValue} {(d.isLocked ? "[LOCKED]" : "")}");
            }

            if (_rollsUsed < maxRollsPerHand)
                sb.AppendLine("\nLock dice you want to keep, then Roll again or Submit.");
            else
                sb.AppendLine("\nMax rolls reached! Submit your combo now.");

            UpdateFeedback(sb.ToString());
        }

        void OnSubmitCombo()
        {
            if (!_isHandActive)
            {
                Debug.LogWarning("[BattleController] No active hand to submit");
                return;
            }

            // Get the dice that were actually submitted (locked dice)
            var submittedDice = new List<BaseDice>();
            var submittedValues = new List<int>();
            
            foreach (var dice in _dice)
            {
                if (dice.isLocked && dice.lastRollValue > 0)
                {
                    submittedDice.Add(dice);
                    submittedValues.Add(dice.lastRollValue);
                }
            }
            
            if (submittedDice.Count == 0)
            {
                Debug.LogWarning("[BattleController] No locked dice to submit!");
                UpdateFeedback("No dice are locked! Lock some dice before submitting.");
                return;
            }

            Debug.Log("[BattleController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[BattleController] Rolls used: {_rollsUsed}/{maxRollsPerHand}");
            Debug.Log($"[BattleController] Submitted {submittedDice.Count} locked dice");
            
            // Display only the submitted (locked) dice values
            var sb = new StringBuilder();
            sb.AppendLine("=== SUBMITTED COMBO ===");
            sb.AppendLine($"Rolls used: {_rollsUsed}/{maxRollsPerHand}");
            sb.AppendLine($"Submitted {submittedDice.Count} dice:");
            
            foreach (var dice in submittedDice)
            {
                sb.AppendLine($"  {dice.diceName}: {dice.lastRollValue} [SUBMITTED]");
                Debug.Log($"  {dice.diceName}: {dice.lastRollValue} [SUBMITTED]");
            }
            
            sb.AppendLine($"\nSubmitted values: [{string.Join(", ", submittedValues)}]");

            // 调用新版 DiceHandEvaluator 进行识别和计分 (only on submitted dice)
            if (submittedValues.Count > 0)
            {
                float mult = 1f; // multiplier 可未来由 relic / buff 改变
                string combo = DiceHandEvaluator.Evaluate(submittedValues, out int score, mult);
                string summary = DiceHandEvaluator.BuildSummary(submittedValues, combo, score, mult);

                sb.AppendLine("\n=== COMBO RESULT ===");
                sb.AppendLine(summary);
            }
            else
            {
                sb.AppendLine("\nNo dice submitted!");
            }
            
            Debug.Log($"[BattleController] Submitted dice values: [{string.Join(", ", submittedValues)}]");
            Debug.Log("[BattleController] ============================");
            
            // Complete the hand in cooldown system with submitted dice
            cooldownSystem.CompleteHand(submittedDice);
            _isHandActive = false;
            
            // Check if we can start a new hand
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining > 0)
            {
                sb.AppendLine($"\nHand completed! {remaining} hands remaining.");
                sb.AppendLine("Starting next hand...");
                UpdateFeedback(sb.ToString());
                
                // Start next hand after a brief delay
                StartCoroutine(DelayedStartNewHand());
            }
            else
            {
                sb.AppendLine("\nAll hands completed! Dice pool refreshing...");
                UpdateFeedback(sb.ToString());
            }
        }

        /// <summary>
        /// Start a new hand after a brief delay
        /// </summary>
        private System.Collections.IEnumerator DelayedStartNewHand()
        {
            yield return new UnityEngine.WaitForSeconds(1f);
            StartNewHand();
        }


        void ResetForNewHand()
        {
            Debug.Log("[BattleController] Resetting for new hand...");
            _rollsUsed = 0;
            _isHandActive = false;
            
            // Reset dice states
            foreach (var d in _dice) d.ResetLockAndValue();
            foreach (var v in _views) v.Refresh();
            
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
                handCounterText.text = $"Hand {current + 1}/{current + remaining} ({remaining} remaining)";
            }
        }

        #region CooldownSystem Event Handlers

        /// <summary>
        /// Called when dice pool refreshes (all hands used)
        /// </summary>
        private void OnDicePoolRefresh()
        {
            Debug.Log("[BattleController] Dice pool refreshed - all hands completed!");
            UpdateFeedback("Dice pool refreshed! All dice are now available again.");
            
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
            
            // Update UI to show available dice count
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
