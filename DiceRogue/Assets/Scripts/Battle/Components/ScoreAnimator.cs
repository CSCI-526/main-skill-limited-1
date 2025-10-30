using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DiceGame.Core;
using DiceGame.Relics;
using DiceGame.UI;

namespace DiceGame
{
    /// <summary>
    /// Clean score animation system with 2-line display
    /// Line 1: Combo name
    /// Line 2: Animated score with sliding bonuses
    /// </summary>
    public class ScoreAnimator : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text comboScoreText;     // Shows combo name and animated score
        public TMP_Text totalScoreText;     // Shows total accumulated score
        public TMP_Text bonusText;          // Floating bonus text (e.g., "+15")
        
        [Header("Component References")]
        public RelicDisplay relicDisplay;   // For triggering relic pop effects
        
        [Header("Animation Settings")]
        public float stepDuration = 0.5f;           // Duration for each animation step
        public float slideDistance = 100f;          // Distance for sliding bonus text
        public float countUpDuration = 0.3f;        // Duration for counting up numbers
        public Color highlightColor = Color.yellow;
        public Color normalColor = Color.white;
        
        private int _currentTotalScore = 0;
        private Coroutine _animationCoroutine;
        
        // References to dice views (set by BattleController)
        private List<DiceView> _diceViews = new List<DiceView>();

        void Start()
        {
            // Initialize displays
            if (comboScoreText != null)
                comboScoreText.text = "<color=#AAAAAA>Submit a combo to see score</color>";
            
            UpdateTotalScore(0);
            
            if (bonusText != null)
                bonusText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set references to dice views for pop effects
        /// </summary>
        public void SetDiceViews(List<DiceView> diceViews)
        {
            _diceViews = new List<DiceView>(diceViews);
        }

        /// <summary>
        /// Animate score calculation with new step-by-step system
        /// </summary>
        public void AnimateScore(ScoreCalculator.ScoreResult scoreResult, List<BaseDice> submittedDice)
        {
            // Stop any existing animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AnimateScoreCoroutine(scoreResult, submittedDice));
        }

        private IEnumerator AnimateScoreCoroutine(ScoreCalculator.ScoreResult scoreResult, List<BaseDice> submittedDice)
        {
            // Ensure text is visible
            if (comboScoreText != null)
            {
                var color = comboScoreText.color;
                color.a = 1f;
                comboScoreText.color = color;
            }

            // Step 1: Show combo name
            if (comboScoreText != null)
            {
                comboScoreText.text = $"<size=120%><b>{scoreResult.comboName}</b></size>\n";
            }
            yield return new WaitForSeconds(0.2f);

            // Step 2: Show base score
            int currentScore = scoreResult.comboBaseScore;
            UpdateComboScore(scoreResult.comboName, currentScore);
            yield return StartCoroutine(PopScoreText(1.0f));
            yield return new WaitForSeconds(0.15f);

            // Step 3: Add dice sum with sliding animation
            yield return StartCoroutine(AnimateSlidingBonus(scoreResult.diceSum, false));
            currentScore += scoreResult.diceSum;
            yield return StartCoroutine(CountToScore(scoreResult.comboName, currentScore, 1.2f));

            // Step 4: Process each step (bonuses and multipliers)
            foreach (var step in scoreResult.steps)
            {
                if (step.isMultiplier)
                {
                    // Show multiplier with pop effect
                    yield return StartCoroutine(AnimateSlidingBonus(0, true, step.multiplier));
                    
                    // Apply multiplier
                    int newScore = Mathf.RoundToInt(currentScore * step.multiplier);
                    
                    // Calculate intensity based on score gain
                    float scoreGain = newScore - currentScore;
                    float intensity = CalculateIntensity(scoreGain);
                    
                    currentScore = newScore;
                    yield return StartCoroutine(CountToScore(scoreResult.comboName, currentScore, intensity));
                    
                    // Trigger pop on source object
                    TriggerSourcePop(step.sourceObject, submittedDice, intensity);
                }
                else
                {
                    // Show addition bonus with sliding animation
                    yield return StartCoroutine(AnimateSlidingBonus(step.amount, false));
                    
                    // Calculate intensity based on bonus amount
                    float intensity = CalculateIntensity(step.amount);
                    
                    currentScore += step.amount;
                    yield return StartCoroutine(CountToScore(scoreResult.comboName, currentScore, intensity));
                    
                    // Trigger pop on source object
                    TriggerSourcePop(step.sourceObject, submittedDice, intensity);
                }
            }

            // Step 5: Show final hand score and update total with big pop
            int handScore = scoreResult.finalScore;
            float finalIntensity = CalculateIntensity(handScore);
            
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateTotalScoreUpdate(handScore, finalIntensity));
            
            _currentTotalScore += handScore;

            // Hold for a moment
            yield return new WaitForSeconds(0.8f);

            // Fade out combo text
            yield return StartCoroutine(FadeOutComboText());
        }

