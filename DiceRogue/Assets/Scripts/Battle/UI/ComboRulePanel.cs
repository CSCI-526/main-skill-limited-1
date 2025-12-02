using UnityEngine;
using UnityEngine.UI;

namespace DiceGame
{
    /// <summary>
    /// 管理组合规则面板的显示和按钮逻辑
    /// </summary>
    public class ComboRulePanel : MonoBehaviour
    {
        [Header("Combo Rule Panel UI")]
        public GameObject comboRulePanel;         // Combo rule panel (main panel)
        public GameObject comboRuleOverlay;      // Shaded background overlay
        public GameObject comboRuleWindow;       // Combo rule window (middle of screen)
        public Button comboRuleButton;           // Button to open combo rule panel
        public Button comboRuleCloseButton;       // Close button (optional)

        private bool _isInitialized = false;

        /// <summary>
        /// Initialize combo rule panel
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Auto-find ComboRulePanel if not assigned
            if (comboRulePanel == null)
            {
                GameObject foundPanel = GameObject.Find("ComboRulePanel");
                if (foundPanel != null)
                {
                    comboRulePanel = foundPanel;
                    Debug.Log("[ComboRulePanel] Auto-found ComboRulePanel");
                }
            }

            // Initialize combo rule panel (hidden by default)
            if (comboRulePanel != null)
            {
                comboRulePanel.SetActive(false);
            }
            
            // Initialize overlay (hidden by default)
            if (comboRuleOverlay != null)
            {
                comboRuleOverlay.SetActive(false);
            }

            // Set up combo rule button
            if (comboRuleButton != null)
            {
                comboRuleButton.onClick.AddListener(Open);
            }

            // Setup overlay to block background clicks (but not close panel)
            if (comboRuleOverlay != null)
            {
                Image overlayImage = comboRuleOverlay.GetComponent<Image>();
                if (overlayImage == null)
                {
                    overlayImage = comboRuleOverlay.AddComponent<Image>();
                }
                // Enable raycast target so overlay blocks clicks to background
                overlayImage.raycastTarget = true;

                // Remove Button component if it exists (we don't want overlay to close panel)
                Button overlayButton = comboRuleOverlay.GetComponent<Button>();
                if (overlayButton != null)
                {
                    DestroyImmediate(overlayButton);
                }
            }

            // Ensure window blocks raycasts
            if (comboRuleWindow != null)
            {
                Image windowImage = comboRuleWindow.GetComponent<Image>();
                if (windowImage == null)
                {
                    windowImage = comboRuleWindow.AddComponent<Image>();
                }
                windowImage.raycastTarget = true;
            }

            // Setup close button if provided
            if (comboRuleCloseButton != null)
            {
                comboRuleCloseButton.onClick.AddListener(Close);
            }
        }

        /// <summary>
        /// Open combo rule panel
        /// </summary>
        public void Open()
        {
            if (comboRulePanel != null)
            {
                comboRulePanel.SetActive(true);
                
                // Ensure overlay is also active to block background clicks
                if (comboRuleOverlay != null)
                {
                    comboRuleOverlay.SetActive(true);
                }
                
                Debug.Log("[ComboRulePanel] Combo rule panel opened");
            }
        }

        /// <summary>
        /// Close combo rule panel
        /// </summary>
        public void Close()
        {
            if (comboRulePanel != null)
            {
                comboRulePanel.SetActive(false);
                
                // Hide overlay when panel is closed
                if (comboRuleOverlay != null)
                {
                    comboRuleOverlay.SetActive(false);
                }
                
                Debug.Log("[ComboRulePanel] Combo rule panel closed");
            }
        }

        void OnDestroy()
        {
            // Clean up button listeners
            if (comboRuleButton != null)
            {
                comboRuleButton.onClick.RemoveAllListeners();
            }
            if (comboRuleCloseButton != null)
            {
                comboRuleCloseButton.onClick.RemoveAllListeners();
            }
        }
    }
}
