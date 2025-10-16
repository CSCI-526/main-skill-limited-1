using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceGame
{
    /// <summary>
    /// Battle scene controller: 5 dice, max 3 rolls per hand, text feedback, lock logic
    /// Player can lock dice to form their combo and submit early
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("UI")]
        public Transform diceRowParent;   // Container for DiceView
        public GameObject diceViewPrefab; // Prefab (with DiceView component)
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;  // NEW: Submit current locked combo
        public TMP_Text rollFeedbackText;

        [Header("Config")]
        public int diceCount = 5;         // Fixed 5 dice per hand
        public int maxRollsPerHand = 3;   // Max 3 rolls per hand

        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private int _rollsUsed = 0;

        void Start()
        {
            // Example: First 4 dice use NormalDice, 5th die uses WeightedDice (6 appears ~30%)
            for (int i = 0; i < diceCount; i++)
            {
                BaseDice d;
                if (i < diceCount - 1)
                {
                    var nd = new NormalDice
                    {
                        diceName = $"D6_{i + 1}",
                        tier = DiceTier.Common,
                        cost = 1,
                        cooldownAfterUse = 1
                    };
                    d = nd;
                }
                else
                {
                    var wd = new WeightedDice
                    {
                        diceName = "Lucky Six",
                        tier = DiceTier.Rare,
                        cost = 2,
                        cooldownAfterUse = 1,
                        // Make 6 appear more often (~30%): 1,1,1,1,1,3 → 3/(1*5+3)=3/8≈37.5%
                        // For closer to 30%, use: 1,1,1,1,1,2 → 2/7≈28.6%
                        weights = new float[] { 1, 1, 1, 1, 1, 2 }
                    };
                    d = wd;
                }

                _dice.Add(d);

                var go = Instantiate(diceViewPrefab, diceRowParent);
                var view = go.GetComponent<DiceView>();
                view.Bind(d);
                _views.Add(view);
            }

            rollButton.onClick.AddListener(OnRollOnce);
            resetRollButton.onClick.AddListener(ResetForNewHand);
            submitComboButton.onClick.AddListener(OnSubmitCombo);
            
            Debug.Log("[BattleController] Battle scene initialized with 5 dice.");
            UpdateFeedback("Ready. Roll dice and LOCK the ones you want to keep. Submit when ready!");
        }

        void OnRollOnce()
        {
            if (_rollsUsed >= maxRollsPerHand)
            {
                UpdateFeedback("Already reached maximum rolls per hand (3). Submit your combo or Reset.");
                Debug.LogWarning("[BattleController] Max rolls reached.");
                return;
            }

            _rollsUsed++;
            Debug.Log($"[BattleController] Rolling dice (Roll {_rollsUsed}/{maxRollsPerHand})");

            // Roll only unlocked dice
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (!d.isLocked)
                {
                    int result = d.Roll();
                    Debug.Log($"  - {d.diceName} rolled: {result}");
                }
                else
                {
                    Debug.Log($"  - {d.diceName} locked at: {d.lastRollValue}");
                }

                _views[i].Refresh();
            }

            // Build feedback
            var sb = new StringBuilder();
            sb.AppendLine($"Roll {_rollsUsed}/{maxRollsPerHand}:");
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                sb.AppendLine($"  {d.diceName}: {d.lastRollValue} {(d.isLocked ? "[LOCKED]" : "")}");
            }
            
            if (_rollsUsed < maxRollsPerHand)
                sb.AppendLine("\nLock dice you want to keep, then Roll again or Submit.");
            else
                sb.AppendLine("\nMax rolls reached! Submit your combo now.");

            UpdateFeedback(sb.ToString());
        }

        void OnSubmitCombo()
        {
            Debug.Log("[BattleController] ====== COMBO SUBMITTED ======");
            Debug.Log($"[BattleController] Rolls used: {_rollsUsed}/{maxRollsPerHand}");
            
            // Collect dice values and lock states
            var sb = new StringBuilder();
            sb.AppendLine("=== SUBMITTED COMBO ===");
            sb.AppendLine($"Rolls used: {_rollsUsed}/{maxRollsPerHand}");
            sb.AppendLine("\nDice values:");
            
            var allValues = new List<int>();
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                sb.AppendLine($"  {d.diceName}: {d.lastRollValue} {(d.isLocked ? "[LOCKED]" : "[UNLOCKED]")}");
                Debug.Log($"  {d.diceName}: {d.lastRollValue} (Locked: {d.isLocked})");
                
                if (d.lastRollValue > 0)
                    allValues.Add(d.lastRollValue);
            }
            
            sb.AppendLine($"\nAll values: [{string.Join(", ", allValues)}]");
            sb.AppendLine("\n(Combo detection & scoring will be implemented next)");
            sb.AppendLine("Use Reset to start a new hand.");
            
            Debug.Log($"[BattleController] All dice values: [{string.Join(", ", allValues)}]");
            Debug.Log("[BattleController] ============================");
            
            UpdateFeedback(sb.ToString());
        }

        void ResetForNewHand()
        {
            Debug.Log("[BattleController] Resetting for new hand...");
            _rollsUsed = 0;
            foreach (var d in _dice) d.ResetLockAndValue();
            foreach (var v in _views) v.Refresh();
            UpdateFeedback("Hand reset. Roll dice and lock the ones you want to keep!");
            Debug.Log("[BattleController] Hand reset complete.");
        }

        void UpdateFeedback(string msg)
        {
            if (rollFeedbackText != null) rollFeedbackText.text = msg;
            else Debug.Log(msg);
        }
    }
}
