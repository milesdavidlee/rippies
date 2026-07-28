using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class RevealDirector : MonoBehaviour
    {
        [SerializeField] private Transform packRoot;
        [SerializeField] private Transform card;
        [SerializeField] private Renderer cardRenderer;
        [SerializeField] private Light revealLight;
        [SerializeField] private RevealGlowPulse revealGlow;
        [SerializeField] private SoftOrbitCamera softOrbit;
        [SerializeField] private float reducedMotionMultiplier = 0.35f;
        [SerializeField, Range(1f, 2f)] private float standardMotionMultiplier = 1.45f;
        [SerializeField] private float silverArrivalSeconds = 0.48f;
        [SerializeField] private float silverTearSeconds = 1.35f;
        [SerializeField] private float silverHeroSeconds = 0.78f;
        [SerializeField] private float silverSettleSeconds = 0f;
        [SerializeField] private float silverFanSeconds = 1.15f;
        [SerializeField, Range(0f, 0.25f)] private float silverGlowStrength = 0f;
        [SerializeField, Range(0.08f, 0.5f)] private float packDragSensitivity = 0.22f;
        [SerializeField, Range(10f, 45f)] private float maximumPackPitch = 28f;
        [SerializeField] private float packRotationSmoothing = 12f;

        private Transform interactiveCard;
        private readonly List<Transform> interactiveCards = new List<Transform>();
        private CardGroupPresentation cardGroup;
        private Vector3 cardStartPosition;
        private Quaternion cardStartRotation;
        private Vector3 cardStartScale;
        private Vector3 packStartPosition;
        private Quaternion packStartRotation;
        private Vector3 packStartScale;
        private Coroutine sequence;
        private bool presentationIdle;
        private float idleStartedAt;
        private float desiredPackYaw;
        private float desiredPackPitch;
        private float currentPackYaw;
        private float currentPackPitch;

        public Transform CardTemplate => card;

        private void Awake()
        {
            softOrbit ??= FindFirstObjectByType<SoftOrbitCamera>();
            interactiveCard = card;
            CaptureInitialPose();
        }

        private void Update()
        {
            if (!presentationIdle || packRoot == null)
            {
                return;
            }

            float time = Time.unscaledTime - idleStartedAt;
            float hover = Mathf.Sin(time * 1.45f);
            float rotationDamp =
                1f - Mathf.Exp(-packRotationSmoothing * Time.unscaledDeltaTime);
            currentPackYaw = Mathf.Lerp(
                currentPackYaw,
                desiredPackYaw,
                rotationDamp);
            currentPackPitch = Mathf.Lerp(
                currentPackPitch,
                desiredPackPitch,
                rotationDamp);
            packRoot.localPosition = packStartPosition +
                new Vector3(0f, hover * 0.028f, 0f);
            packRoot.localRotation = packStartRotation * Quaternion.Euler(
                currentPackPitch + Mathf.Sin(time * 0.72f) * 0.35f,
                currentPackYaw + Mathf.Sin(time * 0.58f + 0.4f) * 0.65f,
                Mathf.Sin(time * 0.9f) * 0.28f);
            packRoot.localScale = packStartScale;
        }

        public void CaptureInitialPose()
        {
            if (packRoot != null)
            {
                packStartPosition = packRoot.localPosition;
                packStartRotation = packRoot.localRotation;
                packStartScale = packRoot.localScale;
            }

            if (card != null)
            {
                cardStartPosition = card.localPosition;
                cardStartRotation = card.localRotation;
                cardStartScale = card.localScale;
            }
        }

        public void SetPalette(Color accent)
        {
            revealGlow?.SetGlowColor(accent);
        }

        public void SetAuthoredPackAvailable(bool available)
        {
            if (card != null)
            {
                card.gameObject.SetActive(!available);
            }
        }

        public bool IsScreenPointOverPresentedPack(Vector2 screenPoint)
        {
            Camera sceneCamera = Camera.main;
            if (!presentationIdle ||
                packRoot == null ||
                sceneCamera == null)
            {
                return false;
            }

            Renderer[] renderers =
                packRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
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

            if (!hasBounds)
            {
                return false;
            }

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float minimumX = float.PositiveInfinity;
            float minimumY = float.PositiveInfinity;
            float maximumX = float.NegativeInfinity;
            float maximumY = float.NegativeInfinity;
            bool hasVisibleCorner = false;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldPoint = center + Vector3.Scale(
                    extents,
                    new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));
                Vector3 projected = sceneCamera.WorldToScreenPoint(worldPoint);
                if (projected.z <= 0f)
                {
                    continue;
                }

                hasVisibleCorner = true;
                minimumX = Mathf.Min(minimumX, projected.x);
                minimumY = Mathf.Min(minimumY, projected.y);
                maximumX = Mathf.Max(maximumX, projected.x);
                maximumY = Mathf.Max(maximumY, projected.y);
            }

            if (!hasVisibleCorner)
            {
                return false;
            }

            const float screenPadding = 18f;
            return new Rect(
                minimumX - screenPadding,
                minimumY - screenPadding,
                maximumX - minimumX + screenPadding * 2f,
                maximumY - minimumY + screenPadding * 2f)
                .Contains(screenPoint);
        }

        public void RotatePresentedPack(Vector2 screenDelta)
        {
            if (!presentationIdle)
            {
                return;
            }

            desiredPackYaw += screenDelta.x * packDragSensitivity;
            desiredPackPitch = Mathf.Clamp(
                desiredPackPitch - screenDelta.y * packDragSensitivity,
                -maximumPackPitch,
                maximumPackPitch);
        }

        public void PlayPresentation(PackRipController owner, bool reducedMotion)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            presentationIdle = false;
            sequence = StartCoroutine(PresentationSequence(owner, reducedMotion));
        }

        public void Play(PackRipController owner, bool reducedMotion)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            presentationIdle = false;
            sequence = StartCoroutine(PlaySequence(owner, reducedMotion));
        }

        public void SkipToComplete(PackRipController owner)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            presentationIdle = false;
            owner.SetCinematicTear(1f, 1f);
            owner.SampleAuthoredOpening(1f);
            SetRevealGlow(owner, 1f);
            interactiveCard = ResolveInteractiveCard(owner);
            SetPackPresentedPose();
            if (interactiveCard != null)
            {
                interactiveCard.gameObject.SetActive(true);
                if (!owner.HasAuthoredPack)
                {
                    interactiveCard.localPosition = new Vector3(0f, 0.02f, -1.6f);
                    interactiveCard.localRotation = Quaternion.identity;
                    interactiveCard.localScale = cardStartScale * 1.05f;
                }
                BuildCardGroup(owner, interactiveCard);
                cardGroup?.SetRevealProgress(1f);
                if (owner.IsInspectionMode)
                {
                    cardGroup?.SelectIndex(0);
                }

                softOrbit?.SetCardGroup(cardGroup);
            }

            if (revealLight != null)
            {
                revealLight.intensity = RevealLightIntensity(owner, 4.5f);
            }

            owner.NotifyOpening();
            owner.NotifyCardVisible();
            owner.NotifyRevealComplete();
            cardGroup?.EnableInteraction();
        }

        public void CloseToCollection(PackRipController owner)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }

            presentationIdle = false;
            sequence = StartCoroutine(CloseSequence(owner));
        }

        public void ResetSequence()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
            }

            presentationIdle = false;
            ResetPackInspectionRotation();
            revealGlow?.SetRevealAmount(0f);
            cardGroup?.DisposeCopies();
            cardGroup = null;
            interactiveCards.Clear();
            interactiveCard = card;
            softOrbit?.SetCard(card);

            if (packRoot != null)
            {
                packRoot.gameObject.SetActive(true);
                packRoot.localPosition = packStartPosition;
                packRoot.localRotation = packStartRotation;
                packRoot.localScale = packStartScale;
            }

            if (card != null)
            {
                card.localPosition = cardStartPosition;
                card.localRotation = cardStartRotation;
                card.localScale = cardStartScale;
                card.gameObject.SetActive(false);
            }

            if (revealLight != null)
            {
                revealLight.intensity = 0f;
            }
        }

        private IEnumerator PresentationSequence(PackRipController owner, bool reducedMotion)
        {
            if (packRoot == null)
            {
                owner.NotifyPresentationComplete();
                sequence = null;
                yield break;
            }

            bool centeredSilver = owner.UsesAuthoredCardFan;
            float motion = reducedMotion ? 0.58f : 1.35f;
            float spinDegrees = centeredSilver
                ? 0f
                : reducedMotion
                    ? 38f
                    : 240f;
            Vector3 incomingPosition = packStartPosition +
                (centeredSilver
                    ? new Vector3(0f, -0.12f, 0.22f)
                    : new Vector3(0f, -0.28f, reducedMotion ? 0.45f : 1.35f));
            Vector3 incomingScale = packStartScale *
                (centeredSilver ? 0.91f : reducedMotion ? 0.88f : 0.64f);

            packRoot.gameObject.SetActive(true);
            packRoot.localPosition = incomingPosition;
            packRoot.localRotation = packStartRotation *
                Quaternion.Euler(-7f, spinDegrees, -9f);
            packRoot.localScale = incomingScale;
            owner.SampleAuthoredPresentation(0f);

            yield return Tween(
                centeredSilver ? 0.04f : reducedMotion ? 0.1f : 0.22f,
                value => { });
            float arrivalDuration = centeredSilver
                ? reducedMotion
                    ? silverArrivalSeconds * reducedMotionMultiplier
                    : silverArrivalSeconds
                : 1.08f * motion;
            yield return Tween(arrivalDuration, value =>
            {
                float eased = EaseOutCubic(value);
                float landed = EaseOutBack(value);
                float remainingSpin = spinDegrees * (1f - eased);

                packRoot.localPosition = Vector3.LerpUnclamped(
                    incomingPosition,
                    packStartPosition,
                    landed);
                packRoot.localRotation = packStartRotation * Quaternion.Euler(
                    Mathf.Lerp(-7f, 0f, eased),
                    remainingSpin,
                    Mathf.Lerp(-9f, 0f, eased));
                packRoot.localScale = Vector3.LerpUnclamped(
                    incomingScale,
                    packStartScale,
                    landed);
                owner.SampleAuthoredPresentation(value);
            });

            packRoot.localPosition = packStartPosition;
            packRoot.localRotation = packStartRotation;
            packRoot.localScale = packStartScale;
            presentationIdle = true;
            idleStartedAt = Time.unscaledTime;
            owner.NotifyPresentationComplete();
            sequence = null;
        }

        private IEnumerator PlaySequence(PackRipController owner, bool reducedMotion)
        {
            bool centeredSilver = owner.UsesAuthoredCardFan;
            float motion = reducedMotion
                ? reducedMotionMultiplier
                : standardMotionMultiplier;
            float startingTear = owner.TearProgress;
            Vector3 openingPackPosition = packRoot == null
                ? packStartPosition
                : packRoot.localPosition;
            Quaternion openingPackRotation = packRoot == null
                ? packStartRotation
                : packRoot.localRotation;
            Vector3 openingPackScale = packRoot == null
                ? packStartScale
                : packRoot.localScale;
            Vector3 perspectivePosition = packStartPosition + new Vector3(0.12f, 0.08f, -0.18f);
            Quaternion perspectiveRotation = packStartRotation * Quaternion.Euler(9f, -18f, -4f);
            Vector3 perspectiveScale = Vector3.Scale(
                packStartScale,
                new Vector3(1.07f, 0.94f, 1f));

            float tearDuration = centeredSilver && !reducedMotion
                ? silverTearSeconds
                : 0.58f * motion;
            yield return Tween(tearDuration, value =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, value);
                float release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, value));
                owner.SetCinematicTear(Mathf.Lerp(startingTear, 1f, eased), release);
                SetRevealGlow(owner, Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.42f, 1f, value)));

                if (!owner.HasAuthoredPack && packRoot != null)
                {
                    float perspectiveEase = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.08f, 0.82f, value));
                    packRoot.localPosition = Vector3.Lerp(
                        openingPackPosition,
                        perspectivePosition,
                        perspectiveEase);
                    packRoot.localRotation = Quaternion.Slerp(
                        openingPackRotation,
                        perspectiveRotation,
                        perspectiveEase);
                    packRoot.localScale = Vector3.Lerp(
                        openingPackScale,
                        perspectiveScale,
                        eased);
                }
                else if (packRoot != null)
                {
                    packRoot.localPosition = Vector3.Lerp(
                        openingPackPosition,
                        packStartPosition,
                        eased);
                    packRoot.localRotation = Quaternion.Slerp(
                        openingPackRotation,
                        packStartRotation,
                        eased);
                    packRoot.localScale = Vector3.Lerp(
                        openingPackScale,
                        packStartScale,
                        eased);
                }

                if (revealLight != null)
                {
                    revealLight.intensity = RevealLightIntensity(
                        owner,
                        Mathf.Lerp(0f, 5.2f, eased));
                }
            });

            owner.NotifyOpening();
            if (!owner.HasAuthoredPack && card != null)
            {
                card.gameObject.SetActive(true);
            }

            Vector3 emergePosition = new Vector3(0f, 1.18f, -0.92f);
            Vector3 earlyPackDrop = packStartPosition + new Vector3(-0.1f, -1.08f, 0.52f);
            float heroDuration = centeredSilver && !reducedMotion
                ? silverHeroSeconds
                : 0.78f * motion;
            yield return Tween(heroDuration, value =>
            {
                float eased = EaseOutBack(value);
                owner.SampleAuthoredOpening(value);
                softOrbit?.TrackOpeningCard(
                    owner.HasAuthoredPack
                        ? owner.AuthoredAnimationCard
                        : card,
                    value);
                if (!owner.HasAuthoredPack && card != null)
                {
                    card.localPosition = Vector3.LerpUnclamped(cardStartPosition, emergePosition, eased);
                    card.localRotation = Quaternion.Slerp(
                        cardStartRotation,
                        Quaternion.Euler(0f, 10f, -2f),
                        Mathf.SmoothStep(0f, 1f, value));
                }

                if (!owner.HasAuthoredPack && packRoot != null)
                {
                    float packEase = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, value));
                    packRoot.localPosition = Vector3.Lerp(
                        perspectivePosition,
                        earlyPackDrop,
                        packEase);
                    packRoot.localRotation = Quaternion.Slerp(
                        perspectiveRotation,
                        packStartRotation * Quaternion.Euler(14f, 24f, -9f),
                        packEase);
                    packRoot.localScale = Vector3.Lerp(
                        perspectiveScale,
                        Vector3.Scale(packStartScale, new Vector3(1.14f, 0.88f, 0.96f)),
                        packEase);
                }
            });

            owner.NotifyCardVisible();
            interactiveCard = ResolveInteractiveCard(owner);
            softOrbit?.SetCard(interactiveCard, true);
            Vector3 interactiveStartPosition = interactiveCard == null
                ? Vector3.zero
                : interactiveCard.localPosition;
            Quaternion interactiveStartRotation = interactiveCard == null
                ? Quaternion.identity
                : interactiveCard.localRotation;
            Vector3 interactiveStartScale = interactiveCard == null
                ? Vector3.one
                : interactiveCard.localScale;
            Vector3 finalCardPosition = new Vector3(0f, 0.02f, -1.6f);
            Vector3 packDropPosition = packStartPosition + new Vector3(-0.35f, -5.2f, 1.15f);
            float settleDuration = centeredSilver && !reducedMotion
                ? silverSettleSeconds
                : 0.82f * motion;
            yield return Tween(settleDuration, value =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, value);
                float fall = value * value;
                if (!owner.HasAuthoredPack && packRoot != null)
                {
                    packRoot.localPosition = Vector3.Lerp(earlyPackDrop, packDropPosition, fall);
                    packRoot.localRotation = Quaternion.Slerp(
                        packStartRotation * Quaternion.Euler(14f, 24f, -9f),
                        packStartRotation * Quaternion.Euler(4f, -6f, -18f),
                        fall);
                    packRoot.localScale = Vector3.Lerp(
                        Vector3.Scale(packStartScale, new Vector3(1.14f, 0.88f, 0.96f)),
                        packStartScale * 0.72f,
                        fall);
                }

                if (!owner.HasAuthoredPack && interactiveCard != null)
                {
                    interactiveCard.localPosition = Vector3.Lerp(
                        interactiveStartPosition,
                        finalCardPosition,
                        eased);
                    interactiveCard.localRotation = Quaternion.Slerp(
                        interactiveStartRotation,
                        Quaternion.identity,
                        eased);
                    interactiveCard.localScale = Vector3.Lerp(
                        interactiveStartScale,
                        owner.HasAuthoredPack
                            ? interactiveStartScale
                            : cardStartScale * 1.05f,
                        eased);
                }
            });

            if (packRoot != null)
            {
                packRoot.gameObject.SetActive(false);
            }

            SetRevealGlow(owner, 1f);
            owner.SampleAuthoredOpening(1f);
            BuildCardGroup(owner, interactiveCard);
            if (cardGroup != null)
            {
                cardGroup.SetRevealProgress(0f);
                float fanDuration = centeredSilver && !reducedMotion
                    ? silverFanSeconds
                    : 0.84f * motion;
                yield return Tween(fanDuration, value =>
                {
                    cardGroup.SetRevealProgress(EaseOutCubic(value));
                });
                cardGroup.SetRevealProgress(1f);
                softOrbit?.SetCardGroup(cardGroup);
            }

            owner.NotifyRevealComplete();
            cardGroup?.EnableInteraction();
            sequence = null;
        }

        private IEnumerator CloseSequence(PackRipController owner)
        {
            if (cardGroup != null)
            {
                cardGroup.PrepareClose();
                yield return Tween(ProductDesignLanguage.StandardSeconds, value =>
                {
                    float eased = Mathf.SmoothStep(0f, 1f, value);
                    SetRevealGlow(owner, 1f - eased * 0.72f);
                    cardGroup.SetCloseProgress(eased);
                    if (revealLight != null)
                    {
                        revealLight.intensity = RevealLightIntensity(
                            owner,
                            Mathf.Lerp(4.5f, 1.8f, eased));
                    }
                });

                owner.NotifyCollectionRequested();
                sequence = null;
                yield break;
            }

            Transform closingCard = interactiveCard;
            Vector3 startingPosition = closingCard == null ? Vector3.zero : closingCard.position;
            Quaternion startingRotation = closingCard == null ? Quaternion.identity : closingCard.localRotation;
            Vector3 startingScale = closingCard == null ? Vector3.one : closingCard.localScale;
            Vector3 endingPosition = startingPosition;

            if (closingCard != null)
            {
                Camera sceneCamera = Camera.main;
                Vector3 screenDown = sceneCamera == null
                    ? Vector3.down
                    : -sceneCamera.transform.up;
                Vector3 awayFromViewer = sceneCamera == null
                    ? Vector3.forward
                    : sceneCamera.transform.forward;
                float cardHeight = GetRendererHeight(closingCard);
                endingPosition +=
                    screenDown * cardHeight * 0.34f +
                    awayFromViewer * cardHeight * 0.38f;
            }

            yield return Tween(ProductDesignLanguage.StandardSeconds, value =>
            {
                float eased = Mathf.SmoothStep(0f, 1f, value);
                SetRevealGlow(owner, 1f - eased * 0.72f);

                if (closingCard != null)
                {
                    closingCard.position = Vector3.Lerp(
                        startingPosition,
                        endingPosition,
                        eased);
                    closingCard.localRotation = Quaternion.Slerp(
                        startingRotation,
                        startingRotation * Quaternion.Euler(3f, 12f, -2f),
                        eased);
                    closingCard.localScale = Vector3.Lerp(
                        startingScale,
                        startingScale * 0.82f,
                        eased);
                }

                if (revealLight != null)
                {
                    revealLight.intensity = RevealLightIntensity(
                        owner,
                        Mathf.Lerp(4.5f, 1.8f, eased));
                }
            });

            owner.NotifyCollectionRequested();
            sequence = null;
        }

        private static float GetRendererHeight(Transform target)
        {
            Bounds bounds = new Bounds(target.position, Vector3.one * 0.01f);
            bool hasBounds = false;
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

            return hasBounds ? Mathf.Max(bounds.size.y, 0.01f) : 1f;
        }

        private Transform ResolveInteractiveCard(PackRipController owner)
        {
            if (!owner.HasAuthoredPack)
            {
                return card;
            }

            IReadOnlyList<Transform> authoredCards = owner.TakeOverAuthoredCards(
                card == null ? transform : card.parent);
            interactiveCards.Clear();
            if (authoredCards != null)
            {
                foreach (Transform authoredCard in authoredCards)
                {
                    if (authoredCard != null)
                    {
                        interactiveCards.Add(authoredCard);
                    }
                }
            }

            if (card != null)
            {
                card.gameObject.SetActive(false);
            }

            return interactiveCards.Count == 0 ? null : interactiveCards[0];
        }

        private void BuildCardGroup(PackRipController owner, Transform primary)
        {
            cardGroup?.DisposeCopies();
            cardGroup = null;
            if (primary == null)
            {
                return;
            }

            if (interactiveCards.Count == 0 || interactiveCards[0] != primary)
            {
                interactiveCards.Clear();
                interactiveCards.Add(primary);
            }

            CardPayload[] payloadCards = owner.PresentationCards;
            int cardCount = owner.IsInspectionMode
                ? Mathf.Min(payloadCards.Length, 1)
                : Mathf.Min(payloadCards.Length, 5);
            for (int index = interactiveCards.Count; index < cardCount; index++)
            {
                CardPayload payloadCard = payloadCards[index];
                Transform copy = owner.HasAuthoredPack
                    ? owner.CreateAuthoredCardCopy(primary, payloadCard)
                    : CreateGeneratedCardCopy(primary, payloadCard);
                if (copy != null)
                {
                    interactiveCards.Add(copy);
                }
            }

            cardGroup = new CardGroupPresentation();
            cardGroup.Configure(
                Camera.main,
                interactiveCards,
                owner.IsInspectionMode || owner.UsesAuthoredCardFan,
                false);
        }

        private void SetRevealGlow(PackRipController owner, float amount)
        {
            float strength = owner != null && owner.UsesAuthoredCardFan
                ? silverGlowStrength
                : 1f;
            revealGlow?.SetRevealAmount(amount * strength);
        }

        private static float RevealLightIntensity(
            PackRipController owner,
            float intensity)
        {
            return owner != null && owner.UsesAuthoredCardFan
                ? Mathf.Min(intensity, 2.2f)
                : intensity;
        }

        private static Transform CreateGeneratedCardCopy(
            Transform source,
            CardPayload payload)
        {
            if (source == null)
            {
                return null;
            }

            GameObject copy = Instantiate(
                source.gameObject,
                source.parent,
                true);
            copy.name = "GeneratedCardPresentation_" +
                (payload == null ? "unassigned" : payload.id);
            copy.GetComponent<GeneratedCardPresenter>()?.Apply(payload);
            return copy.transform;
        }

        private void SetPackPresentedPose()
        {
            if (packRoot == null)
            {
                return;
            }

            packRoot.localPosition = packStartPosition + new Vector3(-0.35f, -5.2f, 1.15f);
            packRoot.localRotation = packStartRotation * Quaternion.Euler(4f, -6f, -18f);
            packRoot.localScale = packStartScale * 0.72f;
            packRoot.gameObject.SetActive(false);
        }

        private static IEnumerator Tween(float duration, System.Action<float> update)
        {
            if (duration <= 0f)
            {
                update(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                update(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            update(1f);
        }

        private void ResetPackInspectionRotation()
        {
            desiredPackYaw = 0f;
            desiredPackPitch = 0f;
            currentPackYaw = 0f;
            currentPackPitch = 0f;
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                overshoot * shifted * shifted;
        }

        private static float EaseOutCubic(float value)
        {
            float shifted = 1f - value;
            return 1f - shifted * shifted * shifted;
        }
    }
}
