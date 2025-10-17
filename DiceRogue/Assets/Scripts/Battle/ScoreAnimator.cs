using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DiceGame
{
    /// <summary>
    /// Balatro-style animated score display system
    /// Shows calculation breakdown with smooth number counting animations
    /// </summary>
    public class ScoreAnimator : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text comboScoreText;
        public TMP_Text totalScoreText;

        [Header("Animation Settings")]
        public float countDuration = 0.5f;        // Duration for number counting animation
        public float stepDelay = 0.3f;            // Delay between calculation steps
        public float multiplierPulseScale = 1.2f; // Scale for multiplier pulse effect
        public float displayDuration = 3.0f;      // How long to show result before fading
        public float fadeOutDuration = 0.5f;      // Duration of fade out animation
        public Color highlightColor = Color.yellow;
        public Color normalColor = Color.white;

        private int _currentTotalScore = 0;
        private Coroutine _animationCoroutine;
        private Coroutine _fadeOutCoroutine;

        void Start()
        {
            // Initialize displays
            UpdateComboDisplay("");
            UpdateTotalScore(0, false);
        }

        /// <summary>
        /// Animate score calculation with Balatro-style breakdown
        /// </summary>
        public void AnimateScore(List<int> diceValues, string comboName, int baseScore, int diceSum, 
                                float comboMultiplier, float diceMultiplier, int finalScore)
        {
            // Stop any existing animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AnimateScoreCoroutine(
                diceValues, comboName, baseScore, diceSum, 
                comboMultiplier, diceMultiplier, finalScore));
        }

        private IEnumerator AnimateScoreCoroutine(List<int> diceValues, string comboName, int baseScore, 
                                                   int diceSum, float comboMultiplier, float diceMultiplier, 
                                                   int finalScore)
        {
            // Ensure combo text is visible at start
            if (comboScoreText != null)
            {
                var color = comboScoreText.color;
                color.a = 1f;
                comboScoreText.color = color;
            }

            // Step 1: Show combo name and dice (SMALLER SIZE)
            string display = $"<size=120%><b>{comboName}</b></size>\n\n";
            display += $"<color=#AAAAAA>Dice: [{string.Join(", ", diceValues)}]</color>\n\n";
            UpdateComboDisplay(display);
            yield return new WaitForSeconds(stepDelay);

            // Step 2: Show base score
            display += $"<color=#88FF88>Base Score:</color> <b>{baseScore}</b>\n";
            UpdateComboDisplay(display);
            yield return new WaitForSeconds(stepDelay);

            // Step 3: Add dice sum
            int currentScore = baseScore;
            display += $"<color=#88FF88>+ Dice Sum:</color> <b>{diceSum}</b>\n";
            display += $"<color=#FFAA44>= {baseScore + diceSum}</color>\n\n";
            UpdateComboDisplay(display);
            yield return new WaitForSeconds(stepDelay);

            // Step 4: Show combo multiplier
            currentScore = baseScore + diceSum;
            display += $"<color=#FF88FF>× Combo Multiplier:</color> <b>×{comboMultiplier:F1}</b>\n";
            UpdateComboDisplay(display);
            
            // Pulse effect for multiplier
            if (comboScoreText != null)
            {
                yield return StartCoroutine(PulseText(comboScoreText));
            }
            
            int afterComboMult = Mathf.RoundToInt(currentScore * comboMultiplier);
            display += $"<color=#FFAA44>= {afterComboMult}</color>\n\n";
            UpdateComboDisplay(display);
            yield return new WaitForSeconds(stepDelay);

            // Step 5: Show dice multiplier (if applicable)
            if (diceMultiplier > 1f)
            {
                display += $"<color=#FF88FF>× Dice Multiplier:</color> <b>×{diceMultiplier:F1}</b>\n";
                UpdateComboDisplay(display);
                
                // Pulse effect for multiplier
                if (comboScoreText != null)
                {
                    yield return StartCoroutine(PulseText(comboScoreText));
                }
                
                display += $"<color=#FFAA44>= {finalScore}</color>\n\n";
                UpdateComboDisplay(display);
                yield return new WaitForSeconds(stepDelay);
            }

            // Step 6: Show final score with emphasis (SMALLER SIZE)
            display += $"\n<size=150%><color=#FFD700><b>FINAL SCORE: {finalScore}</b></color></size>";
            UpdateComboDisplay(display);
            
            // Animate counting up the total score
            yield return StartCoroutine(CountUpScore(_currentTotalScore, _currentTotalScore + finalScore));
            _currentTotalScore += finalScore;

            // Hold the final display for a moment
            yield return new WaitForSeconds(displayDuration);

            // Fade out the combo score text
            yield return StartCoroutine(FadeOutComboText());
        }

        /// <summary>
        /// Pulse animation for emphasis
        /// </summary>
        private IEnumerator PulseText(TMP_Text text)
        {
            Vector3 originalScale = text.transform.localScale;
            Vector3 targetScale = originalScale * multiplierPulseScale;
            
            float elapsed = 0f;
            float duration = 0.2f;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                text.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            // Scale down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                text.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            text.transform.localScale = originalScale;
        }

        /// <summary>
        /// Animate counting from start to end value
        /// </summary>
        private IEnumerator CountUpScore(int startValue, int endValue)
        {
            float elapsed = 0f;
            
            while (elapsed < countDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / countDuration;
                
                // Use ease-out curve for smooth deceleration
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, smoothT));
                
                UpdateTotalScore(currentValue, true);
                yield return null;
            }
            
            UpdateTotalScore(endValue, false);
        }

        /// <summary>
        /// Update combo score display
        /// </summary>
        private void UpdateComboDisplay(string text)
        {
            if (comboScoreText != null)
            {
                comboScoreText.text = text;
            }
        }

        /// <summary>
        /// Update total score display
        /// </summary>
        private void UpdateTotalScore(int score, bool isAnimating)
        {
            if (totalScoreText != null)
            {
                totalScoreText.text = $"<size=80%>Total Score</size>\n<size=150%><b>{score}</b></size>";
                
                // Highlight during animation
                if (isAnimating)
                {
                    totalScoreText.color = highlightColor;
                }
                else
                {
                    totalScoreText.color = normalColor;
                }
            }
        }

        /// <summary>
        /// Reset total score (e.g., for new run)
        /// </summary>
        public void ResetTotalScore()
        {
            _currentTotalScore = 0;
            UpdateTotalScore(0, false);
            UpdateComboDisplay("<color=#AAAAAA>Submit a combo to see score breakdown</color>");
        }

        /// <summary>
        /// Get current total score
        /// </summary>
        public int GetTotalScore()
        {
            return _currentTotalScore;
        }

        /// <summary>
        /// Fade out the combo score text
        /// </summary>
        private IEnumerator FadeOutComboText()
        {
            if (comboScoreText == null) yield break;

            Color startColor = comboScoreText.color;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, t);
                comboScoreText.color = newColor;
                
                yield return null;
            }

            // Fully transparent and clear text
            Color finalColor = comboScoreText.color;
            finalColor.a = 0f;
            comboScoreText.color = finalColor;
            comboScoreText.text = "";
        }

        /// <summary>
        /// Skip animation and show final result immediately
        /// </summary>
        public void SkipAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
            
            if (_fadeOutCoroutine != null)
            {
                StopCoroutine(_fadeOutCoroutine);
                _fadeOutCoroutine = null;
            }
        }
    }
}

