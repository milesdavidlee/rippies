using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rippies.Reveal
{
    public sealed class PackSelectionFlow : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly ThemeDefinition[] Themes =
        {
            new ThemeDefinition("genesis", "GENESIS", "#37F4D1"),
            new ThemeDefinition("solar", "SOLAR FLARE", "#F4F15A"),
            new ThemeDefinition("prism", "PRISMATIC", "#B96CFF"),
            new ThemeDefinition("ember", "EMBER", "#FF7A45"),
            new ThemeDefinition("chrome", "CHROME", "#54D8FF"),
            new ThemeDefinition("midnight", "MIDNIGHT", "#FF4FD8")
        };

        [SerializeField] private PackRipController controller;
        [SerializeField] private NativeRevealBridge bridge;
        [SerializeField] private DemoOverlay revealOverlay;
        [SerializeField] private Transform revealPack;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private float transitionDuration = 0.68f;
        [SerializeField] private float thumbnailScale = 0.36f;

        private readonly List<PackOption> options = new List<PackOption>();
        private Transform gridRoot;
        private Transform packWordmark;
        private Transform packSubtitle;
        private FlowState state;
        private PackOption selected;
        private float transitionElapsed;
        private GUIStyle eyebrowStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle packNameStyle;
        private GUIStyle badgeStyle;
        private GUIStyle buttonStyle;

        public string StateName => state.ToString();
        public string SelectedPackId => selected == null ? string.Empty : selected.Theme.Id;

        private enum FlowState
        {
            Initializing,
            Browse,
            Transition,
            Reveal
        }

        private sealed class PackOption
        {
            public GameObject GameObject;
            public Transform Transform;
            public Vector3 StartPosition;
            public Quaternion StartRotation;
            public Vector3 StartScale;
            public Vector3 FloatOffset;
            public ThemeDefinition Theme;
            public int Index;
        }

        private readonly struct ThemeDefinition
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string AccentHex;

            public ThemeDefinition(string id, string displayName, string accentHex)
            {
                Id = id;
                DisplayName = displayName;
                AccentHex = accentHex;
            }
        }

        private void Awake()
        {
            controller ??= Object.FindFirstObjectByType<PackRipController>();
            bridge ??= Object.FindFirstObjectByType<NativeRevealBridge>();
            revealOverlay ??= Object.FindFirstObjectByType<DemoOverlay>();
            sceneCamera ??= Camera.main;
            if (revealPack == null)
            {
                GameObject pack = GameObject.Find("PackShellVisuals");
                revealPack = pack == null ? null : pack.transform;
            }

            if (controller != null)
            {
                packWordmark = controller.transform.Find("PackWordmark");
                packSubtitle = controller.transform.Find("PackSubtitle");
            }
        }

        private IEnumerator Start()
        {
            state = FlowState.Initializing;
            yield return null;
            ShowBrowse();
        }

        private void Update()
        {
            if (state == FlowState.Browse)
            {
                UpdateBrowseMotion();
                HandlePointer();
            }
            else if (state == FlowState.Transition)
            {
                UpdateTransition();
            }
        }

        public void SelectPack(int index)
        {
            if (state != FlowState.Browse || index < 0 || index >= options.Count)
            {
                return;
            }

            selected = options[index];
            transitionElapsed = 0f;
            state = FlowState.Transition;
        }

        public void ReturnToGrid()
        {
            if (state != FlowState.Reveal)
            {
                return;
            }

            controller?.PrepareRandomReveal();
            ShowBrowse();
        }

        private void ShowBrowse()
        {
            CleanupGrid();
            if (revealOverlay != null)
            {
                revealOverlay.enabled = false;
            }

            if (revealPack == null || sceneCamera == null)
            {
                Debug.LogError("PackSelectionFlow is missing its reveal pack or camera.");
                return;
            }

            revealPack.gameObject.SetActive(true);
            BuildGrid();
            revealPack.gameObject.SetActive(false);
            SetPackLabelsActive(false);
            selected = null;
            state = FlowState.Browse;
        }

        private void BuildGrid()
        {
            var rootObject = new GameObject("PackSelectionGrid");
            gridRoot = rootObject.transform;
            float depth = Vector3.Distance(sceneCamera.transform.position, revealPack.position);
            Vector3 baseScale = revealPack.lossyScale * thumbnailScale;

            for (int index = 0; index < Themes.Length; index++)
            {
                int column = index % 3;
                int row = index / 3;
                float viewportX = 0.22f + column * 0.28f;
                float viewportY = row == 0 ? 0.59f : 0.28f;
                Vector3 worldPosition = sceneCamera.ViewportToWorldPoint(
                    new Vector3(viewportX, viewportY, depth));

                GameObject clone = Instantiate(revealPack.gameObject, gridRoot);
                clone.name = "PackOption_" + Themes[index].Id;
                clone.SetActive(true);
                clone.transform.position = worldPosition;
                clone.transform.rotation = revealPack.rotation * Quaternion.Euler(0f, 0f, (column - 1) * 2.5f);
                clone.transform.localScale = baseScale;

                var collider = clone.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = clone.AddComponent<BoxCollider>();
                }

                collider.center = Vector3.zero;
                collider.size = new Vector3(3.35f, 4.8f, 1.1f);
                ApplyTheme(clone, Themes[index]);

                options.Add(new PackOption
                {
                    GameObject = clone,
                    Transform = clone.transform,
                    StartPosition = worldPosition,
                    StartRotation = clone.transform.rotation,
                    StartScale = baseScale,
                    FloatOffset = new Vector3(0f, index * 0.47f, index * 0.16f),
                    Theme = Themes[index],
                    Index = index
                });
            }
        }

        private void UpdateBrowseMotion()
        {
            float time = Time.unscaledTime;
            for (int index = 0; index < options.Count; index++)
            {
                PackOption option = options[index];
                float phase = option.FloatOffset.y;
                option.Transform.position = option.StartPosition +
                    new Vector3(0f, Mathf.Sin(time * 1.15f + phase) * 0.045f, 0f);
                option.Transform.rotation = option.StartRotation * Quaternion.Euler(
                    Mathf.Sin(time * 0.63f + phase) * 1.2f,
                    Mathf.Sin(time * 0.82f + phase) * 2.2f,
                    Mathf.Sin(time * 0.51f + phase) * 0.7f);
            }
        }

        private void HandlePointer()
        {
            bool pressed = false;
            Vector2 screenPosition = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressed = true;
                screenPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pressed = true;
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (!pressed)
            {
                return;
            }

            Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 40f))
            {
                return;
            }

            for (int index = 0; index < options.Count; index++)
            {
                if (hit.transform == options[index].Transform || hit.transform.IsChildOf(options[index].Transform))
                {
                    SelectPack(index);
                    return;
                }
            }
        }

        private void UpdateTransition()
        {
            if (selected == null || revealPack == null)
            {
                ShowBrowse();
                return;
            }

            transitionElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(transitionElapsed / transitionDuration);
            float eased = EaseInOutCubic(progress);
            selected.Transform.position = Vector3.Lerp(selected.StartPosition, revealPack.position, eased);
            selected.Transform.rotation = Quaternion.Slerp(selected.StartRotation, revealPack.rotation, eased);
            selected.Transform.localScale = Vector3.Lerp(selected.StartScale, revealPack.lossyScale, eased);

            for (int index = 0; index < options.Count; index++)
            {
                PackOption option = options[index];
                if (option == selected)
                {
                    continue;
                }

                float exit = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.62f, progress));
                option.Transform.localScale = option.StartScale * (1f - exit * 0.85f);
                option.Transform.position = option.StartPosition +
                    new Vector3((option.Index % 3 - 1) * exit * 0.8f, -exit * 0.45f, exit * 0.6f);
            }

            if (progress >= 1f)
            {
                CompleteHandoff();
            }
        }

        private void CompleteHandoff()
        {
            RevealPayload payload = DemoCardFactory.CreateRandom();
            payload.packTypeId = "rippies_" + selected.Theme.Id;
            payload.card.accentHex = selected.Theme.AccentHex;

            CleanupGrid();
            if (bridge != null)
            {
                bridge.PrepareReveal(JsonUtility.ToJson(payload));
            }
            else
            {
                controller?.PrepareReveal(payload);
            }

            SetPackLabelsActive(true);
            if (packSubtitle != null)
            {
                TextMesh subtitle = packSubtitle.GetComponent<TextMesh>();
                if (subtitle != null)
                {
                    subtitle.text = selected.Theme.DisplayName + " PACK";
                }
            }


            if (revealOverlay != null)
            {
                revealOverlay.enabled = true;
            }

            state = FlowState.Reveal;
        }

        private void SetPackLabelsActive(bool active)
        {
            if (packWordmark != null)
            {
                packWordmark.gameObject.SetActive(active);
            }

            if (packSubtitle != null)
            {
                packSubtitle.gameObject.SetActive(active);
            }
        }

        private void ApplyTheme(GameObject pack, ThemeDefinition theme)
        {
            Color accent = ColorUtility.TryParseHtmlString(theme.AccentHex, out Color parsed)
                ? parsed
                : new Color(0.2f, 0.95f, 0.8f);
            Color baseColor = Color.Lerp(new Color(0.006f, 0.012f, 0.028f), accent, 0.2f);
            var properties = new MaterialPropertyBlock();

            foreach (Renderer renderer in pack.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer && renderer.GetComponent<TextMesh>() == null)
                {
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor(BaseColorId, baseColor);
                    properties.SetColor(AccentColorId, accent);
                    properties.SetColor(ColorId, baseColor);
                    renderer.SetPropertyBlock(properties);
                }
            }

            foreach (TextMesh text in pack.GetComponentsInChildren<TextMesh>(true))
            {
                if (text.name == "PackSubtitle")
                {
                    text.text = theme.DisplayName;
                    text.color = Color.Lerp(Color.white, accent, 0.3f);
                }
                else if (text.name == "PackWordmark")
                {
                    text.color = Color.Lerp(Color.white, accent, 0.65f);
                }
            }
        }

        private void CleanupGrid()
        {
            options.Clear();
            if (gridRoot != null)
            {
                Destroy(gridRoot.gameObject);
                gridRoot = null;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (state == FlowState.Browse || state == FlowState.Transition)
            {
                DrawBrowseChrome();
            }
            else if (state == FlowState.Reveal && controller != null && controller.State == RipState.Complete)
            {
                if (GUI.Button(new Rect(24f, 24f, 118f, 38f), "‹  PACKS", buttonStyle))
                {
                    ReturnToGrid();
                }
            }
        }

        private void DrawBrowseChrome()
        {
            float center = Screen.width * 0.5f;
            GUI.Label(new Rect(center - 220f, 22f, 440f, 22f), "RIPPIES COLLECTION", eyebrowStyle);
            GUI.Label(new Rect(center - 280f, 48f, 560f, 42f), "Choose your pack", titleStyle);
            GUI.Label(
                new Rect(center - 300f, 90f, 600f, 26f),
                state == FlowState.Transition ? "Preparing your reveal…" : "Tap a pack to bring it forward",
                subtitleStyle);

            if (state != FlowState.Browse || sceneCamera == null)
            {
                return;
            }

            for (int index = 0; index < options.Count; index++)
            {
                PackOption option = options[index];
                Vector3 screen = sceneCamera.WorldToScreenPoint(option.Transform.position);
                float guiY = Screen.height - screen.y + 83f;
                GUI.Label(
                    new Rect(screen.x - 100f, guiY, 200f, 24f),
                    option.Theme.DisplayName,
                    packNameStyle);
                GUI.Label(
                    new Rect(screen.x - 45f, guiY + 24f, 90f, 20f),
                    "AVAILABLE",
                    badgeStyle);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.32f, 0.92f, 0.86f) }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.68f, 0.74f, 0.8f) }
            };
            packNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.95f, 0.78f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        private static float EaseInOutCubic(float value)
        {
            return value < 0.5f
                ? 4f * value * value * value
                : 1f - Mathf.Pow(-2f * value + 2f, 3f) * 0.5f;
        }
    }
}
