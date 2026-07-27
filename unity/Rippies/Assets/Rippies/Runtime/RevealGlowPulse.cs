using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class RevealGlowPulse : MonoBehaviour
    {
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [SerializeField] private Renderer haloRenderer;
        [SerializeField] private Light glowLight;
        [SerializeField] private Color glowColor = new Color(0.25f, 0.85f, 1f);
        [SerializeField] private Vector3 fullScale = new Vector3(6.8f, 6.8f, 1f);
        [SerializeField] private float backgroundDepth = 1.4f;
        [SerializeField] private float maximumIntensity = 4.8f;
        [SerializeField] private float maximumLightIntensity = 6f;

        private MaterialPropertyBlock properties;
        private float revealAmount;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            MoveBehindReveal();
            SetRevealAmount(0f);
        }

        private void Update()
        {
            if (revealAmount <= 0.001f)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.35f) * 0.1f;
            transform.localRotation *= Quaternion.Euler(0f, 0f, 4f * Time.unscaledDeltaTime);
            transform.localScale = fullScale * (pulse * Mathf.Lerp(0.2f, 1f, revealAmount));
            ApplyVisuals(pulse);
        }

        public void SetRevealAmount(float value)
        {
            revealAmount = Mathf.Clamp01(value);
            properties ??= new MaterialPropertyBlock();
            MoveBehindReveal();

            bool visible = revealAmount > 0.001f;
            if (haloRenderer != null)
            {
                haloRenderer.enabled = visible;
            }

            transform.localScale = fullScale * Mathf.Lerp(0.05f, 1f, revealAmount);
            ApplyVisuals(1f);
        }

        public void SetGlowColor(Color color)
        {
            glowColor = Color.Lerp(color, Color.white, 0.08f);
            ApplyVisuals(1f);
        }

        private void MoveBehindReveal()
        {
            Vector3 position = transform.localPosition;
            position.z = backgroundDepth;
            transform.localPosition = position;
        }

        private void ApplyVisuals(float pulse)
        {
            properties ??= new MaterialPropertyBlock();

            if (haloRenderer != null)
            {
                haloRenderer.GetPropertyBlock(properties);
                properties.SetColor(GlowColorId, glowColor);
                properties.SetFloat(IntensityId, revealAmount * maximumIntensity * pulse);
                haloRenderer.SetPropertyBlock(properties);
            }

            if (glowLight != null)
            {
                glowLight.color = glowColor;
                glowLight.intensity = revealAmount * maximumLightIntensity * pulse;
            }
        }
    }
}
