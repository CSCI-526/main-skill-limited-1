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
using UnityEngine.SceneManagement;

namespace DiceGame
{
    public class BattleController : MonoBehaviour
    {
        // Static state for scene transitions
        public static bool ContinuingFromReward = false;
        public static int PendingLevel = 1;
        public static int PendingTargetScore = 300;
        [Header("UI - Main Game")]
        public Transform diceRowParent;      // Container for DiceView
        public GameObject diceViewPrefab;  // Prefab (with DiceView component)
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;
        public Button continueButton;
        public Button openBackpackButton;
        public Button settingsButton;        // Settings button
        
        [Header("UI - Settings Panel")]
        public GameObject settingsPanel;     // Settings panel (window + overlay)
        public GameObject settingsOverlay;   // Shaded background overlay
        public GameObject settingsWindow;   // Settings window (middle of screen)
        public Button settingsResetButton;   // Reset button in settings
        public Button settingsQuitButton;    // Quit button in settings
        public Button settingsCloseButton;   // Close button (optional - can click overlay to close)
        
        [Header("UI - Right Panel")]
        public TMP_Text levelInfoText;      // Level display
        public TMP_Text targetScoreText;    // Target score display
        public TMP_Text comboNameText;      // Combo name preview
        public TMP_Text comboBaseText;       // Combo base score preview
        public TMP_Text comboMultiplierText;// Combo multiplier preview
        public TMP_Text rollCountText;      // Roll count display (remaining rolls)
        public TMP_Text castCountText;      // Cast count display (remaining casts)
        
