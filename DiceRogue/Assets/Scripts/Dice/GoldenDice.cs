using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Golden Dice (Legendary) - All dice num +1
    /// Note: The +1 effect to all dice should be handled by BattleController
    /// This dice itself rolls normally
    /// </summary>
    [System.Serializable]
    public class GoldenDice : BaseDice
    {
        public GoldenDice()
        {
            diceName = "Golden Dice";
            tier = DiceTier.Legendary;
            cost = 3;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            lastRollValue = Random.Range(1, 7);
            return lastRollValue;
        }

        /// <summary>
        /// Apply the +1 bonus to a dice value
        /// </summary>
        public int ApplyBonus(int diceValue)
        {
            int newValue = diceValue + 1;
            // Handle wrap-around or cap at 6 depending on game design
            // Assuming we cap at 6 for now
            return Mathf.Min(newValue, 6);
        }
    }
}

