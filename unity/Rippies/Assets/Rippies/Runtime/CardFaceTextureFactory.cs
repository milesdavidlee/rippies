using System.Collections.Generic;
using UnityEngine;

namespace Rippies.Reveal
{
    internal static class CardFaceTextureFactory
    {
        private const int Width = 512;
        private const int Height = 768;

        private static readonly Dictionary<char, string[]> Glyphs =
            new Dictionary<char, string[]>
            {
                ['A'] = new[] {"01110","10001","10001","11111","10001","10001","10001"},
                ['B'] = new[] {"11110","10001","10001","11110","10001","10001","11110"},
                ['C'] = new[] {"01111","10000","10000","10000","10000","10000","01111"},
                ['D'] = new[] {"11110","10001","10001","10001","10001","10001","11110"},
                ['E'] = new[] {"11111","10000","10000","11110","10000","10000","11111"},
                ['F'] = new[] {"11111","10000","10000","11110","10000","10000","10000"},
                ['G'] = new[] {"01111","10000","10000","10111","10001","10001","01111"},
                ['H'] = new[] {"10001","10001","10001","11111","10001","10001","10001"},
                ['I'] = new[] {"11111","00100","00100","00100","00100","00100","11111"},
                ['J'] = new[] {"00111","00010","00010","00010","10010","10010","01100"},
                ['K'] = new[] {"10001","10010","10100","11000","10100","10010","10001"},
                ['L'] = new[] {"10000","10000","10000","10000","10000","10000","11111"},
                ['M'] = new[] {"10001","11011","10101","10101","10001","10001","10001"},
                ['N'] = new[] {"10001","11001","10101","10011","10001","10001","10001"},
                ['O'] = new[] {"01110","10001","10001","10001","10001","10001","01110"},
                ['P'] = new[] {"11110","10001","10001","11110","10000","10000","10000"},
                ['Q'] = new[] {"01110","10001","10001","10001","10101","10010","01101"},
                ['R'] = new[] {"11110","10001","10001","11110","10100","10010","10001"},
                ['S'] = new[] {"01111","10000","10000","01110","00001","00001","11110"},
                ['T'] = new[] {"11111","00100","00100","00100","00100","00100","00100"},
                ['U'] = new[] {"10001","10001","10001","10001","10001","10001","01110"},
                ['V'] = new[] {"10001","10001","10001","10001","10001","01010","00100"},
                ['W'] = new[] {"10001","10001","10001","10101","10101","11011","10001"},
                ['X'] = new[] {"10001","10001","01010","00100","01010","10001","10001"},
                ['Y'] = new[] {"10001","10001","01010","00100","00100","00100","00100"},
                ['Z'] = new[] {"11111","00001","00010","00100","01000","10000","11111"},
                ['0'] = new[] {"01110","10001","10011","10101","11001","10001","01110"},
                ['1'] = new[] {"00100","01100","00100","00100","00100","00100","01110"},
                ['2'] = new[] {"01110","10001","00001","00010","00100","01000","11111"},
                ['3'] = new[] {"11110","00001","00001","01110","00001","00001","11110"},
                ['4'] = new[] {"00010","00110","01010","10010","11111","00010","00010"},
                ['5'] = new[] {"11111","10000","10000","11110","00001","00001","11110"},
                ['6'] = new[] {"01110","10000","10000","11110","10001","10001","01110"},
                ['7'] = new[] {"11111","00001","00010","00100","01000","01000","01000"},
                ['8'] = new[] {"01110","10001","10001","01110","10001","10001","01110"},
                ['9'] = new[] {"01110","10001","10001","01111","00001","00001","01110"},
                ['/'] = new[] {"00001","00010","00010","00100","01000","01000","10000"},
                ['-'] = new[] {"00000","00000","00000","11111","00000","00000","00000"},
                [':'] = new[] {"00000","00100","00100","00000","00100","00100","00000"}
            };

        public static Texture2D Build(CardPayload card)
        {
            Color accent = ParseColor(card.accentHex, ProductDesignLanguage.Cyan);
            Color deep = Color.Lerp(new Color(0.008f, 0.012f, 0.035f), accent, 0.22f);
            Color text = ProductDesignLanguage.Text;
            var pixels = new Color32[Width * Height];
            int seed = StableHash(card.id + card.name + card.archetype);

            for (int y = 0; y < Height; y++)
            {
                float vertical = y / (Height - 1f);
                for (int x = 0; x < Width; x++)
                {
                    float horizontal = x / (Width - 1f);
                    float glow = Mathf.Clamp01(
                        1f - Vector2.Distance(
                            new Vector2(horizontal, vertical),
                            new Vector2(0.5f, 0.56f)) * 1.35f);
                    pixels[y * Width + x] = Color.Lerp(deep, accent, glow * 0.16f);
                }
            }

            StrokeRect(pixels, 22, 22, 468, 724, 5, accent);
            FillRect(pixels, 42, 42, 428, 58, Color.Lerp(deep, accent, 0.18f));
            DrawCenteredText(
                pixels,
                "RIPPIES // " + card.rarityTier,
                62,
                2,
                accent);
            DrawCenteredText(
                pixels,
                card.name,
                116,
                card.name.Length > 10 ? 4 : 5,
                text);
            DrawCenteredText(pixels, card.archetype, 166, 2, Color.Lerp(text, accent, 0.45f));

            DrawArt(pixels, 78, 208, 356, 250, seed, accent, deep);
            StrokeRect(pixels, 78, 208, 356, 250, 4, Color.Lerp(accent, text, 0.22f));

            DrawCenteredText(
                pixels,
                "ATK " + card.attack.ToString("00") + "   DEF " + card.defense.ToString("00"),
                500,
                3,
                text);
            DrawCenteredText(
                pixels,
                "SPD " + card.speed.ToString("00") + "   LUCK " + card.luck.ToString("00"),
                536,
                3,
                text);
            DrawCenteredText(pixels, card.grade, 594, 2, accent);
            DrawCenteredText(pixels, ShortIdentifier(card.id), 642, 1, Color.Lerp(text, deep, 0.42f));

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                name = "AuthoredCardFace_" + card.id,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 8
            };
            texture.SetPixels32(Rotate180(pixels));
            texture.Apply(true, false);
            return texture;
        }

