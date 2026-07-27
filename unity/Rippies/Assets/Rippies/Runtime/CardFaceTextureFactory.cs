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

        public static Texture2D BuildFront(CardPayload card)
        {
            Color accent = ParseColor(card.accentHex, ProductDesignLanguage.Cyan);
            Color deep = Color.Lerp(new Color(0.008f, 0.012f, 0.035f), accent, 0.22f);
            Color text = ProductDesignLanguage.Text;
            var pixels = new Color32[Width * Height];
            int seed = StableHash(card.id + card.name + card.archetype);

            FillGradient(pixels, deep, accent, new Vector2(0.5f, 0.54f), 0.2f);

            FillRect(pixels, 0, 0, Width, 82, Color.Lerp(deep, accent, 0.12f));
            FillRect(pixels, 0, 0, 8, Height, accent);
            FillRect(pixels, Width - 3, 0, 3, Height, Color.Lerp(accent, text, 0.25f));
            DrawCenteredText(
                pixels,
                "RIPPIES // " + card.rarityTier,
                34,
                2,
                accent);
            DrawCenteredText(
                pixels,
                card.name,
                104,
                card.name.Length > 10 ? 4 : 5,
                text);
            DrawCenteredText(pixels, card.archetype, 154, 2, Color.Lerp(text, accent, 0.45f));

            DrawArt(pixels, 24, 194, 464, 306, seed, accent, deep);
            StrokeRect(pixels, 24, 194, 464, 306, 3, Color.Lerp(accent, text, 0.22f));

            DrawCenteredText(
                pixels,
                "ATK " + card.attack.ToString("00") + "   DEF " + card.defense.ToString("00"),
                538,
                3,
                text);
            DrawCenteredText(
                pixels,
                "SPD " + card.speed.ToString("00") + "   LUCK " + card.luck.ToString("00"),
                576,
                3,
                text);
            DrawCenteredText(pixels, card.grade, 638, 2, accent);
            DrawCenteredText(pixels, ShortIdentifier(card.id), 692, 1, Color.Lerp(text, deep, 0.42f));

            return CreateTexture(pixels, "AuthoredCardFront_" + card.id);
        }

        public static Texture2D BuildBack(CardPayload card, string packTypeId)
        {
            Color accent = ParseColor(card.accentHex, ProductDesignLanguage.Cyan);
            Color deep = Color.Lerp(new Color(0.006f, 0.01f, 0.027f), accent, 0.18f);
            Color text = ProductDesignLanguage.Text;
            string packName = PackName(packTypeId);
            string symbol = packName.Length == 0 ? "R" : packName.Substring(0, 1);
            var pixels = new Color32[Width * Height];

            FillGradient(pixels, deep, accent, new Vector2(0.52f, 0.42f), 0.24f);
            DrawDiagonalBand(
                pixels,
                -180,
                126,
                76,
                Color.Lerp(deep, accent, 0.42f));
            DrawEllipseStroke(
                pixels,
                new Vector2(252f, 292f),
                new Vector2(310f, 196f),
                3f,
                Color.Lerp(deep, accent, 0.72f));
            DrawEllipseStroke(
                pixels,
                new Vector2(266f, 310f),
                new Vector2(178f, 274f),
                2f,
                Color.Lerp(deep, accent, 0.55f));

            DrawCenteredText(pixels, symbol, 248, 14, Color.Lerp(accent, text, 0.12f));
            DrawCenteredText(pixels, "RIPPIES", 558, 4, text);
            DrawCenteredText(pixels, packName + " PACK", 614, 3, accent);
            DrawCenteredText(pixels, "FIRST EDITION", 666, 2, ProductDesignLanguage.TextMuted);

            return CreateTexture(pixels, "AuthoredCardBack_" + card.id);
        }

        private static Texture2D CreateTexture(Color32[] pixels, string name)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 8
            };
            texture.SetPixels32(Rotate180(pixels));
            texture.Apply(true, false);
            return texture;
        }

        private static void FillGradient(
            Color32[] pixels,
            Color deep,
            Color accent,
            Vector2 glowCenter,
            float strength)
        {
            for (int y = 0; y < Height; y++)
            {
                float vertical = y / (Height - 1f);
                for (int x = 0; x < Width; x++)
                {
                    float horizontal = x / (Width - 1f);
                    float glow = Mathf.Clamp01(
                        1f - Vector2.Distance(
                            new Vector2(horizontal, vertical),
                            glowCenter) * 1.35f);
                    pixels[y * Width + x] =
                        Color.Lerp(deep, accent, glow * strength);
                }
            }
        }

        private static void DrawDiagonalBand(
            Color32[] pixels,
            int startX,
            int startY,
            int thickness,
            Color color)
        {
            for (int y = 0; y < Height; y++)
            {
                int centerX = startX + y - startY;
                for (int offset = 0; offset < thickness; offset++)
                {
                    SetTopPixel(pixels, centerX + offset, y, color);
                }
            }
        }

        private static void DrawEllipseStroke(
            Color32[] pixels,
            Vector2 center,
            Vector2 size,
            float thickness,
            Color color)
        {
            float outerX = size.x * 0.5f;
            float outerY = size.y * 0.5f;
            float innerX = Mathf.Max(1f, outerX - thickness);
            float innerY = Mathf.Max(1f, outerY - thickness);
            int left = Mathf.FloorToInt(center.x - outerX);
            int right = Mathf.CeilToInt(center.x + outerX);
            int top = Mathf.FloorToInt(center.y - outerY);
            int bottom = Mathf.CeilToInt(center.y + outerY);
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float outer = dx * dx / (outerX * outerX) +
                        dy * dy / (outerY * outerY);
                    float inner = dx * dx / (innerX * innerX) +
                        dy * dy / (innerY * innerY);
                    if (outer <= 1f && inner >= 1f)
                    {
                        SetTopPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static string PackName(string packTypeId)
        {
            string normalized = Normalize(packTypeId).Replace('-', ' ');
            int separator = normalized.LastIndexOf('_');
            return separator >= 0 && separator < normalized.Length - 1
                ? normalized.Substring(separator + 1)
                : string.IsNullOrWhiteSpace(normalized)
                    ? "ORIGINAL"
                    : normalized;
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
