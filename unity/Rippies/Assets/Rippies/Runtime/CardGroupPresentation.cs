using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Rippies.Reveal
{
    /// <summary>
    /// Presentation-only choreography for the immutable cards supplied by the
    /// reveal receipt. It never chooses or mutates the reveal outcome.
    /// </summary>
    public sealed class CardGroupPresentation
    {
        private readonly List<Transform> cards = new List<Transform>();
        private readonly List<Vector3> gridPositions = new List<Vector3>();
        private readonly List<Quaternion> gridRotations = new List<Quaternion>();
        private readonly List<Vector3> gridScales = new List<Vector3>();
        private readonly List<Vector3> revealStartPositions = new List<Vector3>();
        private readonly List<Quaternion> revealStartRotations = new List<Quaternion>();
        private readonly List<Vector3> revealStartScales = new List<Vector3>();
        private readonly List<Vector3> closePositions = new List<Vector3>();
        private readonly List<Quaternion> closeRotations = new List<Quaternion>();
        private readonly List<Vector3> closeScales = new List<Vector3>();

        private Camera sceneCamera;
        private Vector3 stackPosition;
        private Quaternion stackRotation;
        private Vector3 heroScale;
        private Vector3 heroPosition;
        private Quaternion heroRotation;
        private float heroCardWidth = 1f;
        private float heroCardHeight = 1f;
        private int selectedIndex = -1;
        private bool interactionEnabled;
        private bool pointerWasDown;
        private Vector2 pointerDownPosition;
        private Vector2 previousPointerPosition;
        private Vector2 appliedDragDelta;
        private float desiredYaw;
        private float desiredPitch;
        private float currentYaw;
        private float currentPitch;
        private bool preserveRevealStartLayout;

        public IReadOnlyList<Transform> Cards => cards;
        public Transform PrimaryCard => cards.Count == 0 ? null : cards[0];
        public int SelectedIndex => selectedIndex;

        public void Configure(
            Camera camera,
            IReadOnlyList<Transform> targets,
            bool alignPrimaryToCamera = false,
            bool preserveCurrentLayout = false)
        {
            sceneCamera = camera;
            preserveRevealStartLayout = preserveCurrentLayout;
            cards.Clear();
            gridPositions.Clear();
            gridRotations.Clear();
            gridScales.Clear();
            revealStartPositions.Clear();
            revealStartRotations.Clear();
            revealStartScales.Clear();
            selectedIndex = -1;
            interactionEnabled = false;

            if (targets == null)
            {
                return;
            }

            foreach (Transform target in targets)
            {
                if (target != null)
                {
                    cards.Add(target);
                }
            }

            if (cards.Count == 0)
            {
                return;
            }

            Transform primary = cards[0];
            stackPosition = primary.position;
            if (preserveRevealStartLayout && cards.Count > 1)
            {
                stackPosition = Vector3.zero;
                foreach (Transform target in cards)
                {
                    stackPosition += target.position;
                }

                stackPosition /= cards.Count;
            }

            stackRotation = alignPrimaryToCamera && sceneCamera != null
                ? Quaternion.LookRotation(
                    sceneCamera.transform.forward,
                    sceneCamera.transform.up)
                : primary.rotation;
            heroPosition = stackPosition;
            heroRotation = stackRotation;
            heroScale = primary.localScale;

            Bounds primaryBounds = RendererBounds(primary);
            float cardWidth = Mathf.Max(primaryBounds.size.x, 0.1f);
            float cardHeight = Mathf.Max(primaryBounds.size.y, 0.1f);
            heroCardWidth = cardWidth;
            heroCardHeight = cardHeight;
            Vector3 right = sceneCamera == null ? Vector3.right : sceneCamera.transform.right;
            Vector3 up = sceneCamera == null ? Vector3.up : sceneCamera.transform.up;
            Vector3 forward = sceneCamera == null ? Vector3.forward : sceneCamera.transform.forward;
            float scaleFactor = cards.Count <= 1
                ? 1f
                : preserveRevealStartLayout
                    ? 0.48f
                    : 0.31f;
            float horizontalStep = cardWidth * 0.39f;
            float verticalStep = cardHeight * 0.32f;

            for (int index = 0; index < cards.Count; index++)
            {
                revealStartPositions.Add(cards[index].position);
                revealStartRotations.Add(cards[index].rotation);
                revealStartScales.Add(cards[index].localScale);
                bool singleCard = cards.Count == 1;
                bool topRow = index < 3;
                int rowIndex = topRow ? index : index - 3;
                float column = singleCard
                    ? 0f
                    : topRow
                        ? rowIndex - 1f
                        : rowIndex - 0.5f;
                Vector3 position = stackPosition +
                    right * column * horizontalStep +
                    up * (singleCard
                        ? 0f
                        : topRow
                            ? verticalStep * 0.58f
                            : -verticalStep * 0.62f) +
                    forward * (index * 0.004f);
                gridPositions.Add(position);
                gridRotations.Add(stackRotation);
                gridScales.Add(Vector3.Scale(heroScale, Vector3.one * scaleFactor));

                if (!preserveRevealStartLayout)
                {
                    cards[index].position =
                        stackPosition + forward * (index * 0.006f);
                    cards[index].rotation = stackRotation;
                    cards[index].localScale = heroScale;
                }

                cards[index].gameObject.SetActive(true);
            }
        }

        public void SetRevealProgress(float progress)
        {
            float value = Mathf.Clamp01(progress);
            float fan = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.48f, value));
            float grid = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.36f, 1f, value));
            Vector3 right = sceneCamera == null ? Vector3.right : sceneCamera.transform.right;
            Vector3 up = sceneCamera == null ? Vector3.up : sceneCamera.transform.up;
            Vector3 forward = sceneCamera == null ? Vector3.forward : sceneCamera.transform.forward;

            for (int index = 0; index < cards.Count; index++)
            {
                if (preserveRevealStartLayout)
                {
                    float transition = Mathf.SmoothStep(0f, 1f, value);
                    cards[index].position = Vector3.Lerp(
                        revealStartPositions[index],
                        gridPositions[index],
                        transition);
                    cards[index].rotation = Quaternion.Slerp(
                        revealStartRotations[index],
                        gridRotations[index],
                        transition);
                    cards[index].localScale = Vector3.Lerp(
                        revealStartScales[index],
                        gridScales[index],
                        transition);
                    continue;
                }

                float centered = index - (cards.Count - 1f) * 0.5f;
                Vector3 fanPosition = stackPosition +
                    right * centered * heroCardWidth * 0.17f * fan +
                    up * (0.12f - Mathf.Abs(centered) * 0.025f) * fan +
                    forward * index * 0.006f;
                Quaternion fanRotation = stackRotation *
                    Quaternion.AngleAxis(-centered * 8f * fan, Vector3.forward);
                cards[index].position = Vector3.Lerp(fanPosition, gridPositions[index], grid);
                cards[index].rotation = Quaternion.Slerp(
                    fanRotation,
                    gridRotations[index],
                    grid);
                cards[index].localScale = Vector3.Lerp(
                    heroScale,
                    gridScales[index],
                    grid);
            }
        }

        public void EnableInteraction()
        {
            interactionEnabled = true;
            pointerWasDown = false;
        }

        public void SelectIndex(int index)
        {
            if (index < 0 || index >= cards.Count)
            {
                return;
            }

            selectedIndex = index;
            desiredYaw = 0f;
            desiredPitch = 0f;
            currentYaw = 0f;
            currentPitch = 0f;
        }

        public void TickInput(float smoothing, float dragSensitivity, float maximumPitch)
        {
            if (!interactionEnabled || sceneCamera == null || cards.Count == 0)
            {
                return;
            }

            bool pointerDown = false;
            Vector2 position = Vector2.zero;
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];
                pointerDown = touch.isInProgress;
                position = touch.screenPosition;
            }
            else if (Mouse.current != null)
            {
                pointerDown = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }

            if (pointerDown && !pointerWasDown)
            {
                pointerDownPosition = position;
                previousPointerPosition = position;
                appliedDragDelta = Vector2.zero;
            }
            else if (pointerDown && pointerWasDown && selectedIndex >= 0)
            {
                Vector2 delta = position - previousPointerPosition;
                if ((position - pointerDownPosition).sqrMagnitude > 16f)
                {
                    ApplyDrag(delta, dragSensitivity, maximumPitch);
                    appliedDragDelta += delta;
                }
            }
            else if (!pointerDown && pointerWasDown)
            {
                Vector2 releasePosition =
                    position == Vector2.zero ? previousPointerPosition : position;
                Vector2 totalDrag = releasePosition - pointerDownPosition;
                if (totalDrag.sqrMagnitude < 144f)
                {
                    HandleTap(pointerDownPosition);
                }
                else if (selectedIndex >= 0)
                {
                    ApplyDrag(
                        totalDrag - appliedDragDelta,
                        dragSensitivity,
                        maximumPitch);
                }
            }

            previousPointerPosition = position;
            pointerWasDown = pointerDown;

            float damp = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            currentYaw = Mathf.Lerp(currentYaw, desiredYaw, damp);
            currentPitch = Mathf.Lerp(currentPitch, desiredPitch, damp);
            float time = Time.unscaledTime;
            Vector3 forward = sceneCamera.transform.forward;

            for (int index = 0; index < cards.Count; index++)
            {
                bool selected = index == selectedIndex;
                Vector3 targetPosition = selected
                    ? cards.Count == 1
                        ? heroPosition
                        : heroPosition - forward * heroCardHeight * 0.48f
                    : gridPositions[index];
                Vector3 targetScale = selected ? heroScale : gridScales[index];
                Quaternion targetRotation = selected
                    ? heroRotation * Quaternion.Euler(
                        currentPitch + Mathf.Sin(time * 0.73f) * 0.6f,
                        currentYaw + Mathf.Sin(time * 0.91f) * 0.9f,
                        Mathf.Sin(time * 0.57f) * 0.35f)
                    : gridRotations[index];

                if (selectedIndex >= 0 && !selected)
                {
                    targetPosition += forward * 0.08f;
                    targetScale *= 0.9f;
                }

                cards[index].position = Vector3.Lerp(cards[index].position, targetPosition, damp);
                cards[index].rotation = Quaternion.Slerp(cards[index].rotation, targetRotation, damp);
                cards[index].localScale = Vector3.Lerp(cards[index].localScale, targetScale, damp);
            }
        }

        private void ApplyDrag(
            Vector2 delta,
            float dragSensitivity,
            float maximumPitch)
        {
            desiredYaw += delta.x * dragSensitivity;
            desiredPitch = Mathf.Clamp(
                desiredPitch - delta.y * dragSensitivity,
                -maximumPitch,
                maximumPitch);
        }

        public void PrepareClose()
        {
            interactionEnabled = false;
            closePositions.Clear();
            closeRotations.Clear();
            closeScales.Clear();
            foreach (Transform target in cards)
            {
                closePositions.Add(target.position);
                closeRotations.Add(target.rotation);
                closeScales.Add(target.localScale);
            }
        }

        public void SetCloseProgress(float progress)
        {
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            Vector3 down = sceneCamera == null ? Vector3.down : -sceneCamera.transform.up;
            Vector3 away = sceneCamera == null ? Vector3.forward : sceneCamera.transform.forward;
            float cardHeight = cards.Count == 0 ? 1f : RendererBounds(cards[0]).size.y;
            Vector3 exit = stackPosition + down * cardHeight * 0.46f + away * cardHeight * 0.42f;

            for (int index = 0; index < cards.Count; index++)
            {
                cards[index].position = Vector3.Lerp(closePositions[index], exit, eased);
                cards[index].rotation = Quaternion.Slerp(
                    closeRotations[index],
                    stackRotation * Quaternion.Euler(4f, 14f, index * 1.4f),
                    eased);
                cards[index].localScale = Vector3.Lerp(
                    closeScales[index],
                    heroScale * 0.18f,
                    eased);
            }
        }

        public void DisposeCopies()
        {
            for (int index = 1; index < cards.Count; index++)
            {
                if (cards[index] != null)
                {
                    Object.Destroy(cards[index].gameObject);
                }
            }

            cards.Clear();
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (selectedIndex >= 0 && ContainsScreenPoint(cards[selectedIndex], screenPosition))
            {
                selectedIndex = -1;
                desiredYaw = 0f;
                desiredPitch = 0f;
                currentYaw = 0f;
                currentPitch = 0f;
                return;
            }

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                if (ContainsScreenPoint(cards[index], screenPosition))
                {
                    SelectIndex(index);
                    return;
                }
            }
        }

        private bool ContainsScreenPoint(Transform target, Vector2 point)
        {
            Bounds bounds = RendererBounds(target);
            Vector3 min = sceneCamera.WorldToScreenPoint(bounds.min);
            Vector3 max = sceneCamera.WorldToScreenPoint(bounds.max);
            Rect rect = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x) - 12f,
                Mathf.Min(min.y, max.y) - 12f,
                Mathf.Max(min.x, max.x) + 12f,
                Mathf.Max(min.y, max.y) + 12f);
            return min.z > 0f && rect.Contains(point);
        }

        private static Bounds RendererBounds(Transform target)
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

            return bounds;
        }
    }
}