        [Header("UI - Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system
        
        [Header("Backpack")]
        public BackpackManager backpackManager;
        
        [Header("Relic Display")]
        public RelicDisplay relicDisplay;   // Visual display for equipped relics

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 5;   // Shared roll budget across all hands
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
        private bool _isSelectionMode = false;

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

            // Initialize backpack manager
            if (backpackManager != null)
            {
                backpackManager.Initialize(cooldownSystem, OnDiceSelectedFromBackpack);
            }
            else
            {
                Debug.LogError("[BattleController] BackpackManager not assigned!");
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
            // Check if continuing from reward scene to restore level/target
            if (ContinuingFromReward)
            {
                _progressionManager = new ProgressionManager(baseTargetScore);
                _progressionManager.RestoreLevelState(PendingLevel, PendingTargetScore);
                ContinuingFromReward = false; // Reset flag
                Debug.Log($"[BattleController] Continuing from Reward Scene - Level {PendingLevel}, Target: {PendingTargetScore}");
            }
            else
            {
                _progressionManager = new ProgressionManager(baseTargetScore);
            }
            
            UpdateTargetScoreDisplay();
            UpdateLevelInfo();
            UpdateComboPreview();
            UpdateRollAndCastCount(); // Initialize roll and cast counts
            
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
            cooldownSystem.OnAvailableDiceChanged += OnAvailableDiceChanged;

            // Set up UI
            rollButton.onClick.AddListener(OnRollOnce);
            resetRollButton.onClick.AddListener(ResetForNewHand);
            submitComboButton.onClick.AddListener(OnSubmitCombo);
            
            // Set up settings panel
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
            
            // Initialize settings panel (hidden by default)
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            // Set up settings panel buttons
            if (settingsResetButton != null)
            {
                settingsResetButton.onClick.AddListener(OnSettingsResetClicked);
            }
            
            if (settingsQuitButton != null)
            {
                settingsQuitButton.onClick.AddListener(OnSettingsQuitClicked);
            }
            
            // Setup overlay (just visual, not clickable)
            if (settingsOverlay != null)
            {
                // Ensure overlay has Image component
                Image overlayImage = settingsOverlay.GetComponent<Image>();
                if (overlayImage == null)
                {
                    overlayImage = settingsOverlay.AddComponent<Image>();
                }
                // Disable raycast target so overlay is not clickable
                overlayImage.raycastTarget = false;
                
                // Remove Button component if it exists (we don't want overlay to be clickable)
                Button overlayButton = settingsOverlay.GetComponent<Button>();
                if (overlayButton != null)
                {
                    DestroyImmediate(overlayButton);
                }
            }
            
            // Ensure settings window blocks raycasts
            if (settingsWindow != null)
            {
                Image windowImage = settingsWindow.GetComponent<Image>();
                if (windowImage == null)
                {
                    windowImage = settingsWindow.AddComponent<Image>();
                }
                windowImage.raycastTarget = true; // This blocks raycasts
            }
            
            // Setup close button if provided
            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(CloseSettingsPanel);
            }
            
            // Subscribe to dice lock changes for combo preview updates
            DiceView.OnDiceLockChanged += UpdateComboPreview;
            
            // Check if there are pending reward dice from reward scene
            IntegrateRewardDice();
            
            // Start first hand with a delay to ensure all systems are ready
            StartCoroutine(DelayedStartFirstHand());
            
            Debug.Log("[BattleController] Battle scene initialized with decoupled components.");
        }

        private System.Collections.IEnumerator DelayedStartFirstHand()
        {
            yield return null; // Wait one frame
            StartNewHand();
        }
        
        public void OnBackpackButtonPressed()
        {
            if (!_isSelectionMode)
            {
                backpackManager.ShowBackpack(BackpackMode.ViewOnly);
            }
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
        /// Integrate reward dice from reward scene into the dice pool
        /// </summary>
        private void IntegrateRewardDice()
        {
            // Check if there are pending reward dice
            if (RewardSceneManager.PendingDiceTypeIds.Count == 0)
            {
                Debug.Log("[BattleController] No pending reward dice to integrate");
                return;
            }

            Debug.Log($"[BattleController] Found {RewardSceneManager.PendingDiceTypeIds.Count} reward dice to integrate");

            // Get current dice pool
            var currentPool = cooldownSystem.GetAllDice();
            var newPool = new List<BaseDice>(currentPool);

            // Create and add reward dice
            foreach (var typeId in RewardSceneManager.PendingDiceTypeIds)
            {
                var rewardDice = CreateDiceFromTypeId(typeId);
                if (rewardDice != null)
                {
                    newPool.Add(rewardDice);
                    Debug.Log($"[BattleController] Added reward dice: {rewardDice.diceName} ({rewardDice.tier})");
                }
            }

            // Clear pending list
            RewardSceneManager.PendingDiceTypeIds.Clear();

            // Update cooldown system with new pool
            cooldownSystem.SetPlayerBackpackDice(newPool);

            Debug.Log($"[BattleController] Integrated reward dice. New pool size: {newPool.Count}");
        }

        /// <summary>
        /// Create a dice instance from type ID string
        /// </summary>
        private BaseDice CreateDiceFromTypeId(string typeId)
        {
            // Use DicePool to get all available dice prototypes
            var allDice = DicePool.GetAll();
            var prototype = allDice.FirstOrDefault(d => d.GetType().Name == typeId);

            if (prototype != null)
            {
                // Create a new instance by cloning the prototype
                var diceType = prototype.GetType();
                var newDice = System.Activator.CreateInstance(diceType) as BaseDice;
                
                if (newDice != null)
                {
                    // Copy properties from prototype (since constructors might set these)
                    newDice.diceName = prototype.diceName;
                    newDice.description = prototype.description;
                    newDice.tier = prototype.tier;
                    newDice.cost = prototype.cost;
                    newDice.cooldownAfterUse = prototype.cooldownAfterUse;
                    newDice.cooldownRemain = 0;
                    newDice.isLocked = false;
                    newDice.lastRollValue = 0;
                    
                    return newDice;
                }
            }

            Debug.LogWarning($"[BattleController] Could not create dice from typeId: {typeId}");
            return null;
        }

    /// <summary>
    /// Start a new hand by selecting available dice from the pool
    /// </summary>
    private void StartNewHand()
    {
        _isSelectionMode = true;

        // Check if hands remain (safety check before pool refresh)
        var (handCount, handRemaining) = cooldownSystem.GetHandCounter();
        if (handRemaining <= 0 && handCount > 0) // Don't block the very first hand
        {
            Debug.LogWarning("[BattleController] Cannot start new hand - no hands remaining. Battle complete!");
            UpdateFeedback("Roll or Lock Dice");
            UpdateRollAndCastCount();
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

        // Show backpack for dice selection
        UpdateFeedback("Roll or Lock Dice");
        if (DiceTooltipManager.Instance != null)
            DiceTooltipManager.Instance.HideTooltip();

        backpackManager.ShowBackpack(BackpackMode.Selection);
    }

    private void OnDiceSelectedFromBackpack(List<BaseDice> selectedDice)
    {
        _isSelectionMode = false;

        // Use HandCompositionService to compose the hand
        var composedHand = _compositionService.ComposeHandWithSelection(selectedDice, diceCount);
        
        _dice.Clear(); // Clear existing dice before adding the new selection
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
        
        // Update combo preview after dice selection
        UpdateComboPreview();
        
        // Get hand composition for feedback
        var (specialCount, normalCount) = _compositionService.GetHandComposition(_dice);
        
        // Show idle message after dice selection
        UpdateFeedback("Roll or Lock Dice");
        UpdateRollAndCastCount();
        
        Debug.Log($"[BattleController] Started hand with {diceCount} dice total");
        
        // Auto-roll all dice once (free roll - doesn't count toward roll budget)
        PerformAutoRoll();
    }

        /// <summary>
        /// Perform auto-roll when hand starts (free roll - doesn't count toward budget)
        /// </summary>
        private void PerformAutoRoll()
        {
            Debug.Log("[BattleController] Performing auto-initial roll (free roll)");
            
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

            // Apply all special dice effects using effect handler
            _effectHandler.ApplyRollEffects(_dice);

            // Refresh all views using factory
            _viewFactory.RefreshViews(_views);
            
            // Update combo preview after rolling
            UpdateComboPreview();
            
            // Note: Don't update roll count - this is a free roll
            // Note: Don't update feedback - keep "Roll or Lock Dice" message
        }

        void OnRollOnce()
        {
            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("Roll or Lock Dice");
                Debug.LogWarning("[BattleController] Cannot roll - no hands remaining.");
                return;
            }

            // Check if we can roll using HandManager
            if (!_handManager.CanRoll)
            {
                UpdateFeedback("No rolls remaining. Cast your combo!", isWarning: true);
                Debug.LogWarning("[BattleController] Roll budget exhausted.");
                return;
            }

            // Increment roll counter
            int rollNumber = _handManager.IncrementRoll();
            Debug.Log($"[BattleController] Rolling dice (hand roll {rollNumber}, total {_handManager.TotalRollsUsed}/{maxRollsPerHand})");

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

            // Apply all special dice effects using effect handler
            _effectHandler.ApplyRollEffects(_dice);

            // Refresh all views using factory
            _viewFactory.RefreshViews(_views);
            
            // Update combo preview after rolling
            UpdateComboPreview();
            
            // Update roll count display
            UpdateRollAndCastCount();
            
            // Show idle message after rolling
            UpdateFeedback("Roll or Lock Dice");
        }

        void OnSubmitCombo()
        {
            if (DiceTooltipManager.Instance != null)
                DiceTooltipManager.Instance.HideTooltip();

            // Check if hands remain
            var (current, remaining) = cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                UpdateFeedback("Roll or Lock Dice");
                Debug.LogWarning("[BattleController] Cannot submit - no hands remaining.");
                return;
            }

            // Validate using HandManager
            if (!_handManager.CanSubmit(_dice))
            {
                UpdateFeedback("Select at least one dice!", isWarning: true);
                return;
            }

            // Update cast count (shows remaining casts)
            UpdateRollAndCastCount();

            // Get submitted dice using HandManager
            var submittedDice = _handManager.GetSubmittedDice(_dice);
            var submittedValues = _handManager.GetSubmittedValues(submittedDice);

            Debug.Log("[BattleController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[BattleController] Rolls used this hand: {_handManager.RollsUsed} (total {_handManager.TotalRollsUsed}/{maxRollsPerHand})");
            Debug.Log($"[BattleController] Submitted {submittedDice.Count} locked dice");
            
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
                
                // Trigger animated score display - animation calculates the final score step-by-step
                if (scoreAnimator != null)
                {
                    scoreAnimator.AnimateScore(scoreResult, submittedDice);
                    
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
                UpdateFeedback("Select at least one dice!", isWarning: true);
            }
            
            Debug.Log($"[BattleController] Submitted dice values: [{string.Join(", ", submittedValues)}]");
            Debug.Log("[BattleController] ============================");
            
            // NOTE: Hand completion and UI refresh now happens AFTER animation in AddScoreAfterAnimation()
        }

        /// <summary>
        /// Start a new hand after a brief delay
        /// </summary>
        private System.Collections.IEnumerator DelayedStartNewHand()
        {
            // Brief pause before starting new hand (animation already completed when this is called)
            yield return new UnityEngine.WaitForSeconds(0.5f);
            StartNewHand();
        }

        /// <summary>
        /// Wait for score animation to complete, then refresh UI and add the calculated score to progression
        /// </summary>
        private System.Collections.IEnumerator AddScoreAfterAnimation(string comboName, int handNumber, List<BaseDice> submittedDice)
        {
            // Wait for animation to reach the UI refresh point (variable timing based on number of steps)
            float timeout = 0f;
            float maxTimeout = 15f; // Safety timeout
            
            while (!scoreAnimator.IsReadyForUIRefresh && timeout < maxTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeout += 0.1f;
            }
            
            if (timeout >= maxTimeout)
            {
                Debug.LogWarning("[BattleController] Animation timeout - proceeding with UI refresh");
            }
            
            // REFRESH UI: Dice, Deck, and Feedback (happens AFTER animation steps, BEFORE total score update)
            Debug.Log("[BattleController] Refreshing UI after score animation...");
            
            // Show idle message after score animation completes
            // (Score animation will show in comboScoreText, then fade out)
            
            // Get the final calculated score from the animator (already available at this point)
            int finalScore = scoreAnimator.GetLastHandScore();
            
            // Add score to progression manager (this is the authoritative score from animation)
            _progressionManager.AddScore(finalScore);
            
            // Track analytics
            UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, handNumber, _progressionManager.CurrentLevel);
            UnityGameAnalytics.TrackScoreCombination(comboName);
            
            Debug.Log($"[BattleController] Score added after animation: {finalScore}");
            
            // EARLY WIN DETECTION: Check immediately if target score reached
            if (_progressionManager.TotalScore >= _progressionManager.TargetScore)
            {
                Debug.Log($"[BattleController] Early win detected! Total: {_progressionManager.TotalScore}, Target: {_progressionManager.TargetScore}");
                
                // Skip idle message in ScoreAnimator
                if (scoreAnimator != null)
                {
                    scoreAnimator.SkipIdleMessage();
                }
                
                // Wait for animation to complete INCLUDING fade out (but skip idle message)
                timeout = 0f;
                while (scoreAnimator.IsAnimating && timeout < maxTimeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    timeout += 0.1f;
                }
                
                // Wait for fade out to completely finish (fade duration is 0.3s, plus hold time 0.8s)
                // Animation completes after fade, so we just need a small buffer to ensure text is cleared
                yield return new WaitForSeconds(0.1f);
                
                // Complete the hand first (apply cooldowns)
                var specialDiceOnly = submittedDice.Where(d => !(d is NormalDice)).ToList();
                if (specialDiceOnly.Count > 0)
                {
                    cooldownSystem.CompleteHand(specialDiceOnly);
                }
                else
                {
                    cooldownSystem.CompleteHand(new List<BaseDice>());
                }
                _handManager.EndHand();
                
                // Update UI
                UpdateRollAndCastCount();
                
                // Trigger win evaluation after fade out completes (skip remaining casts)
                StartCoroutine(EvaluateTargetScore());
                yield break; // Exit coroutine - don't continue to next hand
            }
            
            // Wait for the entire animation to complete (total score update + fade out)
            timeout = 0f;
            while (scoreAnimator.IsAnimating && timeout < maxTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeout += 0.1f;
            }
            
            if (timeout >= maxTimeout)
            {
                Debug.LogWarning("[BattleController] Animation completion timeout - proceeding anyway");
            }
            
            // Show idle message after animation completes (only if not win)
            UpdateFeedback("Roll or Lock Dice");
            
            // Complete hand and continue to next hand or evaluation
            CompleteHandAndContinue(submittedDice);
        }

