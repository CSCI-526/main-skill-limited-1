using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 常规均匀分布 D6（faces 可扩展为特殊面）
    /// </summary>
    [System.Serializable]
    public class NormalDice : BaseDice
    {
        [Header("Faces (size=6)")]
        public int[] faces = new int[6] { 1, 2, 3, 4, 5, 6 };

        public NormalDice()
        {
            diceName = "Normal Dice";
            tier = DiceTier.Common; // Changed from Filler to Common so it's treated as a real dice
            cost = 0; // Free dice used to fill empty slots
            cooldownAfterUse = 0; // No cooldown for filler dice
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            int idx = Random.Range(0, faces.Length); // 上界不含
            lastRollValue = faces[idx];
            return lastRollValue;
        }
    }
}
