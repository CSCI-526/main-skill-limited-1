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
        public Vector2 actionPromptSize = new Vector2(500f, 340f);
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
        public Vector2 comboPromptSize = new Vector2(700f, 240f);
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
        private System.Action diceLockChangedHandler;
        [Header("Lock Dice Requirements")]
        [Tooltip("How many dice the player must lock before the tutorial advances.")]
        public int requiredLockedDiceCount = 3;

        private RectTransform promptRect;
        private RectTransform textRect;
        private RectTransform nextButtonRect;
        private TextMeshProUGUI nextButtonLabel;

        // Highlighting system
        private readonly List<GameObject> highlightedElements = new();
        private readonly List<Coroutine> highlightCoroutines = new();
        private readonly Dictionary<Button, bool> originalButtonStates = new();
        private readonly List<Button> allButtons = new();

        void Start()
        {
            ResolveReferences();
            BuildSteps();
            InitializeTutorialUI();
            ConfigurePromptLayoutIfNeeded();
            HookGlobalListeners();
            CollectAllButtons();
            StartTutorialStep(0);
        }

        void OnDestroy()
        {
            UnhookGlobalListeners();
            CleanupDiceViewListeners();
            UnhookDiceLockEvent();
            ClearHighlights();
            RestoreAllButtons();
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
                if (openBackpackButton == null && backpackManager != null) openBackpackButton = backpackManager.openBackpackButton;
                if (rollButton == null) rollButton = battleController.rollButton;
                if (submitComboButton == null) submitComboButton = battleController.submitComboButton;
            }

            // Fallback: try to find DiceRow by name if not found through BattleController
            if (diceRowParent == null)
            {
                GameObject diceRowGO = GameObject.Find("DiceRow");
                if (diceRowGO != null)
                {
                    diceRowParent = diceRowGO.transform;
                }
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
                message = "Select up to five dices from backpack",
                highlightElement = openBackpackButton != null ? openBackpackButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.ConfirmHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Lock Dice",
                message = "Click the dice you want to keep. Locked dice won't change when you roll. Lock any three dice to continue.",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.LockDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Your Dice",
                message = "Press Roll to throw your unselected dice.",
                highlightElement = rollButton != null ? rollButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.RollDice
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Check Combo Rules",
                message = "Click the Combo rule button to see which combinations are valuable",
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.ComboInfo,
                requiredAction = TutorialAction.None
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Again",
                message = "You can roll again. The # of roll and cast left can be seen on the right panel.",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.SecondRoll
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Submit Your Hand",
                message = "Lock the dice you want to use and press Cast to score the hand.",
                highlightElement = submitComboButton != null ? submitComboButton.gameObject : null,
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.SubmitHand
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Score Breakdown",
                message = "Watch how the combo is scored(basic combo + dice effect + relic effect).",
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.CenterBelow,
                requiredAction = TutorialAction.ScoreAnimationComplete
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "Great job! You've learned the basics. Click Next to claim your reward and start Level 1.",
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
            EnsureDiceSelectionButtonHooked();
            EnsureRollButtonHooked();
            EnsureSubmitButtonHooked();
        }

        void EnsureSubmitButtonHooked()
        {
            // Check if already hooked
            foreach (var entry in buttonHandlers)
            {
                if (entry.button != null && entry.handler == OnSubmitClicked)
                {
                    return; // Already hooked
                }
            }

            ResolveReferences();

            // Try to find submitComboButton if still null
            if (submitComboButton == null && battleController != null)
            {
                submitComboButton = battleController.submitComboButton;
            }

            // Fallback: try to find CastComboButton by name
            if (submitComboButton == null)
            {
                GameObject castButtonGO = GameObject.Find("CastComboButton");
                if (castButtonGO == null)
                {
                    // Try to find by searching all buttons for "Cast" text
                    Button[] allButtons = FindObjectsOfType<Button>(true);
                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        
                        if (btn.name.ToUpper().Contains("CAST") || btn.name.ToUpper().Contains("SUBMIT"))
                        {
                            submitComboButton = btn;
                            break;
                        }
                        
                        var text = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        if (text != null && (text.text.ToUpper().Contains("CAST") || text.text.ToUpper().Contains("SUBMIT")))
                        {
                            submitComboButton = btn;
                            break;
                        }
                    }
                }
                else
                {
                    submitComboButton = castButtonGO.GetComponent<Button>();
                }
            }

            // Hook the submit button
            if (submitComboButton != null)
            {
                UnityAction handler = OnSubmitClicked;
                submitComboButton.onClick.AddListener(handler);
                buttonHandlers.Add((submitComboButton, handler));
            }
        }

        bool IsRollButtonHooked()
        {
            foreach (var entry in buttonHandlers)
            {
                if (entry.button != null && entry.handler == OnRollClicked)
                {
                    return true;
                }
            }
            return false;
        }

        void EnsureRollButtonHooked()
        {
            if (IsRollButtonHooked()) return;

            ResolveReferences();

            if (rollButton == null && battleController != null)
            {
                rollButton = battleController.rollButton;
            }

            if (rollButton == null)
            {
                GameObject rollButtonGO = GameObject.Find("Roll");
                if (rollButtonGO == null)
                {
                    Button[] allButtons = FindObjectsOfType<Button>(true);
                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        if (btn.name.ToUpper().Contains("ROLL"))
                        {
                            rollButton = btn;
                            break;
                        }
                        var text = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        if (text != null && text.text.ToUpper().Contains("ROLL"))
                        {
                            rollButton = btn;
                            break;
                        }
                    }
                }
                else
                {
                    rollButton = rollButtonGO.GetComponent<Button>();
                }
            }

            if (rollButton != null)
            {
                UnityAction handler = OnRollClicked;
                rollButton.onClick.AddListener(handler);
                buttonHandlers.Add((rollButton, handler));
            }
        }

        bool IsDiceSelectionButtonHooked()
        {
            foreach (var entry in buttonHandlers)
            {
                if (entry.button != null && entry.handler == OnDiceSelectionConfirmed)
                {
                    return true;
                }
            }
            return false;
        }

        void EnsureDiceSelectionButtonHooked()
        {
            // Check if already hooked
            if (IsDiceSelectionButtonHooked())
            {
                return; // Already hooked
            }

            // Try to resolve references
            ResolveReferences();

            // Try to resolve diceSelectionUI if not set
            if (diceSelectionUI == null && backpackManager != null)
            {
                diceSelectionUI = backpackManager.diceSelectionUI;
            }

            // Try to find diceSelectionUI if still null
            if (diceSelectionUI == null)
            {
                diceSelectionUI = FindObjectOfType<DiceSelectionUI>();
            }

            // Try to hook the submit button
            if (diceSelectionUI != null)
            {
                if (diceSelectionUI.submitButton == null)
                {
                    Button[] buttons = diceSelectionUI.GetComponentsInChildren<Button>(true);
                    foreach (var btn in buttons)
                    {
                        var text = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        if (text != null && (text.text.ToUpper().Contains("CONFIRM") || text.text.ToUpper().Contains("SUBMIT")))
                        {
                            diceSelectionUI.submitButton = btn;
                            break;
                        }
                    }
                }

                if (diceSelectionUI.submitButton != null)
                {
                    UnityAction handler = OnDiceSelectionConfirmed;
                    diceSelectionUI.submitButton.onClick.AddListener(handler);
                    buttonHandlers.Add((diceSelectionUI.submitButton, handler));
                }
            }
        }

        IEnumerator RetryHookDiceSelectionButton()
        {
            float timeout = 10f;
            while (timeout > 0f && !IsDiceSelectionButtonHooked() && currentRequiredAction == TutorialAction.ConfirmHand)
            {
                ResolveReferences();
                EnsureDiceSelectionButtonHooked();
                
                if (IsDiceSelectionButtonHooked())
                {
                    yield break;
                }
                
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!IsDiceSelectionButtonHooked())
            {
                Debug.LogError("[Tutorial] Failed to hook dice selection button after retries.");
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
            SetNextButtonVisible(step.useNextButton);
            ShowPrompt(step.title, step.message);
            
            // Manage button states (disable non-required buttons)
            ManageButtonStates(step);
            
            // Highlight required elements
            if (step.requiredAction == TutorialAction.LockDice)
            {
                HighlightDiceViews();
            }
            else
            {
                HighlightElement(step.highlightElement);
            }
            
            // Force text update after all layout changes
            if (tutorialText != null)
            {
                // Wait one frame for layout to settle, then update text
                StartCoroutine(UpdateTextAfterLayout());
            }

            if (step.waitForAction)
            {
                currentRequiredAction = step.requiredAction;

                if (step.requiredAction == TutorialAction.ConfirmHand)
                {
                    // Ensure submit button is hooked when this step starts
                    EnsureDiceSelectionButtonHooked();
                    // If still not hooked, start a coroutine to retry
                    if (!IsDiceSelectionButtonHooked())
                    {
                        StartCoroutine(RetryHookDiceSelectionButton());
                    }
                    // Re-collect buttons after backpack opens (dice buttons are created dynamically)
                    StartCoroutine(RefreshButtonsAfterBackpackOpens());
                }
                else if (step.requiredAction == TutorialAction.RollDice)
                {
                    // Ensure roll button is hooked and active
                    EnsureRollButtonHooked();
                    // If still not hooked, start a coroutine to retry
                    if (!IsRollButtonHooked())
                    {
                        StartCoroutine(RetryHookRollButton());
                    }
                }
                else if (step.requiredAction == TutorialAction.LockDice)
                {
                    lockStepCompleted = false;
                    // Subscribe to dice lock changed event
                    HookDiceLockEvent();
                    // Wait for dice views to be ready (rolled and interactable)
                    StartCoroutine(WaitForDiceViewsReadyAndAttach());
                    // Re-collect buttons after dice views are spawned
                    StartCoroutine(RefreshButtonsAfterDiceSpawn());
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
                
                // Special handling for cooldown step - show backpack
                if (step.title == "Cooldown System")
                {
                    ShowBackpackForCooldownExplanation();
                }
                
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
                tutorialText.enableWordWrapping = true;
                tutorialText.overflowMode = TextOverflowModes.Overflow;
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
            float leftPadding = textPadding.x;
            float topPadding = textPadding.y;
            float bottomPadding = textPadding.y;
            
            textRect.offsetMin = new Vector2(leftPadding, bottomPadding);
            textRect.offsetMax = new Vector2(-rightPadding, -topPadding);
            
            // Force text to recalculate layout after padding change
            if (tutorialText != null)
            {
                tutorialText.ForceMeshUpdate();
            }
        }

        void NormalizeTextRect()
        {
            if (textRect == null) return;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0f, 1f);
            // Leave padding to be set by UpdateTextPadding based on button state
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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
                    if (tutorialText != null)
                    {
                        tutorialText.alignment = TextAlignmentOptions.TopLeft;
                        tutorialText.enableWordWrapping = true;
                        tutorialText.overflowMode = TextOverflowModes.Overflow;
                    }
                    UpdateTextRectForLayout();
                    break;

                case StepLayout.ComboInfo:
                    promptRect.anchorMin = promptRect.anchorMax = comboPromptAnchor;
                    promptRect.pivot = comboPromptPivot;
                    promptRect.sizeDelta = comboPromptSize;
                    promptRect.anchoredPosition = comboPromptOffset;
                    if (tutorialText != null)
                    {
                        tutorialText.alignment = TextAlignmentOptions.Center;
                        tutorialText.enableWordWrapping = true;
                        tutorialText.overflowMode = TextOverflowModes.Overflow;
                    }
                    UpdateTextRectForLayout();
                    break;

                case StepLayout.CenterBelow:
                    promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
                    promptRect.pivot = new Vector2(0.5f, 0.5f);
                    promptRect.sizeDelta = new Vector2(700f, 240f);
                    promptRect.anchoredPosition = new Vector2(0f, -140f);
                    if (tutorialText != null)
                    {
                        tutorialText.alignment = TextAlignmentOptions.Center;
                        tutorialText.enableWordWrapping = true;
                        tutorialText.overflowMode = TextOverflowModes.Overflow;
                    }
                    UpdateTextRectForLayout();
                    break;

                case StepLayout.IntroCenter:
                default:
                    promptRect.anchorMin = promptRect.anchorMax = introPromptAnchor;
                    promptRect.pivot = introPromptPivot;
                    promptRect.sizeDelta = introPromptSize;
                    promptRect.anchoredPosition = introPromptOffset;
                    if (tutorialText != null)
                    {
                        tutorialText.alignment = TextAlignmentOptions.Center;
                        tutorialText.enableWordWrapping = true;
                        tutorialText.overflowMode = TextOverflowModes.Overflow;
                    }
                    UpdateTextRectForLayout();
                    break;
            }
            
            // Force text to recalculate after layout change
            if (tutorialText != null)
            {
                tutorialText.ForceMeshUpdate();
            }
        }

        void UpdateTextRectForLayout()
        {
            if (textRect == null) return;
            // Ensure text rect fills the panel properly
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0f, 1f);
        }
        
        IEnumerator UpdateTextAfterLayout()
        {
            yield return null; // Wait one frame for layout to settle
            if (tutorialText != null)
            {
                tutorialText.ForceMeshUpdate();
            }
        }

        void CollectAllButtons()
        {
            allButtons.Clear();
            Button[] buttons = FindObjectsOfType<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn != null && !allButtons.Contains(btn))
                {
                    allButtons.Add(btn);
                    originalButtonStates[btn] = btn.interactable;
                }
            }
        }

        void HighlightElement(GameObject element)
        {
            ClearHighlights();
            
            if (element == null) return;

            highlightedElements.Add(element);
            Coroutine highlightCoroutine = StartCoroutine(PulseHighlight(element));
            highlightCoroutines.Add(highlightCoroutine);
        }

        void HighlightDiceViews()
        {
            ClearHighlights();
            
            DiceView[] views = null;
            if (diceRowParent != null)
            {
                views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            }
            
            if (views == null || views.Length == 0)
            {
                views = FindObjectsOfType<DiceView>(true);
            }
            
            if (views != null && views.Length > 0)
            {
                foreach (var view in views)
                {
                    if (view != null && view.model != null && view.model.tier != DiceTier.Filler)
                    {
                        highlightedElements.Add(view.gameObject);
                        Coroutine highlightCoroutine = StartCoroutine(PulseHighlight(view.gameObject));
                        highlightCoroutines.Add(highlightCoroutine);
                    }
                }
            }
        }

        IEnumerator PulseHighlight(GameObject element)
        {
            if (element == null) yield break;
            
            RectTransform rect = element.GetComponent<RectTransform>();
            if (rect == null) yield break;
            
            Vector3 originalScale = rect.localScale;
            float pulseSpeed = 2f;
            float scaleAmount = 0.15f;
            
            while (highlightedElements.Contains(element))
            {
                float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                float scale = 1f + (t * scaleAmount);
                rect.localScale = originalScale * scale;
                yield return null;
            }
            
            rect.localScale = originalScale;
        }

        void ClearHighlights()
        {
            foreach (var coroutine in highlightCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            highlightCoroutines.Clear();
            
            // Reset scales
            foreach (var element in highlightedElements)
            {
                if (element != null)
                {
                    RectTransform rect = element.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.localScale = Vector3.one;
                    }
                }
            }
            
            highlightedElements.Clear();
        }

        void ManageButtonStates(TutorialStep step)
        {
            // Skip button locking for "Build Your Hand" step - allow all buttons
            if (step.title == "Build Your Hand")
            {
                RestoreAllButtons();
                return;
            }
            
            // Determine which buttons should be enabled for this step
            HashSet<Button> allowedButtons = new HashSet<Button>();
            
            // Always allow tutorial UI buttons
            if (tutorialContinueButton != null) allowedButtons.Add(tutorialContinueButton);
            if (skipTutorialButton != null) allowedButtons.Add(skipTutorialButton);
            
            // Combo rules button should always remain usable
            GameObject comboPrefBtn = GameObject.Find("ComboPreferenceButton");
            if (comboPrefBtn != null)
            {
                Button comboBtn = comboPrefBtn.GetComponent<Button>();
                if (comboBtn != null) allowedButtons.Add(comboBtn);
            }
            
            // Step-specific buttons
            switch (step.requiredAction)
            {
                case TutorialAction.ConfirmHand:
                    // Allow open backpack button and confirm button in dice selection UI
                    if (openBackpackButton != null) allowedButtons.Add(openBackpackButton);
                    if (diceSelectionUI != null && diceSelectionUI.submitButton != null)
                    {
                        allowedButtons.Add(diceSelectionUI.submitButton);
                    }
                    // Allow dice buttons in backpack
                    if (diceSelectionUI != null)
                    {
                        DiceButton[] diceButtons = diceSelectionUI.GetComponentsInChildren<DiceButton>(true);
                        foreach (var db in diceButtons)
                        {
                            if (db != null && db.button != null)
                            {
                                allowedButtons.Add(db.button);
                            }
                        }
                    }
                    break;
                    
                case TutorialAction.LockDice:
                    // Allow dice views to be clicked
                    DiceView[] views = null;
                    if (diceRowParent != null)
                    {
                        views = diceRowParent.GetComponentsInChildren<DiceView>(true);
                    }
                    if (views == null || views.Length == 0)
                    {
                        views = FindObjectsOfType<DiceView>(true);
                    }
                    if (views != null)
                    {
                        foreach (var view in views)
                        {
                            if (view != null)
                            {
                                Button diceBtn = view.GetComponent<Button>();
                                if (diceBtn != null) allowedButtons.Add(diceBtn);
                                if (view.lockButton != null) allowedButtons.Add(view.lockButton);
                            }
                        }
                    }
                    break;
                    
                case TutorialAction.RollDice:
                    if (rollButton != null) allowedButtons.Add(rollButton);
                    break;
                    
                case TutorialAction.SecondRoll:
                    if (rollButton != null) allowedButtons.Add(rollButton);
                    // Also allow dice locking during second roll
                    views = null;
                    if (diceRowParent != null)
                    {
                        views = diceRowParent.GetComponentsInChildren<DiceView>(true);
                    }
                    if (views == null || views.Length == 0)
                    {
                        views = FindObjectsOfType<DiceView>(true);
                    }
                    if (views != null)
                    {
                        foreach (var view in views)
                        {
                            if (view != null)
                            {
                                Button diceBtn = view.GetComponent<Button>();
                                if (diceBtn != null) allowedButtons.Add(diceBtn);
                                if (view.lockButton != null) allowedButtons.Add(view.lockButton);
                            }
                        }
                    }
                    break;
                    
                case TutorialAction.SubmitHand:
                    if (submitComboButton != null) allowedButtons.Add(submitComboButton);
                    // Allow dice locking before submitting
                    views = null;
                    if (diceRowParent != null)
                    {
                        views = diceRowParent.GetComponentsInChildren<DiceView>(true);
                    }
                    if (views == null || views.Length == 0)
                    {
                        views = FindObjectsOfType<DiceView>(true);
                    }
                    if (views != null)
                    {
                        foreach (var view in views)
                        {
                            if (view != null)
                            {
                                Button diceBtn = view.GetComponent<Button>();
                                if (diceBtn != null) allowedButtons.Add(diceBtn);
                                if (view.lockButton != null) allowedButtons.Add(view.lockButton);
                            }
                        }
                    }
                    break;
                    
                case TutorialAction.None:
                    // For intro/outro steps, only allow Next button (plus combo rules which was already added)
                    break;
            }
            
            // Disable all buttons except allowed ones
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                
                if (allowedButtons.Contains(btn))
                {
                    // Restore original interactable state if it was saved
                    if (originalButtonStates.ContainsKey(btn))
                    {
                        btn.interactable = originalButtonStates[btn];
                    }
                    else
                    {
                        btn.interactable = true;
                    }
                }
                else
                {
                    // Save current state if not already saved
                    if (!originalButtonStates.ContainsKey(btn))
                    {
                        originalButtonStates[btn] = btn.interactable;
                    }
                    btn.interactable = false;
                }
            }
            
            // Also handle buttons that might not be in allButtons yet (dynamically created)
            // Find and disable any other buttons in the scene
            Button[] allSceneButtons = FindObjectsOfType<Button>(true);
            foreach (var btn in allSceneButtons)
            {
                if (btn == null || allButtons.Contains(btn)) continue;
                
                if (allowedButtons.Contains(btn))
                {
                    btn.interactable = true;
                }
                else
                {
                    // Save state and disable
                    if (!originalButtonStates.ContainsKey(btn))
                    {
                        originalButtonStates[btn] = btn.interactable;
                    }
                    btn.interactable = false;
                }
            }
        }

        void RestoreAllButtons()
        {
            foreach (var kvp in originalButtonStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.interactable = kvp.Value;
                }
            }
            originalButtonStates.Clear();
        }

        void ShowBackpackForCooldownExplanation()
        {
            // Show backpack in view-only mode to display cooldown dice
            if (backpackManager != null)
            {
                backpackManager.ShowBackpack(BackpackMode.ViewOnly);
            }
        }

        void OnTutorialContinue()
        {
            HidePrompt();
            ClearHighlights();
            RestoreAllButtons();
            
            // Check if this is the last step (Tutorial Complete)
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                var currentStep = tutorialSteps[currentStepIndex];
                if (currentStep.title == "Tutorial Complete!")
                {
                    // Last step: transition to RewardScene
                    CompleteTutorialAndGoToReward();
                    return;
                }
            }
            
            StartTutorialStep(currentStepIndex + 1);
        }

        #endregion

        #region Gameplay Hooks

        void OnDiceSelectionConfirmed()
        {
            if (currentRequiredAction == TutorialAction.ConfirmHand)
            {
                RegisterActionCompletion(TutorialAction.ConfirmHand);
                StartCoroutine(WaitForDiceViewsAndAttach());
            }
        }

        void OnRollClicked()
        {
            if (!isTutorialActive) return;
            
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

        IEnumerator RetryHookRollButton()
        {
            float timeout = 5f;
            while (timeout > 0f && !IsRollButtonHooked() && currentRequiredAction == TutorialAction.RollDice)
            {
                ResolveReferences();
                EnsureRollButtonHooked();
                
                if (IsRollButtonHooked())
                {
                    yield break;
                }
                
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!IsRollButtonHooked())
            {
                Debug.LogError("[Tutorial] Failed to hook roll button after retries.");
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

            // Wait until dice views exist or timeout after 5 seconds
            float timeout = 5f;
            while (timeout > 0f && !HasDiceViews())
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (HasDiceViews())
            {
                AttachDiceViewListeners();
            }
            else
            {
                Debug.LogError("[Tutorial] WaitForDiceViewsAndAttach: Timeout - no dice views found!");
            }
        }

        IEnumerator WaitForDiceViewsReadyAndAttach()
        {
            // Wait for auto-roll animation to complete (0.5s + buffer)
            yield return new WaitForSeconds(0.7f);

            // Wait until dice views exist and are ready (have rolled values)
            float timeout = 5f;
            while (timeout > 0f)
            {
                if (HasDiceViews() && AreDiceViewsReady())
                {
                    AttachDiceViewListeners();
                    yield break;
                }
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (HasDiceViews())
            {
                AttachDiceViewListeners();
            }
            else
            {
                Debug.LogError("[Tutorial] WaitForDiceViewsReadyAndAttach: Timeout - no dice views found!");
            }
        }

        bool AreDiceViewsReady()
        {
            // Find all DiceView objects
            DiceView[] views = null;
            
            if (diceRowParent != null)
            {
                views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            }
            
            // Fallback: find all DiceView objects in the scene
            if (views == null || views.Length == 0)
            {
                views = FindObjectsOfType<DiceView>(true);
            }
            
            if (views == null || views.Length == 0) return false;
            
            // Check if at least one non-filler dice has been rolled
            foreach (var view in views)
            {
                if (view != null && view.model != null && view.model.tier != DiceTier.Filler)
                {
                    if (view.model.lastRollValue > 0)
                    {
                        return true; // At least one dice is rolled
                    }
                }
            }
            
            return false;
        }

        bool HasDiceViews()
        {
            // Try to resolve diceRowParent if null
            if (diceRowParent == null)
            {
                ResolveReferences();
            }
            
            if (diceRowParent != null)
            {
                DiceView[] views = diceRowParent.GetComponentsInChildren<DiceView>(true);
                if (views.Length > 0) return true;
            }
            
            DiceView[] allViews = FindObjectsOfType<DiceView>(true);
            return allViews.Length > 0;
        }

        void AttachDiceViewListeners()
        {
            CleanupDiceViewListeners();
            
            // Find all DiceView objects (try multiple methods)
            DiceView[] views = null;
            
            if (diceRowParent != null)
            {
                views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            }
            
            // Fallback: find all DiceView objects in the scene
            if (views == null || views.Length == 0)
            {
                views = FindObjectsOfType<DiceView>(true);
            }
            
            if (views == null || views.Length == 0) return;
            
            foreach (var view in views)
            {
                if (view == null || (view.model != null && view.model.tier == DiceTier.Filler)) continue;
                
                Button diceButton = view.GetComponent<Button>();
                if (diceButton != null)
                {
                    UnityAction handler = () => OnDiceLockClicked(view);
                    diceButton.onClick.AddListener(handler);
                    diceLockHandlers.Add((diceButton, handler));
                }
                else if (view.lockButton != null)
                {
                    UnityAction handler = () => OnDiceLockClicked(view);
                    view.lockButton.onClick.AddListener(handler);
                    diceLockHandlers.Add((view.lockButton, handler));
                }
            }
        }

        void HookDiceLockEvent()
        {
            UnhookDiceLockEvent();
            diceLockChangedHandler = OnDiceLockStateChanged;
            DiceView.OnDiceLockChanged += diceLockChangedHandler;
        }

        void UnhookDiceLockEvent()
        {
            if (diceLockChangedHandler != null)
            {
                DiceView.OnDiceLockChanged -= diceLockChangedHandler;
                diceLockChangedHandler = null;
            }
        }

        void OnDiceLockStateChanged()
        {
            if (!isTutorialActive || lockStepCompleted) return;
            if (currentRequiredAction != TutorialAction.LockDice) return;
            
            DiceView[] views = null;
            if (diceRowParent != null)
            {
                views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            }
            
            if (views == null || views.Length == 0)
            {
                views = FindObjectsOfType<DiceView>(true);
            }
            
            int lockedCount = CountLockedDice(views);
            if (lockedCount >= Mathf.Max(1, requiredLockedDiceCount))
            {
                lockStepCompleted = true;
                RegisterActionCompletion(TutorialAction.LockDice);
                UnhookDiceLockEvent();
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
            if (currentRequiredAction != TutorialAction.LockDice) return;
            
            StartCoroutine(CheckLockStateNextFrame(view));
        }

        IEnumerator CheckLockStateNextFrame(DiceView view)
        {
            yield return null;
            
            if (view == null || view.model == null) yield break;
            
            DiceView[] views = null;
            if (diceRowParent != null)
            {
                views = diceRowParent.GetComponentsInChildren<DiceView>(true);
            }
            if (views == null || views.Length == 0)
            {
                views = FindObjectsOfType<DiceView>(true);
            }
            
            int lockedCount = CountLockedDice(views);
            if (lockedCount >= Mathf.Max(1, requiredLockedDiceCount))
            {
                lockStepCompleted = true;
                RegisterActionCompletion(TutorialAction.LockDice);
                CleanupDiceViewListeners();
            }
        }
        
        int CountLockedDice(DiceView[] views)
        {
            if (views == null) return 0;
            int count = 0;
            foreach (var v in views)
            {
                if (v == null || v.model == null) continue;
                if (v.model.tier == DiceTier.Filler) continue;
                if (v.model.isLocked) count++;
            }
            return count;
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
            // The prompt should be visible when this step started
            // Strategy: Wait for animation to complete (if running), then wait 3 seconds for reading
            
            // Step 1: Wait for animation to start (if not already running)
            if (scoreAnimator != null && !scoreAnimator.IsAnimating)
            {
                // Give animation a chance to start (up to 2 seconds)
                float startCheckTime = 0f;
                float startCheckMax = 2f;
                while (!scoreAnimator.IsAnimating && startCheckTime < startCheckMax)
                {
                    startCheckTime += Time.deltaTime;
                    yield return null;
                }
            }
            
            // Step 2: Wait for animation to complete (if it's running)
            if (scoreAnimator != null && scoreAnimator.IsAnimating)
            {
                float animationWaitTime = 0f;
                float animationMaxWait = 25f; // Safety timeout
                while (scoreAnimator.IsAnimating && animationWaitTime < animationMaxWait)
                {
                    animationWaitTime += Time.deltaTime;
                    yield return null;
                }
            }
            
            // Step 3: Animation is complete (or was never running)
            // Wait exactly 3 seconds so player can read the score breakdown message
            // The prompt should still be visible at this point
            yield return new WaitForSeconds(3f);
            
            // Step 4: Now advance to next step (this will hide the prompt)
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
            ClearHighlights();
            RestoreAllButtons();
            
            // Special handling: Wait a bit after score animation before showing cooldown step
            if (action == TutorialAction.ScoreAnimationComplete)
            {
                StartCoroutine(DelayedAdvanceToCooldownStep());
            }
            else
            {
                StartTutorialStep(currentStepIndex + 1);
            }
        }

        IEnumerator RefreshButtonsAfterDiceSpawn()
        {
            yield return new WaitForSeconds(0.5f);
            CollectAllButtons();
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                ManageButtonStates(tutorialSteps[currentStepIndex]);
            }
        }

        IEnumerator RefreshButtonsAfterBackpackOpens()
        {
            // Wait for backpack to open and dice buttons to be created
            yield return new WaitForSeconds(0.3f);
            CollectAllButtons();
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                ManageButtonStates(tutorialSteps[currentStepIndex]);
            }
        }

        IEnumerator DelayedAdvanceToCooldownStep()
        {
            // Wait for score to be fully added and UI to update, plus extra time to view the score
            yield return new WaitForSeconds(2f);
            StartTutorialStep(currentStepIndex + 1);
        }

        void CompleteTutorial()
        {
            if (!isTutorialActive) return;

            isTutorialActive = false;
            
            // Clean up tutorial state
            HidePrompt();
            ClearHighlights();
            RestoreAllButtons();
            CleanupDiceViewListeners();
            UnhookDiceLockEvent();
            
            // Reset tutorial-specific flags
            currentRequiredAction = TutorialAction.None;
            lockStepCompleted = false;
            awaitingSecondRoll = false;
            
            // Save tutorial completion
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();

            // Transition from tutorial (Level 0) to normal game (Level 1) without scene change
            if (battleController != null)
            {
                battleController.CompleteTutorialAndStartLevel1();
                Debug.Log("[TutorialController] Tutorial completed - transitioning to Level 1");
                return;
            }
            
            // Fallback: if battleController is null, reload scene
            Debug.LogWarning("[TutorialController] BattleController not found - reloading scene");
            BattleController.IsTutorialMode = false;
            if (RunLoader.Instance != null)
            {
                RunLoader.Instance.StartRun();
            }
            else
            {
                SceneManager.LoadScene("BattleScene");
            }
        }

        /// <summary>
        /// Complete tutorial and transition to RewardScene, then return to Level 1
        /// </summary>
        void CompleteTutorialAndGoToReward()
        {
            if (!isTutorialActive) return;

            isTutorialActive = false;
            
            // Clean up tutorial state
            HidePrompt();
            ClearHighlights();
            RestoreAllButtons();
            CleanupDiceViewListeners();
            UnhookDiceLockEvent();
            
            // Reset tutorial-specific flags
            currentRequiredAction = TutorialAction.None;
            lockStepCompleted = false;
            awaitingSecondRoll = false;
            
            // Save tutorial completion
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();

            // Prepare Level 1 state for when returning from RewardScene
            if (battleController != null)
            {
                // Set up state for Level 1 after reward
                BattleController.PendingLevel = 1;
                BattleController.PendingTargetScore = battleController.baseTargetScore;
                BattleController.ContinuingFromReward = true;
                BattleController.IsTutorialMode = false; // No longer in tutorial mode
                
                Debug.Log("[TutorialController] Tutorial completed - transitioning to RewardScene, then Level 1");
                
                // Transition to RewardScene
                SceneManager.LoadScene("RewardScene");
            }
            else
            {
                // Fallback: if battleController is null, reload scene
                Debug.LogWarning("[TutorialController] BattleController not found - reloading scene");
                BattleController.IsTutorialMode = false;
                BattleController.PendingLevel = 1;
                BattleController.PendingTargetScore = 200;
                BattleController.ContinuingFromReward = true;
                SceneManager.LoadScene("RewardScene");
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


