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
    }
}
