using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame.Relics
{
    internal static class RelicUtils
    {
        public static bool IsLargeStraight(IReadOnlyList<int> values)
        {
            var uniq = values.Distinct().OrderBy(x => x).ToList();
            if (uniq.Count < 5) return false;
            return SequenceEquals(uniq, 1, 2, 3, 4, 5) || SequenceEquals(uniq, 2, 3, 4, 5, 6);
        }

        public static bool IsSmallStraight(IReadOnlyList<int> values)
        {
            var uniq = values.Distinct().OrderBy(x => x).ToList();
            if (uniq.Count < 4) return false;
            for (int start = 1; start <= 3; start++)
            {
                if (uniq.Contains(start) && uniq.Contains(start + 1) && uniq.Contains(start + 2) && uniq.Contains(start + 3))
                    return true;
            }
            return false;
        }

        public static (int value, int count) MostFrequent(IReadOnlyList<int> values)
        {
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < values.Count; i++)
            {
                int v = values[i];
                counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
            }
            int bestV = 0, bestC = 0;
            foreach (var kv in counts)
            {
                if (kv.Value > bestC) { bestC = kv.Value; bestV = kv.Key; }
            }
            return (bestV, bestC);
        }

        public static bool HasTwoPair(IReadOnlyList<int> values)
        {
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < values.Count; i++)
            {
                int v = values[i];
                counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
            }
            int pairs = 0;
            foreach (var c in counts.Values) if (c >= 2) pairs++;
            return pairs >= 2;
        }

        private static bool SequenceEquals(List<int> list, params int[] seq)
        {
            if (list.Count != seq.Length) return false;
            for (int i = 0; i < seq.Length; i++) if (list[i] != seq[i]) return false;
            return true;
        }
    }
}


