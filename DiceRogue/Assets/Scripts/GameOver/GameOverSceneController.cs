using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame;

namespace DiceRogue.GameOver
{
    public class GameOverSceneController : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text gameOverTitleText;
        public TMP_Text finalScoreText;
        public TMP_Text shortfallText;
        public Button restartButton;
        public Button quitButton;

        [Header("Fader")]
        public DiceRogue.Boot.ScreenWipeFader wipeFader;

        private void Start()
        {
            // Ensure fader is found
            if (wipeFader == null)
            {
                wipeFader = FindObjectOfType<DiceRogue.Boot.ScreenWipeFader>(true);
            }

            // Fade in on scene load
            if (wipeFader != null && wipeFader.IsCovered())
            {
                StartCoroutine(wipeFader.FadeIn());
            }

            // Retrieve score data from BattleController
            int finalScore = BattleController.GameOverFinalScore;
            int targetScore = BattleController.GameOverTargetScore;
            int shortfall = targetScore - finalScore;

            // Update UI elements
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "<size=96><color=#FF3333><b>GAME OVER</b></color></size>";
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"<size=48>Score Reached</size>\n<size=72><color=#FFD700><b>{finalScore}</b></color></size>";
            }

            if (shortfallText != null)
            {
                shortfallText.text = $"<size=48>Short by</size>\n<size=72><color=#FF8888><b>{shortfall}</b></color></size>";
            }

            // Setup button listeners
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnRestartClicked()
        {
            // Reset progression state
            if (DiceRogue.Boot.RunLoader.Instance != null)
            {
                // Transition to battle scene (which will reset to level 1)
                DiceRogue.Boot.RunLoader.Instance.StartRun();
            }
            else
            {
                // Fallback
                UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }
    }
}

