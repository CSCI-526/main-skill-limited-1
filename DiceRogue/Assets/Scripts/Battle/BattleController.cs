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
<<<<<<< Updated upstream
    /// Battle scene controller: 5 dice, max 3 rolls per hand, text feedback, lock logic
    /// Player can lock dice to form their combo and submit early
    /// Now integrated with CooldownSystem for 8-dice pool management
=======
    /// Orchestrates battle hand flow with cooldown system.
    /// Keeps your decoupled components, adds:
    /// - Top-right total score UI
    /// - Result modal (panel) after Submit; player clicks Close to proceed
>>>>>>> Stashed changes
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("UI - Main")]
        public Transform diceRowParent;         // DiceView parent
        public GameObject diceViewPrefab;       // DiceView prefab
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;
        public TMP_Text rollFeedbackText;       // big text area
        public TMP_Text handCounterText;        // hand counter (existing)

        [Header("UI - Score (Top-Right)")]
        public TMP_Text totalScoreText;         // "Score: 0" (常驻总分)

        [Header("UI - Result Modal")]
        public GameObject resultPanel;          // 弹窗根节点（默认Inactive）
        public TMP_Text resultText;             // 弹窗里的文本
        public Button resultCloseButton;        // 关闭按钮

        [Header("Config")]
        public int diceCount = 5;
        public int maxRollsPerHand = 3;

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem;

<<<<<<< Updated upstream
=======
        // Core components (existing design)
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceMultiplierCalculator _multiplierCalculator;
        private DiceViewFactory _viewFactory;

        // Current hand state
>>>>>>> Stashed changes
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private int _rollsUsed = 0;
        private bool _isHandActive = false;

        // Score state
        private int _totalScore = 0;

        // Last submit cache (用于 Close 时推进回合)
        private List<BaseDice> _lastSubmittedDice = null;

        void Start()
        {
            // Ensure cooldown system
            if (cooldownSystem == null)
            {
                cooldownSystem = FindObjectOfType<CooldownSystem>();
                if (cooldownSystem == null)
                {
                    Debug.LogError("[BattleController] CooldownSystem not found! Assign it in inspector.");
                    return;
                }
            }

<<<<<<< Updated upstream
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
=======
            // Init core components
            _handManager = new HandManager();
            _handManager.SetMaxRolls(maxRollsPerHand);

            _effectHandler = new DiceEffectHandler();
            _multiplierCalculator = new DiceMultiplierCalculator();
            _viewFactory = new DiceViewFactory(diceViewPrefab, diceRowParent);

            // Subscribe events
            cooldownSystem.OnDicePoolRefresh += OnDicePoolRefresh;
            cooldownSystem.OnHandCounterUpdate += OnHandCounterUpdate;
            cooldownSystem.OnAvailableDiceChanged += OnAvailableDiceChanged;

            // Wire main buttons
            rollButton.onClick.AddListener(OnRollOnce);
            resetRollButton.onClick.AddListener(ResetForNewHand);
            submitComboButton.onClick.AddListener(OnSubmitCombo);

            // Wire modal close
            if (resultCloseButton != null)
                resultCloseButton.onClick.AddListener(OnResultCloseClicked);

            // Init UI states
            _totalScore = 0;
            UpdateScoreUI();
            if (resultPanel != null) resultPanel.SetActive(false);
            SetMainButtonsInteractable(true);

            // Start first hand
            StartNewHand();
            Debug.Log("[BattleController] Initialized.");
        }
>>>>>>> Stashed changes

        /// <summary> Start a new hand by selecting available dice from cooldown pool. </summary>
        private void StartNewHand()
        {
<<<<<<< Updated upstream
            cooldownSystem.AdvanceCooldowns();
        }
        
        // Clear previous dice and views
        _dice.Clear();
        foreach (var view in _views)
        {
            if (view != null && view.gameObject != null)
                Destroy(view.gameObject);
        }
        _views.Clear();
=======
            var (handCount, handRemaining) = cooldownSystem.GetHandCounter();
            if (handCount > 0) cooldownSystem.AdvanceCooldowns();
>>>>>>> Stashed changes

            _dice.Clear();
            _viewFactory.DestroyViews(_views);

            var allDice = cooldownSystem.GetAllDice();
            var (currentHand, remainingHands) = cooldownSystem.GetHandCounter();
            Debug.Log($"=== HAND {currentHand + 1} ===  Remaining: {remainingHands}");

            var availableDice = cooldownSystem.GetAvailableDice();
            var selectedDice = new List<BaseDice>();

            if (availableDice.Count > 0)
            {
                int toSelect = Mathf.Min(diceCount, availableDice.Count);
                var shuffled = availableDice.OrderBy(_ => UnityEngine.Random.value).ToList();
                for (int i = 0; i < toSelect; i++) selectedDice.Add(shuffled[i]);

                if (!cooldownSystem.SelectDiceForHand(selectedDice))
                {
                    Debug.LogError("[BattleController] SelectDiceForHand failed.");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[BattleController] No dice available from cooldown system.");
            }

            _dice.AddRange(selectedDice);
            foreach (var d in _dice) d.ResetLockAndValue();

            var newViews = _viewFactory.CreateViews(_dice, diceCount);
            _views.AddRange(newViews);

            _handManager.StartHand();
            UpdateFeedback($"Hand {currentHand + 1}: {_dice.Count} dice ready. Roll & Lock, then Submit.");
            UpdateHandCounter(currentHand, remainingHands);
        }

