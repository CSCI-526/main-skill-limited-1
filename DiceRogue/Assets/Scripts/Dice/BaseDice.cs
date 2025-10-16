using UnityEngine;

namespace DiceGame
{
    public enum DiceTier { Filler = 0, Common = 1, Rare = 2, Legendary = 3 }

    /// <summary>
    /// 可继承的骰子基类：提供公共字段与可覆写的 Roll/锁定逻辑
    /// </summary>
    [System.Serializable]
    public abstract class BaseDice
    {
        [Header("Static Data")]
        public string diceName = "Base Dice";
        public DiceTier tier = DiceTier.Common;
        /// <summary>本手牌预算消耗（GDD：Common=1, Rare=2, Legendary=3, Filler=0）</summary>
        public int cost = 1;
        /// <summary>结算后进入的冷却回合数（GDD：1）</summary>
        public int cooldownAfterUse = 1;

        [Header("Runtime State")]
        public bool isLocked = false;   // 锁定后本次 roll 不会改变
        public int lastRollValue = 0;   // 最近一次点数
        public int cooldownRemain = 0;  // 剩余冷却（供后续同学接入）

        /// <summary>
        /// 掷骰：默认行为交由子类实现（需要返回 1..6）
        /// </summary>
        public abstract int Roll();

        public virtual void ToggleLock() => isLocked = !isLocked;

        public virtual void ResetLockAndValue()
        {
            isLocked = false;
            lastRollValue = 0;
        }
    }
}
