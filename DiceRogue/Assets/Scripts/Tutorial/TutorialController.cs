using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;
using DiceGame.UI;
using DiceGame;
using DiceRogue.Boot;

namespace DiceGame.Tutorial
{
    /// <summary>
    /// Guides the player through the first hand without modifying BattleController logic.
    /// Each step either displays a message (intro/outro) or waits for a concrete gameplay action.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("Gameplay References")]
        public BattleController battleController;
        public BackpackManager backpackManager;
        public DiceSelectionUI diceSelectionUI;
        public Transform diceRowParent;
        public ScoreAnimator scoreAnimator;

        [Header("Gameplay Buttons")]
        public Button openBackpackButton;
        public Button rollButton;
        public Button submitComboButton;

        [Header("Tutorial UI")]
        public GameObject tutorialPromptPanel;
        public TMP_Text tutorialText;
        public Button tutorialContinueButton;
        public Button skipTutorialButton;

        [Header("Layout")]
        public bool configurePromptLayout = true;
        public Vector2 promptSize = new Vector2(640f, 240f);
        public Vector2 promptOffset = Vector2.zero;
        public Vector2 promptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 promptPivot = new Vector2(0.5f, 0.5f);

        [Header("Action Prompt Layout (left side)")]
        public Vector2 actionPromptSize = new Vector2(360f, 220f);
        public Vector2 actionPromptOffset = new Vector2(40f, -40f);
        public Vector2 actionPromptAnchor = new Vector2(0.05f, 0.65f);
        public Vector2 actionPromptPivot = new Vector2(0f, 0.5f);

        public Vector2 introPromptSize = new Vector2(640f, 240f);
        public Vector2 introPromptOffset = Vector2.zero;
        public Vector2 introPromptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 introPromptPivot = new Vector2(0.5f, 0.5f);

        public Vector2 textPadding = new Vector2(24f, 24f);
        public float textRightPaddingWithButton = 150f;

        [Header("Combo Prompt Layout (under dice)")]
        public Vector2 comboPromptSize = new Vector2(520f, 160f);
        public Vector2 comboPromptOffset = new Vector2(0f, -190f);
        public Vector2 comboPromptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 comboPromptPivot = new Vector2(0.5f, 0.5f);

        private readonly List<TutorialStep> tutorialSteps = new();
        private readonly List<(Button button, UnityAction handler)> buttonHandlers = new();
        private readonly List<(Button button, UnityAction handler)> diceLockHandlers = new();

        private int currentStepIndex = -1;
        private bool isTutorialActive = true;
        private TutorialAction currentRequiredAction = TutorialAction.None;
        private Coroutine waitForScoreRoutine;
        private bool lockStepCompleted;
        private bool awaitingSecondRoll;

        private RectTransform promptRect;
        private RectTransform textRect;
        private RectTransform nextButtonRect;
        private TextMeshProUGUI nextButtonLabel;

        void Start()
        {
            ResolveReferences();
            BuildSteps();
            InitializeTutorialUI();
            ConfigurePromptLayoutIfNeeded();
            HookGlobalListeners();
            StartTutorialStep(0);
        }

        void OnDestroy()
        {
            UnhookGlobalListeners();
            CleanupDiceViewListeners();
        }

        #region Setup

        void ResolveReferences()
        {
            if (battleController == null)
            {
                battleController = GetComponent<BattleController>();
                if (battleController == null)
                {
                    battleController = FindObjectOfType<BattleController>();
                }
            }

            if (battleController != null)
            {
                if (backpackManager == null) backpackManager = battleController.backpackManager;
                if (diceRowParent == null) diceRowParent = battleController.diceRowParent;
                if (scoreAnimator == null) scoreAnimator = battleController.scoreAnimator;
                if (openBackpackButton == null) openBackpackButton = battleController.openBackpackButton;
                if (rollButton == null) rollButton = battleController.rollButton;
                if (submitComboButton == null) submitComboButton = battleController.submitComboButton;
            }

            if (backpackManager != null)
            {
                if (diceSelectionUI == null) diceSelectionUI = backpackManager.diceSelectionUI;
            }

            if (tutorialPromptPanel == null)
            {
                tutorialPromptPanel = GameObject.Find("TutorialPromptPanel");
            }

            if (tutorialText == null && tutorialPromptPanel != null)
            {
                tutorialText = tutorialPromptPanel.GetComponentInChildren<TMP_Text>();
            }

            if (tutorialContinueButton == null)
            {
                var nextButton = GameObject.Find("NextButton");
                if (nextButton != null) tutorialContinueButton = nextButton.GetComponent<Button>();
            }
        }