        /// <summary>
        /// Complete the current hand and continue to next hand or evaluation
        /// </summary>
        private void CompleteHandAndContinue(List<BaseDice> submittedDice)
        {
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
            
            // Check if we can start a new hand
            var (currentHand, handsRemaining) = cooldownSystem.GetHandCounter();
            if (handsRemaining > 0)
            {
                // Start next hand after a brief delay
                StartCoroutine(DelayedStartNewHand());
            }
            else
            {
                Debug.Log("[BattleController] All hands completed! Evaluating target score...");
                // Update UI to show battle is complete
                UpdateRollAndCastCount();
                
                // Trigger target score evaluation animation
                StartCoroutine(EvaluateTargetScore());
            }
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
            
            // Reset hand manager roll budget
            _handManager.Reset();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
                // Reset score animator
                if (scoreAnimator != null)
                {
                    scoreAnimator.ResetTotalScore();
                }
                
                // Refresh dice pool and hand counter
                cooldownSystem.RefreshDicePool();
                
                // Update displays
                UpdateLevelInfo();
                UpdateTargetScoreDisplay();
                UpdateRollAndCastCount();
                UpdateFeedback("Roll or Lock Dice");
                
                // Start a new hand after refresh
                StartNewHand();
                return;
            }
            
            // Normal reset behavior during active hands
            // Reset hand state using HandManager
            _handManager.Reset();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            // Reset dice states
            foreach (var d in _dice) d.ResetLockAndValue();
            
