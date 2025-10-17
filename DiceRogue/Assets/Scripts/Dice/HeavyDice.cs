using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Heavy Dice - 70% chance to roll 4,5,6
    /// </summary>
    [System.Serializable]
    public class HeavyDice : BaseDice
    {
        public HeavyDice()
        {
            diceName = "Heavy Dice";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // 70% chance for high numbers (4,5,6), 30% for low (1,2,3)
            float rand = Random.value;
            if (rand < 0.7f)
            {
                // High roll: 4, 5, or 6
                lastRollValue = Random.Range(4, 7); // 4-6 inclusive
            }
            else
            {
                // Low roll: 1, 2, or 3
                lastRollValue = Random.Range(1, 4); // 1-3 inclusive
            }

            return lastRollValue;
        }
    }
}