<<<<<<< Updated upstream
        // Set up dice for this hand (even if fewer than 5)
        _dice.AddRange(selectedDice);
        
        // Create UI views for selected dice and reset their values to "-"
        foreach (var dice in _dice)
        {
            // Reset dice state for new hand
            dice.ResetLockAndValue(); // This sets lastRollValue = 0 and isLocked = false
            
            var go = Instantiate(diceViewPrefab, diceRowParent);
            var view = go.GetComponent<DiceView>();
            view.Bind(dice);
            _views.Add(view);
        }

        // If we have fewer than 5 dice, create placeholder views with "-"
        while (_views.Count < diceCount)
        {
            var go = Instantiate(diceViewPrefab, diceRowParent);
            var view = go.GetComponent<DiceView>();
            // Create a placeholder dice for display
            var placeholderDice = new NormalDice
            {
                diceName = $"Empty_{_views.Count + 1}",
                tier = DiceTier.Filler,
                cost = 0,
                lastRollValue = 0,
                isLocked = false
            };
            view.Bind(placeholderDice);
            view.SetDisplayValue("-"); // Show "-" instead of 0
            _views.Add(view);
        }

        _rollsUsed = 0;
        _isHandActive = true;
        
        UpdateFeedback($"Hand {currentHand + 1}: Ready! {_dice.Count} dice selected. Roll and lock the ones you want to keep!");
        UpdateHandCounter(currentHand, remainingHands);
        
        Debug.Log($"[BattleController] Started hand with {_dice.Count} available dice, {diceCount - _dice.Count} empty slots");
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
=======
        private void OnRollOnce()
        {
            if (!_handManager.CanRoll)
            {
                UpdateFeedback($"Already reached maximum rolls per hand ({maxRollsPerHand}). Submit or Reset.");
                return;
            }

            int rollNumber = _handManager.IncrementRoll();
>>>>>>> Stashed changes

            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (!d.isLocked && d.tier != DiceTier.Filler)
                {
<<<<<<< Updated upstream
                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} rolled: {result}");
                }
                else if (d.isLocked)
                {
                    Debug.Log($"  - {d.diceName} locked at: {d.lastRollValue}");
=======
                    _effectHandler.SetupPlusOneDice(d, i, _dice);
                    d.Roll();
>>>>>>> Stashed changes
                }

                _views[i].Refresh();
            }

<<<<<<< Updated upstream
            // Build feedback
            var sb = new StringBuilder();
            sb.AppendLine($"Roll {_rollsUsed}/{maxRollsPerHand}:");
            for (int i = 0; i < _dice.Count; i++)
=======
            _effectHandler.ApplyRollEffects(_dice);
            _viewFactory.RefreshViews(_views);

            var sb = new StringBuilder();
            sb.AppendLine($"Roll {rollNumber}/{maxRollsPerHand}:");
            foreach (var d in _dice)
>>>>>>> Stashed changes
            {
                if (d.tier != DiceTier.Filler)
                    sb.AppendLine($"  {d.diceName}: {d.lastRollValue} {(d.isLocked ? "[LOCKED]" : "")}");
            }
<<<<<<< Updated upstream

            if (_rollsUsed < maxRollsPerHand)
                sb.AppendLine("\nLock dice you want to keep, then Roll again or Submit.");
            else
                sb.AppendLine("\nMax rolls reached! Submit your combo now.");

=======
            sb.AppendLine(rollNumber < maxRollsPerHand
                ? "\nLock dice you want to keep, then Roll again or Submit."
                : "\nMax rolls reached! Submit your combo now.");
