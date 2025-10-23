using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRogue.Boot
{
    // Put this on your black full-screen Image.
    // It will self-configure the Image as a vertical filled bar (top→bottom).
    public class ScreenWipeFader : MonoBehaviour
    {
        [Header("Assign the same Image this script is on")]
        public Image wipeImage;

        [Header("Timing")]
        public float duration = 0.35f;

        [Header("Boot behavior")]
        public bool coverOnAwake = true;   // true = start fully covered (black)
        public bool logDebug = false;

        void OnValidate() { EnsureSetup(); }
        void Reset()      { wipeImage = GetComponent<Image>(); EnsureSetup(); }

        void Awake()
        {
            if (wipeImage == null) wipeImage = GetComponent<Image>();
            EnsureSetup();

            if (coverOnAwake)
            {
                wipeImage.fillAmount   = 1f;   // fully covered
                wipeImage.raycastTarget = true; // block clicks while covered
            }
            else
            {
                wipeImage.fillAmount   = 0f;   // fully revealed
                wipeImage.raycastTarget = false;
            }
        }

        // Reveal content: top→bottom (fill 1 → 0)
        public IEnumerator FadeIn()
        {
            if (logDebug) Debug.Log("[WipeFader] FadeIn()");
            yield return AnimateFill(1f, 0f, blockDuring: false);
        }

        // Cover screen: top→bottom (fill 0 → 1)
        public IEnumerator FadeOut()
        {
            if (logDebug) Debug.Log("[WipeFader] FadeOut()");
            yield return AnimateFill(0f, 1f, blockDuring: true);
        }

        public void ForceReveal()
        {
            if (wipeImage == null) return;
            wipeImage.fillAmount = 0f;
            wipeImage.raycastTarget = false;
        }

        public bool IsCovered()
        {
            return wipeImage != null && wipeImage.fillAmount > 0.001f;
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

            // When revealed, let UI be clickable
            if (to <= 0.001f) wipeImage.raycastTarget = false;
        }

        void EnsureSetup()
        {
            if (wipeImage == null) wipeImage = GetComponent<Image>();
            if (wipeImage == null) return;

            // Make sure it's a filled vertical image wiping from TOP downward.
            wipeImage.type       = Image.Type.Filled;
            wipeImage.fillMethod = Image.FillMethod.Vertical;
            wipeImage.fillOrigin = (int)Image.OriginVertical.Top;
        }
    }
}
