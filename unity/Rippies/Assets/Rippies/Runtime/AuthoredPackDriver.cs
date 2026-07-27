using System.Collections.Generic;
using UnityEngine;

namespace Rippies.Reveal
{
    /// <summary>
    /// Adapts the locally licensed Animated Card Loot Pack to the Rippies
    /// interaction state machine. The source GLB intentionally stays out of
    /// this public repository; when absent, the procedural pack remains active.
    /// </summary>
    public sealed class AuthoredPackDriver : MonoBehaviour
    {
        private const string ResourcePath =
            "Rippies/ThirdParty/Local/animated_card_loot_pack";
        private const float PresentationStart = 0.04f;
        private const float PresentationEnd = 3.88f;
        private const float TearStart = 5.88f;
        private const float SwipeRelease = 6.84f;
        private const float TearEnd = 7.48f;
        private const float CardSettle = 8.84f;
        private const float TargetPackHeight = 4.72f;

        private static readonly int BaseColorFactorId =
            Shader.PropertyToID("baseColorFactor");
        private static readonly int EmissiveFactorId =
            Shader.PropertyToID("emissiveFactor");
        private static readonly int BaseColorTextureId =
            Shader.PropertyToID("baseColorTexture");
        private static readonly int BaseMapId =
            Shader.PropertyToID("_BaseMap");
        private static readonly int MainTextureId =
            Shader.PropertyToID("_MainTex");

        private readonly List<Renderer> fallbackRenderers = new List<Renderer>();
        private readonly List<Renderer> authoredRenderers = new List<Renderer>();
        private MaterialPropertyBlock materialProperties;
        private Transform contentRoot;
        private Transform demoCard;
        private Transform demoCardOriginalParent;
        private Vector3 demoCardOriginalLocalScale;
        private Transform presentationCard;
        private GameObject instance;
        private AnimationClip clip;
        private Texture originalCardAtlas;
        private Texture2D generatedCardAtlas;
        private bool initialized;

        public bool IsAvailable { get; private set; }
        public Transform PresentationCard => presentationCard;
        public Transform AnimatedCard => demoCard;

        public bool Initialize(Transform packRoot)
        {
            if (initialized)
            {
                return IsAvailable;
            }

            initialized = true;
            if (packRoot == null)
            {
                return false;
            }

            GameObject source = Resources.Load<GameObject>(ResourcePath);
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(ResourcePath);
            if (source == null || clips == null || clips.Length == 0)
            {
                Debug.Log(
                    "Licensed authored pack not found. Using the procedural foil fallback.");
                return false;
            }

            clip = LongestClip(clips);
            if (clip == null)
            {
                return false;
            }

            fallbackRenderers.AddRange(packRoot.GetComponentsInChildren<Renderer>(true));

            var rootObject = new GameObject("AuthoredCardLootPack");
            contentRoot = rootObject.transform;
            contentRoot.SetParent(packRoot, false);

            instance = Instantiate(source, contentRoot);
            instance.name = "AnimatedCardLootPack";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
            }

            clip.SampleAnimation(instance, PresentationStart);
            demoCard = FindDescendant(instance.transform, "Demo_Card_1_3");
            demoCardOriginalParent = demoCard == null ? null : demoCard.parent;
            demoCardOriginalLocalScale =
                demoCard == null ? Vector3.one : demoCard.localScale;
            originalCardAtlas = FindCardAtlas();
            authoredRenderers.AddRange(instance.GetComponentsInChildren<Renderer>(true));
            FitToPack();

            foreach (Renderer renderer in fallbackRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            materialProperties = new MaterialPropertyBlock();
            IsAvailable = true;
            Debug.Log(
                "Using locally licensed Animated Card Loot Pack for the reveal choreography.");
            return true;
        }

        public void ResetModel()
        {
            if (!IsAvailable)
            {
                return;
            }

            RestoreAnimatedCardHierarchy();
            instance.SetActive(true);
            SampleAt(PresentationStart);
        }

