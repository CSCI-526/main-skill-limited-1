using UnityEngine;
using UnityEngine.EventSystems;
using DiceGame.Relics;
using DiceGame.Core;

namespace DiceGame.UI
{
    /// <summary>
    /// Individual relic icon component - handles hover events to show tooltip
    /// Uses the unified DiceTooltipManager for consistent visual style
    /// </summary>
    public class RelicIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [HideInInspector]
        public RelicBase relic;

        /// <summary>
        /// Show tooltip when mouse enters
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (relic != null && DiceTooltipManager.Instance != null)
            {
                DiceTooltipManager.Instance.ShowTooltip(relic);
            }
        }

        /// <summary>
        /// Hide tooltip when mouse exits
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (DiceTooltipManager.Instance != null)
            {
                DiceTooltipManager.Instance.HideTooltip();
            }
        }
    }
}

