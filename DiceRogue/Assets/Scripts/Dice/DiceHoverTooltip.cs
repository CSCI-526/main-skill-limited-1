using UnityEngine;
using UnityEngine.EventSystems;

namespace DiceGame.Core
{
    public class DiceHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BaseDice dice;

        // Delay display
        private float hoverDelay = 0.3f;
        private float hoverTimer = 0f;
        private bool isHovering = false;

        public void BindDice(BaseDice data)
        {
            dice = data;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            hoverTimer = 0f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            DiceTooltipManager.Instance.HideTooltip();
        }

        private void Update()
        {
            if (isHovering && dice != null)
            {
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= hoverDelay)
                {
                    DiceTooltipManager.Instance.ShowTooltip(dice);
                    hoverTimer = -999f; 
                }
            }
        }
    }
}