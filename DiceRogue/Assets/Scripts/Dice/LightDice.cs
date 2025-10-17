using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Light Dice - 70% chance to roll 1,2,3
    /// </summary>
    [System.Serializable]
    public class LightDice : BaseDice
    {
        public LightDice()
        {
            diceName = "Light Dice";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // 70% chance for low numbers (1,2,3), 30% for high (4,5,6)
            float rand = Random.value;
            if (rand < 0.7f)
            {
                // Low roll: 1, 2, or 3
                lastRollValue = Random.Range(1, 4); // 1-3 inclusive
            }
            else
            {
                // High roll: 4, 5, or 6
                lastRollValue = Random.Range(4, 7); // 4-6 inclusive
            }

            return lastRollValue;
        }
    }
}

