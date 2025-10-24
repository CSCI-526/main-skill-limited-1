using System.Collections.Generic;
using UnityEngine;
using DiceGame; // BaseDice

namespace DiceGame.Relics
{
    /// <summary>
    /// A minimal, decoupled context passed to relics during score calculation.
    /// Not wired to BattleController yet. Caller is responsible for populating it.
    /// </summary>
    public class ScoringContext
    {
        // Inputs
        public List<int> submittedValues = new();
        public List<BaseDice> submittedDice = new();
        public int handBudget = 6;
        public int totalSelectedCost = 0;
        public int rollsUsed = 0;
        public int maxRollsPerHand = 3;
        public bool hasFillerInHand = false;

        // Intermediate state (relics modify these)
        public int additionalBase = 0;      // adds to (Base + Sum)
        public float multiplier = 1f;       // multiplies final result
        public int bonusRerolls = 0;        // UI-only for now
        public int nextHandBudgetDelta = 0; // planning hook
        public int nextHandExtraCooldown = 0; // planning hook

        // Helper queries
        public int Sum => ComputeSum();

        private int ComputeSum()
        {
            int s = 0;
            for (int i = 0; i < submittedValues.Count; i++) s += submittedValues[i];
            return s;
        }

        public int CountValue(int value)
        {
            int c = 0;
            for (int i = 0; i < submittedValues.Count; i++) if (submittedValues[i] == value) c++;
            return c;
        }

        public bool HasAnyPair()
        {
            var seen = new HashSet<int>();
            var dup = new HashSet<int>();
            foreach (var v in submittedValues)
            {
                if (!seen.Add(v)) dup.Add(v);
            }
            return dup.Count > 0;
        }

        public (int value, int count) MostFrequent()
        {
            var counts = new Dictionary<int, int>();
            foreach (var v in submittedValues)
            {
                counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
            }
            int bestV = 0, bestC = 0;
            foreach (var kv in counts)
            {
                if (kv.Value > bestC) { bestC = kv.Value; bestV = kv.Key; }
            }
            return (bestV, bestC);
        }
    }
}