        public void SamplePresentation(float progress)
        {
            SampleAt(Mathf.Lerp(
                PresentationStart,
                PresentationEnd,
                Mathf.Clamp01(progress)));
        }

        public void SampleSwipe(float progress)
        {
            SampleAt(Mathf.Lerp(
                TearStart,
                SwipeRelease,
                Mathf.Clamp01(progress)));
        }

        public void SampleCommittedTear(float progress)
        {
            SampleAt(Mathf.Lerp(
                SwipeRelease,
                TearEnd,
                Mathf.Clamp01(progress)));
        }

        public void SampleOpening(float progress)
        {
            SampleAt(Mathf.Lerp(
                TearEnd,
                CardSettle,
                Mathf.Clamp01(progress)));
        }

        public void SampleComplete()
        {
            SampleAt(CardSettle);
        }

        public Transform TakeOverCard(Transform presentationParent)
        {
            if (!IsAvailable || demoCard == null || presentationParent == null)
            {
                return null;
            }

            if (presentationCard != null)
            {
                return presentationCard;
            }

            SampleComplete();
            bool hasBounds = TryGetRendererBounds(demoCard, out Bounds cardBounds);
            var pivotObject = new GameObject("AuthoredCardPresentation");
            presentationCard = pivotObject.transform;
            presentationCard.position = hasBounds ? cardBounds.center : demoCard.position;
            presentationCard.rotation = demoCard.rotation;
            presentationCard.localScale = Vector3.one;
            presentationCard.SetParent(presentationParent, true);
            demoCard.SetParent(presentationCard, true);

            return presentationCard;
        }

        public void SetAccent(Color accent)
        {
            if (!IsAvailable)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            Color tint = Color.Lerp(Color.white, accent, 0.14f);
            Color emission = Color.Lerp(Color.black, accent, 0.08f);
            foreach (Renderer renderer in authoredRenderers)
            {
                if (renderer == null || IsDemoCard(renderer.transform))
                {
                    continue;
                }

                renderer.GetPropertyBlock(materialProperties);
                materialProperties.SetColor(BaseColorFactorId, tint);
                materialProperties.SetColor(EmissiveFactorId, emission);
                renderer.SetPropertyBlock(materialProperties);
            }
        }

        public void SetCard(CardPayload card, string packTypeId)
        {
            if (!IsAvailable || demoCard == null || card == null)
            {
                return;
            }

            if (generatedCardAtlas != null)
            {
                Destroy(generatedCardAtlas);
            }

            Texture2D front = CardFaceTextureFactory.BuildFront(card);
            Texture2D back = CardFaceTextureFactory.BuildBack(card, packTypeId);
            generatedCardAtlas = BakeFacesIntoAtlas(
                originalCardAtlas,
                front,
                back);
            Destroy(front);
            Destroy(back);
            if (generatedCardAtlas == null)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in demoCard.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(materialProperties);
                materialProperties.SetTexture(BaseColorTextureId, generatedCardAtlas);
                materialProperties.SetTexture(BaseMapId, generatedCardAtlas);
                materialProperties.SetTexture(MainTextureId, generatedCardAtlas);
                materialProperties.SetColor(BaseColorFactorId, Color.white);
                renderer.SetPropertyBlock(materialProperties);
            }
        }

