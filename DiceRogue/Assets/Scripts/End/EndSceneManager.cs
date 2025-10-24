using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DiceRogue.Boot; // RunLoader for nice wipes

namespace DiceGame
{
    /// <summary>
    /// Controls the EndScene shown when the player fails to reach the target score.
    /// Reads EndSceneData and populates a minimal, readable UI; provides navigation.
    /// </summary>
    public class EndSceneManager : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private TMP_Text titleText;     // e.g., "GAME OVER"
        [SerializeField] private TMP_Text subtitleText;  // e.g., "You didn't reach the target"
        [SerializeField] private TMP_Text detailText;    // Final score, target, delta

        [Header("Buttons")] 
        [SerializeField] private Button retryButton;     // Retry -> BattleScene
        [SerializeField] private Button menuButton;      // Back to MainScene
        [SerializeField] private Button quitButton;      // Quit application

        [Header("Optional Style")]
        [SerializeField] private Color failColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color normalColor = Color.white;

        void Start()
        {
            // Defensive defaults if UI not wired yet
            if (titleText == null || subtitleText == null || detailText == null)
            {
                Debug.LogWarning("[EndScene] Some UI text references are missing. Assign them in the inspector.");
            }

            // Populate content from EndSceneData
            var finalScore = EndSceneData.LastScore;
            var target = EndSceneData.TargetScore;
            var didWin = EndSceneData.DidWin; // expected false here

            if (titleText != null)
            {
                titleText.text = didWin
                    ? "RUN COMPLETE"
                    : "<size=240%><color=#FF3333><b>GAME OVER</b></color></size>";
            }
            if (subtitleText != null)
            {
                subtitleText.text = didWin
                    ? "You reached the target score"
                    : "You didn't reach the target score";
            }

            if (detailText != null)
            {
                int delta = Mathf.Max(0, target - finalScore);
                detailText.text =
                    $"<size=130%><b>Final Score</b></size>\n" +
                    $"<size=200%><color=#FFD700><b>{finalScore}</b></color></size>\n\n" +
                    $"<size=130%><b>Target Score</b></size>\n" +
                    $"<size=200%><color=#88CCFF><b>{target}</b></color></size>\n\n" +
                    (didWin
                        ? "<color=#88FF88>You met the target!</color>"
                        : $"<size=140%><color=#FF8888>{delta} short of target</color></size>\n\n" +
                          "<color=#AAAAAA>Tip: Try rotating your dice to build stronger combos.</color>");
            }

            // Wire buttons
            if (retryButton != null) retryButton.onClick.AddListener(OnClickRetry);
            if (menuButton != null) menuButton.onClick.AddListener(OnClickMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);
        }

        private void OnClickRetry()
        {
            // Use the persistent RunLoader to perform a nice screen wipe to Battle
            if (RunLoader.Instance != null)
                RunLoader.Instance.StartRun();
            else
                SceneManager.LoadScene("BattleScene");
        }

        private void OnClickMainMenu()
        {
            if (RunLoader.Instance != null)
                RunLoader.Instance.GoToMainMenu();
            else
                SceneManager.LoadScene("MainScene");
        }

        private void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
