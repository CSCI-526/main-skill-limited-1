using UnityEngine;
using UnityEngine.UI;

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
        
        /// <summary>
        /// Initialize combo preference panel
        /// Uses Start() method to delay SetActive calls for WebGL compatibility
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            // Set up button listeners immediately (these are safe)
            if (comboPreferenceButton != null)
            {
                comboPreferenceButton.onClick.AddListener(Open);
            }
            
            if (comboPreferenceCloseButton != null)
            {
                comboPreferenceCloseButton.onClick.AddListener(Close);
            }
            
            // Mark that we need delayed initialization
            // This will be handled in Start() which is guaranteed to run after MonoBehaviour is fully initialized
            _needsDelayedInit = true;
        }
        
        /// <summary>
        /// Unity Start method - called after MonoBehaviour is fully initialized
        /// </summary>
        void Start()
        {
            // Perform delayed initialization if needed
            if (_needsDelayedInit && !_delayedInitScheduled)
            {
                _delayedInitScheduled = true;
                // Use Invoke to delay by one frame
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
            if (comboPreferenceButton != null)
            {
                comboPreferenceButton.onClick.RemoveAllListeners();
            }
            if (comboPreferenceCloseButton != null)
            {
                comboPreferenceCloseButton.onClick.RemoveAllListeners();
            }
        }
    }
}

