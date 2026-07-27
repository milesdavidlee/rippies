using System.Collections;
using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class RevealDirector : MonoBehaviour
    {
        [SerializeField] private Transform packRoot;
        [SerializeField] private Transform card;
        [SerializeField] private Renderer cardRenderer;
        [SerializeField] private Light revealLight;
        [SerializeField] private RevealGlowPulse revealGlow;
        [SerializeField] private float reducedMotionMultiplier = 0.35f;

        private Vector3 cardStartPosition;
        private Quaternion cardStartRotation;
        private Vector3 cardStartScale;
        private Vector3 packStartPosition;
        private Quaternion packStartRotation;
        private Vector3 packStartScale;
        private Coroutine sequence;

        private void Awake()
        {
            CaptureInitialPose();
        }

        public void CaptureInitialPose()
        {
            if (packRoot != null)
            {
                packStartPosition = packRoot.localPosition;
                packStartRotation = packRoot.localRotation;
                packStartScale = packRoot.localScale;
            }

            if (card != null)
            {
                cardStartPosition = card.localPosition;
                cardStartRotation = card.localRotation;
                cardStartScale = card.localScale;
            }
        }

        public void SetPalette(Color accent)
        {
            revealGlow?.SetGlowColor(accent);
        }

        public void Play(PackRipController owner, bool reducedMotion)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            sequence = StartCoroutine(PlaySequence(owner, reducedMotion));
        }

        public void SkipToComplete(PackRipController owner)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            owner.SetCinematicTear(1f, 1f);
            revealGlow?.SetRevealAmount(1f);
            SetPackPresentedPose();

            if (card != null)
            {
                card.gameObject.SetActive(true);
                card.localPosition = new Vector3(0f, 0.02f, -1.6f);
                card.localRotation = Quaternion.identity;
                card.localScale = cardStartScale * 1.05f;
            }

            if (revealLight != null)
            {
                revealLight.intensity = 4.5f;
            }

            owner.NotifyOpening();
            owner.NotifyCardVisible();
            owner.NotifyRevealComplete();
        }

        public void ResetSequence()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            revealGlow?.SetRevealAmount(0f);

            if (packRoot != null)
            {
                packRoot.gameObject.SetActive(true);
                packRoot.localPosition = packStartPosition;
                packRoot.localRotation = packStartRotation;
                packRoot.localScale = packStartScale;
            }

            if (card != null)
            {
                card.localPosition = cardStartPosition;
                card.localRotation = cardStartRotation;
                card.localScale = cardStartScale;
                card.gameObject.SetActive(false);
            }

            if (revealLight != null)
            {
                revealLight.intensity = 0f;
            }
        }

        private IEnumerator PlaySequence(PackRipController owner, bool reducedMotion)
        {
            float motion = reducedMotion ? reducedMotionMultiplier : 1f;
            float startingTear = owner.TearProgress;

            yield return Tween(0.46f * motion, value =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, value);
                float release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, value));
                owner.SetCinematicTear(Mathf.Lerp(startingTear, 1f, eased), release);
                revealGlow?.SetRevealAmount(Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.42f, 1f, value)));

                if (packRoot != null)
                {
                    packRoot.localScale = Vector3.Lerp(
                        packStartScale,
                        Vector3.Scale(packStartScale, new Vector3(1.045f, 0.965f, 1f)),
                        eased);
                }

                if (revealLight != null)
                {
                    revealLight.intensity = Mathf.Lerp(0f, 5.2f, eased);
                }
            });

            owner.NotifyOpening();
            if (card != null)
            {
                card.gameObject.SetActive(true);
            }

            Vector3 emergePosition = new Vector3(0f, 1.28f, -0.7f);
            Vector3 earlyPackDrop = packStartPosition + new Vector3(0f, -0.82f, 0.25f);
            yield return Tween(0.78f * motion, value =>
            {
                float eased = EaseOutBack(value);
                if (card != null)
                {
                    card.localPosition = Vector3.LerpUnclamped(cardStartPosition, emergePosition, eased);
                    card.localRotation = Quaternion.Slerp(
                        cardStartRotation,
                        Quaternion.Euler(0f, 10f, -2f),
                        Mathf.SmoothStep(0f, 1f, value));
                }

                if (packRoot != null)
                {
                    float packEase = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, value));
                    packRoot.localPosition = Vector3.Lerp(packStartPosition, earlyPackDrop, packEase);
                    packRoot.localRotation = Quaternion.Slerp(
                        packStartRotation,
                        packStartRotation * Quaternion.Euler(0f, 0f, -3f),
                        packEase);
                }
            });

            owner.NotifyCardVisible();
            Vector3 finalCardPosition = new Vector3(0f, 0.02f, -1.6f);
            Vector3 packDropPosition = packStartPosition + new Vector3(-0.35f, -5.2f, 1.15f);
            yield return Tween(0.82f * motion, value =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, value);
                float fall = value * value;
                if (packRoot != null)
                {
                    packRoot.localPosition = Vector3.Lerp(earlyPackDrop, packDropPosition, fall);
                    packRoot.localRotation = Quaternion.Slerp(
                        packStartRotation * Quaternion.Euler(0f, 0f, -3f),
                        packStartRotation * Quaternion.Euler(4f, -6f, -18f),
                        fall);
                    packRoot.localScale = Vector3.Lerp(
                        Vector3.Scale(packStartScale, new Vector3(1.045f, 0.965f, 1f)),
                        packStartScale * 0.72f,
                        fall);
                }

                if (card != null)
                {
                    card.localPosition = Vector3.Lerp(emergePosition, finalCardPosition, eased);
                    card.localRotation = Quaternion.Slerp(
                        Quaternion.Euler(0f, 10f, -2f),
                        Quaternion.identity,
                        eased);
                    card.localScale = Vector3.Lerp(cardStartScale, cardStartScale * 1.05f, eased);
                }
            });

            if (packRoot != null)
            {
                packRoot.gameObject.SetActive(false);
            }

            revealGlow?.SetRevealAmount(1f);
            owner.NotifyRevealComplete();
            sequence = null;
        }

        private void SetPackPresentedPose()
        {
            if (packRoot == null)
            {
                return;
            }

            packRoot.localPosition = packStartPosition + new Vector3(-0.35f, -5.2f, 1.15f);
            packRoot.localRotation = packStartRotation * Quaternion.Euler(4f, -6f, -18f);
            packRoot.localScale = packStartScale * 0.72f;
            packRoot.gameObject.SetActive(false);
        }

        private static IEnumerator Tween(float duration, System.Action<float> update)
        {
            if (duration <= 0f)
            {
                update(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                update(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            update(1f);
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                overshoot * shifted * shifted;
        }
    }
}
