using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Rippies.Reveal
{
    public sealed class SoftOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform card;
        [SerializeField] private Transform packShell;
        [SerializeField] private PackRipController controller;
        [SerializeField] private float maximumPitch = 52f;
        [SerializeField] private float dragSensitivity = 0.18f;
        [SerializeField] private float smoothing = 9f;
        [SerializeField, Range(0.55f, 0.9f)] private float frameFill = 0.76f;

        private Vector3 cameraStartPosition;
        private Quaternion cameraStartRotation;
        private Vector3 focusedCameraPosition;
        private Quaternion focusedCameraRotation;
        private Vector3 cardPresentedPosition;
        private Quaternion cardPresentedRotation;
        private Vector3 shellPresentedPosition;
        private Quaternion shellPresentedRotation;
        private float desiredYaw;
        private float desiredPitch;
        private float currentYaw;
        private float currentPitch;
        private bool wasComplete;
        private bool trackingOpening;
        private float openingTrackProgress;
        private CardGroupPresentation cardGroup;

        public void SetCard(Transform target, bool trackUntilComplete = false)
        {
            cardGroup = null;
            card = target;
            trackingOpening = trackUntilComplete;
            openingTrackProgress = trackUntilComplete ? 1f : 0f;
            wasComplete = false;
            desiredYaw = 0f;
            desiredPitch = 0f;
            currentYaw = 0f;
            currentPitch = 0f;
        }

        public void SetCardGroup(CardGroupPresentation group)
        {
            cardGroup = group;
            card = group == null ? null : group.PrimaryCard;
            trackingOpening = false;
            openingTrackProgress = 0f;
            wasComplete = false;
            desiredYaw = 0f;
            desiredPitch = 0f;
            currentYaw = 0f;
            currentPitch = 0f;
        }

        public void TrackOpeningCard(Transform target, float progress)
        {
            if (target == null)
            {
                return;
            }

            card = target;
            trackingOpening = true;
            openingTrackProgress = Mathf.Clamp01(progress);
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                cameraStartPosition = targetCamera.transform.position;
                cameraStartRotation = targetCamera.transform.rotation;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || card == null || controller == null)
            {
                return;
            }

            bool complete = controller.State == RipState.Complete;
            if (!complete)
            {
                wasComplete = false;
                desiredYaw = 0f;
                desiredPitch = 0f;
                currentYaw = Mathf.Lerp(currentYaw, 0f, Damp());
                currentPitch = Mathf.Lerp(currentPitch, 0f, Damp());
                if (trackingOpening &&
                    (controller.State == RipState.Opening ||
                     controller.State == RipState.Revealing))
                {
                    TrackOpeningCamera();
                    return;
                }

                trackingOpening = false;
                targetCamera.transform.position = Vector3.Lerp(
                    targetCamera.transform.position,
                    cameraStartPosition,
                    Damp());
                targetCamera.transform.rotation = Quaternion.Slerp(
                    targetCamera.transform.rotation,
                    cameraStartRotation,
                    Damp());
                return;
            }

            trackingOpening = false;
            if (!wasComplete)
            {
                wasComplete = true;
                cardPresentedPosition = card.localPosition;
                cardPresentedRotation = card.localRotation;
                if (cardGroup == null)
                {
                    FrameCard();
                }
                else
                {
                    FrameCardGroup(cardGroup.Cards);
                }

                if (packShell != null)
                {
                    shellPresentedPosition = packShell.localPosition;
                    shellPresentedRotation = packShell.localRotation;
                }
            }

            if (controller.IsClosing)
            {
                desiredYaw = 0f;
                desiredPitch = 0f;
                currentYaw = Mathf.Lerp(currentYaw, 0f, Damp());
                currentPitch = Mathf.Lerp(currentPitch, 0f, Damp());
                targetCamera.transform.position = Vector3.Lerp(
                    targetCamera.transform.position,
                    focusedCameraPosition,
                    Damp());
                targetCamera.transform.rotation = Quaternion.Slerp(
                    targetCamera.transform.rotation,
                    focusedCameraRotation,
                    Damp());
                return;
            }

            if (cardGroup != null)
            {
                targetCamera.transform.position = Vector3.Lerp(
                    targetCamera.transform.position,
                    focusedCameraPosition,
                    Damp());
                targetCamera.transform.rotation = Quaternion.Slerp(
                    targetCamera.transform.rotation,
                    focusedCameraRotation,
                    Damp());
                cardGroup.TickInput(smoothing, dragSensitivity, maximumPitch);
                return;
            }

            bool dragging = false;
            Vector2 delta = Vector2.zero;
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];
                dragging = touch.isInProgress;
                delta = touch.delta;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                dragging = true;
                delta = Mouse.current.delta.ReadValue();
            }

            if (dragging)
            {
                desiredYaw += delta.x * dragSensitivity;
                desiredPitch = Mathf.Clamp(
                    desiredPitch - delta.y * dragSensitivity,
                    -maximumPitch,
                    maximumPitch);
            }

            currentYaw = Mathf.Lerp(currentYaw, desiredYaw, Damp());
            currentPitch = Mathf.Lerp(currentPitch, desiredPitch, Damp());

            float time = Time.unscaledTime;
            float idleYaw = dragging ? 0f : Mathf.Sin(time * 0.62f) * 1.15f;
            float idlePitch = dragging ? 0f : Mathf.Sin(time * 0.47f + 0.8f) * 0.55f;
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                focusedCameraPosition,
                Damp());
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                focusedCameraRotation,
                Damp());

            card.localPosition = cardPresentedPosition;
            card.localRotation = cardPresentedRotation * Quaternion.Euler(
                currentPitch + Mathf.Sin(time * 0.73f) * 0.7f,
                currentYaw + Mathf.Sin(time * 0.91f + 0.4f) * 1.25f,
                Mathf.Sin(time * 0.57f) * 0.45f);

            if (packShell != null)
            {
                packShell.localPosition = shellPresentedPosition +
                    new Vector3(Mathf.Sin(time * 0.52f) * 0.025f, Mathf.Sin(time * 0.68f) * 0.018f, 0f);
                packShell.localRotation = shellPresentedRotation *
                    Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.49f) * 0.8f);
            }
        }

        private float Damp()
        {
            return 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        }

        private void FrameCard()
        {
            CalculateFocusPose(
                card,
                frameFill,
                out focusedCameraPosition,
                out focusedCameraRotation);
        }

        private void FrameCardGroup(IReadOnlyList<Transform> targets)
        {
            bool hasBounds = false;
            Bounds bounds = default;
            if (targets != null)
            {
                foreach (Transform target in targets)
                {
                    if (target == null)
                    {
                        continue;
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
                }
            }

            if (!hasBounds)
            {
                FrameCard();
                return;
            }

            CalculateFocusPose(
                bounds,
                0.74f,
                out focusedCameraPosition,
                out focusedCameraRotation);
        }

        private void TrackOpeningCamera()
        {
            CalculateFocusPose(
                card,
                Mathf.Min(frameFill, 0.68f),
                out Vector3 cardCameraPosition,
                out Quaternion cardCameraRotation);
            float blend = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.08f, 0.82f, openingTrackProgress));
            Vector3 targetPosition = Vector3.Lerp(
                cameraStartPosition,
                cardCameraPosition,
                blend);
            Quaternion targetRotation = Quaternion.Slerp(
                cameraStartRotation,
                cardCameraRotation,
                blend);
            float trackingDamp =
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPosition,
                trackingDamp);
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRotation,
                trackingDamp);
        }

        private void CalculateFocusPose(
            Transform target,
            float fill,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 center = target.position;
            Bounds bounds = new Bounds(center, Vector3.one * 0.01f);
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

            center = hasBounds ? bounds.center : center;
            CalculateFocusPose(bounds, fill, out position, out rotation);
        }

        private void CalculateFocusPose(
            Bounds bounds,
            float fill,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 center = bounds.center;
            float verticalHalf = Mathf.Max(bounds.extents.y, 0.01f);
            float horizontalHalf = Mathf.Max(bounds.extents.x, 0.01f);
            float effectiveHalf = Mathf.Max(
                verticalHalf,
                horizontalHalf / Mathf.Max(targetCamera.aspect, 0.01f));
            float halfFov = targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float distance = effectiveHalf /
                Mathf.Max(Mathf.Tan(halfFov) * fill, 0.01f);
            Vector3 forward = cameraStartRotation * Vector3.forward;
            position = center - forward * distance;
            rotation = Quaternion.LookRotation(
                center - position,
                cameraStartRotation * Vector3.up);
        }
    }
}