        /// <summary>
        /// Animate sliding bonus text from right to center with fade
        /// </summary>
        private IEnumerator AnimateSlidingBonus(int amount, bool isMultiplier, float multiplier = 1f)
        {
            if (bonusText == null) yield break;

            // Set text
            if (isMultiplier)
            {
                bonusText.text = $"<b>×{multiplier:F1}</b>";
            }
            else
            {
                bonusText.text = $"<b>+{amount}</b>";
            }

            // Position on right side
            RectTransform rt = bonusText.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector3 startPos = rt.anchoredPosition;
                startPos.x += slideDistance;
                Vector3 endPos = rt.anchoredPosition;
                
                rt.anchoredPosition = startPos;

                // Make visible
                bonusText.gameObject.SetActive(true);
                Color startColor = bonusText.color;
                startColor.a = 1f;
                bonusText.color = startColor;

                float elapsed = 0f;
                float duration = stepDuration * 0.6f;

                // Slide and fade
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;

                    // Ease out movement
                    float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                    rt.anchoredPosition = Vector3.Lerp(startPos, endPos, smoothT);

                    // Fade out in second half
                    if (t > 0.5f)
                    {
                        float fadeT = (t - 0.5f) * 2f;
                        Color color = bonusText.color;
                        color.a = Mathf.Lerp(1f, 0f, fadeT);
                        bonusText.color = color;
                    }

                    yield return null;
                }

