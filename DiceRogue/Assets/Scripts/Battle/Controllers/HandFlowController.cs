using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DiceGame.Core;
using DiceGame.Analytics;
using DiceGame.Relics;
using DiceGame.UI;

namespace DiceGame
{
    /// <summary>
    /// 管理手牌流程：开始 -> 滚动 -> 提交 -> 完成
    /// </summary>
    public class HandFlowController : MonoBehaviour
    {
        // Dependencies (injected from BattleController)
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceViewFactory _viewFactory;
        private RelicManager _relicManager;
        private ScoreCalculator _scoreCalculator;
        private ProgressionManager _progressionManager;
        private HandCompositionService _compositionService;
        private MoneyManager _moneyManager;
        private GameStateManager _stateManager;
        
        private CooldownSystem _cooldownSystem;
        private BackpackManager _backpackManager;
        private ScoreAnimator _scoreAnimator;
        private BattleUI _battleUI;
        private SceneTransitionManager _sceneTransitionManager;
        
        private Button _submitComboButton;
        private int _diceCount;
        private int _maxRollsPerHand;
        
        // Current hand state
        private List<BaseDice> _dice;
        private List<DiceView> _views;
        private bool _isSubmitting = false;
        
        // Callbacks for UI updates
        public System.Action OnComboPreviewUpdate;
        public System.Action OnRollAndCastCountUpdate;
        public System.Action<string, bool> OnFeedbackUpdate;
        public System.Action OnMoneyDisplayUpdate;
        
        /// <summary>
        /// Initialize hand flow controller with dependencies
        /// </summary>
        public void Initialize(
            HandManager handManager,
            DiceEffectHandler effectHandler,
            DiceViewFactory viewFactory,
            RelicManager relicManager,
            ScoreCalculator scoreCalculator,
            ProgressionManager progressionManager,
            HandCompositionService compositionService,
            MoneyManager moneyManager,
            GameStateManager stateManager,
            CooldownSystem cooldownSystem,
            BackpackManager backpackManager,
            ScoreAnimator scoreAnimator,
            BattleUI battleUI,
            SceneTransitionManager sceneTransitionManager,
            Button submitComboButton,
            int diceCount,
            int maxRollsPerHand,
            List<BaseDice> dice,
            List<DiceView> views)
        {
            _handManager = handManager;
            _effectHandler = effectHandler;
            _viewFactory = viewFactory;
            _relicManager = relicManager;
            _scoreCalculator = scoreCalculator;
            _progressionManager = progressionManager;
            _compositionService = compositionService;
            _moneyManager = moneyManager;
            _stateManager = stateManager;
            _cooldownSystem = cooldownSystem;
            _backpackManager = backpackManager;
            _scoreAnimator = scoreAnimator;
            _battleUI = battleUI;
            _sceneTransitionManager = sceneTransitionManager;
            _submitComboButton = submitComboButton;
            _diceCount = diceCount;
            _maxRollsPerHand = maxRollsPerHand;
            _dice = dice;
            _views = views;
        }
        
        /// <summary>
        /// Start a new hand by selecting available dice from the pool
        /// </summary>
        public void StartNewHand()
        {
            // Check if hands remain (safety check before pool refresh)
            var (handCount, handRemaining) = _cooldownSystem.GetHandCounter();
            if (handRemaining <= 0 && handCount > 0) // Don't block the very first hand
            {
                Debug.LogWarning("[HandFlowController] Cannot start new hand - no hands remaining. Battle complete!");
                OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
                OnRollAndCastCountUpdate?.Invoke();
                return;
            }

            // Advance cooldowns before starting new hand (except for the very first hand)
            if (handCount > 0) // Only advance cooldowns if this is not the first hand
            {
                _cooldownSystem.AdvanceCooldowns();
            }
            
            // Clear previous dice and views using factory
            _dice.Clear();
            _viewFactory.DestroyViews(_views);

            // Show backpack for dice selection
            OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
            if (DiceTooltipManager.Instance != null)
                DiceTooltipManager.Instance.HideTooltip();

            _backpackManager.ShowBackpack(BackpackMode.Selection);
        }
        
