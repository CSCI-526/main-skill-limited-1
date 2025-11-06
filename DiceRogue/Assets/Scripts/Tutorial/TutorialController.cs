using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;
using DiceGame.Analytics;
using DiceGame.Relics;
using DiceGame.UI;
using UnityEngine.SceneManagement;

namespace DiceGame.Tutorial
{
    /// <summary>
    /// Tutorial controller that reuses BattleController logic but adds tutorial prompts and guided steps.
    /// Similar to Balatro's integrated tutorial approach.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        // Copy all the same references from BattleController
        [Header("UI")]
        public Transform diceRowParent;
        public GameObject diceViewPrefab;
        public Button rollButton;
        public Button resetRollButton;
        public Button submitComboButton;
        public Button continueButton;
        public Button openBackpackButton;
        public TMP_Text rollFeedbackText;
        public TMP_Text handCounterText;
        
        [Header("Tutorial UI")]
        public GameObject tutorialPromptPanel;  // The overlay panel
        public TMP_Text tutorialText;           // Instruction text
        public Button tutorialContinueButton;   // Continue button
        public Button skipTutorialButton;       // Skip button
        
        [Header("Backpack")]
        public BackpackManager backpackManager;
        
        [Header("Score Display")]
        public ScoreAnimator scoreAnimator;
        public TMP_Text targetScoreText;
        
        [Header("Relic Display")]
        public RelicDisplay relicDisplay;
        
        [Header("Config")]
        public int diceCount = 5;
        public int maxRollsPerHand = 2;
        public int baseTargetScore = 300;
        
        [Header("Cooldown System")]
        public CooldownSystem cooldownSystem;
        
        // Tutorial state
        private int currentTutorialStep = 0;
        private bool isTutorialActive = true;
        
        // Core components (same as BattleController)
        private HandManager _handManager;
        private DiceEffectHandler _effectHandler;
        private DiceViewFactory _viewFactory;
        private RelicManager _relicManager;
        private ScoreCalculator _scoreCalculator;
        private ProgressionManager _progressionManager;
        private BattleUIPresenter _uiPresenter;
        private HandCompositionService _compositionService;
        
        // Current hand state
        private readonly List<BaseDice> _dice = new();
        private readonly List<DiceView> _views = new();
        private bool _isSelectionMode = false;
        
        // Tutorial steps
        private readonly List<TutorialStep> tutorialSteps = new();
        
        void Start()
        {
            InitializeTutorial();
            // TODO: Initialize game systems (copy from BattleController)
            // For now, this is a template - you'll need to copy initialization logic
            StartTutorialStep(0);
        }
        
        void InitializeTutorial()
        {
            // Hide tutorial UI initially
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            // Set up tutorial steps
            SetupTutorialSteps();
            
            // Set up continue button
            if (tutorialContinueButton != null)
                tutorialContinueButton.onClick.AddListener(OnTutorialContinue);
            
            // Set up skip button
            if (skipTutorialButton != null)
                skipTutorialButton.onClick.AddListener(OnSkipTutorial);
        }
        
        void SetupTutorialSteps()
        {
            tutorialSteps.Add(new TutorialStep
            {
                title = "Welcome to Dice Roguelike!",
                message = "This tutorial will teach you the basics of the game. Click Continue to start.",
                highlightElement = null,
                waitForAction = false
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Select Your Dice",
                message = "Click on dice from your backpack to build your hand. You need 5 dice per hand.",
                highlightElement = openBackpackButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.OpenBackpack
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Roll Your Dice",
                message = "Once you've selected 5 dice, click the Roll button to roll them.",
                highlightElement = rollButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.Roll
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Select Dice to Keep",
                message = "Click on dice you want to keep. Selected dice will be locked for your final score.",
                highlightElement = null,
                waitForAction = true,
                requiredAction = TutorialAction.SelectDice
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Submit Your Hand",
                message = "Click Submit to lock in your dice and calculate your score.",
                highlightElement = submitComboButton?.gameObject,
                waitForAction = true,
                requiredAction = TutorialAction.Submit
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Score Combinations",
                message = "Your score is based on combinations like Three of a Kind, Full House, etc. Higher combinations = higher scores!",
                highlightElement = scoreAnimator?.gameObject,
                waitForAction = false
            });
            
            tutorialSteps.Add(new TutorialStep
            {
                title = "Tutorial Complete!",
                message = "You're ready to play! This tutorial will be marked as completed.",
                highlightElement = null,
                waitForAction = false
            });
        }
        
