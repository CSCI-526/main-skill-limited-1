using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DiceGame.Core;

namespace DiceGame
{
    /// <summary>
    /// 负责 BattleController 的所有 UI 更新逻辑
    /// 提取自 BattleController 以简化其职责
    /// </summary>
    public class BattleUIUpdater
    {
        private BattleUI _battleUI;
        private HandManager _handManager;
        private ProgressionManager _progressionManager;
        private MoneyManager _moneyManager;
        private ScoreCalculator _scoreCalculator;
        private CooldownSystem _cooldownSystem;
        private List<BaseDice> _dice;
        private int _maxRollsPerHand;

        /// <summary>
        /// Initialize UI updater with required dependencies
        /// </summary>
        public void Initialize(
            BattleUI battleUI,
            HandManager handManager,
            ProgressionManager progressionManager,
            MoneyManager moneyManager,
            ScoreCalculator scoreCalculator,
            CooldownSystem cooldownSystem,
            List<BaseDice> dice,
            int maxRollsPerHand)
        {
            _battleUI = battleUI;
            _handManager = handManager;
            _progressionManager = progressionManager;
            _moneyManager = moneyManager;
            _scoreCalculator = scoreCalculator;
            _cooldownSystem = cooldownSystem;
            _dice = dice;
            _maxRollsPerHand = maxRollsPerHand;
        }

        /// <summary>
        /// Update feedback message
        /// </summary>
        public void UpdateFeedback(string msg, bool isWarning = false)
        {
            _battleUI?.UpdateFeedback(msg, isWarning);
        }

        /// <summary>
        /// Update roll count and cast count displays
        /// </summary>
        public void UpdateRollAndCastCount()
        {
            if (_battleUI == null) return;
            
            // Use TotalRollBudget instead of _maxRollsPerHand to account for bonus rerolls from relics
            int remainingRolls = _handManager != null 
                ? Mathf.Max(0, _handManager.TotalRollBudget - _handManager.TotalRollsUsed) 
                : 0;
            
            int remainingCasts = 0;
            if (_cooldownSystem != null)
            {
                var (current, remaining) = _cooldownSystem.GetHandCounter();
                remainingCasts = Mathf.Max(0, remaining);
            }
            
            _battleUI.UpdateRollAndCastCount(remainingRolls, remainingCasts);
        }

        /// <summary>
        /// Update money display
        /// </summary>
        public void UpdateMoneyDisplay()
        {
            if (_battleUI == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update money - BattleUI is null");
                return;
            }
            if (_moneyManager == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update money - MoneyManager is null");
                return;
            }
            _battleUI.UpdateMoney(_moneyManager.Money);
        }

        /// <summary>
        /// Update target score display
        /// </summary>
        public void UpdateTargetScoreDisplay()
        {
            if (_battleUI == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update target score - BattleUI is null");
                return;
            }
            if (_progressionManager == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update target score - ProgressionManager is null");
                return;
            }
            _battleUI.UpdateTargetScore(_progressionManager.TargetScore, _progressionManager.CurrentLevel);
        }

        /// <summary>
        /// Update level info display
        /// </summary>
        public void UpdateLevelInfo()
        {
            if (_battleUI == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update level info - BattleUI is null");
                return;
            }
            if (_progressionManager == null)
            {
                Debug.LogWarning("[BattleUIUpdater] Cannot update level info - ProgressionManager is null");
                return;
            }
            _battleUI.UpdateLevelInfo(_progressionManager.CurrentLevel, _progressionManager.IsTutorialMode);
        }

        /// <summary>
        /// Update combo preview display based on currently locked dice
        /// Shows default combo values only (no dice/relic effects)
        /// </summary>
        public void UpdateComboPreview()
        {
            if (_battleUI == null) return;
            
            // Get locked dice values (only dice that are locked and have been rolled)
            var lockedValues = _dice
                .Where(d => d.isLocked && d.lastRollValue > 0 && d.tier != DiceTier.Filler)
                .Select(d => d.lastRollValue)
                .ToList();
            
            if (lockedValues.Count == 0)
            {
                // No dice locked - show default state
                _battleUI.UpdateComboPreviewEmpty();
                return;
            }
            
            // Use ScoreCalculator to preview combo (default values only)
            if (_scoreCalculator == null) return;
            var (comboName, baseScore, multiplier) = _scoreCalculator.PreviewCombo(lockedValues);
            
            // Update UI
            _battleUI.UpdateComboPreview(comboName, baseScore, multiplier);
        }
        
        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshAllUI()
        {
            UpdateTargetScoreDisplay();
            UpdateLevelInfo();
            UpdateComboPreview();
            UpdateRollAndCastCount();
            UpdateMoneyDisplay();
        }
    }
}

