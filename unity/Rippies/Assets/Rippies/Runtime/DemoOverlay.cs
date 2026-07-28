using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class DemoOverlay : MonoBehaviour
    {
        [SerializeField] private PackRipController controller;

        private bool productMode;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle productEyebrowStyle;
        private GUIStyle productStatusStyle;
        private GUIStyle productTitleStyle;
        private GUIStyle productDetailStyle;
        private GUIStyle productCenteredTitleStyle;
        private GUIStyle productCenteredDetailStyle;
        private GUIStyle productButtonStyle;
        private GUIStyle productButtonDisabledStyle;
        private Texture2D roundedPillTexture;

        public void SetProductMode(bool enabled)
        {
            productMode = enabled;
            this.enabled = enabled;
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureStyles();
            if (productMode)
            {
                DrawProductPrompt();
                return;
            }

            float width = Mathf.Min(Screen.width - 40f, 520f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 24f, width, 176f);
            GUI.Box(panel, GUIContent.none);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, panel.width - 40f, 32f), "RIPPIES", titleStyle);
            string instruction = controller.State == RipState.Complete
                ? "Reveal complete — press RESET to try again"
                : "Click and drag the pack seam from left to right";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 46f, panel.width - 40f, 28f), instruction, labelStyle);

            Rect track = new Rect(panel.x + 20f, panel.y + 78f, panel.width - 40f, 12f);
            GUI.Box(track, GUIContent.none);
            Color previous = GUI.color;
            GUI.color = new Color(0.1f, 0.92f, 0.82f);
            GUI.Box(new Rect(track.x, track.y, track.width * controller.TearProgress, track.height), GUIContent.none);
            GUI.color = previous;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 98f, panel.width - 40f, 24f), controller.State.ToString(), labelStyle);

            GUI.enabled = controller.State != RipState.Complete;
            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 132f, 120f, 30f), "DEMO RIP", buttonStyle))
            {
                controller.BeginReveal();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.xMax - 120f, panel.y + 132f, 100f, 30f), "RESET", buttonStyle))
            {
                controller.PrepareRandomReveal();
            }
        }

        private void DrawProductPrompt()
        {
            float scale = Mathf.Max(1f, Screen.width / 430f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float logicalWidth = Screen.width / scale;
            float logicalHeight = Screen.height / scale;
            DrawProductStatus(logicalWidth);

            if (controller.State == RipState.Complete)
            {
                DrawCompletionPanel(logicalWidth, logicalHeight);
            }
            else if (controller.AcceptsTearInput)
            {
                DrawTearPanel(logicalWidth, logicalHeight);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawProductStatus(float logicalWidth)
        {
            float pillWidth = 146f;
            Rect pill = new Rect((logicalWidth - pillWidth) * 0.5f, 68f, pillWidth, 30f);
            DrawPanel(pill, ProductDesignLanguage.Surface, ProductDesignLanguage.Line, 1f);

            string status = controller.State == RipState.Complete
                ? "INSPECT MODE"
                : controller.State == RipState.Presenting
                    ? "PACK INCOMING"
                : controller.AcceptsTearInput
                    ? "REVEAL READY"
                    : "REVEALING";
            GUI.Label(pill, "•  " + status, productStatusStyle);
        }

        private void DrawTearPanel(float logicalWidth, float logicalHeight)
        {
            float contentWidth = Mathf.Min(logicalWidth - 64f, 350f);
            float contentX = (logicalWidth - contentWidth) * 0.5f;
            float contentY = logicalHeight - 126f;

            GUI.Label(
                new Rect(contentX, contentY, contentWidth, 18f),
                "SWIPE TO RIP",
                productStatusStyle);
            GUI.Label(
                new Rect(contentX, contentY + 22f, contentWidth, 22f),
                "Drag across the seal from left to right.",
                productCenteredDetailStyle);

            Rect track = new Rect(contentX, contentY + 63f, contentWidth, 2f);
            DrawRect(track, new Color(1f, 1f, 1f, 0.12f));
            DrawRect(
                new Rect(
                    track.x,
                    track.y,
                    track.width * controller.TearProgress,
                    track.height),
                controller.AccentColor);
            DrawRect(
                new Rect(track.x - 3f, track.y - 3f, 8f, 8f),
                controller.AccentColor);
            GUI.Label(
                new Rect(track.x, track.y + 6f, 56f, 18f),
                "START",
                productEyebrowStyle);
            GUI.Label(
                new Rect(track.xMax - 28f, track.y - 16f, 28f, 28f),
                "→",
                productTitleStyle);
        }

        private void DrawCompletionPanel(float logicalWidth, float logicalHeight)
        {
            float contentWidth = Mathf.Min(logicalWidth - 48f, 370f);
            float contentX = (logicalWidth - contentWidth) * 0.5f;

            GUI.Label(
                new Rect(contentX, 112f, contentWidth, 18f),
                "+  COLLECTION",
                productStatusStyle);
            GUI.Label(
                new Rect(contentX, 136f, contentWidth, 34f),
                "Added " + Mathf.Max(1, controller.RevealedCardCount) +
                    " cards to your collection",
                productCenteredTitleStyle);
            GUI.Label(
                new Rect(contentX, 172f, contentWidth, 22f),
                controller.IsClosing
                    ? "Taking you back to your cards…"
                    : "Tap a card to inspect it. Tap again to return.",
                productCenteredDetailStyle);

            Rect button = new Rect(contentX + 42f, logicalHeight - 88f, contentWidth - 84f, 48f);
            DrawRoundedRect(
                button,
                controller.IsClosing
                    ? ProductDesignLanguage.Line
                    : controller.AccentColor);
            GUI.enabled = !controller.IsClosing;
            if (GUI.Button(
                    button,
                    controller.IsClosing ? "OPENING COLLECTION…" : "VIEW COLLECTION  →",
                    controller.IsClosing
                        ? productButtonDisabledStyle
                        : productButtonStyle))
            {
                controller.RequestCollection();
            }

            GUI.enabled = true;
        }

        private static void DrawPanel(Rect rect, Color fill, Color border, float borderWidth)
        {
            DrawRect(rect, border);
            DrawRect(
                new Rect(
                    rect.x + borderWidth,
                    rect.y + borderWidth,
                    rect.width - borderWidth * 2f,
                    rect.height - borderWidth * 2f),
                fill);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawRoundedRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, roundedPillTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 0.92f, 0.82f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
            productEyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ProductDesignLanguage.Cyan }
            };
            productStatusStyle = new GUIStyle(productEyebrowStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            productTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ProductDesignLanguage.Text }
            };
            productDetailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { textColor = ProductDesignLanguage.TextMuted }
            };
            productCenteredTitleStyle = new GUIStyle(productTitleStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            productCenteredDetailStyle = new GUIStyle(productDetailStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            productButtonStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ProductDesignLanguage.Canvas }
            };
            productButtonDisabledStyle = new GUIStyle(productButtonStyle)
            {
                normal = { textColor = ProductDesignLanguage.TextMuted }
            };
            roundedPillTexture = BuildRoundedPillTexture();
        }

        private static Texture2D BuildRoundedPillTexture()
        {
            const int width = 128;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Rippies_RoundedPill",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            float radius = height * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nearestX = Mathf.Clamp(x + 0.5f, radius, width - radius);
                    float nearestY = height * 0.5f;
                    float distance = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(nearestX, nearestY));
                    byte alpha = distance <= radius - 1f
                        ? (byte)255
                        : distance >= radius
                            ? (byte)0
                            : (byte)Mathf.RoundToInt((radius - distance) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
