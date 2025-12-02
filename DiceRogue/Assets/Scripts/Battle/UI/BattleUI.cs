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
        /// Auto-initialize UI text components if they're null (fixes WebGL scene transition issues)
        /// Similar to BattleController's ComboPreferencePanel auto-wiring
        /// </summary>
        void Awake()
        {
            // Auto-find RightInfoPanel and its child text components if references are null
            // This is especially important for WebGL builds where scene transitions can lose Inspector references
            if (levelInfoText == null || targetScoreText == null || comboNameText == null || 
                comboBaseText == null || comboMultiplierText == null || rollCountText == null || 
                castCountText == null || moneyText == null)
            {
                GameObject rightInfoPanel = GameObject.Find("RightInfoPanel");
                if (rightInfoPanel != null)
                {
                    Debug.Log("[BattleUI] Auto-initializing text components from RightInfoPanel");
                    
                    // Find all TMP_Text components in RightInfoPanel and its children
                    TMP_Text[] allTexts = rightInfoPanel.GetComponentsInChildren<TMP_Text>(true);
                    
                    // Try to match text components by exact names (found in BattleScene.unity)
                    foreach (var text in allTexts)
                    {
                        if (text == null) continue;
                        
                        string textName = text.gameObject.name;
                        string textNameLower = textName.ToLower();
                        
                        // Match by exact names first, then fallback to patterns
                        if (levelInfoText == null && textName == "LevelInfoText")
                        {
                            levelInfoText = text;
                            Debug.Log($"[BattleUI] Found levelInfoText: {textName}");
                        }
                        else if (targetScoreText == null && textName == "TargetScoreText")
                        {
                            targetScoreText = text;
                            Debug.Log($"[BattleUI] Found targetScoreText: {textName}");
                        }
                        else if (comboNameText == null && textName == "ComboNameText")
                        {
                            comboNameText = text;
                            Debug.Log($"[BattleUI] Found comboNameText: {textName}");
                        }
                        else if (comboBaseText == null && textName == "ComboBaseText")
                        {
                            comboBaseText = text;
                            Debug.Log($"[BattleUI] Found comboBaseText: {textName}");
                        }
                        else if (comboMultiplierText == null && textName == "MultiplierText")
                        {
                            comboMultiplierText = text;
                            Debug.Log($"[BattleUI] Found comboMultiplierText: {textName}");
                        }
                        else if (rollCountText == null && textName == "RollCntText")
                        {
                            rollCountText = text;
                            Debug.Log($"[BattleUI] Found rollCountText: {textName}");
                        }
                        else if (castCountText == null && textName == "CastCntText")
                        {
                            castCountText = text;
                            Debug.Log($"[BattleUI] Found castCountText: {textName}");
                        }
                        else if (moneyText == null && textName == "MoneyNumText")
                        {
                            moneyText = text;
                            Debug.Log($"[BattleUI] Found moneyText: {textName}");
                        }
                    }
                    
                    // Fallback: try pattern matching if exact names didn't work
                    if (levelInfoText == null || targetScoreText == null || comboNameText == null || 
                        comboBaseText == null || comboMultiplierText == null || rollCountText == null || 
                        castCountText == null || moneyText == null)
                    {
                        foreach (var text in allTexts)
                        {
                            if (text == null) continue;
                            
                            string textNameLower = text.gameObject.name.ToLower();
                            
                            // Pattern matching fallback
                            if (levelInfoText == null && textNameLower.Contains("levelinfo"))
                            {
                                levelInfoText = text;
                                Debug.Log($"[BattleUI] Found levelInfoText (pattern): {text.gameObject.name}");
                            }
                            else if (targetScoreText == null && textNameLower.Contains("targetscore"))
                            {
                                targetScoreText = text;
                                Debug.Log($"[BattleUI] Found targetScoreText (pattern): {text.gameObject.name}");
                            }
                            else if (comboNameText == null && textNameLower.Contains("comboname"))
                            {
                                comboNameText = text;
                                Debug.Log($"[BattleUI] Found comboNameText (pattern): {text.gameObject.name}");
                            }
                            else if (comboBaseText == null && textNameLower.Contains("combobase"))
                            {
                                comboBaseText = text;
                                Debug.Log($"[BattleUI] Found comboBaseText (pattern): {text.gameObject.name}");
                            }
                            else if (comboMultiplierText == null && textNameLower.Contains("multiplier"))
                            {
                                comboMultiplierText = text;
                                Debug.Log($"[BattleUI] Found comboMultiplierText (pattern): {text.gameObject.name}");
                            }
                            else if (rollCountText == null && (textNameLower.Contains("rollcnt") || textNameLower.Contains("rollcount")))
                            {
                                rollCountText = text;
                                Debug.Log($"[BattleUI] Found rollCountText (pattern): {text.gameObject.name}");
                            }
                            else if (castCountText == null && (textNameLower.Contains("castcnt") || textNameLower.Contains("castcount")))
                            {
                                castCountText = text;
                                Debug.Log($"[BattleUI] Found castCountText (pattern): {text.gameObject.name}");
                            }
                            else if (moneyText == null && (textNameLower.Contains("moneynum") || textNameLower.Contains("moneytext")))
                            {
                                moneyText = text;
                                Debug.Log($"[BattleUI] Found moneyText (pattern): {text.gameObject.name}");
                            }
                        }
                    }
                    
                    // Log any components that still couldn't be found
                    if (levelInfoText == null) Debug.LogWarning("[BattleUI] levelInfoText not found in RightInfoPanel");
                    if (targetScoreText == null) Debug.LogWarning("[BattleUI] targetScoreText not found in RightInfoPanel");
                    if (comboNameText == null) Debug.LogWarning("[BattleUI] comboNameText not found in RightInfoPanel");
                    if (comboBaseText == null) Debug.LogWarning("[BattleUI] comboBaseText not found in RightInfoPanel");
                    if (comboMultiplierText == null) Debug.LogWarning("[BattleUI] comboMultiplierText not found in RightInfoPanel");
                    if (rollCountText == null) Debug.LogWarning("[BattleUI] rollCountText not found in RightInfoPanel");
                    if (castCountText == null) Debug.LogWarning("[BattleUI] castCountText not found in RightInfoPanel");
                    if (moneyText == null) Debug.LogWarning("[BattleUI] moneyText not found in RightInfoPanel");
                }
                else
                {
                    Debug.LogWarning("[BattleUI] RightInfoPanel not found! UI text components may not be initialized.");
                }
            }
        }
        
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

