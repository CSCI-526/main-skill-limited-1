using UnityEngine;
using UnityEngine.UI;

namespace DiceGame
{
    /// <summary>
    /// 管理设置面板的显示和按钮逻辑
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Settings Panel UI")]
        public GameObject settingsPanel;     // Settings panel (window + overlay)
        public GameObject settingsOverlay;   // Shaded background overlay
        public GameObject settingsWindow;   // Settings window (middle of screen)
        public Button settingsButton;        // Button to open settings
        public Button settingsResetButton;   // Reset button in settings
        public Button settingsQuitButton;    // Quit button in settings
        public Button settingsCloseButton;   // Close button (optional)
        public Button settingsCheatButton;  // NEW Cheat button

        // Events
        public System.Action OnResetRequested;
        public System.Action OnQuitRequested;
        public System.Action OnCheatRequested;  // NEW event

        private bool _isInitialized = false;
        
        /// <summary>
        /// Initialize settings panel
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            // Initialize settings panel (hidden by default)
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            // Set up settings button
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(Open);
            }
            
            // Set up settings panel buttons
            if (settingsResetButton != null)
            {
                settingsResetButton.onClick.AddListener(OnResetClicked);
            }
            
            if (settingsQuitButton != null)
            {
                settingsQuitButton.onClick.AddListener(OnQuitClicked);
            }
            
            // Setup overlay (just visual, not clickable)
            if (settingsOverlay != null)
            {
                // Ensure overlay has Image component
                Image overlayImage = settingsOverlay.GetComponent<Image>();
                if (overlayImage == null)
                {
                    overlayImage = settingsOverlay.AddComponent<Image>();
                }
                // Disable raycast target so overlay is not clickable
                overlayImage.raycastTarget = false;
                
                // Remove Button component if it exists (we don't want overlay to be clickable)
                Button overlayButton = settingsOverlay.GetComponent<Button>();
                if (overlayButton != null)
                {
                    DestroyImmediate(overlayButton);
                }
            }
            
            // Ensure settings window blocks raycasts
            if (settingsWindow != null)
            {
                Image windowImage = settingsWindow.GetComponent<Image>();
                if (windowImage == null)
                {
                    windowImage = settingsWindow.AddComponent<Image>();
                }
                windowImage.raycastTarget = true; // This blocks raycasts
            }
            
            // Setup close button if provided
            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(Close);
            }

            // Setup cheat button
            if (settingsCheatButton != null)
            {
                settingsCheatButton.onClick.AddListener(OnCheatClicked);
            }

        }

        /// <summary>
        /// Open settings panel
        /// </summary>
        public void Open()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                Debug.Log("[SettingsPanel] Settings panel opened");
            }
        }
        
        /// <summary>
        /// Close settings panel
        /// </summary>
        public void Close()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                Debug.Log("[SettingsPanel] Settings panel closed");
            }
        }
        
        /// <summary>
        /// Handle reset button click
        /// </summary>
        private void OnResetClicked()
        {
            Debug.Log("[SettingsPanel] Reset button clicked");
            Close();
            OnResetRequested?.Invoke();
        }
        
        /// <summary>
        /// Handle quit button click
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("[SettingsPanel] Quit button clicked");
            Close();
            OnQuitRequested?.Invoke();
        }

        private void OnCheatClicked()
        {
            Debug.Log("[SettingsPanel] Cheat button clicked");
            Close();
            OnCheatRequested?.Invoke();
        }

        void OnDestroy()
        {
            // Clean up button listeners
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
            }
            if (settingsResetButton != null)
            {
                settingsResetButton.onClick.RemoveAllListeners();
            }
            if (settingsQuitButton != null)
            {
                settingsQuitButton.onClick.RemoveAllListeners();
            }
            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.RemoveAllListeners();
            }
            if (settingsCheatButton != null)
            {
                settingsCheatButton.onClick.RemoveAllListeners();
            }
        }
    }
}

