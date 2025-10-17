using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;

namespace DiceGame
{
    public class BattleController : MonoBehaviour
    {
        [Header("UI")]
        public Transform diceRowParent;
        public GameObject diceViewPrefab;
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;
        public TMP_Text rollFeedbackText;
        public TMP_Text handCounterText;

        [Header("Config")]
        public int diceCount = 5;
        public int maxRollsPerHand = 3;

        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem;

        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private int _rollsUsed = 0;
        private bool _isHandActive = false;

        [Header("Result UI")]
        public GameObject resultPanel;
        public TMP_Text comboInfoText;
        public TMP_Text diceValuesText;
        public TMP_Text formulaText;
        public TMP_Text totalScoreText;
        public Button closeResultButton;   

        private int totalScore = 0;

        // === 结果显示 ===
        private void ShowSimpleResult(string combo, List<int> diceValues, int score, float mult)
        {
            totalScore += score;
            resultPanel.SetActive(true);

            comboInfoText.text = $"<b>{combo}</b>";
            diceValuesText.text = string.Join("   ", diceValues.Select(v => v.ToString()));
            formulaText.text = $"({string.Join("+", diceValues)}) × {mult:F1} = <b>{score}</b>";
            totalScoreText.text = $"Total Score: <b>{totalScore}</b>";

            // 自动关闭计时
            StartCoroutine(AutoHideResult(3f));
        }

        // 自动关闭逻辑
        private System.Collections.IEnumerator AutoHideResult(float delay)
        {
            yield return new WaitForSeconds(delay);

            
            StartCoroutine(DelayedStartNewHand());

            yield return new WaitForSeconds(0.5f);

            if (resultPanel != null)
                resultPanel.SetActive(false);

            Debug.Log("[BattleController] Result panel closed automatically, next hand started!");
        }

        
        private void OnCloseResultPanel()
        {
            Debug.Log("[BattleController] Close button clicked!");
            StopAllCoroutines(); 
            if (resultPanel != null)
                resultPanel.SetActive(false);
            StartCoroutine(DelayedStartNewHand());
        }

        // 初始化
        void Start()
        {
            if (cooldownSystem == null)
            {
                cooldownSystem = FindObjectOfType<CooldownSystem>();
                if (cooldownSystem == null)
                {
                    Debug.LogError("[BattleController] CooldownSystem not found!");
                    return;
                }
            }

            
            if (closeResultButton != null)
                closeResultButton.onClick.AddListener(OnCloseResultPanel);

            cooldownSystem.OnDicePoolRefresh += OnDicePoolRefresh;
            cooldownSystem.OnHandCounterUpdate += OnHandCounterUpdate;
            cooldownSystem.OnAvailableDiceChanged += OnAvailableDiceChanged;

            rollButton.onClick.AddListener(OnRollOnce);
            resetRollButton.onClick.AddListener(ResetForNewHand);
            submitComboButton.onClick.AddListener(OnSubmitCombo);

            resultPanel?.SetActive(false); 
            StartNewHand();
            Debug.Log("[BattleController] Initialized successfully.");
        }

        // === 新一轮骰子 ===
        private void StartNewHand()
        {
            var (handCount, handRemaining) = cooldownSystem.GetHandCounter();
            if (handCount > 0)
                cooldownSystem.AdvanceCooldowns();

            _dice.Clear();
            foreach (var view in _views)
                if (view != null && view.gameObject != null)
                    Destroy(view.gameObject);
            _views.Clear();

            var availableDice = cooldownSystem.GetAvailableDice();
            var selectedDice = availableDice.OrderBy(x => UnityEngine.Random.value)
                                            .Take(Mathf.Min(diceCount, availableDice.Count))
                                            .ToList();

            cooldownSystem.SelectDiceForHand(selectedDice);
            _dice.AddRange(selectedDice);

            foreach (var dice in _dice)
            {
                dice.ResetLockAndValue();
                var go = Instantiate(diceViewPrefab, diceRowParent);
                var view = go.GetComponent<DiceView>();
                view.Bind(dice);
                _views.Add(view);
            }

            // 占位补满
            while (_views.Count < diceCount)
            {
                var go = Instantiate(diceViewPrefab, diceRowParent);
                var view = go.GetComponent<DiceView>();
                var placeholder = new NormalDice
                {
                    diceName = $"Empty_{_views.Count + 1}",
                    tier = DiceTier.Filler,
                    cost = 0,
                    lastRollValue = 0,
                    isLocked = false
                };
                view.Bind(placeholder);
                view.SetDisplayValue("-");
                _views.Add(view);
            }

            _rollsUsed = 0;
            _isHandActive = true;
            UpdateFeedback($"New Hand Ready! {_dice.Count} dice available.");
        }

        void OnRollOnce()
        {
            if (_rollsUsed >= maxRollsPerHand)
            {
                UpdateFeedback("Max rolls reached.");
                return;
            }

            _rollsUsed++;
            for (int i = 0; i < _dice.Count; i++)
            {
                var d = _dice[i];
                if (!d.isLocked && d.tier != DiceTier.Filler)
                    d.Roll();
                _views[i].Refresh();
            }
        }

        void OnSubmitCombo()
        {
            if (!_isHandActive) return;

            var submittedDice = _dice.Where(d => d.isLocked && d.lastRollValue > 0 && d.tier != DiceTier.Filler).ToList();
            var submittedValues = submittedDice.Select(d => d.lastRollValue).ToList();

            if (submittedDice.Count == 0)
            {
                UpdateFeedback("No dice locked!");
                return;
            }

            float mult = 1f;
            string combo = DiceHandEvaluator.Evaluate(submittedValues, out int score, mult);
            ShowSimpleResult(combo, submittedValues, score, mult);

            cooldownSystem.CompleteHand(submittedDice);
            _isHandActive = false;
        }

        private System.Collections.IEnumerator DelayedStartNewHand()
        {
            yield return new WaitForSeconds(1f);
            StartNewHand();
        }

        void ResetForNewHand()
        {
            _rollsUsed = 0;
            _isHandActive = false;
            foreach (var d in _dice) d.ResetLockAndValue();
            foreach (var v in _views) v.Refresh();
            StartNewHand();
        }

        void UpdateFeedback(string msg)
        {
            if (rollFeedbackText != null)
                rollFeedbackText.text = msg;
            else
                Debug.Log(msg);
        }

        // === Cooldown System Event Handlers ===
        private void OnDicePoolRefresh() => StartNewHand();
        private void OnHandCounterUpdate(int current, int remaining) => UpdateHandCounter(current, remaining);
        private void OnAvailableDiceChanged(List<BaseDice> availableDice) { }
        private void UpdateHandCounter(int current, int remaining)
        {
            if (handCounterText != null)
                handCounterText.text = $"Hand {current + 1}/{current + remaining}";
        }

        void OnDestroy()
        {
            if (cooldownSystem != null)
            {
                cooldownSystem.OnDicePoolRefresh -= OnDicePoolRefresh;
                cooldownSystem.OnHandCounterUpdate -= OnHandCounterUpdate;
                cooldownSystem.OnAvailableDiceChanged -= OnAvailableDiceChanged;
            }

            if (closeResultButton != null)
                closeResultButton.onClick.RemoveListener(OnCloseResultPanel);
        }
    }
}
