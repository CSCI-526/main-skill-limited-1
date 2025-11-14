using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using DiceGame.Core;

namespace DiceGame.UI
{
    /// <summary>
    /// Component for dice prefabs in the backpack selection UI
    /// Handles selection, cooldown state, and click interactions
    /// </summary>
    public class DiceSelectionItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Selection UI")]
        public Image selectionIndicator; // Optional overlay to show selection state
        public Image cooldownOverlay; // Optional overlay to show cooldown state
        public CanvasGroup canvasGroup; // For graying out when on cooldown
        
        [Header("Visual Feedback")]
        [Tooltip("Background image that can be highlighted when hovered")]
        public Image backgroundImage;
        [Tooltip("Border image that can be shown when selected")]
        public Image borderImage;
        [Tooltip("Color for hover state")]
        public Color hoverColor = new Color(1f, 1f, 1f, 0.8f);
        [Tooltip("Scale factor when selected (e.g., 1.1 = 10% larger)")]
        public float selectedScale = 1.1f;
        [Tooltip("Scale factor when hovered")]
        public float hoverScale = 1.05f;
        [Tooltip("Alpha value when selected (0.5-0.6 for more obvious grayed effect)")]
        [Range(0f, 1f)]
        public float selectedAlpha = 0.55f;
        [Tooltip("Animation duration for scale changes")]
        public float animationDuration = 0.2f;

        private BaseDice _dice;
        private Action<BaseDice, DiceSelectionItem> _onClick;
        private DiceUI _diceUI;
        private Button _button;
        private bool _isSelected;
        private bool _isHovered;
        private Vector3 _normalizedScale = Vector3.one; // Always 1.0 for consistent scaling
        private Color _originalColor;
        private float _originalAlpha = 1f;
        private Coroutine _scaleCoroutine;
        private Coroutine _alphaCoroutine;
        private Coroutine _colorCoroutine; // For hover color animations

        private void Awake()
        {
            // Try to find background image if not assigned
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
            
            if (backgroundImage != null)
            {
                _originalColor = backgroundImage.color;
            }
            
            // Try to find or create CanvasGroup for alpha control
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            if (canvasGroup != null)
            {
                _originalAlpha = canvasGroup.alpha;
            }
        }

        /// <summary>
        /// Setup the dice selection item with a dice instance
        /// </summary>
        public void Setup(BaseDice dice, Action<BaseDice, DiceSelectionItem> onClick)
        {
            _dice = dice;
            _onClick = onClick;

            // Normalize scale to 1.0 for consistent scaling across all dice
            // This ensures all dice scale to the same absolute size when selected
            transform.localScale = Vector3.one;
            _normalizedScale = Vector3.one;

            // Get or find DiceUI component and override its boundDice
            _diceUI = GetComponentInChildren<DiceUI>();
            if (_diceUI != null)
            {
                _diceUI.boundDice = dice;
            }

            // Check if there's a Button component (preferred for UI)
            _button = GetComponentInChildren<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
            // If no Button, IPointerClickHandler will handle clicks

            // Setup interactivity based on cooldown
            UpdateInteractableState();

            // Initialize selection state - ensure alpha is set correctly
            SetSelected(false);
            
            // After setting up, ensure alpha respects both cooldown and selection state
            // If not on cooldown, restore to original alpha (selection will override if needed)
            if (canvasGroup != null && _dice != null && _dice.cooldownRemain == 0)
            {
                canvasGroup.alpha = _originalAlpha;
            }
        }

