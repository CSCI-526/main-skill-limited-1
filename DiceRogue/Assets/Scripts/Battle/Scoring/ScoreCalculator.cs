using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DiceGame;
using DiceGame.Relics;

namespace DiceGame.Core
{
    /// <summary>
    /// Centralized score calculation system.
    /// Formula: (ComboBase + DiceSum + RelicBase) × ComboMult × DiceMult1 × DiceMult2 × ... × RelicMult1 × RelicMult2 × ...
    /// 
    /// Calculation sequence:
    /// 1. Evaluate combo → get combo base score and combo multiplier
    /// 2. Calculate dice multipliers (CollectorDice, D8, LuckySix, etc.)
    /// 3. Apply relic effects → get additional base and relic multipliers
    /// 4. Final score = (ComboBase + DiceSum + RelicBase) × ComboMult × AllDiceMults × AllRelicMults
    /// </summary>
    public class ScoreCalculator
    {
        /// <summary>
        /// Individual score contribution step for animation
        /// </summary>
        public class ScoreStep
        {
            public string source;           // Name of dice/relic/system
            public int amount;              // Amount of bonus (for additions)
            public float multiplier;        // Multiplier value (for multiplications)
            public bool isMultiplier;       // true = multiplier, false = addition
            public object sourceObject;     // Reference to actual dice/relic object
            public string description;      // Human-readable description
            public int order;               // Order for sorting (lower = earlier)
        }

        /// <summary>
        /// Complete score calculation result with detailed breakdown
        /// </summary>
        public class ScoreResult
        {
            // Combo information
            public string comboName;
            public int comboBaseScore;
            public float comboMultiplier;
            
            // Dice information
            public int diceSum;
            public float totalDiceMultiplier;
            
            // Relic information
            public int relicAdditionalBase;
            public float relicMultiplier;
            
            // Final calculation
            public int totalBase;  // comboBase + diceSum + relicBase
            public float totalMultiplier;  // comboMult × diceMult × relicMult
            public int finalScore;
            
            // NEW: Step-by-step breakdown for animation
            public List<ScoreStep> steps = new List<ScoreStep>();
        }

        private readonly DiceMultiplierCalculator _diceMultiplierCalculator;

        public ScoreCalculator()
        {
            _diceMultiplierCalculator = new DiceMultiplierCalculator();
        }

