using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceGame
{
    /// <summary>
    /// 单个骰子的 UI 视图：显示点数、锁定按钮、锁定高亮
    /// </summary>
    public class DiceView : MonoBehaviour
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

        void OnToggleLock()
        {
            if (model == null) return;
            model.ToggleLock();
            
            string status = model.isLocked ? "LOCKED" : "UNLOCKED";
            Debug.Log($"[DiceView] {model.diceName} is now {status} (value: {model.lastRollValue})");
            
            Refresh();
        }
    }
}
