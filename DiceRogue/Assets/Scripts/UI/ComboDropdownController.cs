using UnityEngine;
using UnityEngine.UI;

namespace DiceGame
{
    /// <summary>
    /// 管理组合下拉菜单面板的显示和按钮逻辑
    /// </summary>
    public class ComboDropdownController : MonoBehaviour
    {
        [Header("Combo Dropdown Panel UI")]
        public GameObject dropdownPanel;           // Dropdown panel GameObject
        public CanvasGroup dropdownCanvasGroup;    // CanvasGroup for dropdown (optional, for fade transitions)
        public Button comboPreferenceButton;       // Button to toggle dropdown

        private bool _isInitialized = false;
        private bool _isOpen = false;

        /// <summary>
        /// Initialize combo dropdown panel
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Initialize dropdown panel (hidden by default)
            HidePanel();

            // Set up button listener
            if (comboPreferenceButton != null)
            {
                comboPreferenceButton.onClick.AddListener(ToggleDropdown);
            }
        }

        /// <summary>
        /// Unity Start method - auto-initialize if not already initialized
        /// </summary>
        void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Toggle dropdown panel visibility
        /// </summary>
        public void ToggleDropdown()
        {
            if (_isOpen)
                HidePanel();
            else
                ShowPanel();
        }

        /// <summary>
        /// Show dropdown panel
        /// </summary>
        public void ShowPanel()
        {
            if (dropdownPanel != null)
            {
                dropdownPanel.SetActive(true);
            }

            if (dropdownCanvasGroup != null)
            {
                dropdownCanvasGroup.alpha = 1f;
                dropdownCanvasGroup.interactable = true;
                dropdownCanvasGroup.blocksRaycasts = true;
            }

            _isOpen = true;
            Debug.Log("[ComboDropdownController] Dropdown panel opened");
        }

        /// <summary>
        /// Hide dropdown panel
        /// </summary>
        public void HidePanel()
        {
            if (dropdownCanvasGroup != null)
            {
                dropdownCanvasGroup.alpha = 0f;
                dropdownCanvasGroup.interactable = false;
                dropdownCanvasGroup.blocksRaycasts = false;
            }

            if (dropdownPanel != null)
            {
                dropdownPanel.SetActive(false);
            }

            _isOpen = false;
            Debug.Log("[ComboDropdownController] Dropdown panel closed");
        }

        void OnDestroy()
        {
            // Clean up button listeners
            if (comboPreferenceButton != null && comboPreferenceButton.onClick != null)
            {
                comboPreferenceButton.onClick.RemoveListener(ToggleDropdown);
            }
        }
    }
}
