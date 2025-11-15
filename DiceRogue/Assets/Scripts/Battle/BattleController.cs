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
        // Game state manager (replaces static variables)
        private GameStateManager _stateManager;
        
        [Header("UI - Main Game")]
        public Transform diceRowParent;      // Container for DiceView
        public GameObject diceViewPrefab;  // Prefab (with DiceView component)
        public Button rollButton;
        public Button submitComboButton;
        public Button continueButton;
        
        [Header("UI - Settings Panel")]
        public SettingsPanel settingsPanel;  // Settings panel component
        
        [Header("Scene Transition")]
        public SceneTransitionManager sceneTransitionManager; // Scene transition manager
        
        [Header("UI - Combo Preference Panel")]
        public ComboPreferencePanel comboPreferencePanel;  // Combo preference panel component
        
        [Header("Hand Flow")]
        public HandFlowController handFlowController;  // Hand flow controller component
        
        [Header("UI - Battle UI Component")]
        public BattleUI battleUI;           // UI manager component
        
        [Header("UI - Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system (still needed for animation)
        
        [Header("Backpack")]
        public BackpackManager backpackManager;
        
        [Header("Relic Display")]
        public RelicDisplay relicDisplay;   // Visual display for equipped relics

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 5;   // Shared roll budget across all hands
        public int baseTargetScore = 200; // Starting target score

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem; // Reference to cooldown system

        // Core components
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceViewFactory _viewFactory;
        private RelicManager _relicManager;
        private DiceManager _diceManager;  // Dice manager (global pool + player backpack)
        private ScoreCalculator _scoreCalculator;
        private ProgressionManager _progressionManager;
        private BattleUIPresenter _uiPresenter;
        private HandCompositionService _compositionService;
        private MoneyManager _moneyManager;

        // Current hand state (shared with HandFlowController)
        public readonly List<BaseDice> _dice = new();
        public readonly List<DiceView> _views = new();

        void Start()
        {
            InitializeStateManager();
            InitializeAnalytics();
            
            if (!InitializeRequiredComponents())
            {
                return; // Critical components missing, abort initialization
            }
            
            InitializeCoreComponents();
            InitializeUI();
            InitializeManagers();
            InitializePanels();
            InitializeEvents();
            StartGame();
            
            Debug.Log("[BattleController] Battle scene initialized with decoupled components.");
        }
        
        /// <summary>
        /// Initialize state manager
        /// </summary>
        private void InitializeStateManager()
        {
            _stateManager = GameStateManager.Instance;
        }
        
        /// <summary>
        /// Initialize required Unity components (cooldown system, score animator)
        /// Returns false if critical components are missing
        /// </summary>
        private bool InitializeRequiredComponents()
        {
            // Initialize cooldown system if not assigned
            if (cooldownSystem == null)
            {
                cooldownSystem = FindObjectOfType<CooldownSystem>();
                if (cooldownSystem == null)
                {
                    Debug.LogError("[BattleController] CooldownSystem not found! Please assign it in the inspector.");
                    return false;
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
            
            return true;
        }
        
        /// <summary>
        /// Initialize core game components (managers, calculators, services)
        /// </summary>
        private void InitializeCoreComponents()
        {
            _handManager = new HandManager();
            _handManager.SetMaxRolls(maxRollsPerHand);
            
            _effectHandler = new DiceEffectHandler();
            _viewFactory = new DiceViewFactory(diceViewPrefab, diceRowParent);
            _relicManager = new RelicManager();
            
            // Initialize dice manager
            _diceManager = new DiceManager();
            _diceManager.InitializeGlobalDicePool();
            
            _scoreCalculator = new ScoreCalculator();
            _uiPresenter = new BattleUIPresenter();
            _compositionService = new HandCompositionService();
            _moneyManager = new MoneyManager(_stateManager.SaveData.money);
        }
        
        /// <summary>
        /// Initialize UI components and link them together
        /// </summary>
        private void InitializeUI()
        {
            // Link money text to score animator for money animation
            if (scoreAnimator != null && battleUI != null && battleUI.moneyText != null)
            {
                scoreAnimator.moneyText = battleUI.moneyText;
            }
            
            // Initialize BattleUI (after _uiPresenter is created)
            if (battleUI != null)
            {
                battleUI.Initialize(_uiPresenter);
            }
            else
            {
                Debug.LogWarning("[BattleController] BattleUI component not assigned!");
            }
            
            // Initialize and hide continue button
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
                continueButton.onClick.AddListener(OnContinue);
            }
            
            // Set up main game UI buttons (delegated to HandFlowController)
            // Note: Buttons will be connected after HandFlowController is initialized
        }
        
        /// <summary>
        /// Initialize game managers (backpack, progression, relic system)
        /// </summary>
        private void InitializeManagers()
        {
            // Initialize backpack manager
            if (backpackManager != null)
            {
                // Initialize with DiceManager for enhanced functionality
                backpackManager.Initialize(cooldownSystem, _diceManager, OnDiceSelectedFromBackpack);
                
                // Set up open backpack button listener
                if (backpackManager.openBackpackButton != null)
                {
                    backpackManager.openBackpackButton.onClick.AddListener(OpenBackpackForViewing);
                }
                else
                {
                    Debug.LogWarning("[BattleController] BackpackManager.openBackpackButton is not assigned!");
                }
            }
            else
            {
                Debug.LogError("[BattleController] BackpackManager not assigned!");
            }
            
            // Initialize progression manager
            if (_stateManager.State.ContinuingFromReward)
            {
                _progressionManager = new ProgressionManager(baseTargetScore);
                _progressionManager.RestoreLevelState(_stateManager.State.PendingLevel, _stateManager.State.PendingTargetScore);
                _stateManager.State.ContinuingFromReward = false;
                Debug.Log($"[BattleController] Continuing from Reward Scene - Level {_stateManager.State.PendingLevel}, Target: {_stateManager.State.PendingTargetScore}");
            }
            else if (_stateManager.State.IsTutorialMode)
            {
                _progressionManager = new ProgressionManager(baseTargetScore);
                _progressionManager.InitializeTutorialMode();
                Debug.Log("[BattleController] Initialized in Tutorial Mode (Level 0)");
            }
            else
            {
                _progressionManager = new ProgressionManager(baseTargetScore);
            }
            
            // Initialize relic system: load from save data
            InitializeRelicSystem();
            
            // Initialize dice system: load from save data and update cooldown system
            InitializeDiceSystem();
            
            // Check if there are pending reward dice from reward scene
            IntegrateRewardDice();
        }
        
        /// <summary>
        /// Initialize UI panels (settings, combo preference)
        /// </summary>
        private void InitializePanels()
        {
            // Initialize settings panel
            if (settingsPanel != null)
            {
                settingsPanel.Initialize();
                settingsPanel.OnResetRequested += OnSettingsResetClicked;
                settingsPanel.OnQuitRequested += OnSettingsQuitClicked;
            }
            else
            {
                Debug.LogWarning("[BattleController] SettingsPanel component not assigned!");
            }
            
            // Initialize scene transition manager (create if not assigned)
            if (sceneTransitionManager == null)
            {
                sceneTransitionManager = gameObject.AddComponent<SceneTransitionManager>();
                Debug.Log("[BattleController] Created SceneTransitionManager component");
            }
            
            // Initialize combo preference panel
            if (comboPreferencePanel != null)
            {
                comboPreferencePanel.Initialize();
            }
            else
            {
                Debug.LogWarning("[BattleController] ComboPreferencePanel component not assigned!");
            }
            
            // Initialize hand flow controller (create if not assigned)
            if (handFlowController == null)
            {
                handFlowController = gameObject.AddComponent<HandFlowController>();
                Debug.Log("[BattleController] Created HandFlowController component");
            }
        }
        
        /// <summary>
        /// Subscribe to events from various systems
        /// </summary>
        private void InitializeEvents()
        {
            // Subscribe to cooldown system events
            cooldownSystem.OnDicePoolRefresh += OnDicePoolRefresh;
            cooldownSystem.OnAvailableDiceChanged += OnAvailableDiceChanged;
            
            // Subscribe to dice lock changes for combo preview updates
            DiceView.OnDiceLockChanged += UpdateComboPreview;
        }
        
        /// <summary>
        /// Start the game (refresh UI, track analytics, start first hand, activate tutorial if needed)
        /// </summary>
        private void StartGame()
        {
            RefreshAllUI();
            
            // Track initial player progression
            UnityGameAnalytics.TrackPlayerProgression(_progressionManager.TotalScore, 0, _progressionManager.CurrentLevel);
            
            // Initialize hand flow controller with dependencies
            handFlowController.Initialize(
                _handManager,
                _effectHandler,
                _viewFactory,
                _relicManager,
                _scoreCalculator,
                _progressionManager,
                _compositionService,
                _moneyManager,
                _stateManager,
                cooldownSystem,
                backpackManager,
                scoreAnimator,
                battleUI,
                sceneTransitionManager,
                submitComboButton,
                diceCount,
                maxRollsPerHand,
                _dice,
                _views
            );
            
            // Set up callbacks for UI updates
            handFlowController.OnComboPreviewUpdate += UpdateComboPreview;
            handFlowController.OnRollAndCastCountUpdate += UpdateRollAndCastCount;
            handFlowController.OnFeedbackUpdate += UpdateFeedback;
            handFlowController.OnMoneyDisplayUpdate += UpdateMoneyDisplay;
            
            // Set up main game UI buttons (after HandFlowController is initialized)
            if (rollButton != null)
            {
                rollButton.onClick.AddListener(() => handFlowController.OnRollOnce());
            }
            if (submitComboButton != null)
            {
                submitComboButton.onClick.AddListener(() => handFlowController.OnSubmitCombo());
            }
            
            // Start first hand with a delay to ensure all systems are ready
            StartCoroutine(DelayedStartFirstHand());
            
            // Activate TutorialController if in tutorial mode
            if (_stateManager.State.IsTutorialMode)
            {
                var tutorialController = FindObjectOfType<DiceGame.Tutorial.TutorialController>(true);
                if (tutorialController != null)
                {
                    tutorialController.gameObject.SetActive(true);
                    Debug.Log("[BattleController] TutorialController activated for tutorial mode");
                }
                else
                {
                    Debug.LogWarning("[BattleController] TutorialController not found - tutorial may not work!");
                }
            }
        }

        private System.Collections.IEnumerator DelayedStartFirstHand()
        {
            yield return null; // Wait one frame
            handFlowController?.StartNewHand();
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
        /// Initialize relic system: load from save data
        /// </summary>
        private void InitializeRelicSystem()
        {
            // Initialize global relic pool (all relics available this run)
            _relicManager.InitializeGlobalRelicPool();
            
            // Load relics from save data
            foreach (var relicName in _stateManager.SaveData.relicNames)
            {
                _relicManager.AddRelicToBackpackByName(relicName);
            }
            
            // Give player a random starting relic if this is a new game (no relics and not continuing from reward)
            if (_relicManager.PlayerBackpack.Count == 0 && !_stateManager.State.ContinuingFromReward)
            {
                GiveRandomStartingRelic();
            }
            
            // Update relic display UI
            if (relicDisplay != null)
            {
                relicDisplay.DisplayRelics(_relicManager);
            }
        }

        /// <summary>
        /// Initialize dice system: load from save data and update cooldown system
        /// </summary>
        private void InitializeDiceSystem()
        {
            // Load player dice backpack from save data
            if (_diceManager != null)
            {
                _diceManager.LoadFromSaveData(_stateManager.SaveData);
                
                // Update CooldownSystem with backpack dice
                UpdateCooldownSystemFromBackpack();
            }
        }

        /// <summary>
        /// Update CooldownSystem with dice from player backpack
        /// </summary>
        private void UpdateCooldownSystemFromBackpack()
        {
            if (_diceManager == null || cooldownSystem == null)
            {
                return;
            }

            // Get dice from player backpack
            var backpackDice = _diceManager.PlayerDiceBackpack.ToList();
            
            // Update CooldownSystem
            cooldownSystem.SetPlayerBackpackDice(backpackDice);
            
            Debug.Log($"[BattleController] Updated CooldownSystem with {backpackDice.Count} dice from backpack");
        }

        /// <summary>
        /// Give player a random relic from the global pool as starting relic
        /// </summary>
        private void GiveRandomStartingRelic()
        {
            var globalPool = _relicManager.GlobalRelicPool;
            if (globalPool == null || globalPool.Count == 0)
            {
                Debug.LogWarning("[BattleController] Cannot give starting relic - global pool is empty!");
                return;
            }

            // Randomly select a relic from the global pool
            int randomIndex = Random.Range(0, globalPool.Count);
            var startingRelic = globalPool[randomIndex];
            
            if (startingRelic != null)
            {
                bool success = _relicManager.AddRelicToBackpack(startingRelic);
                if (success)
                {
                    Debug.Log($"[BattleController] Gave player starting relic: {startingRelic.relicName} ({startingRelic.rarity})");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] Failed to add starting relic: {startingRelic.relicName}");
                }
            }
        }

        /// <summary>
        /// Add a relic to the player's backpack (called by shop, rewards, etc.)
        /// </summary>
        /// <param name="relic">The relic to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRelicToPlayerBackpack(RelicBase relic)
        {
            bool success = _relicManager.AddRelicToBackpack(relic);
            
            if (success)
            {
                // Save to persistence
                if (!_stateManager.SaveData.relicNames.Contains(relic.relicName))
                {
                    _stateManager.SaveData.relicNames.Add(relic.relicName);
                    _stateManager.Save();
                }
                
                if (relicDisplay != null)
                {
                    // Refresh UI to show the new relic
                    relicDisplay.DisplayRelics(_relicManager);
                }
            }
            
            return success;
        }

        /// <summary>
        /// Add a relic to the player's backpack by name (searches global pool)
        /// </summary>
        /// <param name="relicName">Name of the relic to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRelicToPlayerBackpackByName(string relicName)
        {
            bool success = _relicManager.AddRelicToBackpackByName(relicName);
            
            if (success)
            {
                // Save to persistence
                if (!_stateManager.SaveData.relicNames.Contains(relicName))
                {
                    _stateManager.SaveData.relicNames.Add(relicName);
                    _stateManager.Save();
                }
                
                if (relicDisplay != null)
                {
                    // Refresh UI to show the new relic
                    relicDisplay.DisplayRelics(_relicManager);
                }
            }
            
            return success;
        }

        /// <summary>
        /// Integrate reward dice from reward scene into the dice backpack
        /// </summary>
        private void IntegrateRewardDice()
        {
            // Check if there are pending reward dice
            if (_stateManager.State.PendingDiceTypeIds.Count == 0)
            {
                Debug.Log("[BattleController] No pending reward dice to integrate");
                return;
            }

            Debug.Log($"[BattleController] Found {_stateManager.State.PendingDiceTypeIds.Count} reward dice to integrate");

            // Use DiceManager to add dice to backpack
            foreach (var typeId in _stateManager.State.PendingDiceTypeIds)
            {
                bool success = _diceManager.AddDiceToBackpackByName(typeId);
                if (success)
                {
                    Debug.Log($"[BattleController] Added reward dice to backpack: {typeId}");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] Failed to add reward dice: {typeId}");
                }
            }

            // Clear pending list
            _stateManager.State.PendingDiceTypeIds.Clear();

            // Save to persistence
            _diceManager.SaveToSaveData(_stateManager.SaveData);
            _stateManager.Save();

            // Update CooldownSystem with backpack dice
            UpdateCooldownSystemFromBackpack();

            Debug.Log($"[BattleController] Integrated reward dice. Backpack size: {_diceManager.PlayerDiceBackpack.Count}");
        }

        /// <summary>
        /// Create a dice instance from type ID string
        /// NOTE: This method is kept for backward compatibility but is now deprecated.
        /// Use DiceManager.AddDiceToBackpackByName() instead.
        /// </summary>
        [System.Obsolete("Use DiceManager.AddDiceToBackpackByName() instead")]
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
    /// Start a new hand (delegated to HandFlowController)
    /// </summary>
    public void StartNewHand()
    {
        handFlowController?.StartNewHand();
    }

    private void OnDiceSelectedFromBackpack(List<BaseDice> selectedDice)
    {
        // Delegate to HandFlowController
        handFlowController?.OnDiceSelectedFromBackpack(selectedDice);
    }

        // Hand flow methods moved to HandFlowController

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
                
            // Reset relic system and give new random starting relic
            _relicManager.ClearBackpack();
            _stateManager.SaveData.relicNames.Clear();
            InitializeRelicSystem();
            
            // Reset dice backpack (game over / restart)
            if (_diceManager != null)
            {
                _diceManager.ClearBackpack();
                _diceManager.SaveToSaveData(_stateManager.SaveData);
            }
            
            // Reset money (game over / restart)
            _moneyManager.Reset();
            _stateManager.SaveData.money = 0;
            _stateManager.Save();
            
            // Refresh dice pool and hand counter
            UpdateCooldownSystemFromBackpack();
            cooldownSystem.RefreshDicePool();
            
            // Update displays
            RefreshAllUI();
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
        /// Update feedback message
        /// </summary>
        void UpdateFeedback(string msg, bool isWarning = false)
        {
            battleUI?.UpdateFeedback(msg, isWarning);
        }

        /// <summary>
        /// Update roll count and cast count displays
        /// </summary>
        private void UpdateRollAndCastCount()
        {
            if (battleUI == null) return;
            
            int remainingRolls = _handManager != null 
                ? Mathf.Max(0, maxRollsPerHand - _handManager.TotalRollsUsed) 
                : 0;
            
            int remainingCasts = 0;
            if (cooldownSystem != null)
            {
                var (current, remaining) = cooldownSystem.GetHandCounter();
                remainingCasts = Mathf.Max(0, remaining);
            }
            
            battleUI.UpdateRollAndCastCount(remainingRolls, remainingCasts);
        }

        /// <summary>
        /// Update money display
        /// </summary>
        private void UpdateMoneyDisplay()
        {
            if (battleUI != null && _moneyManager != null)
            {
                battleUI.UpdateMoney(_moneyManager.Money);
            }
        }

        /// <summary>
        /// Get current money amount (for shop, etc.)
        /// </summary>
        public int GetMoney()
        {
            return _moneyManager?.Money ?? 0;
        }

        /// <summary>
        /// Add money (for shop, rewards, etc.)
        /// </summary>
        public void AddMoney(int amount)
        {
            if (_moneyManager != null)
            {
                _moneyManager.Add(amount);
                _stateManager.SaveData.money = _moneyManager.Money;
                _stateManager.Save();
                UpdateMoneyDisplay();
            }
        }

        /// <summary>
        /// Spend money (for shop purchases, etc.)
        /// </summary>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool SpendMoney(int amount)
        {
            if (_moneyManager != null && _moneyManager.Subtract(amount))
            {
                _stateManager.SaveData.money = _moneyManager.Money;
                _stateManager.Save();
                UpdateMoneyDisplay();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Update target score display
        /// </summary>
        private void UpdateTargetScoreDisplay()
        {
            if (battleUI != null && _progressionManager != null)
            {
                battleUI.UpdateTargetScore(_progressionManager.TargetScore, _progressionManager.CurrentLevel);
            }
        }

        /// <summary>
        /// Update level info display
        /// </summary>
        private void UpdateLevelInfo()
        {
            if (battleUI != null && _progressionManager != null)
            {
                battleUI.UpdateLevelInfo(_progressionManager.CurrentLevel, _progressionManager.IsTutorialMode);
            }
        }
        
        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        private void RefreshAllUI()
        {
            UpdateTargetScoreDisplay();
            UpdateLevelInfo();
            UpdateComboPreview();
            UpdateRollAndCastCount();
            UpdateMoneyDisplay();
        }

        /// <summary>
        /// Transition from tutorial mode to normal game (called by TutorialController)
        /// </summary>
        public void CompleteTutorialAndStartLevel1()
        {
            if (_progressionManager != null && _progressionManager.IsTutorialMode)
            {
                _progressionManager.StartNormalGame();
                _stateManager.State.IsTutorialMode = false;
                _stateManager.SaveData.hasCompletedTutorial = true;
                _stateManager.Save();
                
                // Close backpack if open
                if (backpackManager != null)
                {
                    backpackManager.HideBackpack();
                }
                
                // Unlock all dice and reset their state
                foreach (var d in _dice)
                {
                    if (d != null)
                    {
                        d.ResetLockAndValue();
                    }
                }
                
                // Refresh dice views to reflect unlocked state
                if (_viewFactory != null)
                {
                    _viewFactory.RefreshViews(_views);
                }
                
                // Reset hand manager
                if (_handManager != null)
                {
                    _handManager.Reset();
                    _handManager.SetMaxRolls(maxRollsPerHand);
                }
                
                // Clear current dice and views to start fresh
                _dice.Clear();
                _viewFactory?.DestroyViews(_views);
                _views.Clear();
                
                // Update UI
                RefreshAllUI();
                UpdateFeedback("Roll or Lock Dice");
                
                // Start first hand of Level 1
                StartNewHand();
                
                Debug.Log("[BattleController] Tutorial completed - started Level 1");
            }
        }

        /// <summary>
        /// Open backpack for viewing (not selection mode)
        /// Called by Unity button or programmatically
        /// </summary>
        public void OnBackpackButtonPressed()
        {
            if (backpackManager != null)
            {
                backpackManager.ShowBackpack(BackpackMode.ViewOnly);
                Debug.Log("[BattleController] Backpack opened for viewing");
            }
        }
        
        /// <summary>
        /// Open backpack for viewing (not selection mode)
        /// Internal method that can be called programmatically
        /// </summary>
        private void OpenBackpackForViewing()
        {
            OnBackpackButtonPressed();
        }
        
        /// <summary>
        /// Reset game to initial state (called by SettingsPanel)
        /// </summary>
        private void OnSettingsResetClicked()
        {
            Debug.Log("[BattleController] Resetting game to initial state...");
            
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
            
            // Reset runtime state
            _stateManager.ResetState();
            
            // Reset relic system and give new random starting relic
            _relicManager.ClearBackpack();
            _stateManager.SaveData.relicNames.Clear();
            InitializeRelicSystem();
            
            // Reset dice backpack (settings reset)
            if (_diceManager != null)
            {
                _diceManager.ClearBackpack();
                _diceManager.SaveToSaveData(_stateManager.SaveData);
            }
            
            // Reset money (settings reset)
            _moneyManager.Reset();
            _stateManager.SaveData.money = 0;
            _stateManager.ResetSaveData();
            
            // Refresh dice pool and hand counter
            UpdateCooldownSystemFromBackpack();
            cooldownSystem.RefreshDicePool();
            
            // Hide continue button if visible
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            
            // Update displays
            RefreshAllUI();
            UpdateFeedback("Roll or Lock Dice");
            
            // Start a new hand
            StartNewHand();
            
            Debug.Log("[BattleController] Game reset complete");
        }
        
        /// <summary>
        /// Quit game (called by SettingsPanel)
        /// </summary>
        private void OnSettingsQuitClicked()
        {
            Debug.Log("[BattleController] Quitting game...");
            
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
            if (battleUI == null) return;
            
            // Get locked dice values (only dice that are locked and have been rolled)
            var lockedValues = _dice
                .Where(d => d.isLocked && d.lastRollValue > 0 && d.tier != DiceTier.Filler)
                .Select(d => d.lastRollValue)
                .ToList();
            
            if (lockedValues.Count == 0)
            {
                // No dice locked - show default state
                battleUI.UpdateComboPreviewEmpty();
                return;
            }
            
            // Use ScoreCalculator to preview combo (default values only)
            if (_scoreCalculator == null) return;
            var (comboName, baseScore, multiplier) = _scoreCalculator.PreviewCombo(lockedValues);
            
            // Update UI
            battleUI.UpdateComboPreview(comboName, baseScore, multiplier);
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
            RefreshAllUI();
            UpdateFeedback("Roll or Lock Dice");

            // Start first hand of new level
            StartNewHand();
        }

        // EvaluateTargetScore moved to HandFlowController

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
            
            // Clean up backpack button listener
            if (backpackManager != null && backpackManager.openBackpackButton != null)
            {
                backpackManager.openBackpackButton.onClick.RemoveListener(OpenBackpackForViewing);
            }
            
            // Clean up settings panel events
            if (settingsPanel != null)
            {
                settingsPanel.OnResetRequested -= OnSettingsResetClicked;
                settingsPanel.OnQuitRequested -= OnSettingsQuitClicked;
            }
        }
    }
}
