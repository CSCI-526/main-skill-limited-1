using UnityEngine;
using TMPro;

namespace DiceGame
{
    /// <summary>
    /// 统一管理战斗场景的所有 UI 更新
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("UI - Right Panel")]
        public TMP_Text levelInfoText;      // Level display
        public TMP_Text targetScoreText;    // Target score display
        public TMP_Text comboNameText;      // Combo name preview
        public TMP_Text comboBaseText;       // Combo base score preview
        public TMP_Text comboMultiplierText;// Combo multiplier preview
        public TMP_Text rollCountText;      // Roll count display (remaining rolls)
        public TMP_Text castCountText;      // Cast count display (remaining casts)
        public TMP_Text moneyText;          // Money display
        
        [Header("UI - Score Display")]
        public ScoreAnimator scoreAnimator; // Animated score display system
        
        private BattleUIPresenter _uiPresenter;
        
        /// <summary>
        /// Initialize UI presenter
        /// </summary>
        public void Initialize(BattleUIPresenter uiPresenter)
        {
            _uiPresenter = uiPresenter;
        }
        
        /// <summary>
        /// Update level info display
        /// </summary>
        public void UpdateLevelInfo(int level, bool isTutorial)
        {
            if (levelInfoText != null)
            {
                if (isTutorial)
                {
                    levelInfoText.text = "Tutorial";
                }
                else
                {
                    levelInfoText.text = $"Level {level}";
                }
            }
        }
        
        /// <summary>
        /// Update target score display
        /// </summary>
        public void UpdateTargetScore(int targetScore, int currentLevel)
        {
            if (targetScoreText != null)
            {
                if (_uiPresenter != null)
                {
                    targetScoreText.text = _uiPresenter.FormatTargetScore(targetScore, currentLevel);
                }
                else
                {
                    // Fallback if UI presenter is not initialized
                    targetScoreText.text = $"<size=150%><b><color=#2A2A2A>{targetScore}</color></b></size>";
                    Debug.LogWarning("[BattleUI] UpdateTargetScore called but _uiPresenter is null. Using fallback format.");
                }
            }
            else
            {
                Debug.LogWarning("[BattleUI] UpdateTargetScore called but targetScoreText is not assigned in Inspector!");
            }
        }
        
        /// <summary>
        /// Update combo preview display based on locked dice values
        /// </summary>
        public void UpdateComboPreview(string comboName, int baseScore, float multiplier)
        {
            if (comboNameText != null) comboNameText.text = comboName;
            
            // Base score: just the number, orange color (#FF8C00 - DarkOrange)
            if (comboBaseText != null) comboBaseText.text = $"<color=#FF8C00><b>{baseScore}</b></color>";
            
            // Multiplier: just the number, blue color (#4A90E2 - Nice blue)
            if (comboMultiplierText != null) comboMultiplierText.text = $"<color=#4A90E2>{multiplier:F1}</color>";
        }
        
        /// <summary>
        /// Update combo preview to show "No Combo" state
        /// </summary>
        public void UpdateComboPreviewEmpty()
        {
            if (comboNameText != null) comboNameText.text = "No Combo";
            if (comboBaseText != null) comboBaseText.text = "<color=#CCCCCC>0</color>";
            if (comboMultiplierText != null) comboMultiplierText.text = "<color=#CCCCCC>1.0</color>";
        }
        
        /// <summary>
        /// Update roll count and cast count displays
        /// </summary>
        public void UpdateRollAndCastCount(int remainingRolls, int remainingCasts)
        {
            // Update roll count: shows remaining rolls (just the number)
            if (rollCountText != null)
            {
                rollCountText.text = remainingRolls.ToString();
            }
            
            // Update cast count: shows remaining casts (based on remaining hands)
            if (castCountText != null)
            {
                castCountText.text = remainingCasts.ToString();
            }
        }
        
        /// <summary>
        /// Update money display
        /// </summary>
        public void UpdateMoney(int money)
        {
            if (moneyText != null)
            {
                moneyText.text = money.ToString();
            }
        }
        
        /// <summary>
        /// Update feedback message in combo score text (minimal tips only)
        /// </summary>
        public void UpdateFeedback(string msg, bool isWarning = false)
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
            Debug.Log($"[BattleUI] Feedback: {msg}");
        }
    }
}