        void StartTutorialStep(int stepIndex)
        {
            if (stepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            currentTutorialStep = stepIndex;
            var step = tutorialSteps[stepIndex];
            
            // Show tutorial prompt
            ShowTutorialPrompt(step.title, step.message, step.highlightElement);
            
            // If step waits for action, disable other interactions
            if (step.waitForAction)
            {
                DisableNonTutorialInteractions(step.requiredAction);
            }
            else
            {
                // Enable continue button for non-action steps
                if (tutorialContinueButton != null)
                    tutorialContinueButton.gameObject.SetActive(true);
            }
        }
        
        void ShowTutorialPrompt(string title, string message, GameObject highlightElement)
        {
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(true);
            
            if (tutorialText != null)
                tutorialText.text = $"<b>{title}</b>\n\n{message}";
            
            // Highlight specific element if provided
            if (highlightElement != null)
            {
                // TODO: Add highlight effect (e.g., outline, glow, or pointer)
                HighlightElement(highlightElement);
            }
        }
        
        void HighlightElement(GameObject element)
        {
            // Simple highlight: add outline component or change color
            // You can implement this based on your UI system
            // For now, just log it
            Debug.Log($"[Tutorial] Highlighting: {element.name}");
        }
        
        void DisableNonTutorialInteractions(TutorialAction requiredAction)
        {
            // Hide continue button for action steps
            if (tutorialContinueButton != null)
                tutorialContinueButton.gameObject.SetActive(false);
            
            // Disable buttons that aren't part of current step
            // Enable only the button/element needed for current step
            // TODO: Implement button enabling/disabling based on requiredAction
        }
        
        void OnTutorialContinue()
        {
            // Hide prompt
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            // Move to next step
            StartTutorialStep(currentTutorialStep + 1);
        }
        
        void OnSkipTutorial()
        {
            CompleteTutorial();
        }
        
        void CompleteTutorial()
        {
            // Mark tutorial as completed
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();
            
            Debug.Log("[Tutorial] Tutorial marked as completed");
            
            // Hide tutorial UI
            if (tutorialPromptPanel != null)
                tutorialPromptPanel.SetActive(false);
            
            isTutorialActive = false;
            
            // Return to main menu
            if (DiceRogue.Boot.RunLoader.Instance != null)
            {
                StartCoroutine(ReturnToMainMenu());
            }
        }
        
        IEnumerator ReturnToMainMenu()
        {
            // Use RunLoader's fade system
            yield return DiceRogue.Boot.RunLoader.Instance.LoadSceneWithWipe("MainScene");
        }
        
        // Hook into game actions to track tutorial progress
        // These methods should be called from your game action handlers
        public void OnBackpackOpened()
        {
            if (isTutorialActive && currentTutorialStep == 1) // Step for opening backpack
            {
                OnTutorialContinue();
            }
        }
        
        public void OnRollButtonClicked()
        {
            if (isTutorialActive && currentTutorialStep == 2) // Step for rolling
            {
                OnTutorialContinue();
            }
            // Also call actual roll logic
        }
        
        public void OnDiceSelected()
        {
            if (isTutorialActive && currentTutorialStep == 3) // Step for selecting dice
            {
                // Check if enough dice selected, then continue
                if (_dice.Count >= 5)
                {
                    OnTutorialContinue();
                }
            }
        }
        
        public void OnHandSubmitted()
        {
            if (isTutorialActive && currentTutorialStep == 4) // Step for submitting
            {
                OnTutorialContinue();
            }
        }
    }
    
    // Tutorial step data structure
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        public string message;
        public GameObject highlightElement;
        public bool waitForAction;
        public TutorialAction requiredAction;
    }
    
    public enum TutorialAction
    {
        None,
        OpenBackpack,
        SelectDice,
        Roll,
        Submit,
        Continue
    }
}