        /// <summary>
        /// Calculate final score with complete breakdown.
        /// This is the ONLY method that should calculate scores.
        /// </summary>
        /// <param name="submittedDice">List of submitted dice</param>
        /// <param name="submittedValues">List of submitted dice values</param>
        /// <param name="relicManager">Relic manager to apply relic effects (can be null)</param>
        /// <param name="scoringContext">Context for relic calculation (hand budget, rolls used, etc.)</param>
        /// <returns>Complete score result with breakdown</returns>
        public ScoreResult CalculateScore(
            List<BaseDice> submittedDice, 
            List<int> submittedValues,
            RelicManager relicManager = null,
            ScoringContext scoringContext = null)
        {
            var result = new ScoreResult();

            // Validate input
            if (submittedValues == null || submittedValues.Count == 0)
            {
                result.comboName = "Invalid";
                result.finalScore = 0;
                return result;
            }

            // Step 1: Evaluate combo (base score and multiplier)
            result.comboName = EvaluateCombo(submittedValues, out result.comboBaseScore, out result.comboMultiplier);
            
            // Step 2: Calculate dice sum
            result.diceSum = submittedValues.Sum();
            
            // Do NOT add dice sum as a step here because the animator
            // handles +diceSum explicitly as the first addition step.
            
            // Step 3: Calculate individual dice multipliers (from special dice)
            // Order = 200 (dice multipliers come after combo multiplier)
            AddIndividualDiceMultiplierSteps(result, submittedDice, submittedValues);
            result.totalDiceMultiplier = _diceMultiplierCalculator.Calculate(submittedDice, submittedValues);
            
            // Step 4: Apply relic effects with individual tracking
            if (relicManager != null && scoringContext != null)
            {
                // Ensure context has the submitted values and dice
                scoringContext.submittedValues = new List<int>(submittedValues);
                scoringContext.submittedDice = new List<BaseDice>(submittedDice);
                
                // Apply all relics and track individual contributions
                var relics = relicManager.Equipped;
                foreach (var relic in relics)
                {
                    int beforeBase = scoringContext.additionalBase;
                    float beforeMult = scoringContext.multiplier;
                    
                    relic.Apply(scoringContext);
                    
                    int baseBonus = scoringContext.additionalBase - beforeBase;
                    float multBonus = scoringContext.multiplier / beforeMult;
                    
                    // Add step for base bonus (order = 0, additions come first)
                    if (baseBonus != 0)
                    {
                        result.steps.Add(new ScoreStep
                        {
                            source = relic.relicName,
                            amount = baseBonus,
                            isMultiplier = false,
                            sourceObject = relic,
                            description = relic.description,
                            order = 0 // Additions always first
                        });
                    }
                    
                    // Add step for multiplier bonus (order = 300, relic multipliers come last)
                    if (multBonus != 1f)
                    {
                        result.steps.Add(new ScoreStep
                        {
                            source = relic.relicName,
                            multiplier = multBonus,
                            isMultiplier = true,
                            sourceObject = relic,
                            description = relic.description,
                            order = 300 // Relic multipliers last
                        });
                    }
                }
                
                result.relicAdditionalBase = scoringContext.additionalBase;
                result.relicMultiplier = scoringContext.multiplier;
            }
            else
            {
                result.relicAdditionalBase = 0;
                result.relicMultiplier = 1f;
            }
            
            // Add combo multiplier (order = 100, combo multiplier comes first among multipliers)
            if (result.comboMultiplier != 1f)
            {
                result.steps.Add(new ScoreStep
                {
                    source = "Combo",
                    multiplier = result.comboMultiplier,
                    isMultiplier = true,
                    sourceObject = null,
                    description = result.comboName,
                    order = 100 // Combo multiplier first
                });
            }
            
            // SORT STEPS: All additions first, then multipliers in order (Combo → Dice → Relics)
            // Order values: additions=0, combo=100, dice=200, relics=300
            result.steps = result.steps
                .OrderBy(step => step.isMultiplier ? 1 : 0) // Additions (0) before multipliers (1)
                .ThenBy(step => step.order) // Within each group, sort by order
                .ToList();
            
            // Step 5: Calculate reference final score (for comparison/debugging)
            // NOTE: The AUTHORITATIVE score is calculated by ScoreAnimator step-by-step during animation
            // This finalScore is kept for reference/validation purposes only
            // Formula: (ComboBase + DiceSum + RelicBase) × ComboMult × DiceMult × RelicMult
            result.totalBase = result.comboBaseScore + result.diceSum + result.relicAdditionalBase;
            result.totalMultiplier = result.comboMultiplier * result.totalDiceMultiplier * result.relicMultiplier;
            result.finalScore = Mathf.RoundToInt(result.totalBase * result.totalMultiplier);
            
            // Log detailed breakdown
            LogScoreBreakdown(result);
            
            return result;
        }
        
        /// <summary>
        /// Add individual dice multiplier steps for animation
        /// </summary>
        private void AddIndividualDiceMultiplierSteps(ScoreResult result, List<BaseDice> submittedDice, List<int> submittedValues)
        {
            foreach (var dice in submittedDice)
            {
                float multiplier = GetIndividualDiceMultiplier(dice, submittedValues);
                
                if (multiplier > 1f)
                {
                    result.steps.Add(new ScoreStep
                    {
                        source = dice.diceName,
                        multiplier = multiplier,
                        isMultiplier = true,
                        sourceObject = dice,
                        description = GetMultiplierDescription(dice, submittedValues),
                        order = 200 // Dice multipliers come after combo, before relics
                    });
                }
            }
        }
        
