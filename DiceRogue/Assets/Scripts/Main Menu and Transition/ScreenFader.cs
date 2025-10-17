using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRogue.Boot
{
    // Attach to a full-screen Image with a CanvasGroup
    public class ScreenFader : MonoBehaviour
    {
        public CanvasGroup group;
        public float duration = 0.25f;

        private void Reset()  { group = GetComponent<CanvasGroup>(); }
        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.alpha = 1f; // start black → fade in
        }

        public IEnumerator FadeIn()  { yield return FadeTo(0f); group.blocksRaycasts = false; }
        public IEnumerator FadeOut() { group.blocksRaycasts = true; yield return FadeTo(1f);  }

        private IEnumerator FadeTo(float target)
        {
            float start = group.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