        /// <summary>
        /// Handle dice selection from backpack and start hand
        /// </summary>
        public void OnDiceSelectedFromBackpack(List<BaseDice> selectedDice)
        {
            // Use HandCompositionService to compose the hand
            var composedHand = _compositionService.ComposeHandWithSelection(selectedDice, _diceCount);
            
            _dice.Clear(); // Clear existing dice before adding the new selection
            _dice.AddRange(composedHand);
            
            // Separate special dice from normal dice for cooldown registration
            var selectedSpecialDice = composedHand.Where(d => !(d is NormalDice)).ToList();
            
            if (selectedSpecialDice.Count > 0)
            {
                // Register selection with cooldown system
                if (!_cooldownSystem.SelectDiceForHand(selectedSpecialDice))
                {
                    Debug.LogError("[HandFlowController] Failed to select dice for hand!");
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
            var newViews = _viewFactory.CreateViews(_dice, _diceCount);
            _views.AddRange(newViews);
            
            // Pass dice views to score animator for pop effects
            if (_scoreAnimator != null)
            {
                _scoreAnimator.SetDiceViews(_views);
            }

            // Check for filler dice and apply bonus rerolls from relics
            bool hasFiller = _dice.Any(d => d is NormalDice);
            if (hasFiller && _relicManager != null)
            {
                // Create a temporary context to check for bonus rerolls
                var tempContext = new ScoringContext
                {
                    hasFillerInHand = true,
                    submittedDice = _dice,
                    submittedValues = new List<int>()
                };
                
                // Apply all relics to get bonus rerolls
                _relicManager.ApplyAll(tempContext);
                
                // Apply bonus rerolls to HandManager
                if (tempContext.bonusRerolls > 0)
                {
                    _handManager.AddBonusRolls(tempContext.bonusRerolls);
                    Debug.Log($"[HandFlowController] Applied {tempContext.bonusRerolls} bonus rerolls from relics (filler dice detected)");
                }
            }

            // Start new hand in hand manager
            _handManager.StartHand();
            
            // Update combo preview after dice selection
            OnComboPreviewUpdate?.Invoke();
            
            // Get hand composition for feedback
            var (specialCount, normalCount) = _compositionService.GetHandComposition(_dice);
            
            // Show idle message after dice selection
            OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
            OnRollAndCastCountUpdate?.Invoke();
            
            Debug.Log($"[HandFlowController] Started hand with {_diceCount} dice total");
            
            // Auto-roll all dice once (free roll - doesn't count toward roll budget)
            PerformAutoRoll();
        }
        
        /// <summary>
        /// Perform auto-roll when hand starts (free roll - doesn't count toward budget)
        /// </summary>
        private void PerformAutoRoll()
        {
            Debug.Log("[HandFlowController] Performing auto-initial roll (free roll)");
            
            // Roll all dice (they start unlocked)
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                var v = _views[i]; 

                if (d.tier != DiceTier.Filler)
                {
                    _effectHandler.SetupPlusOneDice(d, i, _dice);

                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} auto-rolled: {result}");

                    // Play roll animation
                    if (v != null)
                        v.PlayRollAnimation(result, 0.5f);
                }
            }

            // Wait for roll animations to complete, then apply effects and show golden dice animation
            StartCoroutine(WaitForRollAnimationsThenApplyEffects(0.5f));
            
            // Update combo preview after rolling
            OnComboPreviewUpdate?.Invoke();
            
            // Note: Don't update roll count - this is a free roll
            // Note: Don't update feedback - keep "Roll or Lock Dice" message
        }
        
