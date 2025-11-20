using System.Collections.Generic;
using UnityEngine;
using DiceGame.Core;
using DiceGame.Relics;
using DiceGame.UI;

namespace DiceGame
{
    /// <summary>
    /// 负责 BattleController 的游戏状态管理逻辑
    /// 提取自 BattleController 以简化其职责
    /// </summary>
    public class BattleStateManager
    {
        private BattleController _controller;
        private GameStateManager _gameStateManager;
        private HandManager _handManager;
        private ProgressionManager _progressionManager;
        private MoneyManager _moneyManager;
        private RelicManager _relicManager;
        private DiceManager _diceManager;
        private DiceViewFactory _viewFactory;
        private CooldownSystem _cooldownSystem;
        private ScoreAnimator _scoreAnimator;
        private BackpackManager _backpackManager;
        private List<BaseDice> _dice;
        private List<DiceView> _views;
        private int _maxRollsPerHand;
        private int _baseTargetScore;

        /// <summary>
        /// Initialize state manager with required dependencies
        /// </summary>
        public void Initialize(
            BattleController controller,
            GameStateManager gameStateManager,
            HandManager handManager,
            ProgressionManager progressionManager,
            MoneyManager moneyManager,
            RelicManager relicManager,
            DiceManager diceManager,
            DiceViewFactory viewFactory,
            CooldownSystem cooldownSystem,
            ScoreAnimator scoreAnimator,
            BackpackManager backpackManager,
            List<BaseDice> dice,
            List<DiceView> views,
            int maxRollsPerHand,
            int baseTargetScore)
        {
            _controller = controller;
            _gameStateManager = gameStateManager;
            _handManager = handManager;
            _progressionManager = progressionManager;
            _moneyManager = moneyManager;
            _relicManager = relicManager;
            _diceManager = diceManager;
            _viewFactory = viewFactory;
            _cooldownSystem = cooldownSystem;
            _scoreAnimator = scoreAnimator;
            _backpackManager = backpackManager;
            _dice = dice;
            _views = views;
            _maxRollsPerHand = maxRollsPerHand;
            _baseTargetScore = baseTargetScore;
        }

        /// <summary>
        /// Reset for new hand
        /// </summary>
        public void ResetForNewHand(System.Action onRefreshUI, System.Action<string, bool> onUpdateFeedback, System.Action onStartNewHand, System.Action onUpdateCooldownSystem)
        {
            // Check if hands remain
            var (current, remaining) = _cooldownSystem.GetHandCounter();
            if (remaining <= 0)
            {
                // No hands remain - reset everything to level 1 (game over / try again)
                
                // Reset progression to level 1
                _progressionManager.ResetToLevelOne();
                
                // Reset hand manager roll budget
                _handManager.Reset();
                _handManager.SetMaxRolls(_maxRollsPerHand);
                
                // Reset score animator
                if (_scoreAnimator != null)
                {
                    _scoreAnimator.ResetTotalScore();
                }
                
                // Reset relic system and give new random starting relic
                _relicManager.ClearBackpack();
                _gameStateManager.SaveData.relicNames.Clear();
                InitializeRelicSystem();
                
                // Reset dice backpack (game over / restart)
                if (_diceManager != null)
                {
                    _diceManager.ClearBackpack();
                    _diceManager.SaveToSaveData(_gameStateManager.SaveData);
                }
                
                // Reset money (game over / restart)
                _moneyManager.Reset();
                _gameStateManager.SaveData.money = 0;
                _gameStateManager.Save();
                
                // Refresh dice pool and hand counter
                onUpdateCooldownSystem?.Invoke();
                _cooldownSystem.RefreshDicePool();
                
                // Update displays
                onRefreshUI?.Invoke();
                onUpdateFeedback?.Invoke("Roll or Lock Dice", false);
                
                // Start a new hand after refresh
                onStartNewHand?.Invoke();
                return;
            }
            
            // Normal reset behavior during active hands
            // Reset hand state using HandManager
            _handManager.Reset();
            _handManager.SetMaxRolls(_maxRollsPerHand);
            
            // Reset dice states
            foreach (var d in _dice) d.ResetLockAndValue();
            
            // Refresh views using factory
            _viewFactory.RefreshViews(_views);
            
            // Start a new hand
            onStartNewHand?.Invoke();
        }

        /// <summary>
        /// Continue to next level - reset game state and increase target score
        /// </summary>
        public void ContinueToNextLevel(System.Action onRefreshUI, System.Action<string, bool> onUpdateFeedback, System.Action onStartNewHand, System.Action onHideContinueButton)
        {
            // Hide continue button
            onHideContinueButton?.Invoke();

            // Advance to next level using progression manager
            _progressionManager.AdvanceToNextLevel();
            _progressionManager.ResetScore();

            // Reset score animator
            if (_scoreAnimator != null)
            {
                _scoreAnimator.ResetTotalScore();
            }
            
            // Reset roll budget for new level
            _handManager.Reset();
            _handManager.SetMaxRolls(_maxRollsPerHand);

            // Reset dice pool and hand counter
            _cooldownSystem.RefreshDicePool();

            // Update displays
            onRefreshUI?.Invoke();
            onUpdateFeedback?.Invoke("Roll or Lock Dice", false);

            // Start first hand of new level
            onStartNewHand?.Invoke();
        }

