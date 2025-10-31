using System.Collections.Generic;
using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Holds equipped relics and applies them to a ScoringContext.
    /// Does not enforce slot limits or multiplier caps per user request.
    /// </summary>
    public class RelicManager
    {
        private readonly List<RelicBase> _equipped = new();

        public IReadOnlyList<RelicBase> Equipped => _equipped;

        public bool AddRelic(RelicBase relic)
        {
            if (relic == null) return false;
            if (relic.unique)
            {
                foreach (var r in _equipped)
                {
                    if (r != null && r.relicName == relic.relicName) return false;
                }
            }
            _equipped.Add(relic);
            return true;
        }

        public bool RemoveRelic(RelicBase relic)
        {
            return _equipped.Remove(relic);
        }

        public void Clear()
        {
            _equipped.Clear();
        }

        public void ApplyAll(ScoringContext context)
        {
            if (context == null) return;
            for (int i = 0; i < _equipped.Count; i++)
            {
                var relic = _equipped[i];
                if (relic == null) continue;
                relic.Apply(context);
            }
        }
    }
}


