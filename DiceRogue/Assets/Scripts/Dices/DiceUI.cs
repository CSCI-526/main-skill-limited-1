using UnityEngine;
using UnityEngine.EventSystems;

namespace DiceGame.Core
{
    /// <summary>
    /// 每个骰子的UI逻辑：负责响应鼠标事件并显示Tooltip
    /// </summary>
    public class DiceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public BaseDice boundDice;  // 绑定的骰子逻辑对象

        private void Awake()
        {
            string prefabName = transform.parent != null
                ? transform.parent.name.Replace("(Clone)", "").Trim()
                : gameObject.name.Replace("(Clone)", "").Trim();

            boundDice = DiceFactory.CreateDiceByName(prefabName);

            if (boundDice == null)
                Debug.LogWarning($"[DiceUI] 无法为 {prefabName} 创建骰子实例！");
            else
                Debug.Log($"[DiceUI] {prefabName} 成功绑定 {boundDice.GetType().Name}, diceName={boundDice.diceName}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundDice == null)
            {
                Debug.LogWarning($"[Hover] {gameObject.name} 没有绑定 BaseDice。");
                return;
            }
            DiceTooltipManager.Instance.ShowTooltip(boundDice);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DiceTooltipManager.Instance.HideTooltip();
        }
    }

    // 工厂类：根据名字创建骰子
    public static class DiceFactory
    {
        public static BaseDice CreateDiceByName(string name)
        {
            switch (name)
            {
                case "BigOne":
                    return new BigOne();
                case "BigSix":
                    return new BigSix();
                case "CounterDice":
                    return new CounterDice();
                case "EvenDice":
                    return new EvenDice();
                case "OddDice":
                    return new OddDice();

                case "HeavyDice":
                    return new HeavyDice();
                case "LightDice":
                    return new LightDice();
                case "MirrorDice":
                    return new MirrorDice();

                case "CollectorDice":
                    return new CollectorDice();
                case "LuckySix":
                    return new LuckySix();
                case "PlusOne":
                    return new PlusOne();
                case "SevenSevenSeven":
                    return new SevenSevenSeven();

                case "TwinBond":
                    return new TwinBond();
                case "WeightedEdge":
                    return new WeightedEdge();
                case "D8":
                    return new D8();

                case "GoldenDice":
                    return new GoldenDice();
                case "ZombieDice":
                    return new ZombieDice();

                default:
                    Debug.LogWarning($"[DiceFactory] 未找到骰子：{name}，返回 NormalDice");
                    return new NormalDice();
            }
        }
    }
}