        /// <summary>
        /// Wait for roll animations to complete, then apply dice effects (golden dice, etc.)
        /// This ensures roll animations finish before golden dice effect shows
        /// </summary>
        private System.Collections.IEnumerator WaitForRollAnimationsThenApplyEffects(float animationDuration)
        {
            // Wait for roll animations to complete
            yield return new WaitForSeconds(animationDuration + 0.1f); // Add small buffer
            
            // Now apply all special dice effects (golden dice will trigger its animation)
            _effectHandler.ApplyRollEffects(_dice, _views);
            
            // Refresh all views to show final values (including golden dice bonuses)
            _viewFactory.RefreshViews(_views);
        }
        
        /// <summary>
        /// Roll unlocked dice once
        /// </summary>
        public void OnRollOnce()
        {
            // Check if hands remain
            var (current, remaining) = _cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
                Debug.LogWarning("[HandFlowController] Cannot roll - no hands remaining.");
                return;
            }

            // Check if we can roll using HandManager
            if (!_handManager.CanRoll)
            {
                OnFeedbackUpdate?.Invoke("No rolls remaining. Cast your combo!", true);
                Debug.LogWarning("[HandFlowController] Roll budget exhausted.");
                return;
            }

            // Increment roll counter
            int rollNumber = _handManager.IncrementRoll();
            Debug.Log($"[HandFlowController] Rolling dice (hand roll {rollNumber}, total {_handManager.TotalRollsUsed}/{_maxRollsPerHand})");

            // Roll only unlocked dice (skip placeholder dice)
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                var v = _views[i]; 

                if (!d.isLocked && d.tier != DiceTier.Filler)
                {
                    _effectHandler.SetupPlusOneDice(d, i, _dice);

                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} rolled: {result}");

