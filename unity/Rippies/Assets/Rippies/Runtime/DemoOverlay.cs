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
            float panelWidth = Mathf.Min(logicalWidth - 48f, 342f);
            float panelX = (logicalWidth - panelWidth) * 0.5f;
            float panelY = logicalHeight - 132f;
            string instruction = controller.State == RipState.Complete
                ? "REVEAL SECURED"
                : "SWIPE THE SEAL  →";

            var productLabel = new GUIStyle(labelStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.92f, 0.96f) }
            };
            GUI.Label(
                new Rect(panelX, panelY, panelWidth, 24f),
                instruction,
                productLabel);

            Rect track = new Rect(panelX, panelY + 34f, panelWidth, 3f);
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);
            GUI.color = new Color(0.18f, 0.95f, 0.72f, 0.95f);
            GUI.DrawTexture(
                new Rect(track.x, track.y, track.width * controller.TearProgress, track.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
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
        }
    }
}
