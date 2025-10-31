using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 
using DiceGame.Core;

namespace DiceGame
{
    /// <summary>
    /// 单个骰子的 UI 视图：显示点数、锁定按钮、锁定高亮
    /// </summary>
    public class DiceView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Wiring")]
        public TMP_Text valueText;
        public Button lockButton;
        public Image lockIndicator; // 可选高亮

        [HideInInspector] public BaseDice model;

        void Awake()
        {
            if (lockButton != null)
                lockButton.onClick.AddListener(OnToggleLock);
            Refresh();
        }

        public void Bind(BaseDice dice)
        {
            model = dice;
            Refresh();
        }

        public void Refresh()
        {
            if (model == null) return;
            valueText.text = model.lastRollValue > 0 ? model.lastRollValue.ToString() : "-";
            if (lockButton != null)
            {
                bool hasRolledValue = model.lastRollValue > 0 && model.tier != DiceTier.Filler;
                lockButton.interactable = hasRolledValue;
            }
            if (lockIndicator != null)
                lockIndicator.enabled = model.isLocked;
        }

        /// <summary>
        /// Set a custom display value (for placeholder dice)
        /// </summary>
        public void SetDisplayValue(string value)
        {
            if (valueText != null)
                valueText.text = value;
            if (lockButton != null)
            {
                bool hasRolledValue = model != null && model.lastRollValue > 0 && model.tier != DiceTier.Filler;
                lockButton.interactable = hasRolledValue;
            }
        }

        void OnToggleLock()
        {
            if (model == null) return;
            model.ToggleLock();
            
            string status = model.isLocked ? "LOCKED" : "UNLOCKED";
            Debug.Log($"[DiceView] {model.diceName} is now {status} (value: {model.lastRollValue})");
            
            Refresh();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (model != null && DiceTooltipManager.Instance != null)
            {
                DiceTooltipManager.Instance.ShowTooltip(model);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (DiceTooltipManager.Instance != null)
            {
                DiceTooltipManager.Instance.HideTooltip();
            }
        }

        /// <summary>
        /// Trigger a pop effect animation on this dice view
        /// Scale increases with the magnitude of the score
        /// </summary>
        public void PopEffect(float intensity = 1.0f)
        {
            StopAllCoroutines();
            StartCoroutine(PopEffectCoroutine(intensity));
        }

        private System.Collections.IEnumerator PopEffectCoroutine(float intensity)
        {
            Vector3 originalScale = transform.localScale;
            
            // Scale based on intensity (1.0 = normal, higher = bigger pop)
            // Increased from 0.3f to 0.5f for more dramatic effect
            float targetScale = 1.0f + (0.5f * Mathf.Min(intensity, 2.5f));
            Vector3 popScale = originalScale * targetScale;
            
            float duration = 0.12f; // Slightly faster for snappier feel
            float elapsed = 0f;
            
            // Scale up with ease-out
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease out cubic for snappier start
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(originalScale, popScale, smoothT);
                yield return null;
            }
            
            elapsed = 0f;
            // Scale down with bounce
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease in cubic with slight overshoot for bounce
                float smoothT = Mathf.Pow(t, 2f);
                transform.localScale = Vector3.Lerp(popScale, originalScale * 0.95f, smoothT);
                yield return null;
            }
            
            // Small bounce back to original
            elapsed = 0f;
            duration = 0.08f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale * 0.95f, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
    }
}