        /// <summary>
        /// Get individual dice multiplier (extracted from DiceMultiplierCalculator logic)
        /// </summary>
        private float GetIndividualDiceMultiplier(BaseDice dice, List<int> submittedValues)
        {
            if (dice is CollectorDice collector)
                return collector.GetMultiplier();
            else if (dice is D8 d8)
                return d8.GetMultiplier();
            else if (dice is LuckySix luckySix)
                return luckySix.GetMultiplier();
            else if (dice is SevenSevenSeven sevenSevenSeven)
            {
                bool isThreeOfAKind = sevenSevenSeven.IsPartOfThreeOfAKind(submittedValues.ToArray());
                return sevenSevenSeven.GetMultiplier(isThreeOfAKind);
            }
            
            return 1f;
        }
        
        /// <summary>
        /// Get multiplier description for UI
        /// </summary>
        private string GetMultiplierDescription(BaseDice dice, List<int> submittedValues)
        {
            if (dice is CollectorDice)
                return "Matched previous roll";
            else if (dice is D8)
                return $"Rolled {dice.lastRollValue}";
            else if (dice is LuckySix)
                return "Rolled 6";
            else if (dice is SevenSevenSeven)
                return "Three-of-a-kind";
            
            return dice.diceName;
        }

        /// <summary>
        /// Evaluate combo and return combo name, base score, and combo multiplier.
        /// This is extracted from DiceHandEvaluator to avoid premature score calculation.
        /// </summary>
        private string EvaluateCombo(List<int> values, out int baseScore, out float comboMultiplier)
        {
            // Sort for analysis
            var sorted = new List<int>(values);
            sorted.Sort();
            
            // Build frequency map
            var counts = new Dictionary<int, int>();
            foreach (var v in sorted)
            {
                if (!counts.ContainsKey(v)) counts[v] = 0;
                counts[v]++;
            }
            
            var freq = counts.Values.OrderByDescending(x => x).ToList();
            bool isLargeStraight = CheckLargeStraight(sorted);
            bool isSmallStraight = CheckSmallStraight(sorted);
            bool allEven = sorted.All(v => v % 2 == 0);
            bool allOdd = sorted.All(v => v % 2 == 1);
            bool allLow = sorted.All(v => v <= 3);
            bool allHigh = sorted.All(v => v >= 4);
            int sum = sorted.Sum();

            // Combo evaluation (from highest to lowest priority)
            if (freq.Count == 1 && freq[0] == 5)
            {
                baseScore = 180;
                comboMultiplier = 4.0f;
                return "Five of a Kind (Yahtzee)";
            }
            else if (freq[0] == 4)
            {
                baseScore = 120;
                comboMultiplier = 2.5f;
                return "Four of a Kind";
            }
            else if (freq[0] == 3 && freq.Contains(2))
            {
                baseScore = 100;
                comboMultiplier = 2.0f;
                return "Full House (3+2)";
            }
            else if (isLargeStraight)
            {
                baseScore = 90;
                comboMultiplier = 1.8f;
                return "Large Straight (1–5 or 2–6)";
            }
            else if (isSmallStraight)
            {
                baseScore = 75;
                comboMultiplier = 1.5f;
                return "Small Straight (any 4 in sequence)";
            }
            else if (sum == 21)  // Sum Jackpot has priority over Three of a Kind
            {
                baseScore = 70;
                comboMultiplier = 1.8f;
                return "Sum Jackpot (Total = 21)";
            }
            else if (freq[0] == 3)
            {
                baseScore = 60;
                comboMultiplier = 1.5f;
                return "Three of a Kind";
            }
            else if (freq.Count >= 2 && freq[0] == 2 && freq[1] == 2)
            {
                baseScore = 45;
                comboMultiplier = 1.2f;
                return "Two Pair";
            }
            else if (freq[0] == 2)
            {
                baseScore = 30;
                comboMultiplier = 1.0f;
                return "One Pair";
            }
            else if (allEven)
            {
                baseScore = 35;
                comboMultiplier = 1.2f;
                return "All Even Numbers";
            }
            else if (allOdd)
            {
                baseScore = 35;
                comboMultiplier = 1.2f;
                return "All Odd Numbers";
            }
            else if (allLow)
            {
                baseScore = 25;
                comboMultiplier = 1.0f;
                return "Low Roll (All ≤3)";
            }
            else if (allHigh)
            {
                baseScore = 25;
                comboMultiplier = 1.0f;
                return "High Roll (All ≥4)";
            }
            else
            {
                baseScore = 10;
                comboMultiplier = 0.8f;
                return "No Combo (Bust)";
            }
        }

