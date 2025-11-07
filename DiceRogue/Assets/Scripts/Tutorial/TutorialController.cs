using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;
using DiceGame.UI;
using DiceGame;

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
        public float actionPromptDisplaySeconds = 1.5f;

        private readonly List<TutorialStep> tutorialSteps = new();
        private readonly List<(Button button, UnityAction handler)> buttonHandlers = new();
        private readonly List<(Button button, UnityAction handler)> diceLockHandlers = new();

        private int currentStepIndex = -1;
        private bool isTutorialActive = true;
        private TutorialAction currentRequiredAction = TutorialAction.None;
        private Coroutine autoHideRoutine;
        private Coroutine waitForScoreRoutine;
        private bool lockStepCompleted;

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
                title = "Welcome to Dice Roguelike!",
                message = "This tutorial will teach you the basics of the game.",
                useNextButton = true,
                waitForAction = false,
                autoHidePrompt = false,
                requiredAction = TutorialAction.None
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Build Your Hand",
                message = "Open the backpack and select five dice.", 
                highlightElement = openBackpackButton != null ? openBackpackButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                autoHidePrompt = true,
                requiredAction = TutorialAction.ConfirmHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Your Dice",
                message = "Press Roll to throw your selected dice.",
                highlightElement = rollButton != null ? rollButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                autoHidePrompt = true,
                requiredAction = TutorialAction.RollDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Lock Dice",
                message = "Click the dice you want to keep.",
                useNextButton = false,
                waitForAction = true,
                autoHidePrompt = true,
                requiredAction = TutorialAction.LockDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Submit Your Hand",
                message = "Press Submit to score the hand.",
                highlightElement = submitComboButton != null ? submitComboButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                autoHidePrompt = true,
                requiredAction = TutorialAction.SubmitHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Score Breakdown",
                message = "Watch how the combo is scored.",
                useNextButton = false,
                waitForAction = true,
                autoHidePrompt = false,
                requiredAction = TutorialAction.ScoreAnimationComplete
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "You’re ready to play. Click Next to return to the main menu.",
                useNextButton = true,
                waitForAction = false,
                autoHidePrompt = false,
                requiredAction = TutorialAction.None
            });
        }

        void InitializeTutorialUI()
        {
            if (tutorialPromptPanel != null)
            {
                tutorialPromptPanel.SetActive(false);
            }

            if (tutorialContinueButton != null)
            {
                tutorialContinueButton.onClick.AddListener(OnTutorialContinue);
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }
        }

        void ConfigurePromptLayoutIfNeeded()
        {
            if (!configurePromptLayout || tutorialPromptPanel == null) return;

            RectTransform rect = tutorialPromptPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = promptSize;
                rect.anchoredPosition = promptOffset;
            }

            if (tutorialContinueButton != null)
            {
                RectTransform buttonRect = tutorialContinueButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
                    buttonRect.pivot = new Vector2(0.5f, 0f);
                    buttonRect.anchoredPosition = new Vector2(0f, 30f);
                }
            }
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

            ShowPrompt(step.title, step.message);
            HighlightElement(step.highlightElement);
            SetNextButtonVisible(step.useNextButton);

            if (autoHideRoutine != null)
            {
                StopCoroutine(autoHideRoutine);
                autoHideRoutine = null;
            }

            if (step.autoHidePrompt)
            {
                autoHideRoutine = StartCoroutine(AutoHidePromptAfterDelay(actionPromptDisplaySeconds));
            }

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

        IEnumerator AutoHidePromptAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            HidePrompt();
            autoHideRoutine = null;
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
            RegisterActionCompletion(TutorialAction.RollDice);
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

            if (DiceRogue.Boot.RunLoader.Instance != null)
            {
                StartCoroutine(ReturnToMainMenu());
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
        public bool autoHidePrompt;
        public TutorialAction requiredAction;
    }

    public enum TutorialAction
    {
        None,
        ConfirmHand,
        RollDice,
        LockDice,
        SubmitHand,
        ScoreAnimationComplete
    }
}


