using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRogue.Boot
{
    // Put on the black Image that covers the screen.
    // Image must be: Type = Filled, Method = Vertical, Origin = Top.
    public class ScreenWipeFader : MonoBehaviour
    {
        public Image wipeImage;
        public float duration = 0.35f;

        void Reset()
        {
            wipeImage = GetComponent<Image>();
        }

        void Awake()
        {
            if (wipeImage == null) wipeImage = GetComponent<Image>();
            if (wipeImage != null)
            {
                wipeImage.type = Image.Type.Filled;
                wipeImage.fillMethod = Image.FillMethod.Vertical;
                wipeImage.fillOrigin = (int)Image.OriginVertical.Top;
                wipeImage.fillAmount = 1f;            // start fully covered
                wipeImage.raycastTarget = true;        // block clicks during cover
            }
        }

        public IEnumerator FadeIn()  // reveal: 1 -> 0 (top->bottom uncover)
        {
            yield return AnimateFill(1f, 0f, false);
        }

        public IEnumerator FadeOut() // cover: 0 -> 1 (top->bottom wipe)
        {
            yield return AnimateFill(0f, 1f, true);
        }

        IEnumerator AnimateFill(float from, float to, bool blockDuring)
        {
            if (wipeImage == null) yield break;

            wipeImage.raycastTarget = blockDuring;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                wipeImage.fillAmount = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            wipeImage.fillAmount = to;

            // When fully revealed (fill=0), let UI be clickable
            if (to <= 0.0001f) wipeImage.raycastTarget = false;
        }
    }
}