        private Texture FindCardAtlas()
        {
            if (demoCard == null)
            {
                return null;
            }

            foreach (Renderer renderer in demoCard.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    Texture texture = material.HasProperty(BaseColorTextureId)
                        ? material.GetTexture(BaseColorTextureId)
                        : null;
                    texture ??= material.HasProperty(BaseMapId)
                        ? material.GetTexture(BaseMapId)
                        : null;
                    texture ??= material.HasProperty(MainTextureId)
                        ? material.GetTexture(MainTextureId)
                        : null;
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Texture2D BakeFacesIntoAtlas(
            Texture atlas,
            Texture2D front,
            Texture2D back)
        {
            if (atlas == null ||
                front == null ||
                back == null ||
                atlas.width <= 0 ||
                atlas.height <= 0)
            {
                return null;
            }

            RenderTexture temporary = RenderTexture.GetTemporary(
                atlas.width,
                atlas.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(atlas, temporary);
            RenderTexture.active = temporary;

            var result = new Texture2D(
                atlas.width,
                atlas.height,
                TextureFormat.RGBA32,
                true)
            {
                name = "Rippies_AuthoredCardAtlas",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 8
            };
            result.ReadPixels(new Rect(0f, 0f, atlas.width, atlas.height), 0, 0);

            Color32[] atlasPixels = result.GetPixels32();
            CompositeFace(
                atlasPixels,
                atlas.width,
                atlas.height,
                front,
                0.548f,
                0f,
                0.39f,
                0.765f);
            CompositeFace(
                atlasPixels,
                atlas.width,
                atlas.height,
                back,
                0.047f,
                0f,
                0.404f,
                0.765f);

            result.SetPixels32(atlasPixels);
            result.Apply(true, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            return result;
        }

        private static void CompositeFace(
            Color32[] atlasPixels,
            int atlasWidth,
            int atlasHeight,
            Texture2D face,
            float normalizedLeft,
            float normalizedBottom,
            float normalizedWidth,
            float normalizedHeight)
        {
            Color32[] facePixels = face.GetPixels32();
            int left = Mathf.RoundToInt(atlasWidth * normalizedLeft);
            int bottom = Mathf.RoundToInt(atlasHeight * normalizedBottom);
            int width = Mathf.RoundToInt(atlasWidth * normalizedWidth);
            int height = Mathf.RoundToInt(atlasHeight * normalizedHeight);
            for (int y = 0; y < height; y++)
            {
                int sourceY = Mathf.Clamp(
                    Mathf.RoundToInt(y / Mathf.Max(1f, height - 1f) * (face.height - 1)),
                    0,
                    face.height - 1);
                for (int x = 0; x < width; x++)
                {
                    int sourceX = Mathf.Clamp(
                        Mathf.RoundToInt(x / Mathf.Max(1f, width - 1f) * (face.width - 1)),
                        0,
                        face.width - 1);
                    int targetX = left + x;
                    int targetY = bottom + y;
                    if (targetX < 0 ||
                        targetX >= atlasWidth ||
                        targetY < 0 ||
                        targetY >= atlasHeight)
                    {
                        continue;
                    }

                    atlasPixels[targetY * atlasWidth + targetX] =
                        facePixels[sourceY * face.width + sourceX];
                }
            }
        }

        private void SampleAt(float time)
        {
            if (!IsAvailable || clip == null || instance == null)
            {
                return;
            }

            clip.SampleAnimation(instance, Mathf.Clamp(time, 0f, clip.length));
        }

        private void RestoreAnimatedCardHierarchy()
        {
            if (demoCard == null || demoCardOriginalParent == null)
            {
                return;
            }

            if (presentationCard != null)
            {
                demoCard.SetParent(demoCardOriginalParent, false);
                // The animation does not key scale. Reparenting through the
                // presentation pivot therefore has to restore it explicitly or
                // every reveal inherits the previous pivot's closing scale.
                demoCard.localScale = demoCardOriginalLocalScale;
                Destroy(presentationCard.gameObject);
                presentationCard = null;
            }
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = default;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void FitToPack()
        {
            bool hasBounds = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsDemoCard(renderer.transform))
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 worldCorner = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner =
                                contentRoot.InverseTransformPoint(worldCorner);
                            if (!hasBounds)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            if (!hasBounds || localBounds.size.y <= 0.001f)
            {
                return;
            }

            instance.transform.localPosition -= localBounds.center;
            float scale = TargetPackHeight / localBounds.size.y;
            contentRoot.localScale = Vector3.one * scale;
        }

        private static bool IsDemoCard(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name == "Demo_Card_1_3")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static AnimationClip LongestClip(AnimationClip[] clips)
        {
            AnimationClip longest = null;
            foreach (AnimationClip candidate in clips)
            {
                if (candidate != null &&
                    (longest == null || candidate.length > longest.length))
                {
                    longest = candidate;
                }
            }

            return longest;
        }
    }
}
