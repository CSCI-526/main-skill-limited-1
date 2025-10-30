using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Handles all UI formatting and presentation logic for the battle scene.
    /// Centralizes string building, color codes, and TextMeshPro rich text.
    /// </summary>
    public class BattleUIPresenter
    {
        // Color constants
        private const string COLOR_GOLD = "#FFD700";
        private const string COLOR_RED = "#FF6666";
        private const string COLOR_GREEN = "#88FF88";
        private const string COLOR_PURPLE = "#9370DB";
        private const string COLOR_LIGHT_GREEN = "#90EE90";
        private const string COLOR_GRAY = "#AAAAAA";
        private const string COLOR_WHITE = "#FFFFFF";
        private const string COLOR_DARK_RED = "#FF3333";
        private const string COLOR_BRIGHT_RED = "#FF8888";

        /// <summary>
        /// Format hand counter display
        /// </summary>
        public string FormatHandCounter(int current, int remaining)
        {
            if (remaining <= 0)
            {
                return $"<color={COLOR_BRIGHT_RED}><b>No Hands Remaining!</b></color>\n" +
                       $"Hands: {current}/{current}\n" +
                       $"<size=90%>(Battle complete - Press Reset to test again)</size>";
            }
            else
            {
                return $"Hand {current + 1}/{current + remaining} ({remaining} remaining)";
            }
        }

        /// <summary>
        /// Format deck status display with all dice and their states
        /// </summary>
        public string FormatDeckStatus(List<BaseDice> allDice, HashSet<string> selectedDiceNames)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>DICE DECK</b>\n");

            // Display all dice in simple list with colored names by rarity
            foreach (var dice in allDice)
            {
                AppendDiceStatus(sb, dice, selectedDiceNames);
            }

            // Compact summary
            int available = allDice.Count(d => d.cooldownRemain == 0 && !selectedDiceNames.Contains(d.diceName));
            int selected = selectedDiceNames.Count;
            int onCooldown = allDice.Count(d => d.cooldownRemain > 0);

            sb.AppendLine($"\n<size=90%>Ready: {available} | Active: {selected} | CD: {onCooldown}</size>");

            return sb.ToString();
        }

        /// <summary>
        /// Format target score display
        /// </summary>
        public string FormatTargetScore(int targetScore, int currentLevel)
        {
            return $"<size=70%>Target Score</size>\n" +
                   $"<size=150%><b>{targetScore}</b></size>\n" +
                   $"<size=80%><color={COLOR_GRAY}>Level {currentLevel}</color></size>";
        }

        /// <summary>
        /// Format initial hand ready message
        /// </summary>
        public string FormatHandReady(int handNumber, int diceCount, int specialCount, int normalCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=110%><b>Hand {handNumber}</b></size>\n");
            sb.AppendLine($"<color={COLOR_GREEN}>Ready! {diceCount} dice prepared.</color>");
            
            if (specialCount < diceCount)
            {
                sb.AppendLine($"<color={COLOR_GRAY}>({specialCount} special + {normalCount} normal dice)</color>");
            }
            
            sb.AppendLine("\n<b>Instructions:</b>");
            sb.AppendLine("  • Roll the dice");
            sb.AppendLine("  • Click to lock dice you want to keep");
            sb.AppendLine("  • Submit when ready");
            
            return sb.ToString();
        }

        /// <summary>
        /// Format roll result feedback
        /// </summary>
        public string FormatRollFeedback(List<BaseDice> dice, int rollNumber, int maxRolls)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=110%><b>Roll {rollNumber}/{maxRolls}</b></size>\n");
            
            if (rollNumber < maxRolls)
            {
                sb.AppendLine($"<color={COLOR_GREEN}>Click dice to lock/unlock, then Roll again or Submit.</color>");
            }
            else
            {
                sb.AppendLine($"<color={COLOR_BRIGHT_RED}>Max rolls reached! Submit your combo now.</color>");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format combo submitted message
        /// </summary>
        public string FormatComboSubmitted(List<BaseDice> submittedDice, int rollsUsed, int maxRolls)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<size=110%><b>COMBO SUBMITTED</b></size>\n");
            sb.AppendLine($"<color={COLOR_GRAY}>Rolls used: {rollsUsed}/{maxRolls}</color>");
            sb.AppendLine($"<color={COLOR_GRAY}>Submitted {submittedDice.Count} dice:</color>\n");
            
            foreach (var dice in submittedDice)
            {
                sb.AppendLine($"  • <b>{dice.diceName}:</b> {dice.lastRollValue} <color={COLOR_GOLD}>[SUBMITTED]</color>");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Format no hands remaining message
        /// </summary>
        public string FormatNoHandsRemaining()
        {
            return $"<color={COLOR_BRIGHT_RED}><b>No Hands Remaining!</b></color>\n\n" +
                   "All hands have been used.\n" +
                   $"<color={COLOR_GRAY}>Battle complete! Press Continue to next level.</color>";
        }

        /// <summary>
        /// Format level start message
        /// </summary>
        public string FormatLevelStart(int level, int targetScore)
        {
            return $"<size=120%><b>Level {level} Start!</b></size>\n\n" +
                   $"<color={COLOR_GREEN}>New target: {targetScore}</color>\n\n" +
                   $"<color={COLOR_GRAY}>All dice and hands have been reset.\nGood luck!</color>";
        }

        /// <summary>
        /// Format game over message (failed to reach target)
        /// </summary>
        public string FormatGameOver()
        {
            return $"<color={COLOR_DARK_RED}><b>GAME OVER</b></color>\n\n" +
                   "You didn't reach the target score.\n\n" +
                   $"<color={COLOR_GRAY}>Press Reset to try again from Level 1.</color>";
        }

        /// <summary>
        /// Format reset to level 1 message
        /// </summary>
        public string FormatResetToLevelOne(int targetScore)
        {
            return $"<color={COLOR_GREEN}><b>Starting Fresh!</b></color>\n\n" +
                   "Returning to Level 1.\n" +
                   $"Target: {targetScore}\n\n" +
                   $"<color={COLOR_GRAY}>Good luck!</color>";
        }

        /// <summary>
        /// Format dice pool refreshed message
        /// </summary>
        public string FormatDicePoolRefreshed()
        {
            return $"<color={COLOR_GREEN}><b>Dice Pool Refreshed!</b></color>\n\n" +
                   "All dice are now available again.\n" +
                   "Starting new battle cycle...";
        }

        /// <summary>
        /// Format error message when no dice are locked
        /// </summary>
        public string FormatNoDiceLocked()
        {
            return $"<color={COLOR_BRIGHT_RED}><b>No dice are locked!</b></color>\n\n" +
                   "Lock some dice before submitting.";
        }

        /// <summary>
        /// Get rarity color for a dice tier
        /// </summary>
        public string GetRarityColor(DiceTier tier)
        {
            return tier switch
            {
                DiceTier.Legendary => COLOR_GOLD,
                DiceTier.Rare => COLOR_PURPLE,
                DiceTier.Common => COLOR_LIGHT_GREEN,
                _ => COLOR_WHITE
            };
        }

        /// <summary>
        /// Append dice status line to string builder
        /// </summary>
        private void AppendDiceStatus(StringBuilder sb, BaseDice dice, HashSet<string> selectedDiceNames)
        {
            // Determine rarity color for dice name
            string rarityColor = GetRarityColor(dice.tier);

            // Determine status
            string statusText;
            string statusColor;

            if (selectedDiceNames.Contains(dice.diceName))
            {
                statusText = "ACTIVE";
                statusColor = COLOR_GOLD;
            }
            else if (dice.cooldownRemain > 0)
            {
                statusText = $"CD({dice.cooldownRemain})";
                statusColor = COLOR_RED;
            }
            else
            {
                statusText = "READY";
                statusColor = COLOR_GREEN;
            }

            // Compact format: [Status] Dice Name
            sb.AppendLine($"<color={statusColor}>[{statusText}]</color> <color={rarityColor}>{dice.diceName}</color>");
        }
    }
}

