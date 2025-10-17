using System.Collections.Generic;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Handles special dice effects that occur during rolling phase
    /// Responsible for: PlusOne, TwinBond, ZombieDice, GoldenDice
    /// </summary>
    public class DiceEffectHandler
    {
        /// <summary>
        /// Apply all special dice effects in the correct order
        /// </summary>
        public void ApplyRollEffects(List<BaseDice> dice)
        {
            HandlePlusOne(dice);
            HandleTwinBond(dice);
            HandleZombieInfection(dice);
            HandleGoldenDice(dice);
        }

        /// <summary>
        /// Handle PlusOne dice - needs previous dice value before rolling
        /// Call this BEFORE rolling the PlusOne dice
        /// </summary>
        public void SetupPlusOneDice(BaseDice dice, int index, List<BaseDice> allDice)
        {
            if (dice is PlusOne plusOne && index > 0)
            {
                var prevDice = allDice[index - 1];
                plusOne.SetPreviousDiceValue(prevDice.lastRollValue);
                Debug.Log($"  - {plusOne.diceName}: setting previous value = {prevDice.lastRollValue}");
            }
        }

        /// <summary>
        /// Handle PlusOne dice - sets up context from previous dice
        /// This is kept as a separate method but now only logs since setup happens during roll
        /// </summary>
        private void HandlePlusOne(List<BaseDice> dice)
        {
            // PlusOne setup happens during rolling phase
            // This method is kept for potential future expansion
        }

        /// <summary>
        /// Handle TwinBond dice - copy a random dice value
        /// </summary>
        private void HandleTwinBond(List<BaseDice> dice)
        {
            for (int i = 0; i < dice.Count; i++)
            {
                var d = dice[i];
                if (d is TwinBond twinBond && !d.isLocked && d.tier != DiceTier.Filler)
                {
                    // Find all other dice that are not locked and not filler
                    var otherDice = new List<BaseDice>();
                    for (int j = 0; j < dice.Count; j++)
                    {
                        if (j != i && dice[j].tier != DiceTier.Filler && dice[j].lastRollValue > 0)
                        {
                            otherDice.Add(dice[j]);
                        }
                    }

                    // Copy a random dice if any available
                    if (otherDice.Count > 0)
                    {
                        int randomIdx = Random.Range(0, otherDice.Count);
                        int copiedValue = otherDice[randomIdx].lastRollValue;
                        twinBond.CopyValue(copiedValue);
                        Debug.Log($"  - {twinBond.diceName} copied value {copiedValue} from {otherDice[randomIdx].diceName}");
                    }
                }
            }
        }

        /// <summary>
        /// Handle ZombieDice - infect neighbor dice
        /// </summary>
        private void HandleZombieInfection(List<BaseDice> dice)
        {
            for (int i = 0; i < dice.Count; i++)
            {
                var d = dice[i];
                if (d is ZombieDice zombie && !d.isLocked && d.tier != DiceTier.Filler)
                {
                    if (zombie.ShouldInfectNeighbors())
                    {
                        Debug.Log($"  - {zombie.diceName} is infecting neighbors with value {zombie.GetInfectionValue()}!");
                        
                        // Infect left neighbor
                        if (i > 0 && dice[i - 1].tier != DiceTier.Filler)
                        {
                            zombie.InfectDice(dice[i - 1]);
                            Debug.Log($"    - Infected {dice[i - 1].diceName} (left)");
                        }

                        // Infect right neighbor
                        if (i < dice.Count - 1 && dice[i + 1].tier != DiceTier.Filler)
                        {
                            zombie.InfectDice(dice[i + 1]);
                            Debug.Log($"    - Infected {dice[i + 1].diceName} (right)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handle GoldenDice - add +1 to all dice values
        /// </summary>
        private void HandleGoldenDice(List<BaseDice> dice)
        {
            // Check if GoldenDice is present in hand
            GoldenDice goldenDice = null;
            
            foreach (var d in dice)
            {
                if (d is GoldenDice golden && d.tier != DiceTier.Filler)
                {
                    goldenDice = golden;
                    break;
                }
            }

            // If GoldenDice is present, apply +1 to all dice (except itself)
            if (goldenDice != null)
            {
                Debug.Log($"  - {goldenDice.diceName} is adding +1 to all other dice!");
                
                foreach (var d in dice)
                {
                    if (d != goldenDice && d.tier != DiceTier.Filler && d.lastRollValue > 0)
                    {
                        int oldValue = d.lastRollValue;
                        d.lastRollValue = goldenDice.ApplyBonus(d.lastRollValue);
                        Debug.Log($"    - {d.diceName}: {oldValue} -> {d.lastRollValue}");
                    }
                }
            }
        }
    }
}