        private static Color32[] Rotate180(Color32[] source)
        {
            var rotated = new Color32[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                rotated[source.Length - 1 - index] = source[index];
            }

            return rotated;
        }

        private static void DrawArt(
            Color32[] pixels,
            int left,
            int top,
            int width,
            int height,
            int seed,
            Color accent,
            Color deep)
        {
            float phase = Mathf.Abs(seed % 2048) / 2048f * Mathf.PI * 2f;
            float frequency = 5f + Mathf.Abs(seed % 7);
            Color secondary = Color.HSVToRGB(
                Mathf.Repeat(RgbHue(accent) + 0.24f, 1f),
                0.72f,
                1f);
            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (width - 1f);
                    float dx = u - 0.5f;
                    float dy = v - 0.5f;
                    float ring = 0.5f + 0.5f * Mathf.Sin(
                        Mathf.Sqrt(dx * dx + dy * dy) * frequency * 12f - phase);
                    float wave = 0.5f + 0.5f * Mathf.Sin(
                        (u * 4.2f + v * 6.4f) * Mathf.PI + phase);
                    float beam = Mathf.Pow(
                        Mathf.Clamp01(1f - Mathf.Abs(u - v * 0.58f - 0.18f) * 4f),
                        3f);
                    Color color = Color.Lerp(deep, accent, ring * 0.5f + beam * 0.35f);
                    color = Color.Lerp(color, secondary, wave * ring * 0.24f);
                    SetTopPixel(pixels, left + x, top + y, color);
                }
            }
        }

        private static void DrawCenteredText(
            Color32[] pixels,
            string value,
            int top,
            int scale,
            Color color)
        {
            string normalized = Normalize(value);
            int width = normalized.Length == 0 ? 0 : normalized.Length * 6 * scale - scale;
            DrawText(pixels, normalized, Mathf.Max(24, (Width - width) / 2), top, scale, color);
        }

        private static void DrawText(
            Color32[] pixels,
            string value,
            int left,
            int top,
            int scale,
            Color color)
        {
            int cursor = left;
            foreach (char character in Normalize(value))
            {
                if (Glyphs.TryGetValue(character, out string[] glyph))
                {
                    for (int row = 0; row < glyph.Length; row++)
                    {
                        for (int column = 0; column < glyph[row].Length; column++)
                        {
                            if (glyph[row][column] == '1')
                            {
                                FillRect(
                                    pixels,
                                    cursor + column * scale,
                                    top + row * scale,
                                    scale,
                                    scale,
                                    color);
                            }
                        }
                    }
                }

                cursor += 6 * scale;
            }
        }

        private static void StrokeRect(
            Color32[] pixels,
            int left,
            int top,
            int width,
            int height,
            int thickness,
            Color color)
        {
            FillRect(pixels, left, top, width, thickness, color);
            FillRect(pixels, left, top + height - thickness, width, thickness, color);
            FillRect(pixels, left, top, thickness, height, color);
            FillRect(pixels, left + width - thickness, top, thickness, height, color);
        }

        private static void FillRect(
            Color32[] pixels,
            int left,
            int top,
            int width,
            int height,
            Color color)
        {
            for (int y = Mathf.Max(0, top); y < Mathf.Min(Height, top + height); y++)
            {
                for (int x = Mathf.Max(0, left); x < Mathf.Min(Width, left + width); x++)
                {
                    SetTopPixel(pixels, x, y, color);
                }
            }
        }

        private static void SetTopPixel(Color32[] pixels, int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                pixels[(Height - 1 - y) * Width + x] = color;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToUpperInvariant();
        }

        private static string ShortIdentifier(string value)
        {
            string normalized = Normalize(value).Replace("_", "-");
            return normalized.Length <= 22
                ? normalized
                : normalized.Substring(normalized.Length - 22);
        }

        private static float RgbHue(Color color)
        {
            Color.RGBToHSV(color, out float hue, out _, out _);
            return hue;
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color parsed)
                ? parsed.linear
                : fallback;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int index = 0; index < value.Length; index++)
                {
                    hash = hash * 31 + value[index];
                }

                return hash;
            }
        }
    }
}