                // Hide
                bonusText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Count up to target score with pop effect
        /// </summary>
        private IEnumerator CountToScore(string comboName, int targetScore, float intensity)
        {
            if (comboScoreText == null) yield break;

            // Extract current score from text
            string currentText = comboScoreText.text;
            int startScore = ExtractScoreFromText(currentText);

            float elapsed = 0f;
            float duration = countUpDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease out
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                int score = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, smoothT));

                UpdateComboScore(comboName, score);
                yield return null;
            }

            UpdateComboScore(comboName, targetScore);
            
            // Pop effect based on intensity
            yield return StartCoroutine(PopScoreText(intensity));
        }

        /// <summary>
        /// Pop animation on score text
        /// </summary>
        private IEnumerator PopScoreText(float intensity)
        {
            if (comboScoreText == null) yield break;

            Vector3 originalScale = comboScoreText.transform.localScale;
            float scaleMultiplier = 1.0f + (0.15f * Mathf.Min(intensity, 3f)); // Cap at 3x
            Vector3 targetScale = originalScale * scaleMultiplier;

            float duration = 0.1f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                comboScoreText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            // Scale down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                comboScoreText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            comboScoreText.transform.localScale = originalScale;
        }

        /// <summary>
        /// Animate total score update with +handScore display
        /// </summary>
        private IEnumerator AnimateTotalScoreUpdate(int handScore, float intensity)
        {
            // Show +handScore in bonus text
            if (bonusText != null)
            {
                bonusText.text = $"<size=150%><b>+{handScore}</b></size>";
                bonusText.gameObject.SetActive(true);
                
                Color color = bonusText.color;
                color.a = 1f;
                bonusText.color = color;
            }

            // Count up total score
            int startScore = _currentTotalScore;
            int targetScore = _currentTotalScore + handScore;

            float elapsed = 0f;
            float duration = countUpDuration; // Match standard count duration

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                int score = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, smoothT));

                UpdateTotalScore(score);
                
                // Pop total score text
                if (totalScoreText != null && t > 0.5f)
                {
                    float popT = (t - 0.5f) * 2f;
                    float scale = 1.0f + (0.2f * intensity * Mathf.Sin(popT * Mathf.PI));
                    totalScoreText.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            UpdateTotalScore(targetScore);
            
            if (totalScoreText != null)
            {
                totalScoreText.transform.localScale = Vector3.one;
            }

            // Fade out bonus text
            if (bonusText != null)
            {
                elapsed = 0f;
                duration = 0.3f;
                Color startColor = bonusText.color;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    Color color = bonusText.color;
                    color.a = Mathf.Lerp(startColor.a, 0f, t);
                    bonusText.color = color;
                    yield return null;
                }

                bonusText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Trigger pop effect on source dice or relic
        /// </summary>
        private void TriggerSourcePop(object sourceObject, List<BaseDice> submittedDice, float intensity)
        {
            if (sourceObject == null) return;

            // Check if it's a dice
            if (sourceObject is BaseDice dice)
            {
                // Find corresponding dice view
                int diceIndex = submittedDice.IndexOf(dice);
                if (diceIndex >= 0 && diceIndex < _diceViews.Count && _diceViews[diceIndex] != null)
                {
                    _diceViews[diceIndex].PopEffect(intensity);
                }
            }
            // Check if it's a relic
            else if (sourceObject is RelicBase relic)
            {
                if (relicDisplay != null)
                {
                    relicDisplay.PopRelicByReference(relic, intensity);
                }
            }
        }

        /// <summary>
        /// Calculate animation intensity based on score value
        /// Higher scores = stronger effects
        /// </summary>
        private float CalculateIntensity(float scoreValue)
        {
            // Scale intensity: 0-50 = 1x, 50-200 = 1-2x, 200+ = 2-3x
            if (scoreValue < 50) return 1.0f;
            else if (scoreValue < 200) return 1.0f + ((scoreValue - 50) / 150f);
            else return 2.0f + Mathf.Min((scoreValue - 200) / 200f, 1f);
        }

        /// <summary>
        /// Update combo score display (2 lines: combo name + score)
        /// </summary>
        private void UpdateComboScore(string comboName, int score)
        {
            if (comboScoreText != null)
            {
                comboScoreText.text = $"<size=120%><b>{comboName}</b></size>\n<size=200%><b>{score}</b></size>";
            }
        }

        /// <summary>
        /// Extract current score value from formatted text
        /// </summary>
        private int ExtractScoreFromText(string text)
        {
            // Try to extract number from second line
            string[] lines = text.Split('\n');
            if (lines.Length > 1)
            {
                string scoreLine = lines[1];
                // Remove all formatting tags
                scoreLine = System.Text.RegularExpressions.Regex.Replace(scoreLine, "<.*?>", string.Empty);
                if (int.TryParse(scoreLine.Trim(), out int score))
                {
                    return score;
                }
            }
            return 0;
        }

        /// <summary>
        /// Update total score display
        /// </summary>
        private void UpdateTotalScore(int score)
        {
            if (totalScoreText != null)
            {
                totalScoreText.text = $"<size=80%>Total Score</size>\n<size=150%><b>{score}</b></size>";
            }
        }

        /// <summary>
        /// Reset total score
        /// </summary>
        public void ResetTotalScore()
        {
            _currentTotalScore = 0;
            UpdateTotalScore(0);
            
            if (comboScoreText != null)
            {
                comboScoreText.text = "<color=#AAAAAA>Submit a combo to see score</color>";
            }
        }

        /// <summary>
        /// Get current total score
        /// </summary>
        public int GetTotalScore()
        {
            return _currentTotalScore;
        }

        /// <summary>
        /// Fade out combo text
        /// </summary>
        private IEnumerator FadeOutComboText()
        {
            if (comboScoreText == null) yield break;

            Color startColor = comboScoreText.color;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, t);
                comboScoreText.color = color;

                yield return null;
            }

            Color finalColor = comboScoreText.color;
            finalColor.a = 0f;
            comboScoreText.color = finalColor;
        }

        /// <summary>
        /// Animate target score evaluation with dramatic reveal
        /// </summary>
        public void AnimateTargetEvaluation(int finalScore, int targetScore, bool passed)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AnimateTargetEvaluationCoroutine(finalScore, targetScore, passed));
        }

        private IEnumerator AnimateTargetEvaluationCoroutine(int finalScore, int targetScore, bool passed)
        {
            if (comboScoreText == null) yield break;

            // Make visible
            var color = comboScoreText.color;
            color.a = 1f;
            comboScoreText.color = color;

            // Show "Evaluating..."
            comboScoreText.text = "<size=180%><b>EVALUATING...</b></size>";
            yield return new WaitForSeconds(1.0f);

            // Show final vs target
            comboScoreText.text = $"<size=120%>Final Score</size>\n<size=200%><color=#FFD700><b>{finalScore}</b></color></size>\n\n";
            comboScoreText.text += $"<size=120%>Target</size>\n<size=200%><color=#88CCFF><b>{targetScore}</b></color></size>";
            yield return StartCoroutine(PopScoreText(2.0f));
            yield return new WaitForSeconds(1.5f);

            // Result
            if (passed)
            {
                comboScoreText.text = "<size=250%><color=#00FF00><b>PASSED!</b></color></size>\n\n";
                comboScoreText.text += $"<color=#88FF88>+{finalScore - targetScore} over target!</color>";
                yield return StartCoroutine(PopScoreText(3.0f));
                yield return StartCoroutine(PopScoreText(3.0f));
            }
            else
            {
                comboScoreText.text = "<size=250%><color=#FF3333><b>FAILED!</b></color></size>\n\n";
                comboScoreText.text += $"<color=#FF8888>{targetScore - finalScore} short of target</color>";
                yield return StartCoroutine(ShakeText());
            }

            yield return new WaitForSeconds(3.5f);
        }

        /// <summary>
        /// Shake animation for failure
        /// </summary>
        private IEnumerator ShakeText()
        {
            if (comboScoreText == null) yield break;

            Vector3 originalPosition = comboScoreText.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.5f;
            float magnitude = 10f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float x = originalPosition.x + Random.Range(-magnitude, magnitude);
                float y = originalPosition.y + Random.Range(-magnitude, magnitude);

                comboScoreText.transform.localPosition = new Vector3(x, y, originalPosition.z);

                yield return null;
            }

            comboScoreText.transform.localPosition = originalPosition;
        }
    }
}