        /// <summary>
        /// Check if values form a large straight (1-5 or 2-6)
        /// </summary>
        private bool CheckLargeStraight(List<int> sorted)
        {
            var uniq = sorted.Distinct().ToList();
            if (uniq.Count < 5) return false;
            return uniq.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 }) ||
                   uniq.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
        }

        /// <summary>
        /// Check if values form a small straight (any 4 consecutive)
        /// </summary>
        private bool CheckSmallStraight(List<int> sorted)
        {
            var uniq = sorted.Distinct().ToList();
            if (uniq.Count < 4) return false;
            for (int start = 1; start <= 3; start++)
            {
                if (uniq.Contains(start) && uniq.Contains(start + 1)
                    && uniq.Contains(start + 2) && uniq.Contains(start + 3))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Log detailed score breakdown for debugging
        /// </summary>
        private void LogScoreBreakdown(ScoreResult result)
        {
            Debug.Log("==================== SCORE BREAKDOWN (REFERENCE) ====================");
            Debug.Log("NOTE: Authoritative score is calculated by ScoreAnimator during animation");
            Debug.Log($"Combo: {result.comboName}");
            Debug.Log($"  Combo Base Score: {result.comboBaseScore}");
            Debug.Log($"  Dice Sum: {result.diceSum}");
            
            if (result.relicAdditionalBase != 0)
            {
                Debug.Log($"  Relic Additional Base: +{result.relicAdditionalBase}");
            }
            
            Debug.Log($"  → Total Base: {result.totalBase}");
            Debug.Log("");
            Debug.Log($"Multipliers:");
            Debug.Log($"  Combo Multiplier: ×{result.comboMultiplier:F2}");
            
            if (result.totalDiceMultiplier > 1f)
            {
                Debug.Log($"  Dice Multiplier: ×{result.totalDiceMultiplier:F2}");
            }
            
            if (result.relicMultiplier != 1f)
            {
                Debug.Log($"  Relic Multiplier: ×{result.relicMultiplier:F2}");
            }
            
            Debug.Log($"  → Total Multiplier: ×{result.totalMultiplier:F2}");
            Debug.Log("");
            Debug.Log($"REFERENCE FINAL SCORE: {result.totalBase} × {result.totalMultiplier:F2} = {result.finalScore}");
            Debug.Log("(Actual score may differ due to step-by-step rounding in animation)");
            Debug.Log("=====================================================================");
        }

        /// <summary>
        /// Get human-readable score breakdown text for UI
        /// </summary>
        public string GetBreakdownText(ScoreResult result)
        {
            var text = $"<b>{result.comboName}</b>\n\n";
            text += $"Base: {result.comboBaseScore}\n";
            text += $"Sum: {result.diceSum}\n";
            
            if (result.relicAdditionalBase != 0)
            {
                text += $"Relic Bonus: +{result.relicAdditionalBase}\n";
            }
            
            text += $"<b>Total Base: {result.totalBase}</b>\n\n";
            
            text += $"Combo: ×{result.comboMultiplier:F2}\n";
            
            if (result.totalDiceMultiplier > 1f)
            {
                text += $"Dice: ×{result.totalDiceMultiplier:F2}\n";
            }
            
            if (result.relicMultiplier != 1f)
            {
                text += $"Relics: ×{result.relicMultiplier:F2}\n";
            }
            
            text += $"<b>Total Mult: ×{result.totalMultiplier:F2}</b>\n\n";
            text += $"<size=120%><b>SCORE: {result.finalScore}</b></size>";
            
            return text;
        }
    }
}

