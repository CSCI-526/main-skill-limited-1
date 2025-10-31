using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DiceGame; // BaseDice, DiceTier 等

public static class DicePool
{
    /// 統一來源：不要在別處重建或複製清單
    public static List<BaseDice> GetAll()
    {
        return new List<BaseDice>
        {
            new BigOne(), new BigSix(), new CounterDice(), new EvenDice(), new OddDice(),
            new HeavyDice(), new LightDice(), new MirrorDice(),
            new CollectorDice(), new LuckySix(), new PlusOne(), new SevenSevenSeven(),
            new TwinBond(), new WeightedEdge(),
            new D8(), new GoldenDice(), new ZombieDice()
        };
    }

    public static List<BaseDice> GetNonFiller() =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler).ToList();

    public static List<BaseDice> GetByTier(DiceTier tier) =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler && d.tier == tier).ToList();
}