        void BuildSteps()
        {
            tutorialSteps.Clear();
            tutorialSteps.Add(new TutorialStep
            {
                title = "Welcome to Straight or Bust!",
                message = "This tutorial will teach you the basics of the game.",
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.IntroCenter,
                requiredAction = TutorialAction.None
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Build Your Hand",
                message = "Open the backpack and select up to five dice.",
                highlightElement = openBackpackButton != null ? openBackpackButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.ConfirmHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Your Dice",
                message = "Press Roll to throw your selected dice.",
                highlightElement = rollButton != null ? rollButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.RollDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Lock Dice",
                message = "Click the dice you want to keep, up to five dice can be locked.",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.LockDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Check Combo Preference",
                message = "Review the Combo Preference panel to see which combinations are valuable.",
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.ComboInfo,
                requiredAction = TutorialAction.None
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Again",
                message = "You can roll again. Locked dice stay at their current values.",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.SecondRoll
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Submit Your Hand",
                message = "Press Submit to score the hand.",
                highlightElement = submitComboButton != null ? submitComboButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.SubmitHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Score Breakdown",
                message = "Watch how the combo is scored.",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.CenterBelow,
                requiredAction = TutorialAction.ScoreAnimationComplete
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "You’re ready to play. Click Next to return to the main menu.",
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.IntroCenter,
                requiredAction = TutorialAction.None
            });
        }

        void InitializeTutorialUI()
        {
            if (tutorialPromptPanel != null)
            {
                tutorialPromptPanel.SetActive(false);
                promptRect = tutorialPromptPanel.GetComponent<RectTransform>();
            }

            if (tutorialText != null)
            {
                textRect = tutorialText.GetComponent<RectTransform>();
                NormalizeTextRect();
                tutorialText.enableWordWrapping = true;
                tutorialText.overflowMode = TextOverflowModes.Overflow;
            }

            if (tutorialContinueButton != null)
            {
                tutorialContinueButton.onClick.AddListener(OnTutorialContinue);
                nextButtonRect = tutorialContinueButton.GetComponent<RectTransform>();
                nextButtonLabel = tutorialContinueButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }
        }

        void ConfigurePromptLayoutIfNeeded()
        {
            if (!configurePromptLayout || tutorialPromptPanel == null) return;

            if (promptRect == null) promptRect = tutorialPromptPanel.GetComponent<RectTransform>();
            if (promptRect != null)
            {
                promptRect.anchorMin = promptAnchor;
                promptRect.anchorMax = promptAnchor;
                promptRect.pivot = promptPivot;
                promptRect.sizeDelta = promptSize;
                promptRect.anchoredPosition = promptOffset;
            }

            if (tutorialText != null)
            {
                textRect = tutorialText.GetComponent<RectTransform>();
                NormalizeTextRect();
            }

            if (nextButtonRect != null)
            {
                nextButtonRect.anchorMin = nextButtonRect.anchorMax = new Vector2(1f, 0.5f);
                nextButtonRect.pivot = new Vector2(1f, 0.5f);
                nextButtonRect.anchoredPosition = new Vector2(-textPadding.x, 0f);
            }

            UpdateTextPadding(true);
        }

        void HookGlobalListeners()
        {
            if (diceSelectionUI != null && diceSelectionUI.submitButton != null)
            {
                UnityAction handler = OnDiceSelectionConfirmed;
                diceSelectionUI.submitButton.onClick.AddListener(handler);
                buttonHandlers.Add((diceSelectionUI.submitButton, handler));
            }
            else if (backpackManager != null && backpackManager.diceSelectionUI != null && backpackManager.diceSelectionUI.submitButton != null)
            {
                diceSelectionUI = backpackManager.diceSelectionUI;
                UnityAction handler = OnDiceSelectionConfirmed;
                diceSelectionUI.submitButton.onClick.AddListener(handler);
                buttonHandlers.Add((diceSelectionUI.submitButton, handler));
            }

            if (rollButton != null)
            {
                UnityAction handler = OnRollClicked;
                rollButton.onClick.AddListener(handler);
                buttonHandlers.Add((rollButton, handler));
            }

            if (submitComboButton != null)
            {
                UnityAction handler = OnSubmitClicked;
                submitComboButton.onClick.AddListener(handler);
                buttonHandlers.Add((submitComboButton, handler));
            }
        }

        void UnhookGlobalListeners()
        {
            foreach (var entry in buttonHandlers)
            {
                if (entry.button != null)
                {
                    entry.button.onClick.RemoveListener(entry.handler);
                }
            }
            buttonHandlers.Clear();
        }

        #endregion

        #region Step Flow