>>>>>>> Stashed changes
            UpdateFeedback(sb.ToString());
        }

        private void OnSubmitCombo()
        {
            if (!_isHandActive)
            {
                Debug.LogWarning("[BattleController] No active hand to submit");
                return;
            }

            // Get the dice that were actually submitted (locked dice, excluding placeholder dice)
            var submittedDice = new List<BaseDice>();
            var submittedValues = new List<int>();
            
            foreach (var dice in _dice)
            {
                if (dice.isLocked && dice.lastRollValue > 0 && dice.tier != DiceTier.Filler)
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

<<<<<<< Updated upstream
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
=======
            // Get submitted dice using HandManager
            var submittedDice = _handManager.GetSubmittedDice(_dice);
            var submittedValues = _handManager.GetSubmittedValues(submittedDice);

            // cache for Close (推进回合时要用)
            _lastSubmittedDice = new List<BaseDice>(submittedDice);

            // --- scoring using your existing systems ---
            float mult = _multiplierCalculator.Calculate(submittedDice, submittedValues);

            int handScore = 0;
            string summary;
            if (submittedValues.Count > 0)
            {
                // Evaluate + BuildSummary already formats something like:
                // "Combo: XXX\nSum: Y, Base: Z, Mult: xN"
                string combo = DiceHandEvaluator.Evaluate(submittedValues, out handScore, mult);
                summary = DiceHandEvaluator.BuildSummary(submittedValues, combo, handScore, mult);
>>>>>>> Stashed changes
            }
            else
            {
                summary = "No dice submitted!";
            }
<<<<<<< Updated upstream
            
            Debug.Log($"[BattleController] Submitted dice values: [{string.Join(", ", submittedValues)}]");
            Debug.Log("[BattleController] ============================");
            
            // Complete the hand in cooldown system with submitted dice
            cooldownSystem.CompleteHand(submittedDice);
            _isHandActive = false;
            
            // Check if we can start a new hand
=======

            // Update total score now (右上角立即生效)
            _totalScore += handScore;
            UpdateScoreUI();

            // (Optional) Background feedback area：只给一句提示，避免把“提交明细”塞进去
            // 你也可以注释掉这一行，让背景区域保持之前的文本
            var feedbackSb = new StringBuilder();
            feedbackSb.AppendLine("Result ready — see panel.");
            UpdateFeedback(feedbackSb.ToString());

            // --- ONLY result goes to the modal panel ---
            var resultSb = new StringBuilder();
            resultSb.AppendLine("=== RESULT ===");
            resultSb.AppendLine(summary);
            resultSb.AppendLine($"\nHand Score: {handScore}");
            resultSb.AppendLine($"Total Score: {_totalScore}");

            // Show modal & disable main buttons
            ShowResultPanel(resultSb.ToString());
            SetMainButtonsInteractable(false);
        }


        private void OnResultCloseClicked()
        {
            // 关闭弹窗
            if (resultPanel != null) resultPanel.SetActive(false);

            // 推进回合（真正应用冷却、结束本手、开下一手/等事件驱动）
            if (_lastSubmittedDice == null) _lastSubmittedDice = new List<BaseDice>();
            cooldownSystem.CompleteHand(_lastSubmittedDice);
            _handManager.EndHand();

>>>>>>> Stashed changes
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining > 0)
            {
                UpdateFeedback($"Hand completed! {remaining} hands remaining.\nStarting next hand...");
                StartNewHand();
            }
            else
            {
                UpdateFeedback("All hands completed! Dice pool refreshing...");
                // 你的 CooldownSystem 完成后会触发 OnDicePoolRefresh → StartNewHand()
            }

            // 重新允许按钮
            SetMainButtonsInteractable(true);
            _lastSubmittedDice = null;
        }

        private void ResetForNewHand()
        {
<<<<<<< Updated upstream
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
=======
            _handManager.Reset();
            foreach (var d in _dice) d.ResetLockAndValue();
            _viewFactory.RefreshViews(_views);
>>>>>>> Stashed changes
            StartNewHand();
        }

        private void ShowResultPanel(string text)
        {
            if (resultText != null) resultText.text = text;
            if (resultPanel != null) resultPanel.SetActive(true);
            else UpdateFeedback(text); // 兜底：如果没连面板，就直接用旧区域显示
        }

        private void SetMainButtonsInteractable(bool interactable)
        {
            if (rollButton) rollButton.interactable = interactable;
            if (resetRollButton) resetRollButton.interactable = interactable;
            if (submitComboButton) submitComboButton.interactable = interactable;
        }

        private void UpdateScoreUI()
        {
            if (totalScoreText != null)
                totalScoreText.text = $"Score: {_totalScore}";
        }

        private void UpdateFeedback(string msg)
        {
            if (rollFeedbackText != null) rollFeedbackText.text = msg;
            else Debug.Log(msg);
        }

        private void UpdateHandCounter(int current, int remaining)
        {
            if (handCounterText != null)
                handCounterText.text = $"Hand {current + 1}/{current + remaining} ({remaining} remaining)";
        }

        #region CooldownSystem Event Handlers
        private void OnDicePoolRefresh()
        {
            UpdateFeedback("Dice pool refreshed! All dice are now available again.");
            StartNewHand();
        }

        private void OnHandCounterUpdate(int current, int remaining)
        {
            UpdateHandCounter(current, remaining);
        }

        private void OnAvailableDiceChanged(List<BaseDice> availableDice)
        {
            // 可选：这里保留日志或轻量提示
            // Debug.Log($"[BattleController] Available changed: {availableDice.Count}");
        }
        #endregion

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
