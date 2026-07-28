using System;
using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class PackRipController : MonoBehaviour
    {
        private static readonly int TearProgressId = Shader.PropertyToID("_TearProgress");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer packBody;
        [SerializeField] private Renderer topStripRenderer;
        [SerializeField] private Transform topStrip;
        [SerializeField] private FoilPackDeformer foilPack;
        [SerializeField] private GeneratedCardPresenter cardPresenter;
        [SerializeField] private RevealDirector revealDirector;
        [SerializeField] private SwipeTearInteractor interactor;
        [SerializeField] private AuthoredPackDriver authoredPack;
        [SerializeField] private float commitThreshold = 0.94f;
        [SerializeField] private bool reducedMotion;

        private MaterialPropertyBlock bodyProperties;
        private MaterialPropertyBlock stripProperties;
        private Vector3 stripStartPosition;
        private Quaternion stripStartRotation;
        private Vector3 packStartScale;
        private bool committed;
        private bool closing;
        private RevealPayload payload;
        private Color accentColor = ProductDesignLanguage.Cyan;

        public RipState State { get; private set; } = RipState.Loading;
        public float TearProgress { get; private set; }
        public RevealPayload Payload => payload;
        public CardPayload[] RevealCards =>
            payload == null ? Array.Empty<CardPayload>() : payload.Cards;
        public int RevealedCardCount => RevealCards.Length;
        public Color AccentColor => accentColor;
        public bool IsClosing => closing;
        public bool HasAuthoredPack => authoredPack != null && authoredPack.IsAvailable;
        public bool AcceptsTearInput =>
            State == RipState.Ready ||
            State == RipState.Grabbing ||
            State == RipState.Tearing;

        public event Action<RipState> StateChanged;
        public event Action<float> TearProgressChanged;
        public event Action<string, string> BridgeEvent;

        private void Awake()
        {
            bodyProperties = new MaterialPropertyBlock();
            stripProperties = new MaterialPropertyBlock();
            if (packBody == null && foilPack != null)
            {
                packBody = foilPack.GetComponent<Renderer>();
            }

            if (topStripRenderer == null)
            {
                topStripRenderer = packBody;
            }

            stripStartPosition = topStrip == null ? Vector3.zero : topStrip.localPosition;
            stripStartRotation = topStrip == null ? Quaternion.identity : topStrip.localRotation;
            packStartScale = transform.localScale;

            authoredPack ??= GetComponent<AuthoredPackDriver>();
            authoredPack ??= gameObject.AddComponent<AuthoredPackDriver>();
            authoredPack.Initialize(transform.Find("PackShellVisuals"));
            revealDirector?.SetAuthoredPackAvailable(HasAuthoredPack);
        }

        private void Start()
        {
            PrepareRandomReveal();
        }

        public void PrepareReveal(RevealPayload revealPayload)
        {
            payload = revealPayload ?? DemoCardFactory.CreateRandom();
            // Restore the authored hierarchy before touching the next card.
            // Unity stays resident between pack openings, so this ordering
            // prevents presentation-pivot transforms from leaking forward.
            ResetReveal();
            CardPayload primaryCard = payload.PrimaryCard;
            cardPresenter?.Apply(primaryCard);
            authoredPack?.SetCard(primaryCard, payload.packTypeId);
            ApplyPackPalette(primaryCard);
            ApplyPackIdentity(payload.packTypeId);
            SetState(RipState.Presenting);
            revealDirector?.PlayPresentation(this, reducedMotion);
            Emit("sceneReady", payload.revealId);
        }

        public void PrepareRandomReveal()
        {
            PrepareReveal(DemoCardFactory.CreateRandom());
        }

        public void SetTearProgress(float value)
        {
            if (!AcceptsTearInput || committed)
            {
                return;
            }

            SetState(RipState.Tearing);
            TearProgress = Mathf.Max(TearProgress, Mathf.Clamp01(value));
            ApplyTearVisuals(TearProgress);
            authoredPack?.SampleSwipe(TearProgress);
            TearProgressChanged?.Invoke(TearProgress);

            if (TearProgress >= commitThreshold)
            {
                CommitReveal();
            }
        }

        public void SetCinematicTear(float progress, float release)
        {
            TearProgress = Mathf.Max(TearProgress, Mathf.Clamp01(progress));
            ApplyTearVisuals(TearProgress);
            foilPack?.ApplyStripRelease(release);
            authoredPack?.SampleCommittedTear(release);
            TearProgressChanged?.Invoke(TearProgress);
        }

        public void BeginReveal()
        {
            if (State == RipState.Ready)
            {
                SetTearProgress(commitThreshold);
            }
        }

        public void SkipReveal()
        {
            if (State == RipState.Complete)
            {
                return;
            }

            committed = true;
            TearProgress = 1f;
            ApplyTearVisuals(1f);
            foilPack?.ApplyStripRelease(1f);
            TearProgressChanged?.Invoke(TearProgress);
            revealDirector?.SkipToComplete(this);
        }

        public void ResetReveal()
        {
            committed = false;
            closing = false;
            TearProgress = 0f;
            transform.localScale = packStartScale;
            if (topStrip != null)
            {
                topStrip.localPosition = stripStartPosition;
                topStrip.localRotation = stripStartRotation;
                topStrip.gameObject.SetActive(true);
            }

            foilPack?.ApplyStripRelease(0f);
            SetPackLabelsVisible(true);
            revealDirector?.ResetSequence();
            authoredPack?.ResetModel();
            interactor?.ResetInteraction();
            ApplyTearVisuals(0f);
            SetState(RipState.Loading);
        }

        public void NotifyOpening()
        {
            SetPackLabelsVisible(false);
            SetState(RipState.Opening);
        }

        public void NotifyPresentationComplete()
        {
            if (State == RipState.Presenting)
            {
                SetState(RipState.Ready);
            }
        }

        public void SampleAuthoredPresentation(float progress)
        {
            authoredPack?.SamplePresentation(progress);
        }

        public void SampleAuthoredOpening(float progress)
        {
            authoredPack?.SampleOpening(progress);
        }

        public Transform TakeOverAuthoredCard(Transform presentationParent)
        {
            return authoredPack?.TakeOverCard(presentationParent);
        }

        public Transform CreateAuthoredCardCopy(
            Transform sourcePresentation,
            CardPayload cardPayload)
        {
            return authoredPack?.CreatePresentationCardCopy(
                sourcePresentation,
                cardPayload,
                payload == null ? "" : payload.packTypeId);
        }

        public Transform AuthoredAnimationCard =>
            authoredPack == null ? null : authoredPack.AnimatedCard;

        public void NotifyCardVisible()
        {
            SetState(RipState.Revealing);
            CardPayload primaryCard = payload == null ? null : payload.PrimaryCard;
            Emit("cardVisible", primaryCard == null ? "" : primaryCard.id);
        }

        public void NotifyRevealComplete()
        {
            SetState(RipState.Complete);
            Emit("revealComplete", payload == null ? "" : payload.revealId);
        }

        public void RequestCollection()
        {
            if (State != RipState.Complete || closing)
            {
                return;
            }

            closing = true;
            revealDirector?.CloseToCollection(this);
        }

        public void NotifyCollectionRequested()
        {
            Emit("collectionRequested", payload == null ? "" : payload.revealId);
        }

        private void CommitReveal()
        {
            committed = true;
            SetState(RipState.SealBroken);
            Emit("tearStarted", payload == null ? "" : payload.revealId);
            revealDirector?.Play(this, reducedMotion);
        }

        private void ApplyPackPalette(CardPayload card)
        {
            if (card == null)
            {
                return;
            }

            Color fallback = ProductDesignLanguage.Cyan;
            Color accent = ColorUtility.TryParseHtmlString(card.accentHex, out Color parsed)
                ? parsed.linear
                : fallback;
            accentColor = accent;
            Color baseColor = Color.Lerp(ProductDesignLanguage.Canvas, accent, 0.18f);

            ApplyPalette(packBody, bodyProperties, baseColor, accent);
            ApplyPalette(topStripRenderer, stripProperties, baseColor, accent);
            revealDirector?.SetPalette(accent);
            authoredPack?.SetAccent(accent);

            Transform wordmark = transform.Find("PackWordmark");
            Transform subtitle = transform.Find("PackSubtitle");
            TextMesh wordmarkText = wordmark == null ? null : wordmark.GetComponent<TextMesh>();
            TextMesh subtitleText = subtitle == null ? null : subtitle.GetComponent<TextMesh>();
            if (wordmarkText != null)
            {
                wordmarkText.color = Color.Lerp(Color.white, accent, 0.65f);
            }

            if (subtitleText != null)
            {
                subtitleText.color = Color.Lerp(Color.white, accent, 0.32f);
            }
        }

        private void ApplyPackIdentity(string packTypeId)
        {
            Transform subtitle = transform.Find("PackSubtitle");
            TextMesh subtitleText = subtitle == null ? null : subtitle.GetComponent<TextMesh>();
            if (subtitleText == null || string.IsNullOrWhiteSpace(packTypeId))
            {
                return;
            }

            string label = packTypeId;
            int separator = label.LastIndexOf('_');
            if (separator >= 0 && separator < label.Length - 1)
            {
                label = label.Substring(separator + 1);
            }

            subtitleText.text = label.Replace('-', ' ').ToUpperInvariant() + " PACK";
        }

        private static void ApplyPalette(
            Renderer target,
            MaterialPropertyBlock properties,
            Color baseColor,
            Color accent)
        {
            if (target == null || properties == null)
            {
                return;
            }

            target.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, baseColor);
            properties.SetColor(AccentColorId, accent);
            properties.SetColor(ColorId, baseColor);
            target.SetPropertyBlock(properties);
        }

        private void ApplyTearVisuals(float progress)
        {
            bodyProperties ??= new MaterialPropertyBlock();
            stripProperties ??= new MaterialPropertyBlock();
            foilPack?.ApplyTearProgress(progress);

            if (packBody != null)
            {
                packBody.GetPropertyBlock(bodyProperties);
                bodyProperties.SetFloat(TearProgressId, progress);
                packBody.SetPropertyBlock(bodyProperties);
            }

            if (topStripRenderer != null)
            {
                topStripRenderer.GetPropertyBlock(stripProperties);
                stripProperties.SetFloat(TearProgressId, progress);
                topStripRenderer.SetPropertyBlock(stripProperties);
            }

            if (topStrip != null)
            {
                float curl = Mathf.SmoothStep(0f, 1f, progress);
                topStrip.localRotation = stripStartRotation * Quaternion.Euler(0f, 0f, -28f * curl);
                topStrip.localPosition = stripStartPosition +
                    new Vector3(0.18f * curl, 0.18f * curl, -0.35f * curl);
            }
        }

        private void SetState(RipState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(State);
        }

        private void Emit(string eventName, string value)
        {
            BridgeEvent?.Invoke(eventName, value);
            NativeRevealBridge.Emit(eventName, value);
        }

        private void SetPackLabelsVisible(bool visible)
        {
            Transform wordmark = transform.Find("PackWordmark");
            Transform subtitle = transform.Find("PackSubtitle");
            if (wordmark != null)
            {
                wordmark.gameObject.SetActive(visible);
            }

            if (subtitle != null)
            {
                subtitle.gameObject.SetActive(visible);
            }
        }
    }
}
