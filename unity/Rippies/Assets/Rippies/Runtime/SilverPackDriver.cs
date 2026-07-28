using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rippies.Reveal
{
    /// <summary>
    /// Adapts the locally licensed silver packet GLB to the shared Rippies
    /// reveal state machine. The GLB supplies the packet blow-apart and initial
    /// four-card fan; immutable receipt cards are rendered with the checked-in
    /// Rippies card template and handed to CardGroupPresentation for the final
    /// five-card grid and inspection interaction.
    /// </summary>
    public sealed class SilverPackDriver : MonoBehaviour
    {
        private const string ResourcePath =
            "Rippies/ThirdParty/Local/loot_packet_silver";
        private const float ClosedTime = 0f;
        private const float BlowStart = 0.78f;
        private const float PacketGone = 1.6f;
        private const float CardFanStart = 1.8166667f;
        private const float CardFanEnd = 2.7833333f;
        private const float TargetPackHeight = 3.18f;
        private const float HeroRevealStart = 0.16f;
        private const float HeroRevealSettle = 0.72f;

        private static readonly int BaseColorFactorId =
            Shader.PropertyToID("baseColorFactor");
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");
        private static readonly int EmissiveFactorId =
            Shader.PropertyToID("emissiveFactor");

        private readonly List<Transform> cardNodes = new List<Transform>();
        private readonly List<Transform> generatedCards = new List<Transform>();
        private readonly List<Transform> presentationCards = new List<Transform>();
        private readonly List<Renderer> packetRenderers = new List<Renderer>();

        private Transform contentRoot;
        private Transform cardTemplate;
        private Transform heroCardPivot;
        private AuthoredPackDriver authoredCardSource;
        private GameObject instance;
        private AnimationClip clip;
        private MaterialPropertyBlock materialProperties;
        private CardPayload[] assignedCards = Array.Empty<CardPayload>();
        private string assignedPackTypeId = "";
        private bool initialized;
        private bool experienceActive;

        public bool IsAvailable { get; private set; }
        public Transform AnimatedCard =>
            generatedCards.Count > 0
                ? generatedCards[0]
                : cardNodes.Count > 0
                    ? cardNodes[0]
                    : null;

        public bool Initialize(
            Transform packRoot,
            Transform generatedCardTemplate,
            AuthoredPackDriver cardSource)
        {
            if (initialized)
            {
                return IsAvailable;
            }

            initialized = true;
            cardTemplate = generatedCardTemplate;
            authoredCardSource = cardSource;
            if (packRoot == null || cardTemplate == null)
            {
                return false;
            }

            GameObject source = Resources.Load<GameObject>(ResourcePath);
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(ResourcePath);
            clip = LongestClip(clips);
            if (source == null || clip == null)
            {
                Debug.Log(
                    "Silver packet reveal not found. The alternate authored reveal remains available.");
                return false;
            }

            var rootObject = new GameObject("SilverPacketReveal");
            contentRoot = rootObject.transform;
            contentRoot.SetParent(packRoot, false);
            contentRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);

            instance = Instantiate(source, contentRoot);
            instance.name = "LootPacketSilver";
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

            SampleAt(ClosedTime);
            AddCardNode("Card1");
            AddCardNode("Card3");
            AddCardNode("Card4");
            AddCardNode("Card2");

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && !IsCardNode(renderer.transform))
                {
                    packetRenderers.Add(renderer);
                }
            }

            FitToPack();
            SetOriginalCardRenderers(false);
            materialProperties = new MaterialPropertyBlock();
            IsAvailable = cardNodes.Count > 0;
            SetExperienceActive(false);

            if (IsAvailable)
            {
                Debug.Log(
                    "Using locally licensed Silver Packet for the default reveal choreography.");
            }

            return IsAvailable;
        }

        public void SetExperienceActive(bool active)
        {
            experienceActive = active && IsAvailable;
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(experienceActive);
            }

            if (instance != null)
            {
                instance.SetActive(experienceActive);
            }
        }

        public void ResetModel()
        {
            ClearCardVisuals();
            assignedCards = Array.Empty<CardPayload>();
            assignedPackTypeId = "";
            if (!IsAvailable)
            {
                return;
            }

            instance.SetActive(experienceActive);
            SampleAt(ClosedTime);
            SetOriginalCardRenderers(false);
        }

        public void SetCards(CardPayload[] cards, string packTypeId)
        {
            if (!IsAvailable)
            {
                return;
            }

            ClearCardVisuals();
            assignedCards = cards ?? Array.Empty<CardPayload>();
            assignedPackTypeId = packTypeId ?? "";
            SampleAt(ClosedTime);

            int animatedCount = Mathf.Min(
                Mathf.Min(cardNodes.Count, assignedCards.Length),
                4);
            for (int index = 0; index < animatedCount; index++)
            {
                Transform node = cardNodes[index];
                Renderer sourceRenderer = node.GetComponent<Renderer>();
                if (sourceRenderer == null)
                {
                    continue;
                }

                Bounds targetBounds = sourceRenderer.bounds;
                Transform visual = CreateCardVisual(
                    node,
                    assignedCards[index]);
                if (visual == null)
                {
                    continue;
                }

                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                FitVisualToBounds(visual, targetBounds);
                visual.gameObject.SetActive(false);
                generatedCards.Add(visual);
            }

            SetOriginalCardRenderers(false);
        }

        public void SetAccent(Color accent)
        {
            if (!IsAvailable)
            {
                return;
            }

            materialProperties ??= new MaterialPropertyBlock();
            Color tint = Color.Lerp(Color.white, accent, 0.08f);
            Color emission = Color.Lerp(Color.black, accent, 0.12f);
            foreach (Renderer renderer in packetRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(materialProperties);
                materialProperties.SetColor(BaseColorFactorId, tint);
                materialProperties.SetColor(BaseColorId, tint);
                materialProperties.SetColor(ColorId, tint);
                materialProperties.SetColor(EmissiveFactorId, emission);
                renderer.SetPropertyBlock(materialProperties);
            }
        }

        public void SamplePresentation(float progress)
        {
            SetGeneratedCardsVisible(false);
            SampleAt(ClosedTime);
        }

        public void SampleSwipe(float progress)
        {
            SetGeneratedCardsVisible(false);
            SampleAt(Mathf.Lerp(
                ClosedTime,
                BlowStart,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress))));
        }

        public void SampleCommittedTear(float progress)
        {
            float value = Mathf.Clamp01(progress);
            SampleAt(Mathf.Lerp(
                BlowStart,
                PacketGone,
                Mathf.SmoothStep(0f, 1f, value)));
            SetOnlyHeroVisible(false);
            if (value >= HeroRevealStart)
            {
                EnsureCenteredHero(value);
            }
        }

        public void SampleOpening(float progress)
        {
            SampleAt(Mathf.Lerp(
                CardFanStart,
                CardFanEnd,
                Mathf.Clamp01(progress)));
            if (presentationCards.Count > 0)
            {
                SetGeneratedCardsVisible(true);
                return;
            }

            SetOnlyHeroVisible(true);
            EnsureCenteredHero(1f);
        }

        public IReadOnlyList<Transform> TakeOverCards(
            Transform presentationParent,
            int maximumCardCount)
        {
            if (!IsAvailable ||
                presentationParent == null ||
                generatedCards.Count == 0)
            {
                return Array.Empty<Transform>();
            }

            if (presentationCards.Count > 0)
            {
                return presentationCards;
            }

            SampleAt(CardFanEnd);
            SetGeneratedCardsVisible(true);
            int targetCount = Mathf.Min(
                Mathf.Min(maximumCardCount, assignedCards.Length),
                5);
            int animatedCount = Mathf.Min(generatedCards.Count, targetCount);
            for (int index = 0; index < animatedCount; index++)
            {
                Transform visual = generatedCards[index];
                if (visual == null)
                {
                    continue;
                }

                if (index == 0 && heroCardPivot != null)
                {
                    heroCardPivot.SetParent(presentationParent, true);
                    presentationCards.Add(heroCardPivot);
                    continue;
                }

                TryGetRendererBounds(visual, out Bounds bounds);
                var pivotObject = new GameObject(
                    "SilverCardPresentation_" + assignedCards[index].id);
                Transform pivot = pivotObject.transform;
                pivot.position = bounds.center;
                pivot.rotation = visual.rotation;
                pivot.localScale = Vector3.one;
                pivot.SetParent(presentationParent, true);
                visual.SetParent(pivot, true);
                presentationCards.Add(pivot);
            }

            if (presentationCards.Count > 0 &&
                presentationCards.Count < targetCount)
            {
                AddFifthCard(presentationParent, presentationCards.Count);
            }

            return presentationCards;
        }

        private void AddFifthCard(Transform presentationParent, int cardIndex)
        {
            if (cardIndex >= assignedCards.Length ||
                presentationCards.Count == 0)
            {
                return;
            }

            Bounds referenceBounds = RendererBounds(presentationCards[0]);
            Vector3 center = Vector3.zero;
            foreach (Transform target in presentationCards)
            {
                center += target.position;
            }

            center /= presentationCards.Count;
            var pivotObject = new GameObject(
                "SilverCardPresentation_" + assignedCards[cardIndex].id);
            Transform pivot = pivotObject.transform;
            pivot.position = center +
                (Camera.main == null
                    ? Vector3.forward * 0.012f
                    : Camera.main.transform.forward * 0.012f);
            pivot.rotation = presentationCards[0].rotation;
            pivot.localScale = Vector3.one;
            pivot.SetParent(presentationParent, true);

            Transform visual = CreateCardVisual(
                pivot,
                assignedCards[cardIndex]);
            if (visual == null)
            {
                Destroy(pivotObject);
                return;
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            FitVisualToBounds(
                visual,
                new Bounds(pivot.position, referenceBounds.size));

            generatedCards.Add(visual);
            presentationCards.Add(pivot);
        }

        private Transform CreateCardVisual(
            Transform parent,
            CardPayload card)
        {
            Transform authoredVisual = authoredCardSource?.CreateDetachedCardVisual(
                parent,
                card,
                assignedPackTypeId);
            if (authoredVisual != null)
            {
                return authoredVisual;
            }

            GameObject visualObject = Instantiate(
                cardTemplate.gameObject,
                parent,
                false);
            visualObject.name = "SilverAnimatedCard_" + card.id;
            visualObject.SetActive(true);
            visualObject.GetComponent<GeneratedCardPresenter>()?.Apply(card);
            return visualObject.transform;
        }

        private void AddCardNode(string nodeName)
        {
            Transform found = FindDescendant(instance.transform, nodeName);
            if (found != null)
            {
                cardNodes.Add(found);
            }
        }

        private void ClearCardVisuals()
        {
            foreach (Transform visual in generatedCards)
            {
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
            }

            foreach (Transform pivot in presentationCards)
            {
                if (pivot != null)
                {
                    Destroy(pivot.gameObject);
                }
            }

            generatedCards.Clear();
            presentationCards.Clear();
            if (heroCardPivot != null)
            {
                Destroy(heroCardPivot.gameObject);
                heroCardPivot = null;
            }
        }

        private void FitToPack()
        {
            if (packetRenderers.Count == 0)
            {
                return;
            }

            Bounds worldBounds = packetRenderers[0].bounds;
            for (int index = 1; index < packetRenderers.Count; index++)
            {
                worldBounds.Encapsulate(packetRenderers[index].bounds);
            }

            Vector3 localCenter = contentRoot.InverseTransformPoint(
                worldBounds.center);
            instance.transform.localPosition -= localCenter;

            float localHeight = worldBounds.size.y /
                Mathf.Max(contentRoot.lossyScale.y, 0.0001f);
            if (localHeight > 0.001f)
            {
                contentRoot.localScale = Vector3.one *
                    (TargetPackHeight / localHeight);
            }
        }

        private static void FitVisualToBounds(Transform visual, Bounds target)
        {
            if (visual == null ||
                !TryGetRendererBounds(visual, out Bounds visualBounds))
            {
                return;
            }

            float widthRatio = target.size.x /
                Mathf.Max(visualBounds.size.x, 0.001f);
            float heightRatio = target.size.y /
                Mathf.Max(visualBounds.size.y, 0.001f);
            float scale = Mathf.Max(
                0.001f,
                Mathf.Min(widthRatio, heightRatio) * 0.96f);
            visual.localScale *= scale;

            TryGetRendererBounds(visual, out visualBounds);
            visual.position += target.center - visualBounds.center;
        }

        private void SetOriginalCardRenderers(bool visible)
        {
            foreach (Transform node in cardNodes)
            {
                foreach (Renderer renderer in node.GetComponents<Renderer>())
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void SetGeneratedCardsVisible(bool visible)
        {
            foreach (Transform visual in generatedCards)
            {
                if (visual != null)
                {
                    visual.gameObject.SetActive(visible);
                }
            }
        }

        private void SetOnlyHeroVisible(bool visible)
        {
            for (int index = 0; index < generatedCards.Count; index++)
            {
                Transform visual = generatedCards[index];
                if (visual != null)
                {
                    visual.gameObject.SetActive(visible && index == 0);
                }
            }
        }

        private void EnsureCenteredHero(float progress)
        {
            if (generatedCards.Count == 0 || generatedCards[0] == null)
            {
                return;
            }

            Transform visual = generatedCards[0];
            Camera sceneCamera = Camera.main;
            Transform packTransform = contentRoot == null
                ? null
                : contentRoot.parent;
            if (heroCardPivot == null)
            {
                TryGetRendererBounds(visual, out Bounds sourceBounds);
                var pivotObject = new GameObject("SilverCenteredHeroCard");
                heroCardPivot = pivotObject.transform;
                Transform stableParent = packTransform == null
                    ? transform
                    : packTransform.parent;
                heroCardPivot.SetParent(stableParent, true);
                heroCardPivot.position = packTransform == null
                    ? transform.position
                    : packTransform.position;
                heroCardPivot.rotation = sceneCamera == null
                    ? visual.rotation
                    : Quaternion.LookRotation(
                        sceneCamera.transform.forward,
                        sceneCamera.transform.up);
                heroCardPivot.localScale = Vector3.one;

                visual.SetParent(heroCardPivot, false);
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                FitVisualToBounds(
                    visual,
                    new Bounds(heroCardPivot.position, sourceBounds.size));
            }

            float reveal = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(
                    HeroRevealStart,
                    HeroRevealSettle,
                    Mathf.Clamp01(progress)));
            Vector3 center = packTransform == null
                ? transform.position
                : packTransform.position;
            if (sceneCamera != null)
            {
                center += sceneCamera.transform.forward *
                    Mathf.Lerp(0.08f, -0.14f, reveal);
            }

            heroCardPivot.position = center;
            heroCardPivot.localScale = Vector3.one *
                Mathf.Lerp(0.84f, 1f, reveal);
            visual.gameObject.SetActive(true);
        }

        private void SampleAt(float time)
        {
            if (clip == null || instance == null)
            {
                return;
            }

            clip.SampleAnimation(instance, Mathf.Clamp(time, 0f, clip.length));
        }

        private static bool IsCardNode(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name == "Card1" ||
                    current.name == "Card2" ||
                    current.name == "Card3" ||
                    current.name == "Card4")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Bounds RendererBounds(Transform target)
        {
            TryGetRendererBounds(target, out Bounds bounds);
            return bounds;
        }

        private static bool TryGetRendererBounds(
            Transform target,
            out Bounds bounds)
        {
            bounds = new Bounds(
                target == null ? Vector3.zero : target.position,
                Vector3.one * 0.01f);
            bool hasBounds = false;
            if (target == null)
            {
                return false;
            }

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
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
            if (clips == null)
            {
                return null;
            }

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
