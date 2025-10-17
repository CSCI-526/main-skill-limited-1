using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Plus One (Rare) - 70% chance to roll last dice number + 1
    /// Note: Requires access to the previous dice's value
    /// </summary>
    [System.Serializable]
    public class PlusOne : BaseDice
    {
        private int previousDiceValue = 0;

        public PlusOne()
        {
            diceName = "Plus One";
            tier = DiceTier.Rare;
            cost = 2;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            float rand = Random.value;
            if (rand < 0.7f && previousDiceValue > 0)
            {
                // 70% chance: previous + 1, wrap around if needed
                lastRollValue = previousDiceValue + 1;
                if (lastRollValue > 6) lastRollValue = 1;
            }
            else
            {
                // 30% chance: normal roll
                lastRollValue = Random.Range(1, 7);
            }

            return lastRollValue;
        }

        /// <summary>
        /// Set the previous dice value before rolling
        /// </summary>
        public void SetPreviousDiceValue(int value)
        {
            previousDiceValue = value;
        }
    }
}

