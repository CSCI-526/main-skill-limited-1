using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 加权掷骰：给每一面一个权重，按权重随机
    /// 例：weights = [1,1,1,1,1,3] ≈ “6 的概率约 30%”
    /// </summary>
    [System.Serializable]
    public class WeightedDice : BaseDice
    {
        [Header("Weighted Faces")]
        public int[] faces = new int[6] { 1, 2, 3, 4, 5, 6 };

        [Tooltip("与 faces 等长；不必归一化，会在 Roll 时求和")]
        public float[] weights = new float[6] { 1, 1, 1, 1, 1, 1 };

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // 计算总权重
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += Mathf.Max(0f, weights[i]);
            if (sum <= 0f) sum = 1f;

            // 在 [0, sum) 上采样
            float r = Random.Range(0f, sum);
            float acc = 0f;
            for (int i = 0; i < faces.Length; i++)
            {
                acc += Mathf.Max(0f, weights[i]);
                if (r <= acc)
                {
                    lastRollValue = faces[i];
                    return lastRollValue;
                }
            }

            // 理论上不会到达：兜底返回最后一面
            lastRollValue = faces[faces.Length - 1];
            return lastRollValue;
        }
    }
}
