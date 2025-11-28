using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;
using DiceGame.Analytics;
using DiceGame.Relics;
using DiceGame.UI;
using DiceGame.Audio;
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
        public int baseTargetScore = 300; // Starting target score

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
        
        // Services
        private BattleUIUpdater _uiUpdater;
        private BattleStateManager _stateManagerService;

        // Current hand state (shared with HandFlowController)
        public readonly List<BaseDice> _dice = new();
        public readonly List<DiceView> _views = new();

        void Awake()
        {
            // Auto-wire optional combo panel so initialization does not crash on missing inspector references
            if (comboPreferencePanel == null)
            {
                comboPreferencePanel = GetComponentInChildren<ComboPreferencePanel>(true);
                if (comboPreferencePanel == null)
                {
                    comboPreferencePanel = Object.FindObjectOfType<ComboPreferencePanel>(true);
                }
            }
        }

        void Start()
        {
            _stateManager = GameStateManager.Instance;
            
            // Initialize SoundManager early to ensure it's ready for button clicks
            if (SoundManager.Instance != null)
            {
                Debug.Log("[BattleController] SoundManager initialized");
            }
            
            // Use BattleInitializer to handle all initialization logic
            var initializer = new BattleInitializer();
            var result = initializer.Initialize(this, _stateManager);
            
            if (result == null)
            {
                Debug.LogError("[BattleController] Initialization failed!");
                return;
            }
            
            // Store initialized components for use by other methods
            _handManager = result.HandManager;
            _effectHandler = result.EffectHandler;
            _viewFactory = result.ViewFactory;
            _relicManager = result.RelicManager;
            _diceManager = result.DiceManager;
            _scoreCalculator = result.ScoreCalculator;
            _progressionManager = result.ProgressionManager;
            _uiPresenter = result.UIPresenter;
            _compositionService = result.CompositionService;
            _moneyManager = result.MoneyManager;
            
            // Initialize services (must be done after components are stored)
            InitializeServices();
            
            // Refresh UI after services are initialized
            // This ensures UI updater is ready and all systems are initialized
            RefreshAllUI();
        }
        
        /// <summary>
        /// Initialize UI updater and state manager services
        /// </summary>
        private void InitializeServices()
        {
            // Initialize UI updater
            _uiUpdater = new BattleUIUpdater();
            _uiUpdater.Initialize(
                battleUI,
                _handManager,
                _progressionManager,
                _moneyManager,
                _scoreCalculator,
                cooldownSystem,
                _dice,
                maxRollsPerHand
            );
            
            // Initialize state manager
            _stateManagerService = new BattleStateManager();
            _stateManagerService.Initialize(
                this,
                _stateManager,
                _handManager,
                _progressionManager,
                _moneyManager,
                _relicManager,
                _diceManager,
                _viewFactory,
                cooldownSystem,
                scoreAnimator,
                backpackManager,
                _dice,
                _views,
                maxRollsPerHand,
                baseTargetScore
            );
        }
        
        // All initialization methods have been moved to BattleInitializer service class

        /// <summary>
        /// Add a relic to the player's backpack (called by shop, rewards, etc.)
        /// Uses PlayerResourceManager for cross-scene persistence
        /// </summary>
        /// <param name="relic">The relic to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRelicToPlayerBackpack(RelicBase relic)
        {
            // Use PlayerResourceManager to add relic (automatically saves to SaveData)
            bool success = PlayerResourceManager.Instance?.AddRelicToBackpack(relic) ?? false;
            
            if (success)
            {
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
        /// Uses PlayerResourceManager for cross-scene persistence
        /// </summary>
        /// <param name="relicName">Name of the relic to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRelicToPlayerBackpackByName(string relicName)
        {
            // Use PlayerResourceManager to add relic (automatically saves to SaveData)
            bool success = PlayerResourceManager.Instance?.AddRelicToBackpackByName(relicName) ?? false;
            
            if (success)
            {
                if (relicDisplay != null)
                {
                    // Refresh UI to show the new relic
                    relicDisplay.DisplayRelics(_relicManager);
                }
            }
            
            return success;
        }

        // IntegrateRewardDice has been moved to BattleInitializer service class
        
        /// <summary>
        /// Update CooldownSystem with dice from player backpack
        /// (Still needed by other methods like ResetForNewHand)
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
        }


    /// <summary>
    /// Start a new hand (delegated to HandFlowController)
    /// </summary>
    public void StartNewHand()
    {
        handFlowController?.StartNewHand();
    }

    public void OnDiceSelectedFromBackpack(List<BaseDice> selectedDice)
    {
        // Delegate to HandFlowController
        handFlowController?.OnDiceSelectedFromBackpack(selectedDice);
    }

        // Hand flow methods moved to HandFlowController

        void ResetForNewHand()
        {
            // Hide continue button if visible
            System.Action hideContinueButton = () =>
            {
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(false);
                }
            };
            
            _stateManagerService?.ResetForNewHand(
                RefreshAllUI,
                UpdateFeedback,
                StartNewHand,
                UpdateCooldownSystemFromBackpack
            );
        }

        /// <summary>
        /// Update feedback message
        /// </summary>
        public void UpdateFeedback(string msg, bool isWarning = false)
        {
            _uiUpdater?.UpdateFeedback(msg, isWarning);
        }

        /// <summary>
        /// Update roll count and cast count displays
        /// </summary>
        public void UpdateRollAndCastCount()
        {
            _uiUpdater?.UpdateRollAndCastCount();
        }

        /// <summary>
        /// Update money display
        /// </summary>
        public void UpdateMoneyDisplay()
        {
            _uiUpdater?.UpdateMoneyDisplay();
        }

        /// <summary>
        /// Get current money amount (for shop, etc.)
        /// Uses PlayerResourceManager for cross-scene access
        /// </summary>
        public int GetMoney()
        {
            return PlayerResourceManager.Instance?.GetMoney() ?? 0;
        }

        /// <summary>
        /// Add money (for shop, rewards, etc.)
        /// Uses PlayerResourceManager for cross-scene access
        /// </summary>
        public void AddMoney(int amount)
        {
            PlayerResourceManager.Instance?.AddMoney(amount);
            UpdateMoneyDisplay();
        }

        /// <summary>
        /// Spend money (for shop purchases, etc.)
        /// Uses PlayerResourceManager for cross-scene access
        /// </summary>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool SpendMoney(int amount)
        {
            bool success = PlayerResourceManager.Instance?.SpendMoney(amount) ?? false;
            if (success)
            {
                UpdateMoneyDisplay();
            }
            return success;
        }

        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshAllUI()
        {
            _uiUpdater?.RefreshAllUI();
        }

        /// <summary>
        /// Transition from tutorial mode to normal game (called by TutorialController)
        /// </summary>
        public void CompleteTutorialAndStartLevel1()
        {
            _stateManagerService?.CompleteTutorialAndStartLevel1(
                RefreshAllUI,
                UpdateFeedback,
                StartNewHand
            );
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
            }
        }
        
        /// <summary>
        /// Reset game to initial state (called by SettingsPanel)
        /// </summary>
        public void OnSettingsResetClicked()
        {
            System.Action hideContinueButton = () =>
            {
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(false);
                }
            };
            
            _stateManagerService?.ResetGame(
                RefreshAllUI,
                UpdateFeedback,
                StartNewHand,
                hideContinueButton,
                UpdateCooldownSystemFromBackpack
            );
        }
        
        /// <summary>
        /// Quit game (called by SettingsPanel)
        /// </summary>
        public void OnSettingsQuitClicked()
        {
            _stateManagerService?.QuitGame();
        }

        public void OnSettingsCheatClicked()
        {
            Debug.Log("[BattleController] Cheat mode activated!");

            // 1. Add ALL relics
            var allRelics = _relicManager.GlobalRelicPool;
            foreach (var relic in allRelics)
            {
                PlayerResourceManager.Instance.AddRelicToBackpack(relic);
            }

            // 2. Add ALL dice
            var allDice = _diceManager.GlobalDicePool;
            foreach (var d in allDice)
            {
                PlayerResourceManager.Instance.AddDiceToBackpack(d);
            }

            // 3. Refresh UI and backpack
            if (relicDisplay != null)
                relicDisplay.DisplayRelics(_relicManager);

            UpdateCooldownSystemFromBackpack();
            RefreshAllUI();

            Debug.Log("[BattleController] Cheat completed: All dice + relics granted.");
        }

        /// <summary>
        /// Update combo preview display based on currently locked dice
        /// Shows default combo values only (no dice/relic effects)
        /// </summary>
        public void UpdateComboPreview()
        {
            _uiUpdater?.UpdateComboPreview();
        }

        /// <summary>
        /// Continue to next level - reset game state and increase target score
        /// </summary>
        public void OnContinue()
        {
            System.Action hideContinueButton = () =>
            {
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(false);
                }
            };
            
            _stateManagerService?.ContinueToNextLevel(
                RefreshAllUI,
                UpdateFeedback,
                StartNewHand,
                hideContinueButton
            );
        }

        // EvaluateTargetScore moved to HandFlowController

        #region CooldownSystem Event Handlers

        /// <summary>
        /// Called when dice pool refreshes (manual refresh via Reset button)
        /// </summary>
        public void OnDicePoolRefresh()
        {
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
        public void OnAvailableDiceChanged(List<BaseDice> availableDice)
        {
            // Dice availability changed - UI will update automatically
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
                backpackManager.openBackpackButton.onClick.RemoveListener(OnBackpackButtonPressed);
            }
            
            // Clean up settings panel events
            if (settingsPanel != null)
            {
                settingsPanel.OnResetRequested -= OnSettingsResetClicked;
                settingsPanel.OnQuitRequested -= OnSettingsQuitClicked;
                settingsPanel.OnCheatRequested -= OnSettingsCheatClicked;
            }
        }
    }
}
