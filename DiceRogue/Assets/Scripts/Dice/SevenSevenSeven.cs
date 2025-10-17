using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 777 (Rare) - If selected on submit and is three of a kind, x2
    /// Note: The multiplier effect should be handled in the scoring/damage calculation
    /// </summary>
    [System.Serializable]
    public class SevenSevenSeven : BaseDice
    {
        public SevenSevenSeven()
        {
            diceName = "777";
            tier = DiceTier.Rare;
            cost = 2;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            lastRollValue = Random.Range(1, 7);
            return lastRollValue;
        }

        /// <summary>
        /// Check if this dice is part of a three-of-a-kind
        /// Should be called during scoring to determine the 2x multiplier
        /// </summary>
        public bool IsPartOfThreeOfAKind(int[] allDiceValues)
        {
            int count = 0;
            foreach (int value in allDiceValues)
            {
                if (value == lastRollValue) count++;
            }
            return count >= 3;
        }

        /// <summary>
        /// Get the multiplier for this dice (2x if part of three-of-a-kind)
        /// </summary>
        public float GetMultiplier(bool isThreeOfAKind)
        {
            return isThreeOfAKind ? 2.0f : 1.0f;
        }
    }
}

