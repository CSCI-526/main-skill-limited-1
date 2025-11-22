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
        public void ApplyRollEffects(List<BaseDice> dice, List<DiceView> views)
        {
            HandlePlusOne(dice);
            HandleTwinBond(dice);
            HandleZombieInfection(dice);
            HandleGoldenDice(dice);
        }

        private System.Collections.IEnumerator PlaySequentialFX(int triggerIndex, List<int> targetIndices, Color color)
        {
            BattleController bc = GameObject.FindObjectOfType<BattleController>();
            if (bc == null || bc._views == null) yield break;

            // 1) trigger dice pop first
            if (triggerIndex >= 0 && triggerIndex < bc._views.Count)
            {
                var tr = bc._views[triggerIndex];
                tr.TriggerInfluencedEffect(color);
                tr.PopEffect(1.35f);
            }

            // 2) wait so player is sure "this dice caused the effect"
            yield return new WaitForSeconds(0.25f);

            // 3) all target dice pop together (simultaneous)
            foreach (int idx in targetIndices)
            {
                if (idx >= 0 && idx < bc._views.Count)
                {
                    var v = bc._views[idx];
                    v.TriggerInfluencedEffect(color);
                    v.PopEffect(1.25f);
                    // Refresh the view to show updated value (golden dice bonus)
                    v.Refresh();
                }
            }
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
            BattleController bc = GameObject.FindObjectOfType<BattleController>();
            if (bc == null) return;

            for (int i = 0; i < dice.Count; i++)
            {
                if (dice[i] is ZombieDice zombie && !dice[i].isLocked && dice[i].tier != DiceTier.Filler)
                {
                    if (!zombie.ShouldInfectNeighbors())
                        continue;

                    List<int> targets = new List<int>();

                    // left
                    if (i > 0 && dice[i - 1].tier != DiceTier.Filler && !dice[i - 1].isLocked)
                    {
                        zombie.InfectDice(dice[i - 1]);
                        targets.Add(i - 1);
                    }

                    // right
                    if (i < dice.Count - 1 && dice[i + 1].tier != DiceTier.Filler && !dice[i + 1].isLocked)
                    {
                        zombie.InfectDice(dice[i + 1]);
                        targets.Add(i + 1);
                    }

                    // animate: trigger first → delay → all targets
                    bc.StartCoroutine(PlaySequentialFX(
                        i,
                        targets,
                        new Color32(0, 255, 100, 255)
                    ));
                }
            }
        }



        /// <summary>
        /// Handle GoldenDice - add +1 to all dice values
        /// Note: Applies to all dice with rolled values, including locked dice
        /// </summary>
        private void HandleGoldenDice(List<BaseDice> dice)
        {
            BattleController bc = GameObject.FindObjectOfType<BattleController>();
            if (bc == null) return;

            int goldenIndex = -1;
            GoldenDice golden = null;

            for (int i = 0; i < dice.Count; i++)
            {
                if (dice[i] is GoldenDice g && dice[i].tier != DiceTier.Filler)
                {
                    golden = g;
                    goldenIndex = i;
                    break;
                }
            }

            if (golden == null) return;

            List<int> targets = new List<int>();

            // compute new values first - apply to ALL dice with rolled values (including locked dice)
            for (int i = 0; i < dice.Count; i++)
            {
                if (i == goldenIndex) continue;
                var d = dice[i];
                if (d.tier == DiceTier.Filler) continue;
                if (d.lastRollValue <= 0) continue;
                // Removed: if (d.isLocked) continue; - locked dice should also get +1 bonus

                int old = d.lastRollValue;
                d.lastRollValue = golden.ApplyBonus(old);
                Debug.Log($"    - {d.diceName}: {old} -> {d.lastRollValue} (locked: {d.isLocked})");
                targets.Add(i);
            }

            // play animation after computing ALL dice values
            bc.StartCoroutine(PlaySequentialFX(
                goldenIndex,
                targets,
                new Color32(255, 215, 0, 255)
            ));
        }

    }
}

