using UnityEngine;

namespace DiceRogue.Main
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Idle dice to freeze on start")]
        public DiceOrnamentAnimator[] animatedDice;

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
