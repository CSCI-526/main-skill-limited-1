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
        
        // Services
        private BattleUIUpdater _uiUpdater;
        private BattleStateManager _stateManagerService;

        // Current hand state (shared with HandFlowController)
        public readonly List<BaseDice> _dice = new();
        public readonly List<DiceView> _views = new();

        void Start()
        {
            _stateManager = GameStateManager.Instance;
            
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
        /// Initialize relic system: load from save data
        /// (Still needed for reset operations)
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
        /// Give player a random relic from the global pool as starting relic
        /// (Still needed for reset operations)
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
            
            Debug.Log($"[BattleController] Updated CooldownSystem with {backpackDice.Count} dice from backpack");
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
                UpdateCooldownSystemFromBackpack,
                (relicManager) => InitializeRelicSystem()
            );
        }
        
        /// <summary>
        /// Quit game (called by SettingsPanel)
        /// </summary>
        public void OnSettingsQuitClicked()
        {
            _stateManagerService?.QuitGame();
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
        public void OnAvailableDiceChanged(List<BaseDice> availableDice)
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
