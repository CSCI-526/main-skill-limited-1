using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Calculates damage/score multipliers from special dice effects
    /// Responsible for: CollectorDice, D8, LuckySix, SevenSevenSeven
    /// </summary>
    public class DiceMultiplierCalculator
    {
        private StringBuilder _lastBreakdown;

        /// <summary>
        /// Calculate total multiplier from all special dice in the submitted hand
        /// </summary>
        public float Calculate(List<BaseDice> submittedDice, List<int> submittedValues)
        {
            float totalMultiplier = 1f;
            _lastBreakdown = new StringBuilder();
            _lastBreakdown.AppendLine("Multiplier Breakdown:");

            foreach (var dice in submittedDice)
            {
                float diceMultiplier = GetDiceMultiplier(dice, submittedValues);
                
                if (diceMultiplier > 1f)
                {
                    string reason = GetMultiplierReason(dice, submittedValues);
                    _lastBreakdown.AppendLine($"  {dice.diceName}: x{diceMultiplier} ({reason})");
                }

                totalMultiplier *= diceMultiplier;
            }

            if (totalMultiplier > 1f)
            {
                _lastBreakdown.AppendLine($"Total Multiplier: x{totalMultiplier:F2}");
                Debug.Log(_lastBreakdown.ToString());
            }

            return totalMultiplier;
        }

        /// <summary>
        /// Get the multiplier breakdown as a formatted string (for UI display)
        /// </summary>
        public string GetBreakdownText()
        {
            return _lastBreakdown?.ToString() ?? "";
        }

        /// <summary>
        /// Get multiplier for a specific dice type
        /// </summary>
        private float GetDiceMultiplier(BaseDice dice, List<int> submittedValues)
        {
            // CollectorDice - x1.5 if matches previous roll
            if (dice is CollectorDice collector)
            {
                return collector.GetMultiplier();
            }
            // D8 - x5 for 7, x10 for 8
            else if (dice is D8 d8)
            {
                return d8.GetMultiplier();
            }
            // LuckySix - x1.5 if rolled a 6
            else if (dice is LuckySix luckySix)
            {
                return luckySix.GetMultiplier();
            }
            // SevenSevenSeven - x2 if part of three-of-a-kind
            else if (dice is SevenSevenSeven sevenSevenSeven)
            {
                bool isThreeOfAKind = sevenSevenSeven.IsPartOfThreeOfAKind(submittedValues.ToArray());
                return sevenSevenSeven.GetMultiplier(isThreeOfAKind);
            }

            return 1f;
        }

        /// <summary>
        /// Get human-readable reason for the multiplier
        /// </summary>
        private string GetMultiplierReason(BaseDice dice, List<int> submittedValues)
        {
            if (dice is CollectorDice)
            {
                return "matched previous roll";
            }
            else if (dice is D8)
            {
                return $"rolled {dice.lastRollValue}";
            }
            else if (dice is LuckySix)
            {
                return "rolled 6";
            }
            else if (dice is SevenSevenSeven)
            {
                return "three-of-a-kind";
            }

            return "unknown";
        }
    }
}

