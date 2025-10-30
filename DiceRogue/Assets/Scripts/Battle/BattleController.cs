using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;
using DiceGame.Analytics;
using DiceGame.Relics;
using DiceGame.UI;

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

        [Header("Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system
        public TMP_Text targetScoreText;    // Target score display
        
        [Header("Relic Display")]
        public RelicDisplay relicDisplay;   // Visual display for equipped relics

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 2;   // Max 2 rolls per hand
        public int baseTargetScore = 300; // Starting target score

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem; // Reference to cooldown system

        // Core components
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceViewFactory _viewFactory;
        private RelicManager _relicManager;
        private ScoreCalculator _scoreCalculator;
        private ProgressionManager _progressionManager;
        private BattleUIPresenter _uiPresenter;
        private HandCompositionService _compositionService;

        // Current hand state
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();

        void Start()
        {
            // Initialize analytics first
            InitializeAnalytics();
            
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
            
            // Link score animator to relic display for pop effects
            if (scoreAnimator != null && relicDisplay != null)
            {
                scoreAnimator.relicDisplay = relicDisplay;
            }

            // Initialize core components first (needed for other initialization)
            _handManager = new HandManager();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            _effectHandler = new DiceEffectHandler();
            _viewFactory = new DiceViewFactory(diceViewPrefab, diceRowParent);
            _relicManager = new RelicManager();
            _scoreCalculator = new ScoreCalculator();
            _uiPresenter = new BattleUIPresenter();
            _compositionService = new HandCompositionService();
            
            // Initialize progression manager
            _progressionManager = new ProgressionManager(baseTargetScore);
            UpdateTargetScoreDisplay();
            
            // Track initial player progression
            UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, 0, _progressionManager.CurrentLevel);

            // Initialize and hide continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
                continueButton.onClick.AddListener(OnContinue);
            }
            
            // Add test relics (for demonstration - will be removed when proper relic acquisition system is added)
            InitializeTestRelics();

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
        /// Initialize analytics system by creating the UnityGameAnalytics GameObject if it doesn't exist
        /// </summary>
        private void InitializeAnalytics()
        {
            // Check if UnityGameAnalytics already exists
            if (FindObjectOfType<UnityGameAnalytics>() == null)
            {
                // Create the analytics GameObject
                GameObject analyticsGO = new GameObject("UnityGameAnalytics");
                analyticsGO.AddComponent<UnityGameAnalytics>();
                
                Debug.Log("[BattleController] Created UnityGameAnalytics GameObject");
            }
            else
            {
                Debug.Log("[BattleController] UnityGameAnalytics already exists");
            }
        }

        /// <summary>
        /// Initialize test relics for demonstration (will load from ScriptableObjects after Unity setup)
        /// </summary>
        private void InitializeTestRelics()
        {
            // Try to load relic ScriptableObjects from Resources
            // User should create these in Unity and place them in Assets/Resources/Relics/
            var relicAssets = Resources.LoadAll<RelicBase>("Relics");
            
            if (relicAssets != null && relicAssets.Length > 0)
            {
                Debug.Log($"[BattleController] Found {relicAssets.Length} relic(s) in Resources/Relics/");
                foreach (var relic in relicAssets)
                {
                    if (_relicManager.AddRelic(relic))
                    {
                        Debug.Log($"[BattleController] Equipped relic: {relic.relicName} ({relic.rarity})");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[BattleController] No relics found in Resources/Relics/. Create ScriptableObject relics in Unity to test the system.");
            }
            
            // Update relic display UI
            if (relicDisplay != null)
            {
                relicDisplay.DisplayRelics(_relicManager);
            }
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
            UpdateFeedback(_uiPresenter.FormatNoHandsRemaining());
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
        
        // Use HandCompositionService to compose the hand
        var composedHand = _compositionService.ComposeHand(availableDice, diceCount, shuffle: true);
        _dice.AddRange(composedHand);
        
        // Separate special dice from normal dice for cooldown registration
        var selectedSpecialDice = composedHand.Where(d => !(d is NormalDice)).ToList();
        
        if (selectedSpecialDice.Count > 0)
        {
            // Register selection with cooldown system
            if (!cooldownSystem.SelectDiceForHand(selectedSpecialDice))
            {
                Debug.LogError("[BattleController] Failed to select dice for hand!");
                return;
            }
            
            // Track dice usage for analytics
            foreach (var dice in selectedSpecialDice)
            {
                UnityGameAnalytics.TrackDiceUsage(dice.diceName);
            }
        }
        
        // Reset dice state for new hand
        _compositionService.ResetHandDice(_dice);

        // Create views using factory (includes placeholders for empty slots)
        var newViews = _viewFactory.CreateViews(_dice, diceCount);
        _views.AddRange(newViews);
        
        // Pass dice views to score animator for pop effects
        if (scoreAnimator != null)
        {
            scoreAnimator.SetDiceViews(_views);
        }

        // Start new hand in hand manager
        _handManager.StartHand();
        
        // Get hand composition for feedback
        var (specialCount, normalCount) = _compositionService.GetHandComposition(_dice);
        
        // Build feedback message using UI presenter
        string feedbackMsg = _uiPresenter.FormatHandReady(currentHand + 1, diceCount, specialCount, normalCount);
        
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

            // Build feedback using UI presenter
            string feedbackMsg = _uiPresenter.FormatRollFeedback(_dice, rollNumber, maxRollsPerHand);
            UpdateFeedback(feedbackMsg);
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
                UpdateFeedback(_uiPresenter.FormatNoDiceLocked());
                return;
            }

            // Get submitted dice using HandManager
            var submittedDice = _handManager.GetSubmittedDice(_dice);
            var submittedValues = _handManager.GetSubmittedValues(submittedDice);

            Debug.Log("[BattleController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[BattleController] Rolls used: {_handManager.RollsUsed}/{maxRollsPerHand}");
            Debug.Log($"[BattleController] Submitted {submittedDice.Count} locked dice");
            
            // Update feedback to show submitted dice (using UI presenter)
            string submittedFeedback = _uiPresenter.FormatComboSubmitted(submittedDice, _handManager.RollsUsed, maxRollsPerHand);
            UpdateFeedback(submittedFeedback);
            
            // Log submitted dice
            foreach (var dice in submittedDice)
            {
                Debug.Log($"  {dice.diceName}: {dice.lastRollValue} [SUBMITTED]");
            }

            // Calculate score using centralized ScoreCalculator
            if (submittedValues.Count > 0)
            {
                // Create and populate ScoringContext for relics
                var context = CreateScoringContext(submittedDice, submittedValues);
                
                // Calculate final score with complete breakdown
                // This handles: combo evaluation, dice multipliers, and relic effects
                var scoreResult = _scoreCalculator.CalculateScore(submittedDice, submittedValues, _relicManager, context);
                
                // Trigger new animated score display with step-by-step breakdown
                if (scoreAnimator != null)
                {
                    scoreAnimator.AnimateScore(scoreResult, submittedDice);
                }
                
                // Add score to progression manager
                _progressionManager.AddScore(scoreResult.finalScore);
                
                // Track analytics for hand completion and score combination
                // Track player progression
                UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, current + 1, _progressionManager.CurrentLevel);
                
                // Track score combination
                UnityGameAnalytics.TrackScoreCombination(scoreResult.comboName);
            }
            else
            {
                UpdateFeedback(submittedFeedback + "\n<color=#FF8888>No dice submitted!</color>");
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
        /// Create and populate ScoringContext for relic application
        /// </summary>
        private ScoringContext CreateScoringContext(List<BaseDice> submittedDice, List<int> submittedValues)
        {
            var context = new ScoringContext
            {
                submittedValues = new List<int>(submittedValues),
                submittedDice = new List<BaseDice>(submittedDice),
                handBudget = 6, // Default hand budget (could be modified by relics in future)
                totalSelectedCost = submittedDice.Sum(d => d.cost),
                rollsUsed = _handManager.RollsUsed,
                maxRollsPerHand = maxRollsPerHand,
                hasFillerInHand = submittedDice.Any(d => d is NormalDice)
            };
            
            return context;
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
                
                // Reset progression to level 1
                _progressionManager.ResetToLevelOne();
                
                // Reset score animator
                if (scoreAnimator != null)
                {
                    scoreAnimator.ResetTotalScore();
                }
                
                // Refresh dice pool and hand counter
                cooldownSystem.RefreshDicePool();
                
                // Update displays
                UpdateTargetScoreDisplay();
                UpdateFeedback(_uiPresenter.FormatResetToLevelOne(_progressionManager.TargetScore));
                
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
                handCounterText.text = _uiPresenter.FormatHandCounter(current, remaining);
            }
        }

        /// <summary>
        /// Update deck status display showing all dice and their states
        /// </summary>
        private void UpdateDeckStatus()
        {
            if (deckStatusText == null) return;

            var allDice = cooldownSystem.GetAllDice();
            var selectedDiceNames = _dice.Where(d => !(d is NormalDice)).Select(d => d.diceName).ToHashSet();

            deckStatusText.text = _uiPresenter.FormatDeckStatus(allDice, selectedDiceNames);
        }

        /// <summary>
        /// Update target score display
        /// </summary>
        private void UpdateTargetScoreDisplay()
        {
            if (targetScoreText != null)
            {
                targetScoreText.text = _uiPresenter.FormatTargetScore(_progressionManager.TargetScore, _progressionManager.CurrentLevel);
            }
        }

        /// <summary>
        /// Continue to next level - reset game state and increase target score
        /// </summary>
        private void OnContinue()
        {
            Debug.Log($"[BattleController] Continuing to next level from Level {_progressionManager.CurrentLevel}...");

            // Hide continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            // Advance to next level using progression manager
            _progressionManager.AdvanceToNextLevel();
            _progressionManager.ResetScore();

            // Reset score animator
            if (scoreAnimator != null)
            {
                scoreAnimator.ResetTotalScore();
            }

            // Reset dice pool and hand counter
            cooldownSystem.RefreshDicePool();

            // Update displays
            UpdateTargetScoreDisplay();
            UpdateFeedback(_uiPresenter.FormatLevelStart(_progressionManager.CurrentLevel, _progressionManager.TargetScore));

            // Start first hand of new level
            StartNewHand();
        }

        /// <summary>
        /// Evaluate if player passed target score with dramatic animation
        /// </summary>
        private System.Collections.IEnumerator EvaluateTargetScore()
        {
            // Wait for score animation to finish
            yield return new UnityEngine.WaitForSeconds(3f);

            int finalScore = scoreAnimator != null ? scoreAnimator.GetTotalScore() : _progressionManager.TotalScore;
            bool passed = _progressionManager.EvaluateTargetScore();

            // Trigger pass/fail animation in ScoreAnimator
            if (scoreAnimator != null)
            {
                scoreAnimator.AnimateTargetEvaluation(finalScore, _progressionManager.TargetScore, passed);
            }
            else
            {
                // Fallback if no animator
                string resultMsg = passed 
                    ? "<color=#FFD700><b>TARGET PASSED!</b></color>\n\n" 
                    : "<color=#FF6666><b>TARGET FAILED</b></color>\n\n";
                resultMsg += $"Final Score: {finalScore}\nTarget: {_progressionManager.TargetScore}\n\n";
                resultMsg += "<color=#AAAAAA>Press Reset to start new battle cycle.</color>";
                UpdateFeedback(resultMsg);
            }

            // Wait for evaluation animation to complete, then show Continue button ONLY if passed
            yield return new UnityEngine.WaitForSeconds(4.5f);
            
            if (passed && continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
            else if (!passed)
            {
                // Player failed - show game over message
                UpdateFeedback(_uiPresenter.FormatGameOver());
            }
        }

        #region CooldownSystem Event Handlers

        /// <summary>
        /// Called when dice pool refreshes (manual refresh via Reset button)
        /// </summary>
        private void OnDicePoolRefresh()
        {
            Debug.Log("[BattleController] Dice pool refreshed - starting new battle cycle!");
            UpdateFeedback(_uiPresenter.FormatDicePoolRefreshed());
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
