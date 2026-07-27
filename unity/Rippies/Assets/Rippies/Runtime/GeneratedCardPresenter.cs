using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class GeneratedCardPresenter : MonoBehaviour
    {
        [SerializeField] private Renderer cardFrame;
        [SerializeField] private Renderer cardInset;
        [SerializeField] private Renderer artPanel;
        [SerializeField] private TextMesh brandText;
        [SerializeField] private TextMesh nameText;
        [SerializeField] private TextMesh typeText;
        [SerializeField] private TextMesh statsText;
        [SerializeField] private TextMesh serialText;
        [SerializeField] private TextMesh flavorText;

        private MaterialPropertyBlock frameProperties;
        private MaterialPropertyBlock artProperties;
        private Texture2D generatedArt;

        public void Apply(CardPayload card)
        {
            if (card == null)
            {
                return;
            }

            frameProperties ??= new MaterialPropertyBlock();
            artProperties ??= new MaterialPropertyBlock();

            if (cardInset == null)
            {
                Transform inset = transform.Find("CardInset");
                cardInset = inset == null ? null : inset.GetComponent<Renderer>();
            }

            Color accent = ParseColor(card.accentHex, ProductDesignLanguage.Cyan);
            Color deep = Color.Lerp(ProductDesignLanguage.Surface, accent, 0.26f);
            ApplyMaterial(cardFrame, frameProperties, deep, accent);
            ApplyMaterial(cardInset, frameProperties, deep, accent);
            ApplyGeneratedArt(card, accent);

            if (brandText != null)
            {
                brandText.text = "RIPPIES  //  " + card.rarityTier.ToUpperInvariant();
                brandText.color = accent;
            }

            if (nameText != null)
            {
                nameText.text = card.name.ToUpperInvariant();
                nameText.color = ProductDesignLanguage.Text;
            }

            if (typeText != null)
            {
                typeText.text = card.archetype.ToUpperInvariant();
                typeText.color = Color.Lerp(ProductDesignLanguage.Text, accent, 0.45f);
            }

            if (statsText != null)
            {
                statsText.text =
                    "ATK  " + card.attack.ToString("00") + "     DEF  " + card.defense.ToString("00") + "\n" +
                    "SPD  " + card.speed.ToString("00") + "     LUCK " + card.luck.ToString("00");
                statsText.color = ProductDesignLanguage.Text;
            }

            if (serialText != null)
            {
                serialText.text = card.grade;
                serialText.color = accent;
            }

            if (flavorText != null)
            {
                flavorText.text = card.flavorText;
                flavorText.color = ProductDesignLanguage.TextMuted;
            }
        }

        private void ApplyGeneratedArt(CardPayload card, Color accent)
        {
            if (artPanel == null)
            {
                return;
            }

            if (generatedArt != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedArt);
                }
                else
                {
                    DestroyImmediate(generatedArt);
                }
            }

            generatedArt = BuildPatternTexture(card, accent);
            artPanel.GetPropertyBlock(artProperties);
            artProperties.SetTexture("_BaseMap", generatedArt);
            artProperties.SetTexture("_MainTex", generatedArt);
            artProperties.SetColor("_BaseColor", Color.white);
            artProperties.SetColor("_Color", Color.white);
            artProperties.SetFloat("_Metallic", 0.72f);
            artProperties.SetFloat("_Smoothness", 0.86f);
            artPanel.SetPropertyBlock(artProperties);
        }

        private static Texture2D BuildPatternTexture(CardPayload card, Color accent)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GeneratedArt_" + card.id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            int seed = StableHash(card.id + card.name + card.archetype);
            float phase = Mathf.Abs(seed % 1000) / 1000f * Mathf.PI * 2f;
            float frequencyX = 2f + Mathf.Abs(seed % 5);
            float frequencyY = 3f + Mathf.Abs((seed / 7) % 6);
            float ringFrequency = 8f + Mathf.Abs((seed / 17) % 9);
            Color.RGBToHSV(accent, out float hue, out float saturation, out float value);
            Color secondary = Color.HSVToRGB(Mathf.Repeat(hue + 0.28f, 1f), Mathf.Clamp01(saturation * 0.9f), Mathf.Clamp01(value));
            Color dark = Color.Lerp(new Color(0.005f, 0.01f, 0.035f), accent, 0.12f);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (size - 1f);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (size - 1f);
                    float wave = 0.5f + 0.5f * Mathf.Sin(
                        (u * frequencyX + v * frequencyY) * Mathf.PI * 2f + phase);
                    float dx = u - 0.5f;
                    float dy = v - 0.48f;
                    float ring = 0.5f + 0.5f * Mathf.Sin(
                        Mathf.Sqrt(dx * dx + dy * dy) * ringFrequency * Mathf.PI * 2f - phase);
                    float beam = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - v * 0.55f - 0.2f) * 4f), 3f);
                    float mix = Mathf.Clamp01(wave * 0.42f + ring * 0.34f + beam * 0.65f);
                    Color color = Color.Lerp(dark, accent, mix);
                    color = Color.Lerp(color, secondary, ring * wave * 0.34f);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static void ApplyMaterial(
            Renderer target,
            MaterialPropertyBlock properties,
            Color baseColor,
            Color accent)
        {
            if (target == null)
            {
                return;
            }

            target.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", baseColor);
            properties.SetColor("_AccentColor", accent);
            properties.SetColor("_Color", baseColor);
            properties.SetFloat("_Metallic", 0.82f);
            properties.SetFloat("_Smoothness", 0.9f);
            target.SetPropertyBlock(properties);
        }

        private static Color ParseColor(string html, Color fallback)
        {
            if (!string.IsNullOrWhiteSpace(html) &&
                ColorUtility.TryParseHtmlString(html, out Color parsed))
            {
                return parsed.linear;
            }

            return fallback;
        }
    }
}
