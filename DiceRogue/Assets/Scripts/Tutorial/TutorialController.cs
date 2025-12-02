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
        [Header("Relic Display Reference")]
        public GameObject relicDisplayPanel; // Relic display panel in top right corner (RelicDisplayPanel GameObject)
        
        [Header("Right Panel Reference")]
        public GameObject rightInfoPanel; // Right panel showing combos, cast, and roll counts (RightInfoPanel GameObject)
        
        [Header("Shop References (for Shop Tutorial Step)")]
        public ShopManager shopManager;

        [Header("Gameplay Buttons")]
        public Button openBackpackButton;
        public Button rollButton;
        public Button submitComboButton;

        [Header("Tutorial UI")]
        public GameObject tutorialPromptPanel;
        public TMP_Text tutorialText;
        public Button tutorialContinueButton;
        public Button skipTutorialButton;
        
        [Header("Tutorial Text Settings")]
        [Tooltip("Base font size for tutorial text at reference resolution (1920x1080). Will be scaled based on Canvas scale factor.")]
        public float tutorialFontSize = 24f;
        
        [Tooltip("Reference resolution for font size calculation (should match BattleScene Canvas reference resolution)")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Header("Layout")]
        public bool configurePromptLayout = true;
        public Vector2 promptSize = new Vector2(640f, 240f);
        public Vector2 promptOffset = Vector2.zero;
        public Vector2 promptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 promptPivot = new Vector2(0.5f, 0.5f);

        [Header("Action Prompt Layout (left side)")]
        public Vector2 actionPromptSize = new Vector2(500f, 340f);
        public Vector2 actionPromptOffset = new Vector2(-80.0f, -40.0f);
        public Vector2 actionPromptAnchor = new Vector2(0.05f, 0.65f);
        public Vector2 actionPromptPivot = new Vector2(0f, 0.5f);

        public Vector2 introPromptSize = new Vector2(640f, 240f);
        public Vector2 introPromptOffset = Vector2.zero;
        public Vector2 introPromptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 introPromptPivot = new Vector2(0.5f, 0.5f);
        
        [Header("Outro Prompt Layout (Tutorial Complete)")]
        public Vector2 outroPromptSize = new Vector2(1080f, 480f);
        public Vector2 outroPromptOffset = Vector2.zero;
        public Vector2 outroPromptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 outroPromptPivot = new Vector2(0.5f, 0.5f);
        [Tooltip("Size for the Next button in Tutorial Complete step")]
        public Vector2 outroNextButtonSize = new Vector2(180f, 75f);

        public Vector2 textPadding = new Vector2(24f, 24f);
        public float textRightPaddingWithButton = 150f;
        [Tooltip("Right padding for text when Next button is shown in Tutorial Complete step (larger button needs more padding)")]
        public float textRightPaddingWithOutroButton = 220f;

        [Header("Combo Prompt Layout (under dice)")]
        public Vector2 comboPromptSize = new Vector2(700f, 240f);
        public Vector2 comboPromptOffset = new Vector2(0f, -190f);
        public Vector2 comboPromptAnchor = new Vector2(0.5f, 0.5f);
        public Vector2 comboPromptPivot = new Vector2(0.5f, 0.5f);

        private readonly List<TutorialStep> tutorialSteps = new();
        private readonly List<(Button button, UnityAction handler)> buttonHandlers = new();
        private readonly List<(Button button, UnityAction handler)> diceLockHandlers = new();
        private readonly List<(ShopItemUI shopItem, System.Func<bool> originalCallback)> shopItemHandlers = new();
        private Button comboRulesButton;
        private GameObject comboRulesButtonGO;
        private UnityAction comboRulesButtonHandler;
        private bool comboRulesButtonClicked;

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
        
        // Track dice views modified during Relics step for restoration
        private readonly List<(DiceView diceView, CanvasGroup canvasGroup, bool wasAdded)> modifiedDiceViews = new();
        
        private static TutorialController instance;

        void Awake()
        {
            // Singleton pattern - only one TutorialController should exist
            if (instance != null && instance != this)
            {
                Debug.Log("[TutorialController] Duplicate TutorialController found, destroying duplicate");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            // Persist across scenes so tutorial can continue in ShopScene
            DontDestroyOnLoad(gameObject);
            Debug.Log("[TutorialController] Awake() - TutorialController set to persist across scenes");
            
            // Subscribe to scene loaded event to handle ShopScene transition
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[TutorialController] OnSceneLoaded() called for scene: {scene.name}");
            
            if (scene.name == "ShopScene")
            {
                // Wait one frame for scene to fully initialize
                StartCoroutine(InitializeShopSceneTutorial());
            }
        }
        
        IEnumerator InitializeShopSceneTutorial()
        {
            yield return null; // Wait one frame
            
            var stateManager = GameStateManager.Instance;
            bool isTutorialMode = stateManager != null && stateManager.State.IsTutorialMode;
            Debug.Log($"[TutorialController] In ShopScene (OnSceneLoaded), IsTutorialMode: {isTutorialMode}");
            
            if (isTutorialMode)
            {
                ResolveReferences();
                
                // Find shop manager
                if (shopManager == null)
                {
                    shopManager = FindObjectOfType<ShopManager>();
                    Debug.Log($"[TutorialController] ShopManager found: {shopManager != null}");
                }
                
                // Initialize tutorial in ShopScene
                BuildSteps();
                Debug.Log($"[TutorialController] Built {tutorialSteps.Count} tutorial steps");
                
                InitializeTutorialUI();
                Debug.Log($"[TutorialController] TutorialPromptPanel: {tutorialPromptPanel != null}, TutorialText: {tutorialText != null}, ContinueButton: {tutorialContinueButton != null}");
                
                if (tutorialPromptPanel == null)
                {
                    Debug.LogError("[TutorialController] TutorialPromptPanel is NULL! Make sure it exists in ShopScene with exact name 'TutorialPromptPanel'");
                }
                else
                {
                    // Enable the panel if it's disabled
                    if (!tutorialPromptPanel.activeSelf)
                    {
                        Debug.Log("[TutorialController] TutorialPromptPanel was disabled, enabling it now");
                        tutorialPromptPanel.SetActive(true);
                    }
                }
                
                if (tutorialText == null)
                {
                    Debug.LogError("[TutorialController] TutorialText is NULL! Make sure it exists as a child of TutorialPromptPanel");
                }
                
                if (tutorialContinueButton == null)
                {
                    Debug.LogError("[TutorialController] TutorialContinueButton is NULL! Make sure NextButton exists in ShopScene");
                }
                else if (tutorialContinueButton.gameObject != null && !tutorialContinueButton.gameObject.activeSelf)
                {
                    Debug.Log("[TutorialController] NextButton was disabled, enabling it now");
                    tutorialContinueButton.gameObject.SetActive(true);
                }
                
                ConfigurePromptLayoutIfNeeded();
                CollectAllButtons();
                Debug.Log("[TutorialController] Starting shop tutorial step (index 10)");
                StartTutorialStep(10); // Start at shop tutorial step (index 10)
            }
        }

        void Start()
        {
            ResolveReferences();
            
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[TutorialController] Start() called in scene: {currentScene}, GameObject active: {gameObject.activeSelf}, enabled: {enabled}");
            
            // Check if we're in ShopScene and continuing tutorial from battle
            if (currentScene == "ShopScene")
            {
                var stateManager = GameStateManager.Instance;
                bool isTutorialMode = stateManager != null && stateManager.State.IsTutorialMode;
                Debug.Log($"[TutorialController] In ShopScene, IsTutorialMode: {isTutorialMode}, StateManager: {stateManager != null}");
                
                if (isTutorialMode)
                {
                    // Find shop manager
                    if (shopManager == null)
                    {
                        shopManager = FindObjectOfType<ShopManager>();
                        Debug.Log($"[TutorialController] ShopManager found: {shopManager != null}");
                    }
                    
                    // Initialize tutorial in ShopScene
                    BuildSteps();
                    Debug.Log($"[TutorialController] Built {tutorialSteps.Count} tutorial steps");
                    
                    InitializeTutorialUI();
                    Debug.Log($"[TutorialController] TutorialPromptPanel: {tutorialPromptPanel != null}, TutorialText: {tutorialText != null}, ContinueButton: {tutorialContinueButton != null}");
                    
                    if (tutorialPromptPanel == null)
                    {
                        Debug.LogError("[TutorialController] TutorialPromptPanel is NULL! Make sure it exists in ShopScene with exact name 'TutorialPromptPanel'");
                    }
                    else
                    {
                        // Enable the panel if it's disabled
                        if (!tutorialPromptPanel.activeSelf)
                        {
                            Debug.Log("[TutorialController] TutorialPromptPanel was disabled, enabling it now");
                            tutorialPromptPanel.SetActive(true);
                        }
                    }
                    
                    if (tutorialText == null)
                    {
                        Debug.LogError("[TutorialController] TutorialText is NULL! Make sure it exists as a child of TutorialPromptPanel");
                    }
                    
                    if (tutorialContinueButton == null)
                    {
                        Debug.LogError("[TutorialController] TutorialContinueButton is NULL! Make sure NextButton exists in ShopScene");
                    }
                    else if (tutorialContinueButton.gameObject != null && !tutorialContinueButton.gameObject.activeSelf)
                    {
                        Debug.Log("[TutorialController] NextButton was disabled, enabling it now");
                        tutorialContinueButton.gameObject.SetActive(true);
                    }
                    
                    ConfigurePromptLayoutIfNeeded();
                    CollectAllButtons();
                    Debug.Log("[TutorialController] Starting shop tutorial step (index 8)");
                    StartTutorialStep(8); // Start at shop tutorial step (index 8)
                    return;
                }
                else
                {
                    // Not in tutorial mode, ensure tutorial UI is hidden and destroy this controller
                    Debug.Log("[TutorialController] Not in tutorial mode, destroying controller");
                    if (tutorialPromptPanel != null)
                    {
                        tutorialPromptPanel.SetActive(false);
                    }
                    Destroy(gameObject);
                    return;
                }
            }
            
            // Normal tutorial start in BattleScene
            Debug.Log("[TutorialController] Starting tutorial in BattleScene");
            BuildSteps();
            InitializeTutorialUI();
            ConfigurePromptLayoutIfNeeded();
            HookGlobalListeners();
            CollectAllButtons();
            StartTutorialStep(0);
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnhookGlobalListeners();
            CleanupDiceViewListeners();
            UnhookDiceLockEvent();
            UnhookShopPurchaseEvents();
            UnhookComboRulesButton();
            ClearHighlights();
            RestoreAllButtons();
        }

        #region Setup

        void ResolveReferences()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[TutorialController] ResolveReferences() in scene: {currentScene}");
            
            if (currentScene == "ShopScene")
            {
                if (shopManager == null)
                {
                    shopManager = FindObjectOfType<ShopManager>();
                    Debug.Log($"[TutorialController] Resolved ShopManager: {shopManager != null}");
                }
                
                // Find tutorial UI elements in ShopScene - search in active AND inactive objects
                if (tutorialPromptPanel == null)
                {
                    tutorialPromptPanel = GameObject.Find("TutorialPromptPanel");
                    if (tutorialPromptPanel == null)
                    {
                        // Try finding in all GameObjects including inactive
                        // Last resort: search by name in scene
                        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
                        foreach (var t in allTransforms)
                        {
                            if (t.name == "TutorialPromptPanel")
                            {
                                tutorialPromptPanel = t.gameObject;
                                break;
                            }
                        }
                    }
                    Debug.Log($"[TutorialController] Resolved TutorialPromptPanel: {tutorialPromptPanel != null}");
                }
                
                if (tutorialText == null && tutorialPromptPanel != null)
                {
                    tutorialText = tutorialPromptPanel.GetComponentInChildren<TMP_Text>(true); // Include inactive
                    Debug.Log($"[TutorialController] Resolved TutorialText: {tutorialText != null}");
                }
                
                if (tutorialContinueButton == null)
                {
                    var nextButton = GameObject.Find("NextButton");
                    if (nextButton == null)
                    {
                        // Search in all objects including inactive
                        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
                        foreach (var btn in allButtons)
                        {
                            if (btn.name == "NextButton")
                            {
                                nextButton = btn.gameObject;
                                break;
                            }
                        }
                    }
                    if (nextButton != null) tutorialContinueButton = nextButton.GetComponent<Button>();
                    Debug.Log($"[TutorialController] Resolved TutorialContinueButton: {tutorialContinueButton != null}");
                }
            }
            
            if (battleController == null && currentScene == "BattleScene")
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
                
                // Find relic display panel if not set
                if (relicDisplayPanel == null && battleController.relicDisplay != null)
                {
                    relicDisplayPanel = battleController.relicDisplay.gameObject;
                }
            }
            
            // Fallback: try to find RelicDisplayPanel by name
            if (relicDisplayPanel == null)
            {
                GameObject foundPanel = GameObject.Find("RelicDisplayPanel");
                if (foundPanel != null) relicDisplayPanel = foundPanel;
            }
            
            // Find right info panel if not set
            if (rightInfoPanel == null)
            {
                GameObject foundPanel = GameObject.Find("RightInfoPanel");
                if (foundPanel != null) rightInfoPanel = foundPanel;
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
                title = "Relics",
                message = "Relics are powerful items that provide permanent bonuses and some have side effects. Hover over them to see their effects!",
                highlightElement = relicDisplayPanel, // Will be resolved dynamically
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.None
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
                title = "Right Panel Info",
                message = "The right panel shows important information. You can see your current combo preview, and how many rolls and casts you have left.",
                highlightElement = rightInfoPanel, // Will be resolved dynamically
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.None
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
                highlightElement = null, // Will be set dynamically to combo rules button
                useNextButton = false, // Initially hidden, shown after combo rules button is clicked
                waitForAction = true,
                layout = StepLayout.ComboInfo,
                requiredAction = TutorialAction.ClickComboRulesButton
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
                title = "Shop Tutorial",
                message = "Welcome to the shop! You can buy dice and relics here with your money. One dice is always free - try buying it!",
                highlightElement = null, // Will be set dynamically for shop items
                useNextButton = false,
                waitForAction = true,
                layout = StepLayout.ActionLeft,
                requiredAction = TutorialAction.BuyDiceInShop
            });

            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "Great job! You've learned the basics. Click Next to claim your reward and start Level 1.",
                useNextButton = true,
                waitForAction = false,
                layout = StepLayout.IntroCenter, // Uses same size as intro step (introPromptSize: 640x240)
                requiredAction = TutorialAction.None
            });
        }

        void InitializeTutorialUI()
        {
            // Always start with prompt hidden - it will be shown when needed
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
                // Only set default layout - ApplyLayoutForStep() will override with step-specific values
                promptRect.anchorMin = promptAnchor;
                promptRect.anchorMax = promptAnchor;
                promptRect.pivot = promptPivot;
                promptRect.sizeDelta = promptSize;
                // Don't set anchoredPosition here - let ApplyLayoutForStep() handle it per step
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
                // This should not happen as last step handles completion via Next button
                CompleteTutorialAndGoToReward();
                return;
            }

            currentStepIndex = stepIndex;
            var step = tutorialSteps[stepIndex];
            
            Debug.Log($"[TutorialController] StartTutorialStep({stepIndex}) - Title: {step.title}, Panel: {tutorialPromptPanel != null}");

            ApplyLayoutForStep(step);
            SetNextButtonVisible(step.useNextButton);
            ShowPrompt(step.title, step.message);
            
            // Double-check prompt is visible after ShowPrompt
            if (tutorialPromptPanel != null && !tutorialPromptPanel.activeSelf)
            {
                Debug.LogWarning("[TutorialController] Prompt panel became inactive after ShowPrompt, reactivating");
                tutorialPromptPanel.SetActive(true);
            }
            
            // Manage button states (disable non-required buttons)
            ManageButtonStates(step);
            
            // Highlight required elements
            if (step.requiredAction == TutorialAction.LockDice)
            {
                HighlightDiceViews();
            }
            else if (step.requiredAction == TutorialAction.BuyDiceInShop)
            {
                // Shop items will be highlighted in the action handler
            }
            else
            {
                // Resolve highlight elements dynamically
                GameObject elementToHighlight = step.highlightElement;
                if (step.title == "Relics")
                {
                    ResolveReferences();
                    // Use the resolved relicDisplayPanel
                    if (relicDisplayPanel != null)
                    {
                        elementToHighlight = relicDisplayPanel;
                        Debug.Log($"[TutorialController] Highlighting relic display panel: {relicDisplayPanel.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialController] RelicDisplayPanel not found for highlighting");
                    }
                }
                else if (step.title == "Right Panel Info")
                {
                    ResolveReferences();
                    // Use the resolved rightInfoPanel
                    if (rightInfoPanel != null)
                    {
                        elementToHighlight = rightInfoPanel;
                        Debug.Log($"[TutorialController] Highlighting right info panel: {rightInfoPanel.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialController] RightInfoPanel not found for highlighting");
                    }
                }
                else if (step.title == "Check Combo Rules")
                {
                    ResolveReferences();
                    // Find and highlight the combo rules button
                    GameObject comboPrefBtn = GameObject.Find("ComboPreferenceButton");
                    if (comboPrefBtn == null && battleController != null && battleController.comboRulePanel != null)
                    {
                        if (battleController.comboRulePanel.comboRuleButton != null)
                        {
                            comboPrefBtn = battleController.comboRulePanel.comboRuleButton.gameObject;
                        }
                    }
                    if (comboPrefBtn != null)
                    {
                        elementToHighlight = comboPrefBtn;
                        Debug.Log($"[TutorialController] Highlighting combo rules button: {comboPrefBtn.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialController] ComboPreferenceButton not found for highlighting");
                    }
                }
                HighlightElement(elementToHighlight);
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
                else if (step.requiredAction == TutorialAction.BuyDiceInShop)
                {
                    // Wait a bit for shop to fully initialize, then hook events and highlight
                    StartCoroutine(InitializeShopTutorialStep());
                }
                else if (step.requiredAction == TutorialAction.ClickComboRulesButton)
                {
                    // Hook into combo rules button click
                    comboRulesButtonClicked = false;
                    HookComboRulesButton();
                    // Ensure Next button is hidden initially
                    SetNextButtonVisible(false);
                }
            }
            else
            {
                currentRequiredAction = TutorialAction.None;
            }
        }

        void ShowPrompt(string title, string message)
        {
            Debug.Log($"[TutorialController] ShowPrompt() called - title: {title}, panel: {tutorialPromptPanel != null}, panel active: {tutorialPromptPanel != null && tutorialPromptPanel.activeSelf}");
            
            if (tutorialPromptPanel != null)
            {
                // Ensure the panel and all its parents are active
                Transform parent = tutorialPromptPanel.transform.parent;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        Debug.Log($"[TutorialController] Parent {parent.name} was inactive, enabling it");
                        parent.gameObject.SetActive(true);
                    }
                    parent = parent.parent;
                }
                
                tutorialPromptPanel.SetActive(true);
                
                // Check if this is the shop tutorial step (declare before use)
                bool isShopTutorialStep = currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count && 
                                         tutorialSteps[currentStepIndex].requiredAction == TutorialAction.BuyDiceInShop;
                
                // Ensure it's visible (check Canvas and CanvasGroup)
                Canvas canvas = tutorialPromptPanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    // Ensure Canvas has high sorting order to appear on top
                    if (canvas.sortingOrder < 999)
                    {
                        canvas.sortingOrder = 999;
                        Debug.Log($"[TutorialController] Set Canvas sorting order to 999");
                    }
                    
                    Debug.Log($"[TutorialController] Canvas found: {canvas.name}, sortingOrder: {canvas.sortingOrder}, active: {canvas.gameObject.activeSelf}");
                }
                else
                {
                    Debug.LogWarning("[TutorialController] No Canvas found in parent hierarchy!");
                }
                
                CanvasGroup canvasGroup = tutorialPromptPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    // For shop tutorial step, don't block raycasts so clicks can pass through to shop buttons
                    canvasGroup.blocksRaycasts = !isShopTutorialStep;
                    Debug.Log($"[TutorialController] CanvasGroup found and configured - alpha: {canvasGroup.alpha}, blocksRaycasts: {canvasGroup.blocksRaycasts} (shop step: {isShopTutorialStep})");
                }
                
                // Also disable all raycast targets in the panel hierarchy for shop tutorial step
                if (isShopTutorialStep)
                {
                    // Disable Image raycast target on panel
                    UnityEngine.UI.Image panelImage = tutorialPromptPanel.GetComponent<UnityEngine.UI.Image>();
                    if (panelImage != null)
                    {
                        panelImage.raycastTarget = false;
                        Debug.Log("[TutorialController] Disabled Image raycast target on TutorialPromptPanel for shop tutorial step");
                    }
                    
                    // Disable raycast on TextMeshPro component (it also has raycastTarget property)
                    if (tutorialText != null)
                    {
                        tutorialText.raycastTarget = false;
                        Debug.Log("[TutorialController] Disabled TextMeshPro raycast target for shop tutorial step");
                    }
                    
                    // Disable raycast on all child UI elements recursively
                    UnityEngine.UI.Graphic[] allGraphics = tutorialPromptPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                    foreach (var graphic in allGraphics)
                    {
                        if (graphic != null)
                        {
                            graphic.raycastTarget = false;
                        }
                    }
                    Debug.Log($"[TutorialController] Disabled raycast targets on {allGraphics.Length} graphics in TutorialPromptPanel for shop tutorial step");
                }
                else
                {
                    // Re-enable raycast targets for non-shop steps (including the Next button)
                    UnityEngine.UI.Image panelImage = tutorialPromptPanel.GetComponent<UnityEngine.UI.Image>();
                    if (panelImage != null)
                    {
                        panelImage.raycastTarget = true;
                    }
                    if (tutorialText != null)
                    {
                        tutorialText.raycastTarget = true;
                    }
                    
                    // Re-enable raycast on all child UI elements recursively (including buttons)
                    UnityEngine.UI.Graphic[] allGraphics = tutorialPromptPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                    foreach (var graphic in allGraphics)
                    {
                        if (graphic != null)
                        {
                            graphic.raycastTarget = true;
                        }
                    }
                    Debug.Log($"[TutorialController] Re-enabled raycast targets on {allGraphics.Length} graphics for non-shop step");
                }
                
                // Force update the RectTransform to ensure it's visible
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tutorialPromptPanel.GetComponent<RectTransform>());
                
                Debug.Log($"[TutorialController] TutorialPromptPanel activated, now active: {tutorialPromptPanel.activeSelf}, enabled: {tutorialPromptPanel.activeInHierarchy}, position: {tutorialPromptPanel.transform.position}");
            }
            else
            {
                Debug.LogError("[TutorialController] Cannot show prompt - TutorialPromptPanel is null!");
            }

            if (tutorialText != null)
            {
                tutorialText.text = $"<b>{title}</b>\n\n{message}";
                tutorialText.enableWordWrapping = true;
                tutorialText.overflowMode = TextOverflowModes.Overflow;
                
                // Calculate font size based on Canvas scaling to ensure consistent visual size across scenes
                // Only adjust font size in ShopScene; BattleScene should always use base font size
                float calculatedFontSize = tutorialFontSize;
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                
                // Only apply scaling adjustment in ShopScene
                if (currentScene == "ShopScene")
                {
                    Canvas canvas = tutorialPromptPanel.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                        if (canvasRect != null)
                        {
                            // Check the Canvas's local scale - if it's not 1.0, we need to compensate
                            Vector3 localScale = canvasRect.localScale;
                            float scaleX = localScale.x;
                            
                            // Adjust font size inversely to compensate for Canvas scaling
                            // If Canvas is scaled to 0.5, font should be 2x larger to appear same size
                            if (Mathf.Abs(scaleX - 1.0f) > 0.01f)
                            {
                                calculatedFontSize = tutorialFontSize / scaleX;
                                Debug.Log($"[TutorialController] ShopScene - Canvas localScale: {scaleX}, adjusting fontSize from {tutorialFontSize} to {calculatedFontSize}");
                            }
                            else
                            {
                                Debug.Log($"[TutorialController] ShopScene - Canvas localScale is 1.0, using base fontSize: {tutorialFontSize}");
                            }
                        }
                    }
                }
                else
                {
                    // BattleScene or other scenes - always use base font size unchanged
                    Debug.Log($"[TutorialController] {currentScene} - Using base fontSize: {tutorialFontSize} (no scaling adjustment)");
                }
                
                // Apply the calculated font size
                if (calculatedFontSize > 0)
                {
                    tutorialText.fontSize = calculatedFontSize;
                    tutorialText.fontSizeMin = calculatedFontSize;
                    tutorialText.fontSizeMax = calculatedFontSize;
                    tutorialText.enableAutoSizing = false; // Disable auto-sizing to use fixed size
                }
                tutorialText.ForceMeshUpdate(); // Force update to apply font size changes
                Debug.Log($"[TutorialController] TutorialText updated with: {title}, fontSize: {tutorialText.fontSize}");
            }
            else
            {
                Debug.LogError("[TutorialController] Cannot show prompt - TutorialText is null!");
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
            
            // Set button size for Tutorial Complete step (larger button)
            if (visible && nextButtonRect != null)
            {
                bool isTutorialComplete = currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count && 
                                        tutorialSteps[currentStepIndex].title == "Tutorial Complete!";
                
                if (isTutorialComplete)
                {
                    // Use larger button size for outro
                    nextButtonRect.sizeDelta = outroNextButtonSize;
                    Debug.Log($"[TutorialController] Using larger Next button size: {outroNextButtonSize} for Tutorial Complete step");
                }
            }
        }

        void UpdateTextPadding(bool hasNextButton)
        {
            if (textRect == null) return;
            
            // Check if this is the Tutorial Complete step (outro) which has a larger button
            bool isTutorialComplete = currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count && 
                                    tutorialSteps[currentStepIndex].title == "Tutorial Complete!";
            
            float rightPadding;
            if (hasNextButton)
            {
                // Use larger padding for Tutorial Complete step with larger button
                rightPadding = isTutorialComplete ? textRightPaddingWithOutroButton : textRightPaddingWithButton;
            }
            else
            {
                rightPadding = textPadding.x;
            }
            
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
                    // Use larger size for shop tutorial step (index 10)
                    if (currentStepIndex == 10 && step.requiredAction == TutorialAction.BuyDiceInShop)
                    {
                        promptRect.sizeDelta = new Vector2(900f, 450f); // Larger size for shop tutorial
                        Debug.Log("[TutorialController] Using larger size for shop tutorial step");
                    }
                    else
                    {
                        promptRect.sizeDelta = actionPromptSize;
                    }
                    promptRect.anchoredPosition = actionPromptOffset;
                    Debug.Log($"[TutorialController] Applied ActionLeft layout - Size: {promptRect.sizeDelta}, Offset: {actionPromptOffset}, Actual Position: {promptRect.anchoredPosition}");
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
                    // Use larger size for Tutorial Complete step (outro)
                    bool isTutorialComplete = currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count && 
                                            tutorialSteps[currentStepIndex].title == "Tutorial Complete!";
                    
                    if (isTutorialComplete)
                    {
                        // Use larger outro panel size
                        promptRect.anchorMin = promptRect.anchorMax = outroPromptAnchor;
                        promptRect.pivot = outroPromptPivot;
                        promptRect.sizeDelta = outroPromptSize;
                        promptRect.anchoredPosition = outroPromptOffset;
                        Debug.Log($"[TutorialController] Using outro panel size: {outroPromptSize} for Tutorial Complete step");
                    }
                    else
                    {
                        // Use normal intro size
                        promptRect.anchorMin = promptRect.anchorMax = introPromptAnchor;
                        promptRect.pivot = introPromptPivot;
                        promptRect.sizeDelta = introPromptSize;
                        promptRect.anchoredPosition = introPromptOffset;
                    }
                    
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
            
            // For "Relics" step, only allow Next button (skip adding combo rules button)
            if (step.title == "Relics")
            {
                if (tutorialContinueButton != null) allowedButtons.Add(tutorialContinueButton);
                
                // Explicitly ensure all dice buttons and lock buttons are disabled
                // Find all dice views and disable their buttons AND block all pointer interactions
                // First, restore any previously modified dice views
                RestoreDiceViews();
                
                DiceView[] diceViews = null;
                if (diceRowParent != null)
                {
                    diceViews = diceRowParent.GetComponentsInChildren<DiceView>(true);
                }
                if (diceViews == null || diceViews.Length == 0)
                {
                    diceViews = FindObjectsOfType<DiceView>(true);
                }
                if (diceViews != null)
                {
                    foreach (var diceView in diceViews)
                    {
                        if (diceView == null) continue;
                        
                        // Disable the dice view's main button (if it has one)
                        Button diceBtn = diceView.GetComponent<Button>();
                        if (diceBtn != null)
                        {
                            if (!originalButtonStates.ContainsKey(diceBtn))
                            {
                                originalButtonStates[diceBtn] = diceBtn.interactable;
                            }
                            diceBtn.interactable = false;
                        }
                        
                        // Disable the lock button
                        if (diceView.lockButton != null)
                        {
                            if (!originalButtonStates.ContainsKey(diceView.lockButton))
                            {
                                originalButtonStates[diceView.lockButton] = diceView.lockButton.interactable;
                            }
                            diceView.lockButton.interactable = false;
                        }
                        
                        // Block all pointer interactions on the dice view GameObject itself
                        // This prevents any clicks from reaching the dice view
                        CanvasGroup diceCanvasGroup = diceView.GetComponent<CanvasGroup>();
                        bool wasAdded = false;
                        if (diceCanvasGroup == null)
                        {
                            diceCanvasGroup = diceView.gameObject.AddComponent<CanvasGroup>();
                            wasAdded = true;
                        }
                        diceCanvasGroup.blocksRaycasts = false;
                        diceCanvasGroup.interactable = false;
                        
                        // Track this dice view for restoration
                        modifiedDiceViews.Add((diceView, diceCanvasGroup, wasAdded));
                        
                        // Also disable all Graphic components' raycast targets to be extra sure
                        UnityEngine.UI.Graphic[] diceGraphics = diceView.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                        foreach (var graphic in diceGraphics)
                        {
                            if (graphic != null)
                            {
                                graphic.raycastTarget = false;
                            }
                        }
                    }
                }
                
                // Fall through to common button disabling logic below
            }
            else
            {
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
                    
                case TutorialAction.BuyDiceInShop:
                    // Only allow the FREE dice button - find all ShopItemUI components and filter for free one
                    ShopItemUI[] shopItems = FindObjectsOfType<ShopItemUI>(true);
                    foreach (var shopItem in shopItems)
                    {
                        if (shopItem != null && shopItem.buyBtn != null && shopItem.gameObject.activeSelf)
                        {
                            // Only allow the button if it's the free dice (price text shows "FREE")
                            if (shopItem.priceText != null && shopItem.priceText.text == "FREE")
                            {
                                allowedButtons.Add(shopItem.buyBtn);
                                Debug.Log("[TutorialController] Allowed free dice button for shop tutorial step");
                            }
                        }
                    }
                    // Also allow continue button (if present) and tutorial UI buttons
                    break;
                    
                case TutorialAction.ClickComboRulesButton:
                    // Allow combo rules button
                    GameObject comboRulesBtnGO = GameObject.Find("ComboPreferenceButton");
                    if (comboRulesBtnGO == null && battleController != null && battleController.comboRulePanel != null)
                    {
                        if (battleController.comboRulePanel.comboRuleButton != null)
                        {
                            comboRulesBtnGO = battleController.comboRulePanel.comboRuleButton.gameObject;
                        }
                    }
                    if (comboRulesBtnGO != null)
                    {
                        Button comboBtn = comboRulesBtnGO.GetComponent<Button>();
                        if (comboBtn != null) allowedButtons.Add(comboBtn);
                    }
                    // Don't allow Next button initially (will be shown after combo rules button is clicked)
                    break;
                    
                case TutorialAction.None:
                    // For intro/outro steps, only allow Next button (plus combo rules which was already added)
                    break;
            }
            }
            
            // Disable all buttons except allowed ones (common logic for all steps)
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
            
            // Also restore dice views that were modified during Relics step
            RestoreDiceViews();
        }
        
        void RestoreDiceViews()
        {
            foreach (var (diceView, canvasGroup, wasAdded) in modifiedDiceViews)
            {
                if (diceView == null) continue;
                
                // Restore CanvasGroup settings
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.interactable = true;
                    
                    // If we added the CanvasGroup, remove it
                    if (wasAdded)
                    {
                        Destroy(canvasGroup);
                    }
                }
                
                // Re-enable all Graphic components' raycast targets
                UnityEngine.UI.Graphic[] diceGraphics = diceView.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var graphic in diceGraphics)
                {
                    if (graphic != null)
                    {
                        graphic.raycastTarget = true;
                    }
                }
            }
            
            modifiedDiceViews.Clear();
        }

        void OnTutorialContinue()
        {
            // Check if this is the last step (Tutorial Complete)
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                var currentStep = tutorialSteps[currentStepIndex];
                if (currentStep.title == "Tutorial Complete!")
                {
                    CompleteTutorialAndGoToReward();
                    return;
                }
                
                // Check if we're in "Check Combo Rules" step and combo rules button was clicked
                // In this case, Next button click should close the panel and advance to next step
                if (currentStep.title == "Check Combo Rules" && comboRulesButtonClicked)
                {
                    comboRulesButtonClicked = false;
                    UnhookComboRulesButton();
                    
                    // Close the combo rule panel
                    if (battleController != null && battleController.comboRulePanel != null)
                    {
                        battleController.comboRulePanel.Close();
                        Debug.Log("[TutorialController] Closed combo rule panel on Next button click");
                    }
                    
                    HidePrompt();
                    ClearHighlights();
                    RestoreAllButtons();
                    StartTutorialStep(currentStepIndex + 1);
                    return;
                }
            }
            
            HidePrompt();
            ClearHighlights();
            RestoreAllButtons();
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

        void HookComboRulesButton()
        {
            UnhookComboRulesButton();
            
            ResolveReferences();
            
            // Find combo rules button
            GameObject comboPrefBtn = GameObject.Find("ComboPreferenceButton");
            if (comboPrefBtn == null && battleController != null && battleController.comboRulePanel != null)
            {
                if (battleController.comboRulePanel.comboRuleButton != null)
                {
                    comboPrefBtn = battleController.comboRulePanel.comboRuleButton.gameObject;
                }
            }
            
            if (comboPrefBtn != null)
            {
                comboRulesButtonGO = comboPrefBtn;
                comboRulesButton = comboPrefBtn.GetComponent<Button>();
                if (comboRulesButton != null)
                {
                    comboRulesButtonClicked = false;
                    comboRulesButtonHandler = OnComboRulesButtonClicked;
                    comboRulesButton.onClick.AddListener(comboRulesButtonHandler);
                    Debug.Log("[TutorialController] Hooked combo rules button");
                }
            }
            else
            {
                Debug.LogWarning("[TutorialController] ComboPreferenceButton not found for hooking");
            }
        }

        void UnhookComboRulesButton()
        {
            if (comboRulesButton != null && comboRulesButtonHandler != null)
            {
                comboRulesButton.onClick.RemoveListener(comboRulesButtonHandler);
                comboRulesButton = null;
                comboRulesButtonGO = null;
                comboRulesButtonHandler = null;
                Debug.Log("[TutorialController] Unhooked combo rules button");
            }
        }

        void OnComboRulesButtonClicked()
        {
            if (currentRequiredAction == TutorialAction.ClickComboRulesButton && !comboRulesButtonClicked)
            {
                comboRulesButtonClicked = true;
                
                // Clear highlight on combo rules button
                if (comboRulesButtonGO != null)
                {
                    ClearHighlights();
                }
                
                // Show and enable the Next button
                SetNextButtonVisible(true);
                // Ensure the Next button is in allowedButtons and enabled
                if (tutorialContinueButton != null)
                {
                    if (!originalButtonStates.ContainsKey(tutorialContinueButton))
                    {
                        originalButtonStates[tutorialContinueButton] = tutorialContinueButton.interactable;
                    }
                    tutorialContinueButton.interactable = true;
                    
                    // Highlight the Next button
                    HighlightElement(tutorialContinueButton.gameObject);
                }
                Debug.Log("[TutorialController] Combo rules button clicked, showing Next button and highlighting it");
            }
        }

        void HookShopPurchaseEvents()
        {
            UnhookShopPurchaseEvents();

            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
            }

            if (shopManager == null) return;

            // Hook into shop item buttons by finding all ShopItemUI components
            ShopItemUI[] shopItems = FindObjectsOfType<ShopItemUI>(true);
            foreach (var shopItem in shopItems)
            {
                if (shopItem == null || shopItem.buyBtn == null) continue;
                if (!shopItem.gameObject.activeSelf) continue;
                
                Button buyButton = shopItem.buyBtn;
                
                // Store original interactable state
                bool wasInteractable = buyButton.interactable;
                
                UnityAction wrappedHandler = () =>
                {
                    // Wait a frame to check if purchase was successful
                    StartCoroutine(CheckShopPurchaseAfterClick(shopItem, buyButton, wasInteractable));
                };
                
                // Add our handler alongside existing listeners
                buyButton.onClick.AddListener(wrappedHandler);
                shopItemHandlers.Add((shopItem, null)); // Store reference
            }
        }

        IEnumerator CheckShopPurchaseAfterClick(ShopItemUI shopItem, Button buyButton, bool wasInteractable)
        {
            yield return null; // Wait one frame for purchase to complete
            
            // Check if purchase was successful by checking button state or sold overlay
            bool purchaseSuccessful = false;
            if (buyButton != null && !buyButton.interactable && wasInteractable)
            {
                // Button became non-interactable, likely sold
                purchaseSuccessful = true;
            }
            else if (shopItem != null && shopItem.soldOverlay != null && shopItem.soldOverlay.activeSelf)
            {
                // Sold overlay is active
                purchaseSuccessful = true;
            }
            
            if (purchaseSuccessful && currentRequiredAction == TutorialAction.BuyDiceInShop)
            {
                RegisterActionCompletion(TutorialAction.BuyDiceInShop);
            }
        }

        void UnhookShopPurchaseEvents()
        {
            // Note: We don't remove listeners here as they're part of ShopItemUI's normal flow
            // The shop items will be cleaned up when scene changes
            shopItemHandlers.Clear();
        }

        void HighlightShopItems()
        {
            ClearHighlights();
            
            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
            }

            if (shopManager == null)
            {
                // Retry after a short delay if shop manager not found yet
                StartCoroutine(RetryHighlightShopItems());
                return;
            }

            // Find all shop item UI components
            ShopItemUI[] shopItems = FindObjectsOfType<ShopItemUI>(true);
            
            if (shopItems == null || shopItems.Length == 0)
            {
                // Retry after a short delay if shop items not found yet
                StartCoroutine(RetryHighlightShopItems());
                return;
            }

            // Only highlight the free dice
            GameObject freeItem = null;
            
            foreach (var shopItem in shopItems)
            {
                if (shopItem == null || shopItem.gameObject == null) continue;
                if (!shopItem.gameObject.activeSelf) continue;
                
                // Check if this is the free item (price text shows "FREE")
                if (shopItem.priceText != null && shopItem.priceText.text == "FREE")
                {
                    freeItem = shopItem.gameObject;
                    break; // Found the free item, no need to continue
                }
            }
            
            // Only highlight the free item
            if (freeItem != null)
            {
                highlightedElements.Add(freeItem);
                Coroutine highlightCoroutine = StartCoroutine(PulseHighlight(freeItem));
                highlightCoroutines.Add(highlightCoroutine);
                Debug.Log("[TutorialController] Highlighted free dice item");
            }
            else
            {
                Debug.LogWarning("[TutorialController] No free dice item found to highlight");
            }
        }

        IEnumerator RetryHighlightShopItems()
        {
            float timeout = 2f;
            while (timeout > 0f)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
                
                if (shopManager == null)
                {
                    shopManager = FindObjectOfType<ShopManager>();
                }
                
                if (shopManager != null)
                {
                    ShopItemUI[] shopItems = FindObjectsOfType<ShopItemUI>(true);
                    if (shopItems != null && shopItems.Length > 0)
                    {
                        HighlightShopItems();
                        yield break;
                    }
                }
            }
        }

        IEnumerator InitializeShopTutorialStep()
        {
            Debug.Log("[TutorialController] InitializeShopTutorialStep() started");
            
            // Wait for shop to fully initialize (items rendered, buttons created)
            yield return new WaitForSeconds(0.5f);
            
            // Resolve shop manager if needed
            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
            }
            
            // Retry if shop manager or items not found yet
            float timeout = 3f;
            while (timeout > 0f && (shopManager == null || FindObjectsOfType<ShopItemUI>(true).Length == 0))
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
                if (shopManager == null)
                {
                    shopManager = FindObjectOfType<ShopManager>();
                }
            }
            
            Debug.Log($"[TutorialController] Shop initialized - ShopManager: {shopManager != null}, ShopItems: {FindObjectsOfType<ShopItemUI>(true).Length}");
            
            // Hook into shop purchase events
            HookShopPurchaseEvents();
            
            // Collect buttons after shop is initialized
            CollectAllButtons();
            
            // Update button states for shop step
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                ManageButtonStates(tutorialSteps[currentStepIndex]);
            }
            
            // Highlight shop items, especially the free one
            HighlightShopItems();
            
            // Ensure prompt is visible after shop initialization and raycast settings are applied
            if (tutorialPromptPanel != null)
            {
                if (!tutorialPromptPanel.activeSelf)
                {
                    Debug.LogWarning("[TutorialController] Prompt panel was inactive after shop init, reactivating");
                    if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
                    {
                        ShowPrompt(tutorialSteps[currentStepIndex].title, tutorialSteps[currentStepIndex].message);
                    }
                    else
                    {
                        tutorialPromptPanel.SetActive(true);
                    }
                }
                
                // Re-apply raycast blocking settings for shop tutorial step (in case they were reset)
                CanvasGroup canvasGroup = tutorialPromptPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false; // Don't block raycasts for shop step
                }
                
                // Disable all raycast targets in panel hierarchy
                UnityEngine.UI.Image panelImage = tutorialPromptPanel.GetComponent<UnityEngine.UI.Image>();
                if (panelImage != null)
                {
                    panelImage.raycastTarget = false;
                }
                if (tutorialText != null)
                {
                    tutorialText.raycastTarget = false;
                }
                UnityEngine.UI.Graphic[] allGraphics = tutorialPromptPanel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var graphic in allGraphics)
                {
                    if (graphic != null)
                    {
                        graphic.raycastTarget = false;
                    }
                }
                Debug.Log($"[TutorialController] Re-applied raycast settings - disabled on {allGraphics.Length} graphics");
                
                Debug.Log($"[TutorialController] InitializeShopTutorialStep() completed - Panel active: {tutorialPromptPanel.activeSelf}, enabled: {tutorialPromptPanel.activeInHierarchy}");
            }
            
            Debug.Log($"[TutorialController] InitializeShopTutorialStep() completed - Panel active: {tutorialPromptPanel != null && tutorialPromptPanel.activeSelf}");
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
            // Wait exactly 1 seconds so player can read the score breakdown message
            // The prompt should still be visible at this point
            yield return new WaitForSeconds(1f);
            
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
            
            // Check if next step is shop tutorial - if so, transition to ShopScene
            if (currentStepIndex + 1 < tutorialSteps.Count)
            {
                var nextStep = tutorialSteps[currentStepIndex + 1];
                if (nextStep.requiredAction == TutorialAction.BuyDiceInShop)
                {
                    // Transition to ShopScene for shop tutorial step
                    CompleteTutorialAndGoToReward();
                }
                else
                {
                    // Continue with next step in current scene
                    StartTutorialStep(currentStepIndex + 1);
                }
            }
            else
            {
                // No more steps, complete tutorial
                CompleteTutorialAndGoToReward();
            }
        }

        /// <summary>
        /// Clean up all tutorial state and reset flags
        /// </summary>
        void CleanupTutorialState()
        {
            HidePrompt();
            ClearHighlights();
            RestoreAllButtons();
            CleanupDiceViewListeners();
            UnhookDiceLockEvent();
            UnhookComboRulesButton();
            
            // Close combo rule panel if it's open
            if (battleController != null && battleController.comboRulePanel != null)
            {
                battleController.comboRulePanel.Close();
            }
            
            currentRequiredAction = TutorialAction.None;
            lockStepCompleted = false;
            awaitingSecondRoll = false;
            comboRulesButtonClicked = false;
        }

        /// <summary>
        /// Transition to ShopScene for shop tutorial step, then complete tutorial
        /// </summary>
        void CompleteTutorialAndGoToReward()
        {
            if (!isTutorialActive) return;

            // Check if we've completed the shop tutorial step
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                var currentStep = tutorialSteps[currentStepIndex];
                if (currentStep.title == "Tutorial Complete!")
                {
                    // Final completion - actually finish the tutorial
                    isTutorialActive = false;
                    CleanupTutorialState();
                    
                    // Save tutorial completion
                    PlayerPrefs.SetInt("HasCompletedTutorial", 1);
                    PlayerPrefs.Save();

                    // Prepare Level 1 state for when returning from ShopScene
                    int targetScore = battleController != null ? battleController.baseTargetScore : 200;
                    
                    var stateManager = GameStateManager.Instance;
                    if (stateManager != null)
                    {
                        stateManager.State.PendingLevel = 1;
                        stateManager.State.PendingTargetScore = targetScore;
                        stateManager.State.ContinuingFromReward = true;
                        stateManager.State.IsTutorialMode = false;
                    }
                    
                    if (battleController == null)
                    {
                        Debug.LogWarning("[TutorialController] BattleController not found - using fallback values");
                    }
                    
                    Debug.Log("[TutorialController] Tutorial fully completed - transitioning to Level 1");
                    SceneManager.LoadScene("BattleScene");
                    return;
                }
            }
            
            // Transition to ShopScene for shop tutorial step (keep tutorial active)
            var stateManager2 = GameStateManager.Instance;
            if (stateManager2 != null)
            {
                stateManager2.State.PendingLevel = 1;
                int targetScore = battleController != null ? battleController.baseTargetScore : 200;
                stateManager2.State.PendingTargetScore = targetScore;
                stateManager2.State.ContinuingFromReward = true;
                stateManager2.State.IsTutorialMode = true; // Keep tutorial mode active
            }
            
            Debug.Log("[TutorialController] Transitioning to ShopScene for shop tutorial step");
            SceneManager.LoadScene("ShopScene");
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
        ScoreAnimationComplete,
        BuyDiceInShop,
        ClickComboRulesButton
    }

    public enum StepLayout
    {
        IntroCenter,
        ActionLeft,
        ComboInfo,
        CenterBelow
    }
}