        void StartTutorialStep(int stepIndex)
        {
            if (!isTutorialActive) return;

            if (stepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            currentStepIndex = stepIndex;
            var step = tutorialSteps[stepIndex];

            ApplyLayoutForStep(step);
            ShowPrompt(step.title, step.message);
            HighlightElement(step.highlightElement);
            SetNextButtonVisible(step.useNextButton);

            if (step.waitForAction)
            {
                currentRequiredAction = step.requiredAction;

                if (step.requiredAction == TutorialAction.LockDice)
                {
                    lockStepCompleted = false;
                    StartCoroutine(WaitForDiceViewsAndAttach());
                }
                else if (step.requiredAction == TutorialAction.ScoreAnimationComplete)
                {
                    StartScoreWatcher();
                }
                else if (step.requiredAction == TutorialAction.SecondRoll)
                {
                    awaitingSecondRoll = true;
                }
            }
            else
            {
                currentRequiredAction = TutorialAction.None;
                if (!step.useNextButton)
                {
                    // Auto-advance if there is no action and no button (not used in current flow)
                    StartCoroutine(AutoAdvanceAfterDelay(1.0f));
                }
            }
        }

        IEnumerator AutoAdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (currentRequiredAction == TutorialAction.None && isTutorialActive)
            {
                StartTutorialStep(currentStepIndex + 1);
            }
        }

        void ShowPrompt(string title, string message)
        {
            if (tutorialPromptPanel != null)
            {
                tutorialPromptPanel.SetActive(true);
            }

            if (tutorialText != null)
            {
                tutorialText.text = $"<b>{title}</b>\n\n{message}";
            }
        }

        void HidePrompt()
        {
            if (tutorialPromptPanel != null)
            {
                tutorialPromptPanel.SetActive(false);
            }
        }

        void SetNextButtonVisible(bool visible)
        {
            if (tutorialContinueButton == null) return;
            tutorialContinueButton.gameObject.SetActive(visible);
            tutorialContinueButton.interactable = visible;
            UpdateTextPadding(visible);
            if (nextButtonLabel != null)
            {
                nextButtonLabel.text = "Next";
            }
        }

        void UpdateTextPadding(bool hasNextButton)
        {
            if (textRect == null) return;
            float rightPadding = hasNextButton ? textRightPaddingWithButton : textPadding.x;
            textRect.offsetMin = new Vector2(textPadding.x, textPadding.y);
            textRect.offsetMax = new Vector2(-rightPadding, -textPadding.y);
        }

