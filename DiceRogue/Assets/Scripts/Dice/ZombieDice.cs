using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Zombie (Legendary) - 20% chance to change neighbor dice values to match its own
    /// Infects both left and right neighbors if they exist
    /// Note: The infection effect should be handled by BattleController after rolling
    /// </summary>
    [System.Serializable]
    public class ZombieDice : BaseDice
    {
        private bool shouldInfect = false;

        public ZombieDice()
        {
            diceName = "Zombie";
            tier = DiceTier.Legendary;
            cost = 3;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // Roll normally
            lastRollValue = Random.Range(1, 7);

            // 20% chance to trigger infection
            float rand = Random.value;
            shouldInfect = (rand < 0.2f);

            return lastRollValue;
        }

        /// <summary>
        /// Check if the zombie should infect neighbors this roll
        /// </summary>
        public bool ShouldInfectNeighbors()
        {
            return shouldInfect;
        }

        /// <summary>
        /// Get the infection value (the zombie's current roll)
        /// </summary>
        public int GetInfectionValue()
        {
            return lastRollValue;
        }

        /// <summary>
        /// Apply infection to a neighbor dice
        /// </summary>
        public void InfectDice(BaseDice targetDice)
        {
            if (targetDice != null && !targetDice.isLocked)
            {
                targetDice.lastRollValue = lastRollValue;
            }
        }

        public override void ResetLockAndValue()
        {
            base.ResetLockAndValue();
            shouldInfect = false;
        }
    }
}