        /// <summary>
        /// Set the selection state with visual feedback
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            
            // Update selection indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.enabled = isSelected;
            }
            
            // Update border visibility (but don't change border color)
            if (borderImage != null)
            {
                borderImage.enabled = isSelected;
            }
            
            // Animate scale - all dice use normalized scale (1.0 base) for consistency
            float targetScale = isSelected ? selectedScale : (_isHovered ? hoverScale : 1f);
            AnimateScale(targetScale);
            
            // Update alpha for graying effect when selected
            if (canvasGroup != null)
            {
                float targetAlpha = isSelected ? selectedAlpha : _originalAlpha;
                AnimateAlpha(targetAlpha);
            }
            
            // Only update color for hover (not selection)
            if (!isSelected && backgroundImage != null)
            {
                Color targetColor = _isHovered ? hoverColor : _originalColor;
                AnimateColor(targetColor);
            }
        }

        /// <summary>
        /// Update interactable state based on cooldown
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            // Only allow making interactable if the dice is not on cooldown
            if (_dice != null && _dice.cooldownRemain == 0)
            {
                UpdateInteractableState(interactable);
            }
        }

        private void UpdateInteractableState(bool? forceInteractable = null)
        {
            bool isInteractable = forceInteractable ?? (_dice != null && _dice.cooldownRemain == 0);

            // Update button interactability if it exists
            if (_button != null)
            {
                _button.interactable = isInteractable;
            }

            // Update canvas group alpha for cooldown visual feedback
            // IMPORTANT: Don't override selection alpha - preserve it if dice is selected
            if (canvasGroup != null)
            {
                // Only update alpha for cooldown state, not selection state
                // Selection alpha is handled separately in SetSelected()
                if (!isInteractable)
                {
                    // On cooldown: use low alpha (cooldown overrides selection)
                    canvasGroup.alpha = 0.5f;
                }
                else if (!_isSelected)
                {
                    // If interactable and NOT selected, restore to original alpha
                    // This handles the case when dice is deselected
                    canvasGroup.alpha = _originalAlpha;
                }
                // If interactable AND selected, keep current alpha (selection alpha)
                // Don't reset to 1.0 here - let SetSelected() manage selection alpha
                canvasGroup.interactable = isInteractable;
            }

            // Show/hide cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.enabled = !isInteractable;
            }
        }

        private void HandleClick()
        {
            if (_dice != null && _dice.cooldownRemain == 0)
            {
                // Add a quick "pulse" animation on click
                StartCoroutine(PulseAnimation());
                _onClick?.Invoke(_dice, this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only handle if there's no Button component (Button handles clicks itself)
            if (_button == null)
            {
                HandleClick();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_dice != null && _dice.cooldownRemain == 0)
            {
                _isHovered = true;
                if (!_isSelected)
                {
                    AnimateScale(hoverScale);
                    if (backgroundImage != null)
                    {
                        AnimateColor(hoverColor);
                    }
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isSelected)
            {
                AnimateScale(1f);
                if (backgroundImage != null)
                {
                    AnimateColor(_originalColor);
                }
            }
        }

        private void AnimateScale(float targetScale)
        {
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
            }
            _scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale));
        }

        private void AnimateColor(Color targetColor)
        {
            if (_colorCoroutine != null)
            {
                StopCoroutine(_colorCoroutine);
            }
            if (backgroundImage != null)
            {
                _colorCoroutine = StartCoroutine(ColorAnimation(targetColor));
            }
        }

        private void AnimateAlpha(float targetAlpha)
        {
            if (_alphaCoroutine != null)
            {
                StopCoroutine(_alphaCoroutine);
            }
            if (canvasGroup != null)
            {
                _alphaCoroutine = StartCoroutine(AlphaAnimation(targetAlpha));
            }
        }

        private IEnumerator ScaleAnimation(float targetScale)
        {
            // Always start from normalized scale (1.0) to ensure consistency
            // This prevents scale drift and ensures all dice scale to the same absolute size
            Vector3 startScale = _normalizedScale;
            Vector3 endScale = _normalizedScale * targetScale;
            float elapsed = 0f;

            // If current scale is not at normalized scale, snap to it first for consistency
            // This handles cases where pulse animation or other effects modified the scale
            if (Vector3.Distance(transform.localScale, startScale) > 0.01f)
            {
                transform.localScale = startScale;
            }

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                // Smooth easing
                t = t * t * (3f - 2f * t); // Smoothstep
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        private IEnumerator ColorAnimation(Color targetColor)
        {
            if (backgroundImage == null) yield break;

            Color startColor = backgroundImage.color;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            backgroundImage.color = targetColor;
        }

        private IEnumerator AlphaAnimation(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator PulseAnimation()
        {
            Vector3 currentScale = transform.localScale;
            float pulseScale = 1.15f;
            float pulseDuration = 0.1f;

            // Scale up
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                transform.localScale = Vector3.Lerp(currentScale, currentScale * pulseScale, t);
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            Vector3 peakScale = currentScale * pulseScale;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                transform.localScale = Vector3.Lerp(peakScale, currentScale, t);
                yield return null;
            }

            transform.localScale = currentScale;
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}

