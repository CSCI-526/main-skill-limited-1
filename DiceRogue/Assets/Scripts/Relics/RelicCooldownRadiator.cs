using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Cooldown Radiator: can include one cooling die (not enforced here); payback next hand cooldown
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Cooldown Radiator", fileName = "Relic_CooldownRadiator")]
    public class RelicCooldownRadiator : RelicBase
    {
        public int futureExtraCooldown = 1;

        private void Reset()
        {
            relicName = "Cooldown Radiator";
            rarity = RelicRarity.Rare;
            description = "Can select one cooling die. Next hand: +1 extra cooldown.";
        }

        public override void Apply(ScoringContext context)
        {
            // Only record the debt; actual selection rule handled by gameplay later
            context.nextHandExtraCooldown = Mathf.Max(context.nextHandExtraCooldown, futureExtraCooldown);
        }
    }
}