                    // play animation
                    if (v != null)
                        v.PlayRollAnimation(result, 0.5f); // second parameter is lasting time
                }
                else if (d.isLocked)
                {
                    Debug.Log($"  - {d.diceName} locked at: {d.lastRollValue}");
                }
            }

            // Wait for roll animations to complete, then apply effects and show golden dice animation
            StartCoroutine(WaitForRollAnimationsThenApplyEffects(0.5f));
            
            // Update combo preview after rolling
            OnComboPreviewUpdate?.Invoke();
            
            // Update roll count display
            OnRollAndCastCountUpdate?.Invoke();
            
            // Show idle message after rolling
            OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
        }
        
        /// <summary>
        /// Submit combo for scoring
        /// </summary>
        public void OnSubmitCombo()
        {
            // Prevent multiple submissions during animation
            if (_isSubmitting)
            {
                Debug.LogWarning("[HandFlowController] Already submitting - ignoring duplicate submission");
                return;
            }

            // Check if hands remain
            var (current, remaining) = _cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
                Debug.LogWarning("[HandFlowController] Cannot submit - no hands remaining.");
                return;
            }

            // Validate using HandManager
            if (!_handManager.CanSubmit(_dice))
            {
                OnFeedbackUpdate?.Invoke("Select at least one dice!", true);
                return;
            }

            // Set submission flag and disable button
            _isSubmitting = true;
            if (_submitComboButton != null)
            {
                _submitComboButton.interactable = false;
            }

            // Update cast count (shows remaining casts)
            OnRollAndCastCountUpdate?.Invoke();

            // Get submitted dice using HandManager
            var submittedDice = _handManager.GetSubmittedDice(_dice);
            var submittedValues = _handManager.GetSubmittedValues(submittedDice);

            Debug.Log("[HandFlowController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[HandFlowController] Rolls used this hand: {_handManager.RollsUsed} (total {_handManager.TotalRollsUsed}/{_maxRollsPerHand})");
            Debug.Log($"[HandFlowController] Submitted {submittedDice.Count} locked dice");
            
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
                
                // Calculate score breakdown (but final score will come from animation)
                // This handles: combo evaluation, dice multipliers, and relic effects
                var scoreResult = _scoreCalculator.CalculateScore(submittedDice, submittedValues, _relicManager, context);
                
                // Apply extra cooldown for next hand (from relics like Cooldown Radiator)
                if (context.nextHandExtraCooldown > 0)
                {
                    _cooldownSystem.SetNextHandExtraCooldown(context.nextHandExtraCooldown);
                }
                
                // Trigger animated score display - animation calculates the final score step-by-step
                if (_scoreAnimator != null)
                {
                    _scoreAnimator.AnimateScore(scoreResult, submittedDice);
                    
                    // Start coroutine to handle post-animation logic (UI refresh, score addition, next hand)
                    StartCoroutine(AddScoreAfterAnimation(scoreResult.comboName, current + 1, submittedDice));
                }
                else
                {
                    // Fallback if no animator: use calculator's final score and proceed immediately
                    _progressionManager.AddScore(scoreResult.finalScore);
                    UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, current + 1, _progressionManager.CurrentLevel);
                    UnityGameAnalytics.TrackScoreCombination(scoreResult.comboName);
                    
                    // Complete hand and continue flow
                    CompleteHandAndContinue(submittedDice);
                }
            }
            else
            {
                OnFeedbackUpdate?.Invoke("Select at least one dice!", true);
            }
            
            Debug.Log($"[HandFlowController] Submitted dice values: [{string.Join(", ", submittedValues)}]");
            Debug.Log("[HandFlowController] ============================");
        }
        
        /// <summary>
        /// Wait for score animation to complete, then refresh UI and add the calculated score to progression
        /// </summary>
        private IEnumerator AddScoreAfterAnimation(string comboName, int handNumber, List<BaseDice> submittedDice)
        {
            // Wait for animation to reach the UI refresh point (variable timing based on number of steps)
            float timeout = 0f;
            float maxTimeout = 15f; // Safety timeout
            
            while (!_scoreAnimator.IsReadyForUIRefresh && timeout < maxTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeout += 0.1f;
            }
            
            if (timeout >= maxTimeout)
            {
                Debug.LogWarning("[HandFlowController] Animation timeout - proceeding with UI refresh");
            }
            
            // REFRESH UI: Dice, Deck, and Feedback (happens AFTER animation steps, BEFORE total score update)
            Debug.Log("[HandFlowController] Refreshing UI after score animation...");
            
            // Get the final calculated score from the animator (already available at this point)
            int finalScore = _scoreAnimator.GetLastHandScore();
            
            // Add score to progression manager (this is the authoritative score from animation)
            _progressionManager.AddScore(finalScore);
            
            // Track analytics
            UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, handNumber, _progressionManager.CurrentLevel);
            UnityGameAnalytics.TrackScoreCombination(comboName);
            
            Debug.Log($"[HandFlowController] Score added after animation: {finalScore}");
            
            // EARLY WIN DETECTION: Check immediately if target score reached
            if (_progressionManager.TotalScore >= _progressionManager.TargetScore)
            {
                Debug.Log($"[HandFlowController] Early win detected! Total: {_progressionManager.TotalScore}, Target: {_progressionManager.TargetScore}");
                
                // Skip idle message in ScoreAnimator
                if (_scoreAnimator != null)
                {
                    _scoreAnimator.SkipIdleMessage();
                }
                
                // Wait for animation to complete INCLUDING fade out (but skip idle message)
                timeout = 0f;
                while (_scoreAnimator.IsAnimating && timeout < maxTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    timeout += 0.1f;
                }
                
                // Wait for fade out to completely finish
                yield return new WaitForSeconds(0.1f);
                
                // Complete the hand first (apply cooldowns)
                var specialDiceOnly = submittedDice.Where(d => !(d is NormalDice)).ToList();
                if (specialDiceOnly.Count > 0)
                {
                    _cooldownSystem.CompleteHand(specialDiceOnly);
                }
                else
                {
                    _cooldownSystem.CompleteHand(new List<BaseDice>());
                }
                _handManager.EndHand();
                
                // Update UI
                OnRollAndCastCountUpdate?.Invoke();
                
                // Trigger win evaluation after fade out completes (skip remaining casts)
                StartEvaluateTargetScore();
                yield break; // Exit coroutine - don't continue to next hand
            }
            
            // Wait for the entire animation to complete (total score update + fade out)
            timeout = 0f;
            while (_scoreAnimator.IsAnimating && timeout < maxTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeout += 0.1f;
            }
            
            if (timeout >= maxTimeout)
            {
                Debug.LogWarning("[HandFlowController] Animation completion timeout - proceeding anyway");
            }
            
            // Show idle message after animation completes (only if not win)
            OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
            
            // Complete hand and continue to next hand or evaluation
            CompleteHandAndContinue(submittedDice);
        }
        
        /// <summary>
        /// Complete the current hand and continue to next hand or evaluation
        /// </summary>
        private void CompleteHandAndContinue(List<BaseDice> submittedDice)
        {
            // Clear submission flag and re-enable button
            _isSubmitting = false;
            if (_submitComboButton != null)
            {
                _submitComboButton.interactable = true;
            }

            // Complete the hand in cooldown system with submitted dice
            // Filter out NormalDice (temporary fillers) - only submit special dice from the pool
            var specialDiceOnly = submittedDice.Where(d => !(d is NormalDice)).ToList();
            if (specialDiceOnly.Count > 0)
            {
                Debug.Log($"[HandFlowController] Passing {specialDiceOnly.Count} special dice to cooldown system");
                _cooldownSystem.CompleteHand(specialDiceOnly);
            }
            else
            {
                Debug.Log("[HandFlowController] No special dice submitted, only normal dice used");
                _cooldownSystem.CompleteHand(new List<BaseDice>()); // Complete hand without cooldown
            }
            _handManager.EndHand();
            
            // Check if we can start a new hand
            var (currentHand, handsRemaining) = _cooldownSystem.GetHandCounter();
            if (handsRemaining > 0)
            {
                // Start next hand after a brief delay
                StartCoroutine(DelayedStartNewHand());
            }
            else
            {
                Debug.Log("[HandFlowController] All hands completed! Evaluating target score...");
                // Update UI to show battle is complete
                OnRollAndCastCountUpdate?.Invoke();
                
                // Trigger target score evaluation animation
                StartEvaluateTargetScore();
            }
        }
        
        /// <summary>
        /// Start a new hand after a brief delay
        /// </summary>
        private IEnumerator DelayedStartNewHand()
        {
            // Brief pause before starting new hand (animation already completed when this is called)
            yield return new WaitForSeconds(0.5f);
            StartNewHand();
        }
        
        /// <summary>
        /// Start evaluating target score (public method to start coroutine)
        /// </summary>
        public void StartEvaluateTargetScore()
        {
            StartCoroutine(EvaluateTargetScore());
        }
        
        /// <summary>
        /// Evaluate if player passed target score with dramatic animation
        /// </summary>
        private IEnumerator EvaluateTargetScore()
        {
            // Skip evaluation in tutorial mode
            if (_progressionManager != null && _progressionManager.IsTutorialMode)
            {
                Debug.Log("[HandFlowController] Skipping target evaluation - tutorial mode");
                yield break;
            }
            
            if (DiceTooltipManager.Instance != null)
                DiceTooltipManager.Instance.HideTooltip();

            // No wait needed - animation already completed before this is called

            int finalScore = _scoreAnimator != null ? _scoreAnimator.GetTotalScore() : _progressionManager.TotalScore;
            bool passed = _progressionManager.EvaluateTargetScore();

            // Trigger pass/fail animation in ScoreAnimator
            if (_scoreAnimator != null)
            {
                _scoreAnimator.AnimateTargetEvaluation(finalScore, _progressionManager.TargetScore, passed);
                
                // Wait for "You pass!" message to complete
                float timeout = 0f;
                float maxTimeout = 10f; // Safety timeout to prevent infinite wait
                while (_scoreAnimator.IsAnimating && timeout < maxTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    timeout += 0.1f;
                }
                
                if (timeout >= maxTimeout)
                {
                    Debug.LogWarning("[HandFlowController] Animation completion timeout - proceeding anyway");
                }
            }
            else
            {
                // Fallback if no animator - show idle message
                OnFeedbackUpdate?.Invoke("Roll or Lock Dice", false);
                yield return new WaitForSeconds(1.0f); // Brief delay for fallback
            }
            
            if (passed)
            {
                // Reward money: 5 + remaining casts
                var (current, remaining) = _cooldownSystem.GetHandCounter();
                int rewardMoney = 5 + remaining;
                int currentMoney = _moneyManager.Money;
                
                // Show reward message
                if (_scoreAnimator != null && _scoreAnimator.comboScoreText != null)
                {
                    _scoreAnimator.comboScoreText.text = $"<size=150%><color=#FFD700>Level reward: 5+{remaining}</color></size>";
                    yield return new WaitForSeconds(1.0f);
                }
                
                // Animate money increase (similar to score animation)
                if (_scoreAnimator != null && _scoreAnimator.moneyText != null)
                {
                    _scoreAnimator.AnimateMoneyIncrease(rewardMoney, currentMoney, (money) =>
                    {
                        _moneyManager.Set(money);
                        _stateManager.SaveData.money = money;
                        _stateManager.Save();
                        OnMoneyDisplayUpdate?.Invoke();
                    });
                    
                    // Wait for animation to complete before transitioning
                    yield return new WaitForSeconds(_scoreAnimator.countUpDuration + 0.3f);
                }
                else
                {
                    // Fallback: add money immediately if no animator
                    _moneyManager.Add(rewardMoney);
                    _stateManager.SaveData.money = _moneyManager.Money;
                    _stateManager.Save();
                    OnMoneyDisplayUpdate?.Invoke();
                }
                
                Debug.Log($"[HandFlowController] Level passed! Money reward: +{rewardMoney} (5 + {remaining} remaining casts), Total: {_stateManager.SaveData.money}");

                // Prepare next level state for when we return from ShopScene
                int nextLevel = _progressionManager.CurrentLevel + 1;
                int nextTarget = _progressionManager.CalculateTargetScore(nextLevel);

                _stateManager.State.PendingLevel = nextLevel;
                _stateManager.State.PendingTargetScore = nextTarget;
                _stateManager.State.ContinuingFromReward = true;

                // Transition to shop scene
                Debug.Log($"[HandFlowController] Target passed! Loading ShopScene. Next Level: {nextLevel}, Next Target: {nextTarget}");
                _sceneTransitionManager.TransitionToRewardScene();
            }
            else
            {
                // Player failed - store score data and transition to game over scene
                _stateManager.State.GameOverFinalScore = finalScore;
                _stateManager.State.GameOverTargetScore = _progressionManager.TargetScore;
                
                Debug.Log($"[HandFlowController] ========== GAME OVER ==========");
                Debug.Log($"[HandFlowController] Target failed! Final: {finalScore}, Target: {_progressionManager.TargetScore}");
                Debug.Log($"[HandFlowController] Stored scores - Final: {_stateManager.State.GameOverFinalScore}, Target: {_stateManager.State.GameOverTargetScore}");
                
                // Transition to game over scene
                yield return _sceneTransitionManager.TransitionToGameOverScene();
            }
        }
        
        /// <summary>
        /// Create and populate ScoringContext for relic application
        /// </summary>
        private ScoringContext CreateScoringContext(List<BaseDice> submittedDice, List<int> submittedValues)
        {
            var (current, remaining) = _cooldownSystem.GetHandCounter();
            
            var context = new ScoringContext
            {
                submittedValues = new List<int>(submittedValues),
                submittedDice = new List<BaseDice>(submittedDice),
                handBudget = 6, // Default hand budget (could be modified by relics in future)
                totalSelectedCost = submittedDice.Sum(d => d.cost),
                rollsUsed = _handManager.RollsUsed,
                maxRollsPerHand = _maxRollsPerHand,
                hasFillerInHand = submittedDice.Any(d => d is NormalDice),
                handsRemaining = remaining // Number of hands remaining after this submission
            };
            
            return context;
        }
    }
}

