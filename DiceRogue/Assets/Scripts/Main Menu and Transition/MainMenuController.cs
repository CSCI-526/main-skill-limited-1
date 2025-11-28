using UnityEngine;
using DiceGame.Audio;

namespace DiceRogue.Main
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Idle dice to freeze on start")]
        public DiceOrnamentAnimator[] animatedDice;

        void Start()
        {
            // Initialize SoundManager and start background music when main menu loads
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBackgroundMusic();
            }
        }

        public void OnClickStart()
        {
            // Stop both dice animations instantly
            if (animatedDice != null)
            {
                foreach (var d in animatedDice)
                    if (d != null) d.Pause();
            }

            // Kick off the top->bottom wipe and load Battle
            DiceRogue.Boot.RunLoader.Instance.StartRun();
        }

        public void OnClickTutorial()
        {
            // Stop both dice animations instantly
            if (animatedDice != null)
            {
                foreach (var d in animatedDice)
                    if (d != null) d.Pause();
            }

            // Kick off the tutorial
            DiceRogue.Boot.RunLoader.Instance.StartTutorial();
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
