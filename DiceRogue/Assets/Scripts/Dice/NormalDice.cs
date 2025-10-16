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

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            int idx = Random.Range(0, faces.Length); // 上界不含
            lastRollValue = faces[idx];
            return lastRollValue;
        }
    }
}
