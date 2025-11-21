using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DiceGame
{
    /// <summary>
    /// 管理组合偏好面板的显示和按钮逻辑
    /// </summary>
    public class ComboPreferencePanel : MonoBehaviour
    {
        [Header("Combo Preference Panel UI")]
        public GameObject comboPreferencePanel;  // Combo preference panel (window + overlay)
        public GameObject comboPreferenceOverlay; // Shaded background overlay
        public GameObject comboPreferenceWindow; // Combo preference window (middle of screen)
        public Button comboPreferenceButton;     // Button to open combo preference
        public Button comboPreferenceCloseButton; // Close button (optional)
        
        private bool _isInitialized = false;
        private bool _delayedInitScheduled = false;
        private bool _needsDelayedInit = false;

        private void RegisterButton(Button button, UnityAction handler, string buttonDescription)
        {
            if (button == null || handler == null)
            {
                return;
            }

            try
            {
                // Unity's Button.onClick is always initialized, so we can directly add listeners
                // Creating a new ButtonClickedEvent can cause memory issues in WebGL
                button.onClick.AddListener(handler);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ComboPreferencePanel] Failed to register {buttonDescription}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize combo preference panel
        /// Marks for delayed initialization to ensure WebGL compatibility
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            // Mark that we need delayed initialization
            // Button registration will happen in Start() when components are fully initialized
            _needsDelayedInit = true;
        }
        
        /// <summary>
        /// Unity Start method - called after MonoBehaviour is fully initialized
        /// This is when we safely register button listeners in WebGL
        /// </summary>
        void Start()
        {
            // Register button listeners now that components are fully initialized
            // This prevents memory access errors in WebGL
            if (_isInitialized && !_delayedInitScheduled)
            {
                // Set up button listeners (only if buttons are assigned, they're optional)
                if (comboPreferenceButton != null)
                {
                    RegisterButton(comboPreferenceButton, Open, "open button");
                }
                if (comboPreferenceCloseButton != null)
                {
                    RegisterButton(comboPreferenceCloseButton, Close, "close button");
                }
                
                _delayedInitScheduled = true;
                
                // Use Invoke to delay SetActive calls by one frame for WebGL compatibility
                try
                {
                    Invoke(nameof(DelayedInitialize), 0.01f);
                }
                catch (System.Exception)
                {
                    // If Invoke fails, try to do it synchronously with extra safety
                    DelayedInitialize();
                }
            }
        }
        
        /// <summary>
        /// Delayed initialization - called after a short delay to ensure GameObjects are fully initialized
        /// </summary>
        private void DelayedInitialize()
        {
            // Initialize combo preference panel (hidden by default)
            if (comboPreferencePanel != null)
            {
                comboPreferencePanel.SetActive(false);
            }
            
            // Setup overlay (just visual, not clickable)
            if (comboPreferenceOverlay != null)
            {
                Image overlayImage = comboPreferenceOverlay.GetComponent<Image>();
                if (overlayImage == null)
                {
                    overlayImage = comboPreferenceOverlay.AddComponent<Image>();
                }
                overlayImage.raycastTarget = false;
                
                Button overlayButton = comboPreferenceOverlay.GetComponent<Button>();
                if (overlayButton != null)
                {
                    DestroyImmediate(overlayButton);
                }
            }
            
            // Ensure window blocks raycasts
            if (comboPreferenceWindow != null)
            {
                Image windowImage = comboPreferenceWindow.GetComponent<Image>();
                if (windowImage == null)
                {
                    windowImage = comboPreferenceWindow.AddComponent<Image>();
                }
                windowImage.raycastTarget = true;
            }
        }
        
        /// <summary>
        /// Open combo preference panel
        /// </summary>
        public void Open()
        {
            if (comboPreferencePanel != null)
            {
                comboPreferencePanel.SetActive(true);
                Debug.Log("[ComboPreferencePanel] Combo preference panel opened");
            }
        }
        
        /// <summary>
        /// Close combo preference panel
        /// </summary>
        public void Close()
        {
            if (comboPreferencePanel != null)
            {
                comboPreferencePanel.SetActive(false);
                Debug.Log("[ComboPreferencePanel] Combo preference panel closed");
            }
        }
        
        void OnDestroy()
        {
            // Cancel any pending delayed initialization
            CancelInvoke(nameof(DelayedInitialize));
            
            // Clean up button listeners
            if (comboPreferenceButton != null && comboPreferenceButton.onClick != null)
            {
                comboPreferenceButton.onClick.RemoveListener(Open);
            }
            if (comboPreferenceCloseButton != null && comboPreferenceCloseButton.onClick != null)
            {
                comboPreferenceCloseButton.onClick.RemoveListener(Close);
            }
        }
    }
}
