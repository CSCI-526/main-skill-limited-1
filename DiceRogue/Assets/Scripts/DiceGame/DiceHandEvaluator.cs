using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DiceGame.Core
{
    /// <summary>
    /// 支持丰富组合与趣味规则的骰子识别 + 计分系统
    /// </summary>
    public static class DiceHandEvaluator
    {
        /// <summary>
        /// 主入口：识别组合并计算分数
        /// </summary>
        public static string Evaluate(List<int> values, out int totalScore, out float comboMultiplierOut, float diceMultiplier = 1f)
        {
            totalScore = 0;
            comboMultiplierOut = 1f;
            if (values == null || values.Count == 0)
                return "Invalid";

            values.Sort();
            var counts = new Dictionary<int, int>();
            foreach (var v in values)
            {
                if (!counts.ContainsKey(v)) counts[v] = 0;
                counts[v]++;
            }

            var freq = counts.Values.OrderByDescending(x => x).ToList();
            bool isLargeStraight = CheckLargeStraight(values);
            bool isSmallStraight = CheckSmallStraight(values);
            bool allEven = values.All(v => v % 2 == 0);
            bool allOdd = values.All(v => v % 2 == 1);
            bool allLow = values.All(v => v <= 3);
            bool allHigh = values.All(v => v >= 4);
            int sum = values.Sum();

            string combo;
            int baseScore;
            float comboMultiplier;

            // ===== 核心组合判断（从高到低） =====
            if (freq.Count == 1 && freq[0] == 5)
            {
                combo = "Five of a Kind (Yahtzee)";
                baseScore = 180;
                comboMultiplier = 4.0f;
            }
            else if (freq[0] == 4)
            {
                combo = "Four of a Kind";
                baseScore = 120;
                comboMultiplier = 2.5f;
            }
            else if (freq[0] == 3 && freq.Contains(2))
            {
                combo = "Full House (3+2)";
                baseScore = 100;
                comboMultiplier = 2.0f;
            }
            else if (isLargeStraight)
            {
                combo = "Large Straight (1–5 or 2–6)";
                baseScore = 90;
                comboMultiplier = 1.8f;
            }
            else if (isSmallStraight)
            {
                combo = "Small Straight (any 4 in sequence)";
                baseScore = 75;
                comboMultiplier = 1.5f;
            }
            else if (sum == 21)  // Check Sum Jackpot before Three of a Kind for priority
            {
                combo = "Sum Jackpot (Total = 21)";
                baseScore = 70;
                comboMultiplier = 1.8f;
            }
            else if (freq[0] == 3)
            {
                combo = "Three of a Kind";
                baseScore = 60;
                comboMultiplier = 1.5f;
            }
            else if (freq.Count == 3 && freq[0] == 2 && freq[1] == 2)
            {
                combo = "Two Pair";
                baseScore = 45;
                comboMultiplier = 1.2f;
            }
            else if (freq[0] == 2)
            {
                combo = "One Pair";
                baseScore = 30;
                comboMultiplier = 1.0f;
            }
            else if (allEven)
            {
                combo = "All Even Numbers";
                baseScore = 35;
                comboMultiplier = 1.2f;
            }
            else if (allOdd)
            {
                combo = "All Odd Numbers";
                baseScore = 35;
                comboMultiplier = 1.2f;
            }
            else if (allLow)
            {
                combo = "Low Roll (All ≤3)";
                baseScore = 25;
                comboMultiplier = 1.0f;
            }
            else if (allHigh)
            {
                combo = "High Roll (All ≥4)";
                baseScore = 25;
                comboMultiplier = 1.0f;
            }
            else
            {
                combo = "No Combo (Bust)";
                baseScore = 10;
                comboMultiplier = 0.8f;
            }

            // 计算总分: (Base + Sum) × Combo Multiplier × Dice Multipliers
            comboMultiplierOut = comboMultiplier;
            totalScore = Mathf.RoundToInt((baseScore + sum) * comboMultiplier * diceMultiplier);

            // 输出调试信息
            Debug.Log($"[DiceHandEvaluator] Combo={combo}, Base={baseScore}, Sum={sum}, ComboMult=x{comboMultiplier}, DiceMult=x{diceMultiplier}, Total={totalScore}");

            return combo;
        }

        // ====== 检查顺子相关 ======
        private static bool CheckLargeStraight(List<int> sorted)
        {
            var uniq = sorted.Distinct().ToList();
            if (uniq.Count < 5) return false;
            return uniq.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 }) ||
                   uniq.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
        }

        private static bool CheckSmallStraight(List<int> sorted)
        {
            var uniq = sorted.Distinct().ToList();
            if (uniq.Count < 4) return false;
            for (int start = 1; start <= 3; start++)
            {
                if (uniq.Contains(start) && uniq.Contains(start + 1)
                    && uniq.Contains(start + 2) && uniq.Contains(start + 3))
                    return true;
            }
            return false;
        }

        // ====== 输出UI总结 ======
        public static string BuildSummary(List<int> values, string combo, int totalScore, float comboMultiplier, float diceMultiplier = 1f)
        {
            var sb = new StringBuilder();
            sb.AppendLine(" === RESULT SUMMARY ===");
            sb.AppendLine($"Dice: [{string.Join(", ", values)}]");
            sb.AppendLine($"Combo: {combo}");
            sb.AppendLine($"Combo Multiplier: x{comboMultiplier:F1}");
            if (diceMultiplier > 1f)
            {
                sb.AppendLine($"Dice Multiplier: x{diceMultiplier:F1}");
                sb.AppendLine($"Total Multiplier: x{(comboMultiplier * diceMultiplier):F2}");
            }
            sb.AppendLine($"Final Score: {totalScore}");
            sb.AppendLine("=========================");
            return sb.ToString();
        }
    }
}
