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
        private GUIStyle productButtonStyle;
        private GUIStyle productButtonDisabledStyle;

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
                : controller.AcceptsTearInput
                    ? "REVEAL READY"
                    : "REVEALING";
            GUI.Label(pill, "•  " + status, productStatusStyle);
        }

        private void DrawTearPanel(float logicalWidth, float logicalHeight)
        {
            float panelWidth = Mathf.Min(logicalWidth - 36f, 394f);
            float panelX = (logicalWidth - panelWidth) * 0.5f;
            Rect panel = new Rect(panelX, logicalHeight - 142f, panelWidth, 104f);
            DrawPanel(
                panel,
                new Color(
                    ProductDesignLanguage.Surface.r,
                    ProductDesignLanguage.Surface.g,
                    ProductDesignLanguage.Surface.b,
                    0.96f),
                ProductDesignLanguage.Line,
                1f);

            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 15f, panel.width - 36f, 18f),
                "SWIPE TO RIP",
                productEyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 35f, panel.width - 36f, 22f),
                "Drag across the seal from left to right.",
                productDetailStyle);

            Rect track = new Rect(panel.x + 18f, panel.y + 74f, panel.width - 36f, 3f);
            DrawRect(track, new Color(1f, 1f, 1f, 0.12f));
            DrawRect(
                new Rect(
                    track.x,
                    track.y,
                    track.width * controller.TearProgress,
                    track.height),
                controller.AccentColor);
            GUI.Label(
                new Rect(track.xMax - 28f, panel.y + 57f, 28f, 28f),
                "→",
                productTitleStyle);
        }

        private void DrawCompletionPanel(float logicalWidth, float logicalHeight)
        {
            float panelWidth = Mathf.Min(logicalWidth - 28f, 402f);
            float panelX = (logicalWidth - panelWidth) * 0.5f;
            Rect panel = new Rect(panelX, logicalHeight - 238f, panelWidth, 204f);
            DrawPanel(
                panel,
                new Color(
                    ProductDesignLanguage.SurfaceRaised.r,
                    ProductDesignLanguage.SurfaceRaised.g,
                    ProductDesignLanguage.SurfaceRaised.b,
                    0.97f),
                ProductDesignLanguage.Line,
                1f);

            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 17f, panel.width - 40f, 18f),
                "+  COLLECTION",
                productEyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 39f, panel.width - 40f, 34f),
                "Added to your collection",
                productTitleStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 77f, panel.width - 40f, 22f),
                controller.IsClosing
                    ? "Taking you back to your cards…"
                    : "Drag the card to inspect every angle.",
                productDetailStyle);

            Rect button = new Rect(panel.x + 20f, panel.y + 124f, panel.width - 40f, 54f);
            DrawRect(
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
        }
    }
}