            // Refresh views using factory
            _viewFactory.RefreshViews(_views);
            
            // Start a new hand
            StartNewHand();
            
            Debug.Log("[BattleController] Hand reset complete.");
        }

        /// <summary>
        /// Update feedback message in combo score text (minimal tips only)
        /// </summary>
        void UpdateFeedback(string msg, bool isWarning = false)
        {
            if (scoreAnimator != null && scoreAnimator.comboScoreText != null)
            {
                // Only show feedback if not currently animating a score
                if (!scoreAnimator.IsAnimating)
                {
                    // White color for idle tips, red color for warnings
                    string color = isWarning ? "#FF6666" : "#FFFFFF";
                    scoreAnimator.comboScoreText.text = $"<color={color}>{msg}</color>";
                    
                    // Ensure text is visible (reset alpha in case it was faded out)
                    var color2 = scoreAnimator.comboScoreText.color;
                    color2.a = 1f;
                    scoreAnimator.comboScoreText.color = color2;
                }
            }
            Debug.Log($"[BattleController] Feedback: {msg}");
        }

        /// <summary>
        /// Update roll count and cast count displays
        /// </summary>
        private void UpdateRollAndCastCount()
        {
            // Update roll count: shows remaining rolls (just the number)
            if (rollCountText != null)
            {
                int remainingRolls = Mathf.Max(0, maxRollsPerHand - _handManager.TotalRollsUsed);
                rollCountText.text = remainingRolls.ToString();
            }
            
            // Update cast count: shows remaining casts (based on remaining hands)
            if (castCountText != null)
            {
                var (current, remaining) = cooldownSystem.GetHandCounter();
                int remainingCasts = Mathf.Max(0, remaining);
                castCountText.text = remainingCasts.ToString();
            }
        }


