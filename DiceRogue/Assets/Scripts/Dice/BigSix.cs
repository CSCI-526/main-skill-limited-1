using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Lucky Six (Common) - 25% chance to roll 6
    /// </summary>
    [System.Serializable]
    public class BigSix : BaseDice
    {
        public BigSix()
        {
            diceName = "Big Six";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // 25% chance for 6, 75% for others (1-5)
            float rand = Random.value;
            if (rand < 0.25f)
            {
                lastRollValue = 6;
            }
            else
            {
                lastRollValue = Random.Range(1, 6); // 1-5 inclusive
            }

            return lastRollValue;
        }
    }
}

