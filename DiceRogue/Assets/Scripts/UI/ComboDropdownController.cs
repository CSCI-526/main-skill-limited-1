using UnityEngine;
using UnityEngine.UI;

public class ComboDropdownController : MonoBehaviour
{
    [Header("References")]
    public Button comboPreferenceButton;   // 按钮
    public CanvasGroup dropdownPanel;      // 下拉菜单面板

    private bool isOpen = false;

    void Start()
    {
        // 确保一开始隐藏
        HidePanel();

        // 注册点击事件
        comboPreferenceButton.onClick.AddListener(ToggleDropdown);
    }

    public void ToggleDropdown()
    {
        if (isOpen)
            HidePanel();
        else
            ShowPanel();
    }

    private void ShowPanel()
    {
        dropdownPanel.alpha = 1;
        dropdownPanel.interactable = true;
        dropdownPanel.blocksRaycasts = true;
        isOpen = true;
    }

    private void HidePanel()
    {
        dropdownPanel.alpha = 0;
        dropdownPanel.interactable = false;
        dropdownPanel.blocksRaycasts = false;
        isOpen = false;
    }
}