        void NormalizeTextRect()
        {
            if (textRect == null) return;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0f, 1f);
            textRect.offsetMin = new Vector2(textPadding.x, textPadding.y);
            textRect.offsetMax = new Vector2(-textPadding.x, -textPadding.y);
        }

        void ApplyLayoutForStep(TutorialStep step)
        {
            if (promptRect == null) return;

            switch (step.layout)
            {
                case StepLayout.ActionLeft:
                    promptRect.anchorMin = promptRect.anchorMax = actionPromptAnchor;
                    promptRect.pivot = actionPromptPivot;
                    promptRect.sizeDelta = actionPromptSize;
                    promptRect.anchoredPosition = actionPromptOffset;
                    if (tutorialText != null) tutorialText.alignment = TextAlignmentOptions.TopLeft;
                    break;

                case StepLayout.ComboInfo:
                    promptRect.anchorMin = promptRect.anchorMax = comboPromptAnchor;
                    promptRect.pivot = comboPromptPivot;
                    promptRect.sizeDelta = comboPromptSize;
                    promptRect.anchoredPosition = comboPromptOffset;
                    if (tutorialText != null) tutorialText.alignment = TextAlignmentOptions.Center;
                    break;

                case StepLayout.CenterBelow:
                    promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
                    promptRect.pivot = new Vector2(0.5f, 0.5f);
                    promptRect.sizeDelta = new Vector2(520f, 160f);
                    promptRect.anchoredPosition = new Vector2(0f, -140f);
                    if (tutorialText != null) tutorialText.alignment = TextAlignmentOptions.Center;
                    break;

                case StepLayout.IntroCenter:
                default:
                    promptRect.anchorMin = promptRect.anchorMax = introPromptAnchor;
                    promptRect.pivot = introPromptPivot;
                    promptRect.sizeDelta = introPromptSize;
                    promptRect.anchoredPosition = introPromptOffset;
                    if (tutorialText != null) tutorialText.alignment = TextAlignmentOptions.Center;
                    break;
            }
        }

        void HighlightElement(GameObject element)
        {
            if (element == null) return;
            // Placeholder: log highlight. You can add visual effects (pulse, outline) here later.
            Debug.Log($"[Tutorial] Highlight {element.name}");
        }

        void OnTutorialContinue()
        {
            HidePrompt();
            StartTutorialStep(currentStepIndex + 1);
        }

        #endregion

        #region Gameplay Hooks

        void OnDiceSelectionConfirmed()
        {
            RegisterActionCompletion(TutorialAction.ConfirmHand);
            StartCoroutine(WaitForDiceViewsAndAttach());
        }

        void OnRollClicked()
        {
            if (currentRequiredAction == TutorialAction.RollDice)
            {
                RegisterActionCompletion(TutorialAction.RollDice);
            }
            else if (currentRequiredAction == TutorialAction.SecondRoll && awaitingSecondRoll)
            {
                awaitingSecondRoll = false;
                RegisterActionCompletion(TutorialAction.SecondRoll);
            }
        }

        void OnSubmitClicked()
        {
            RegisterActionCompletion(TutorialAction.SubmitHand);
        }

        IEnumerator WaitForDiceViewsAndAttach()
        {
            // Allow BattleController to spawn dice views
            yield return new WaitForSeconds(0.1f);

            if (diceSelectionUI == null && backpackManager != null)
            {
                diceSelectionUI = backpackManager.diceSelectionUI;
            }

            // Wait until dice views exist or timeout after 3 seconds
            float timeout = 3f;
            while (timeout > 0f && !HasDiceViews())
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            AttachDiceViewListeners();
        }

        bool HasDiceViews()
        {
            if (diceRowParent == null) return false;
            return diceRowParent.GetComponentsInChildren<DiceView>(true).Length > 0;
        }

        void AttachDiceViewListeners()
        {
            CleanupDiceViewListeners();
            if (diceRowParent == null) return;

            DiceView[] views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            foreach (var view in views)
            {
                if (view != null && view.lockButton != null)
                {
                    UnityAction handler = () => OnDiceLockClicked(view);
                    view.lockButton.onClick.AddListener(handler);
                    diceLockHandlers.Add((view.lockButton, handler));
                }
            }
        }

        void CleanupDiceViewListeners()
        {
            foreach (var entry in diceLockHandlers)
            {
                if (entry.button != null)
                {
                    entry.button.onClick.RemoveListener(entry.handler);
                }
            }
            diceLockHandlers.Clear();
        }

        void OnDiceLockClicked(DiceView view)
        {
            if (!isTutorialActive || lockStepCompleted) return;
            StartCoroutine(CheckLockStateNextFrame(view));
        }

        IEnumerator CheckLockStateNextFrame(DiceView view)
        {
            yield return null; // wait one frame so DiceView updates
            if (view != null && view.model != null && view.model.isLocked)
            {
                lockStepCompleted = true;
                RegisterActionCompletion(TutorialAction.LockDice);
                CleanupDiceViewListeners();
            }
        }

        void StartScoreWatcher()
        {
            if (waitForScoreRoutine != null)
            {
                StopCoroutine(waitForScoreRoutine);
            }
            waitForScoreRoutine = StartCoroutine(WaitForScoreAnimationComplete());
        }

        IEnumerator WaitForScoreAnimationComplete()
        {
            if (scoreAnimator == null)
            {
                yield return new WaitForSeconds(1f);
                RegisterActionCompletion(TutorialAction.ScoreAnimationComplete);
                yield break;
            }

            // Wait until animation starts (timeout to avoid getting stuck)
            float timeout = 10f;
            while (!scoreAnimator.IsAnimating && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Then wait for it to finish
            timeout = 15f;
            while (scoreAnimator.IsAnimating && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            RegisterActionCompletion(TutorialAction.ScoreAnimationComplete);
        }

        #endregion

        #region Completion

        void RegisterActionCompletion(TutorialAction action)
        {
            if (!isTutorialActive) return;
            if (currentRequiredAction == TutorialAction.None) return;
            if (currentRequiredAction != action) return;

            currentRequiredAction = TutorialAction.None;
            HidePrompt();
            StartTutorialStep(currentStepIndex + 1);
        }

        void CompleteTutorial()
        {
            if (!isTutorialActive) return;

            isTutorialActive = false;
            HidePrompt();
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();

            if (RunLoader.Instance != null)
            {
                RunLoader.Instance.StartRun();
            }
            else
            {
                SceneManager.LoadScene("BattleScene");
            }
        }

        IEnumerator ReturnToMainMenu()
        {
            yield return DiceRogue.Boot.RunLoader.Instance.LoadSceneWithWipe("MainScene");
        }

        #endregion
    }

    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        public string message;
        public GameObject highlightElement;
        public bool useNextButton;
        public bool waitForAction;
        public StepLayout layout = StepLayout.ActionLeft;
        public TutorialAction requiredAction;
    }

    public enum TutorialAction
    {
        None,
        ConfirmHand,
        RollDice,
        LockDice,
        SecondRoll,
        SubmitHand,
        ScoreAnimationComplete
    }

    public enum StepLayout
    {
        IntroCenter,
        ActionLeft,
        ComboInfo,
        CenterBelow
    }
}


