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
    /// 负责 BattleController 的所有初始化逻辑
    /// 提取自 BattleController 以简化其职责
    /// </summary>
    public class BattleInitializer
    {
        /// <summary>
        /// 初始化结果，包含所有创建的组件引用
        /// </summary>
        public class InitializationResult
        {
            // Core components
            public HandManager HandManager { get; set; }
            public DiceEffectHandler EffectHandler { get; set; }
            public DiceViewFactory ViewFactory { get; set; }
            public RelicManager RelicManager { get; set; }
            public DiceManager DiceManager { get; set; }
            public ScoreCalculator ScoreCalculator { get; set; }
            public ProgressionManager ProgressionManager { get; set; }
            public BattleUIPresenter UIPresenter { get; set; }
            public HandCompositionService CompositionService { get; set; }
            public MoneyManager MoneyManager { get; set; }
        }

        /// <summary>
        /// 初始化所有战斗系统
        /// </summary>
        public InitializationResult Initialize(BattleController controller, GameStateManager stateManager)
        {
            var result = new InitializationResult();

            // Step 1: Initialize analytics
            InitializeAnalytics();

            // Step 2: Initialize required Unity components
            if (!InitializeRequiredComponents(controller))
            {
                Debug.LogError("[BattleInitializer] Critical components missing, aborting initialization");
                return null;
            }

            // Step 3: Initialize core game components
            result.HandManager = InitializeHandManager(controller.maxRollsPerHand);
            result.EffectHandler = new DiceEffectHandler();
            result.ViewFactory = new DiceViewFactory(controller.diceViewPrefab, controller.diceRowParent);
            result.RelicManager = new RelicManager();
            result.DiceManager = InitializeDiceManager();
            result.ScoreCalculator = new ScoreCalculator();
            result.UIPresenter = new BattleUIPresenter();
            result.CompositionService = new HandCompositionService();
            result.MoneyManager = new MoneyManager(stateManager.SaveData.money);

            // Step 4: Initialize UI components
            InitializeUI(controller, result);

            // Step 5: Initialize managers
            InitializeManagers(controller, stateManager, result);

            // Step 6: Initialize panels
            InitializePanels(controller, stateManager);

            // Step 7: Initialize events
            InitializeEvents(controller, result);

            // Step 8: Start game
            StartGame(controller, stateManager, result);

            Debug.Log("[BattleInitializer] Battle scene initialized with decoupled components.");
            return result;
        }

        /// <summary>
        /// Initialize analytics system
        /// </summary>
        private void InitializeAnalytics()
        {
            if (Object.FindObjectOfType<UnityGameAnalytics>() == null)
            {
                GameObject analyticsGO = new GameObject("UnityGameAnalytics");
                analyticsGO.AddComponent<UnityGameAnalytics>();
                Debug.Log("[BattleInitializer] Created UnityGameAnalytics GameObject");
            }
            else
            {
                Debug.Log("[BattleInitializer] UnityGameAnalytics already exists");
            }
        }

        /// <summary>
        /// Initialize required Unity components (cooldown system, score animator)
        /// </summary>
        private bool InitializeRequiredComponents(BattleController controller)
        {
            // Initialize cooldown system if not assigned
            if (controller.cooldownSystem == null)
            {
                controller.cooldownSystem = Object.FindObjectOfType<CooldownSystem>();
                if (controller.cooldownSystem == null)
                {
                    Debug.LogError("[BattleInitializer] CooldownSystem not found! Please assign it in the inspector.");
                    return false;
                }
            }

            // Initialize score animator if not assigned
            if (controller.scoreAnimator == null)
            {
                controller.scoreAnimator = Object.FindObjectOfType<ScoreAnimator>();
                if (controller.scoreAnimator == null)
                {
                    Debug.LogWarning("[BattleInitializer] ScoreAnimator not found! Score animations will be disabled.");
                }
                else
                {
                    controller.scoreAnimator.ResetTotalScore();
                }
            }

            // Link score animator to relic display for pop effects
            if (controller.scoreAnimator != null && controller.relicDisplay != null)
            {
                controller.scoreAnimator.relicDisplay = controller.relicDisplay;
            }

            return true;
        }

        /// <summary>
        /// Initialize hand manager
        /// </summary>
        private HandManager InitializeHandManager(int maxRollsPerHand)
        {
            var handManager = new HandManager();
            handManager.SetMaxRolls(maxRollsPerHand);
            return handManager;
        }

        /// <summary>
        /// Initialize dice manager
        /// </summary>
        private DiceManager InitializeDiceManager()
        {
            var diceManager = new DiceManager();
            diceManager.InitializeGlobalDicePool();
            return diceManager;
        }

        /// <summary>
        /// Initialize UI components and link them together
        /// </summary>
        private void InitializeUI(BattleController controller, InitializationResult result)
        {
            // Link money text to score animator for money animation
            if (controller.scoreAnimator != null && controller.battleUI != null && controller.battleUI.moneyText != null)
            {
                controller.scoreAnimator.moneyText = controller.battleUI.moneyText;
            }

            // Initialize BattleUI (after _uiPresenter is created)
            if (controller.battleUI != null)
            {
                controller.battleUI.Initialize(result.UIPresenter);
            }
            else
            {
                Debug.LogWarning("[BattleInitializer] BattleUI component not assigned!");
            }

            // Initialize and hide continue button
            if (controller.continueButton != null)
            {
                controller.continueButton.gameObject.SetActive(false);
                controller.continueButton.onClick.AddListener(() => controller.OnContinue());
            }
        }

        /// <summary>
        /// Initialize game managers (backpack, progression, relic system)
        /// </summary>
        private void InitializeManagers(BattleController controller, GameStateManager stateManager, InitializationResult result)
        {
            // Initialize backpack manager
            if (controller.backpackManager != null)
            {
                controller.backpackManager.Initialize(
                    controller.cooldownSystem,
                    result.DiceManager,
                    (selectedDice) => controller.OnDiceSelectedFromBackpack(selectedDice)
                );

                // Set up open backpack button listener
                if (controller.backpackManager.openBackpackButton != null)
                {
                    controller.backpackManager.openBackpackButton.onClick.AddListener(() => controller.OnBackpackButtonPressed());
                }
                else
                {
                    Debug.LogWarning("[BattleInitializer] BackpackManager.openBackpackButton is not assigned!");
                }
            }
            else
            {
                Debug.LogError("[BattleInitializer] BackpackManager not assigned!");
            }

            // Initialize progression manager
            result.ProgressionManager = InitializeProgressionManager(stateManager, controller.baseTargetScore);

            // Initialize relic system: load from save data
            InitializeRelicSystem(controller, stateManager, result);

            // Initialize dice system: load from save data and update cooldown system
            InitializeDiceSystem(controller, stateManager, result);

            // Check if there are pending reward dice from reward scene
            IntegrateRewardDice(controller, stateManager, result);
        }

        /// <summary>
        /// Initialize progression manager based on game state
        /// </summary>
        private ProgressionManager InitializeProgressionManager(GameStateManager stateManager, int baseTargetScore)
        {
            ProgressionManager progressionManager;

            if (stateManager.State.ContinuingFromReward)
            {
                progressionManager = new ProgressionManager(baseTargetScore);
                progressionManager.RestoreLevelState(stateManager.State.PendingLevel, stateManager.State.PendingTargetScore);
                stateManager.State.ContinuingFromReward = false;
                Debug.Log($"[BattleInitializer] Continuing from Reward Scene - Level {stateManager.State.PendingLevel}, Target: {stateManager.State.PendingTargetScore}");
            }
            else if (stateManager.State.IsTutorialMode)
            {
                progressionManager = new ProgressionManager(baseTargetScore);
                progressionManager.InitializeTutorialMode();
                Debug.Log("[BattleInitializer] Initialized in Tutorial Mode (Level 0)");
            }
            else
            {
                progressionManager = new ProgressionManager(baseTargetScore);
            }

            return progressionManager;
        }

        /// <summary>
        /// Initialize relic system: load from save data
        /// </summary>
        private void InitializeRelicSystem(BattleController controller, GameStateManager stateManager, InitializationResult result)
        {
            // Initialize global relic pool (all relics available this run)
            result.RelicManager.InitializeGlobalRelicPool();

            // Load relics from save data
            foreach (var relicName in stateManager.SaveData.relicNames)
            {
                result.RelicManager.AddRelicToBackpackByName(relicName);
            }

            // Give player a random starting relic if this is a new game (no relics and not continuing from reward)
            if (result.RelicManager.PlayerBackpack.Count == 0 && !stateManager.State.ContinuingFromReward)
            {
                GiveRandomStartingRelic(result.RelicManager);
            }

            // Update relic display UI
            if (controller.relicDisplay != null)
            {
                controller.relicDisplay.DisplayRelics(result.RelicManager);
            }
        }

        /// <summary>
        /// Give player a random relic from the global pool as starting relic
        /// </summary>
        private void GiveRandomStartingRelic(RelicManager relicManager)
        {
            var globalPool = relicManager.GlobalRelicPool;
            if (globalPool == null || globalPool.Count == 0)
            {
                Debug.LogWarning("[BattleInitializer] Cannot give starting relic - global pool is empty!");
                return;
            }

            // Randomly select a relic from the global pool
            int randomIndex = Random.Range(0, globalPool.Count);
            var startingRelic = globalPool[randomIndex];

            if (startingRelic != null)
            {
                bool success = relicManager.AddRelicToBackpack(startingRelic);
                if (success)
                {
                    Debug.Log($"[BattleInitializer] Gave player starting relic: {startingRelic.relicName} ({startingRelic.rarity})");
                }
                else
                {
                    Debug.LogWarning($"[BattleInitializer] Failed to add starting relic: {startingRelic.relicName}");
                }
            }
        }

        /// <summary>
        /// Initialize dice system: load from save data and update cooldown system
        /// </summary>
        private void InitializeDiceSystem(BattleController controller, GameStateManager stateManager, InitializationResult result)
        {
            // Load player dice backpack from save data
            if (result.DiceManager != null)
            {
                result.DiceManager.LoadFromSaveData(stateManager.SaveData);

                // Update CooldownSystem with backpack dice
                UpdateCooldownSystemFromBackpack(controller, result);
            }
        }

        /// <summary>
        /// Update CooldownSystem with dice from player backpack
        /// </summary>
        private void UpdateCooldownSystemFromBackpack(BattleController controller, InitializationResult result)
        {
            if (result.DiceManager == null || controller.cooldownSystem == null)
            {
                return;
            }

            // Get dice from player backpack
            var backpackDice = result.DiceManager.PlayerDiceBackpack.ToList();

            // Update CooldownSystem
            controller.cooldownSystem.SetPlayerBackpackDice(backpackDice);

            Debug.Log($"[BattleInitializer] Updated CooldownSystem with {backpackDice.Count} dice from backpack");
        }

        /// <summary>
        /// Integrate reward dice from reward scene into the dice backpack
        /// </summary>
        private void IntegrateRewardDice(BattleController controller, GameStateManager stateManager, InitializationResult result)
        {
            // Check if there are pending reward dice
            if (stateManager.State.PendingDiceTypeIds.Count == 0)
            {
                Debug.Log("[BattleInitializer] No pending reward dice to integrate");
                return;
            }

            Debug.Log($"[BattleInitializer] Found {stateManager.State.PendingDiceTypeIds.Count} reward dice to integrate");

            // Use DiceManager to add dice to backpack
            foreach (var typeId in stateManager.State.PendingDiceTypeIds)
            {
                bool success = result.DiceManager.AddDiceToBackpackByName(typeId);
                if (success)
                {
                    Debug.Log($"[BattleInitializer] Added reward dice to backpack: {typeId}");
                }
                else
                {
                    Debug.LogWarning($"[BattleInitializer] Failed to add reward dice: {typeId}");
                }
            }

            // Clear pending list
            stateManager.State.PendingDiceTypeIds.Clear();

            // Save to persistence
            result.DiceManager.SaveToSaveData(stateManager.SaveData);
            stateManager.Save();

            // Update CooldownSystem with backpack dice
            UpdateCooldownSystemFromBackpack(controller, result);

            Debug.Log($"[BattleInitializer] Integrated reward dice. Backpack size: {result.DiceManager.PlayerDiceBackpack.Count}");
        }

        /// <summary>
        /// Initialize UI panels (settings, combo preference)
        /// </summary>
        private void InitializePanels(BattleController controller, GameStateManager stateManager)
        {
            // Initialize settings panel
            if (controller.settingsPanel != null)
            {
                controller.settingsPanel.Initialize();
                controller.settingsPanel.OnResetRequested += () => controller.OnSettingsResetClicked();
                controller.settingsPanel.OnQuitRequested += () => controller.OnSettingsQuitClicked();
            }
            else
            {
                Debug.LogWarning("[BattleInitializer] SettingsPanel component not assigned!");
            }

            // Initialize scene transition manager (create if not assigned)
            if (controller.sceneTransitionManager == null)
            {
                controller.sceneTransitionManager = controller.gameObject.AddComponent<SceneTransitionManager>();
                Debug.Log("[BattleInitializer] Created SceneTransitionManager component");
            }

            // Initialize combo preference panel
            if (controller.comboPreferencePanel != null)
            {
                controller.comboPreferencePanel.Initialize();
            }
            else
            {
                Debug.LogWarning("[BattleInitializer] ComboPreferencePanel component not assigned!");
            }

            // Initialize hand flow controller (create if not assigned)
            if (controller.handFlowController == null)
            {
                controller.handFlowController = controller.gameObject.AddComponent<HandFlowController>();
                Debug.Log("[BattleInitializer] Created HandFlowController component");
            }
        }

        /// <summary>
        /// Subscribe to events from various systems
        /// </summary>
        private void InitializeEvents(BattleController controller, InitializationResult result)
        {
            // Subscribe to cooldown system events
            controller.cooldownSystem.OnDicePoolRefresh += () => controller.OnDicePoolRefresh();
            controller.cooldownSystem.OnAvailableDiceChanged += (availableDice) => controller.OnAvailableDiceChanged(availableDice);

            // Subscribe to dice lock changes for combo preview updates
            DiceView.OnDiceLockChanged += () => controller.UpdateComboPreview();
        }

        /// <summary>
        /// Start the game (refresh UI, track analytics, start first hand, activate tutorial if needed)
        /// </summary>
        private void StartGame(BattleController controller, GameStateManager stateManager, InitializationResult result)
        {
            // Note: UI refresh is now handled by BattleController after services are initialized

            // Track initial player progression
            UnityGameAnalytics.TrackPlayerProgression(result.ProgressionManager.TotalScore, 0, result.ProgressionManager.CurrentLevel);

            // Initialize hand flow controller with dependencies
            controller.handFlowController.Initialize(
                result.HandManager,
                result.EffectHandler,
                result.ViewFactory,
                result.RelicManager,
                result.ScoreCalculator,
                result.ProgressionManager,
                result.CompositionService,
                result.MoneyManager,
                stateManager,
                controller.cooldownSystem,
                controller.backpackManager,
                controller.scoreAnimator,
                controller.battleUI,
                controller.sceneTransitionManager,
                controller.submitComboButton,
                controller.diceCount,
                controller.maxRollsPerHand,
                controller._dice,
                controller._views
            );

            // Set up callbacks for UI updates
            controller.handFlowController.OnComboPreviewUpdate += () => controller.UpdateComboPreview();
            controller.handFlowController.OnRollAndCastCountUpdate += () => controller.UpdateRollAndCastCount();
            controller.handFlowController.OnFeedbackUpdate += (msg, isWarning) => controller.UpdateFeedback(msg, isWarning);
            controller.handFlowController.OnMoneyDisplayUpdate += () => controller.UpdateMoneyDisplay();

            // Set up main game UI buttons (after HandFlowController is initialized)
            if (controller.rollButton != null)
            {
                controller.rollButton.onClick.AddListener(() => controller.handFlowController.OnRollOnce());
            }
            if (controller.submitComboButton != null)
            {
                controller.submitComboButton.onClick.AddListener(() => controller.handFlowController.OnSubmitCombo());
            }

            // Start first hand with a delay to ensure all systems are ready
            controller.StartCoroutine(DelayedStartFirstHand(controller));

            // Activate TutorialController if in tutorial mode
            if (stateManager.State.IsTutorialMode)
            {
                var tutorialController = Object.FindObjectOfType<DiceGame.Tutorial.TutorialController>(true);
                if (tutorialController != null)
                {
                    tutorialController.gameObject.SetActive(true);
                    Debug.Log("[BattleInitializer] TutorialController activated for tutorial mode");
                }
                else
                {
                    Debug.LogWarning("[BattleInitializer] TutorialController not found - tutorial may not work!");
                }
            }
        }

        /// <summary>
        /// Delayed start first hand coroutine
        /// </summary>
        private IEnumerator DelayedStartFirstHand(BattleController controller)
        {
            yield return null; // Wait one frame
            controller.handFlowController?.StartNewHand();
        }
    }
}

