using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Manages level progression, target scores, and score evaluation.
    /// Handles the progression curve and win/loss conditions.
    /// </summary>
    public class ProgressionManager
    {
        // Configuration
        private readonly int _baseTargetScore;
        
        // State
        private int _currentLevel;
        private int _currentTargetScore;
        private int _totalScore;
        private float _sessionStartTime;

        public int CurrentLevel => _currentLevel;
        public int TargetScore => _currentTargetScore;
        public int TotalScore => _totalScore;
        public float SessionDuration => Time.time - _sessionStartTime;

        public ProgressionManager(int baseTargetScore = 300)
        {
            _baseTargetScore = baseTargetScore;
            _currentLevel = 1;
            _currentTargetScore = baseTargetScore;
            _totalScore = 0;
            _sessionStartTime = Time.time;
        }

        /// <summary>
        /// Add score from a completed hand
        /// </summary>
        public void AddScore(int score)
        {
            _totalScore += score;
            Debug.Log($"[ProgressionManager] Score added: +{score}, Total: {_totalScore}/{_currentTargetScore}");
        }

        /// <summary>
        /// Reset total score (used when starting a new level or restarting)
        /// </summary>
        public void ResetScore()
        {
            _totalScore = 0;
            Debug.Log($"[ProgressionManager] Score reset to 0");
        }

        /// <summary>
        /// Evaluate if the player passed the target score
        /// </summary>
        public bool EvaluateTargetScore()
        {
            bool passed = _totalScore >= _currentTargetScore;
            Debug.Log($"[ProgressionManager] Target Evaluation - Target: {_currentTargetScore}, Final: {_totalScore}, Passed: {passed}");
            return passed;
        }

        /// <summary>
        /// Advance to the next level
        /// </summary>
        public void AdvanceToNextLevel()
        {
            _currentLevel++;
            _currentTargetScore = CalculateTargetScore(_currentLevel);
            Debug.Log($"[ProgressionManager] Advanced to Level {_currentLevel}, Target: {_currentTargetScore}");
        }

        /// <summary>
        /// Reset to level 1 (game over / restart)
        /// </summary>
        public void ResetToLevelOne()
        {
            _currentLevel = 1;
            _currentTargetScore = _baseTargetScore;
            _totalScore = 0;
            _sessionStartTime = Time.time;
            Debug.Log($"[ProgressionManager] Reset to Level 1, Target: {_currentTargetScore}");
        }

        /// <summary>
        /// Restore level state when returning from reward scene
        /// </summary>
        public void RestoreLevelState(int level, int targetScore)
        {
            _currentLevel = level;
            _currentTargetScore = targetScore;
            _totalScore = 0; // Reset score for new level
            Debug.Log($"[ProgressionManager] Restored state - Level {_currentLevel}, Target: {_currentTargetScore}");
        }

        /// <summary>
        /// Calculate target score for a given level using progressive formula
        /// Progressive increase: +300, +400, +500, +600, +700, ...
        /// Formula: Base + sum of (300 + i*100) for i = 0 to level-2
        /// Level 1: 300 (base)
        /// Level 2: 300 + 300 = 600
        /// Level 3: 600 + 400 = 1000
        /// Level 4: 1000 + 500 = 1500
        /// </summary>
        public int CalculateTargetScore(int level)
        {
            if (level <= 1) return _baseTargetScore;
            
            int target = _baseTargetScore;
            for (int i = 0; i < level - 1; i++)
            {
                int increase = 300 + i * 100; // 300, 400, 500, 600, 700, ...
                target += increase;
            }
            
            return target;
        }

        /// <summary>
        /// Get progression info for display
        /// </summary>
        public (int currentLevel, int targetScore, int totalScore, float progress) GetProgressionInfo()
        {
            float progress = _currentTargetScore > 0 ? (float)_totalScore / _currentTargetScore : 0f;
            return (_currentLevel, _currentTargetScore, _totalScore, progress);
        }
    }
}