        /// <summary>
        /// Update deck status display showing all dice and their states
        /// </summary>
        private void UpdateDeckStatus()
        {
            // This is now handled by the backpack system.
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
        /// Update level info display
        /// </summary>
        private void UpdateLevelInfo()
        {
            if (levelInfoText != null)
            {
                levelInfoText.text = $"Level {_progressionManager.CurrentLevel}";
            }
        }

        /// <summary>
        /// Open settings panel
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                Debug.Log("[BattleController] Settings panel opened");
            }
        }
        
        /// <summary>
        /// Close settings panel
        /// </summary>
        private void CloseSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                Debug.Log("[BattleController] Settings panel closed");
            }
        }
        
        /// <summary>
        /// Reset game to initial state (from settings panel)
        /// </summary>
        private void OnSettingsResetClicked()
        {
            Debug.Log("[BattleController] Resetting game to initial state...");
            
            // Close settings panel
            CloseSettingsPanel();
            
            // Reset progression to level 1
            _progressionManager.ResetToLevelOne();
            
            // Reset hand manager roll budget
            _handManager.Reset();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            // Reset score animator
            if (scoreAnimator != null)
            {
                scoreAnimator.ResetTotalScore();
            }
            
            // Clear current dice and views
            _dice.Clear();
            _viewFactory.DestroyViews(_views);
            
            // Reset static state
            ContinuingFromReward = false;
            PendingLevel = 1;
            PendingTargetScore = baseTargetScore;
            
            // Refresh dice pool and hand counter
            cooldownSystem.RefreshDicePool();
            
            // Hide continue button if visible
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            
            // Update displays
            UpdateLevelInfo();
            UpdateTargetScoreDisplay();
            UpdateRollAndCastCount();
            UpdateFeedback("Roll or Lock Dice");
            
            // Start a new hand
            StartNewHand();
            
            Debug.Log("[BattleController] Game reset complete");
        }
        
        /// <summary>
        /// Quit game (from settings panel)
        /// </summary>
        private void OnSettingsQuitClicked()
        {
            Debug.Log("[BattleController] Quitting game...");
            
            // Close settings panel
            CloseSettingsPanel();
            
            // Quit application (works in builds)
            // In editor, this will stop play mode
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        /// <summary>
        /// Update combo preview display based on currently locked dice
        /// Shows default combo values only (no dice/relic effects)
        /// </summary>
        private void UpdateComboPreview()
        {
            // Get locked dice values (only dice that are locked and have been rolled)
            var lockedValues = _dice
                .Where(d => d.isLocked && d.lastRollValue > 0 && d.tier != DiceTier.Filler)
                .Select(d => d.lastRollValue)
                .ToList();
            
            if (lockedValues.Count == 0)
            {
                // No dice locked - show default state
                if (comboNameText != null) comboNameText.text = "No Combo";
                if (comboBaseText != null) comboBaseText.text = "<color=#CCCCCC>0</color>";
                if (comboMultiplierText != null) comboMultiplierText.text = "<color=#CCCCCC>1.0</color>";
                return;
            }
            
            // Use ScoreCalculator to preview combo (default values only)
            var (comboName, baseScore, multiplier) = _scoreCalculator.PreviewCombo(lockedValues);
            
            // Update UI
            // Combo name stays as is
            if (comboNameText != null) comboNameText.text = comboName;
            
            // Base score: just the number, orange color (#FF8C00 - DarkOrange)
            if (comboBaseText != null) comboBaseText.text = $"<color=#FF8C00><b>{baseScore}</b></color>";
            
            // Multiplier: just the number, blue color (#4A90E2 - Nice blue)
            if (comboMultiplierText != null) comboMultiplierText.text = $"<color=#4A90E2>{multiplier:F1}</color>";
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

            // Reset roll budget for new level
            _handManager.Reset();
            _handManager.SetMaxRolls(maxRollsPerHand);

            // Reset dice pool and hand counter
            cooldownSystem.RefreshDicePool();

            // Update displays
            UpdateLevelInfo();
            UpdateTargetScoreDisplay();
            UpdateRollAndCastCount();
            UpdateFeedback("Roll or Lock Dice");

            // Start first hand of new level
            StartNewHand();
        }

        /// <summary>
        /// Evaluate if player passed target score with dramatic animation
        /// </summary>
        private System.Collections.IEnumerator EvaluateTargetScore()
        {
            if (DiceTooltipManager.Instance != null)
                DiceTooltipManager.Instance.HideTooltip();

            // No wait needed - animation already completed before this is called

            int finalScore = scoreAnimator != null ? scoreAnimator.GetTotalScore() : _progressionManager.TotalScore;
            bool passed = _progressionManager.EvaluateTargetScore();

            // Trigger pass/fail animation in ScoreAnimator
            if (scoreAnimator != null)
            {
                scoreAnimator.AnimateTargetEvaluation(finalScore, _progressionManager.TargetScore, passed);
            }
            else
            {
                // Fallback if no animator - show idle message
                UpdateFeedback("Roll or Lock Dice");
            }

            // Wait for evaluation animation to complete, then show Continue button ONLY if passed
            yield return new UnityEngine.WaitForSeconds(4.5f);
            
            if (passed)
            {
                // Prepare next level state for when we return from RewardScene
                int nextLevel = _progressionManager.CurrentLevel + 1;
                int nextTarget = _progressionManager.CalculateTargetScore(nextLevel);

                PendingLevel = nextLevel;
                PendingTargetScore = nextTarget;
                ContinuingFromReward = true;

                // Transition to reward scene
                Debug.Log($"[BattleController] Target passed! Loading RewardScene. Next Level: {nextLevel}, Next Target: {nextTarget}");
                SceneManager.LoadScene("RewardScene");
            }
            else
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
            
            _handManager.Reset();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            // Update cast count when pool refreshes
            UpdateRollAndCastCount();
            
            // Start a new hand with refreshed dice
            StartNewHand();
        }


        /// <summary>
        /// Called when available dice list changes
        /// </summary>
        private void OnAvailableDiceChanged(List<BaseDice> availableDice)
        {
            Debug.Log($"[BattleController] Available dice changed: {availableDice.Count} dice available");
            
            // Log details
            var sb = new StringBuilder();
            sb.AppendLine($"Available dice: {availableDice.Count}/8");
            sb.AppendLine("Dice pool:");
            foreach (var dice in availableDice)
            {
                sb.AppendLine($"  - {dice.diceName} ({dice.tier}, cost: {dice.cost})");
            }
            
            // Add hand counter info to debug log
            var (current, remaining) = cooldownSystem.GetHandCounter();
            sb.AppendLine($"\nHands: {current + 1}/{current + remaining} ({remaining} remaining)");
            
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
                cooldownSystem.OnAvailableDiceChanged -= OnAvailableDiceChanged;
            }
            
            // Unsubscribe from dice lock changes
            DiceView.OnDiceLockChanged -= UpdateComboPreview;
        }
    }
}