        /// <summary>
        /// Reset game to initial state (called by SettingsPanel)
        /// </summary>
        public void ResetGame(System.Action onRefreshUI, System.Action<string, bool> onUpdateFeedback, System.Action onStartNewHand, System.Action onHideContinueButton, System.Action onUpdateCooldownSystem)
        {
            // Reset progression to level 1
            _progressionManager.ResetToLevelOne();
            
            // Reset hand manager roll budget
            _handManager.Reset();
            _handManager.SetMaxRolls(_maxRollsPerHand);
            
            // Reset score animator
            if (_scoreAnimator != null)
            {
                _scoreAnimator.ResetTotalScore();
            }
            
            // Clear current dice and views
            _dice.Clear();
            _viewFactory.DestroyViews(_views);
            
            // Reset runtime state
            _gameStateManager.ResetState();
            
            // Reset relic system and give new random starting relic
            _relicManager.ClearBackpack();
            _gameStateManager.SaveData.relicNames.Clear();
            InitializeRelicSystem();
            
            // Reset dice backpack (settings reset)
            if (_diceManager != null)
            {
                _diceManager.ClearBackpack();
                _diceManager.SaveToSaveData(_gameStateManager.SaveData);
            }
            
            // Reset money (settings reset)
            _moneyManager.Reset();
            _gameStateManager.SaveData.money = 0;
            _gameStateManager.ResetSaveData();
            
            // Refresh dice pool and hand counter
            onUpdateCooldownSystem?.Invoke();
            _cooldownSystem.RefreshDicePool();
            
            // Hide continue button if visible
            onHideContinueButton?.Invoke();
            
            // Update displays
            onRefreshUI?.Invoke();
            onUpdateFeedback?.Invoke("Roll or Lock Dice", false);
            
            // Start a new hand
            onStartNewHand?.Invoke();
        }

        /// <summary>
        /// Quit game (called by SettingsPanel)
        /// </summary>
        public void QuitGame()
        {
            // Quit application (works in builds)
            // In editor, this will stop play mode
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// Transition from tutorial mode to normal game (called by TutorialController)
        /// </summary>
        public void CompleteTutorialAndStartLevel1(System.Action onRefreshUI, System.Action<string, bool> onUpdateFeedback, System.Action onStartNewHand)
        {
            if (_progressionManager != null && _progressionManager.IsTutorialMode)
            {
                _progressionManager.StartNormalGame();
                _gameStateManager.State.IsTutorialMode = false;
                _gameStateManager.SaveData.hasCompletedTutorial = true;
                _gameStateManager.Save();
                
                // Close backpack if open
                if (_backpackManager != null)
                {
                    _backpackManager.HideBackpack();
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
                    _handManager.SetMaxRolls(_maxRollsPerHand);
                }
                
                // Clear current dice and views to start fresh
                _dice.Clear();
                _viewFactory?.DestroyViews(_views);
                _views.Clear();
                
                // Update UI
                onRefreshUI?.Invoke();
                onUpdateFeedback?.Invoke("Roll or Lock Dice", false);
                
                // Start first hand of Level 1
                onStartNewHand?.Invoke();
            }
        }

        /// <summary>
        /// Initialize relic system: load from save data
        /// (Helper method for reset operations)
        /// </summary>
        private void InitializeRelicSystem()
        {
            // Initialize global relic pool (all relics available this run)
            _relicManager.InitializeGlobalRelicPool();
            
            // Load relics from save data
            foreach (var relicName in _gameStateManager.SaveData.relicNames)
            {
                _relicManager.AddRelicToBackpackByName(relicName);
            }
            
            // Give player a random starting relic if this is a new game (no relics and not continuing from reward)
            if (_relicManager.PlayerBackpack.Count == 0 && !_gameStateManager.State.ContinuingFromReward)
            {
                GiveRandomStartingRelic();
            }
            
            // Update relic display UI
            if (_controller.relicDisplay != null)
            {
                _controller.relicDisplay.DisplayRelics(_relicManager);
            }
        }

        /// <summary>
        /// Give player a random relic from the global pool as starting relic
        /// Uses PlayerResourceManager to ensure persistence
        /// </summary>
        private void GiveRandomStartingRelic()
        {
            var globalPool = _relicManager.GlobalRelicPool;
            if (globalPool == null || globalPool.Count == 0)
            {
                Debug.LogWarning("[BattleStateManager] Cannot give starting relic - global pool is empty!");
                return;
            }

            // Randomly select a relic from the global pool
            int randomIndex = Random.Range(0, globalPool.Count);
            var startingRelic = globalPool[randomIndex];
            
            if (startingRelic != null)
            {
                // Use PlayerResourceManager to add relic (automatically saves to SaveData)
                PlayerResourceManager.Instance?.AddRelicToBackpack(startingRelic);
            }
        }
    }
}

