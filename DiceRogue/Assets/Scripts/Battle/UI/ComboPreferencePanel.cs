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
        
        /// <summary>
        /// Initialize combo preference panel
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            // Initialize combo preference panel (hidden by default)
            if (comboPreferencePanel != null)
            {
                comboPreferencePanel.SetActive(false);
            }
            
            // Set up combo preference button
            if (comboPreferenceButton != null)
            {
                comboPreferenceButton.onClick.AddListener(Open);
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
            
            // Setup close button if provided
            if (comboPreferenceCloseButton != null)
            {
                comboPreferenceCloseButton.onClick.AddListener(Close);
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

