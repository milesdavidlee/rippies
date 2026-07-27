using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Rippies.Reveal
{
    public sealed class SoftOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform card;
        [SerializeField] private Transform packShell;
        [SerializeField] private PackRipController controller;
        [SerializeField] private float maximumYaw = 72f;
        [SerializeField] private float maximumPitch = 24f;
        [SerializeField] private float dragSensitivity = 0.18f;
        [SerializeField] private float smoothing = 9f;

        private Vector3 cameraStartPosition;
        private Quaternion cameraStartRotation;
        private Vector3 cardPresentedPosition;
        private Quaternion cardPresentedRotation;
        private Vector3 shellPresentedPosition;
        private Quaternion shellPresentedRotation;
        private float desiredYaw;
        private float desiredPitch;
        private float currentYaw;
        private float currentPitch;
        private bool wasComplete;

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

            bool complete = controller.State == RipState.Complete && !controller.IsClosing;
            if (!complete)
            {
                wasComplete = false;
                desiredYaw = 0f;
                desiredPitch = 0f;
                currentYaw = Mathf.Lerp(currentYaw, 0f, Damp());
                currentPitch = Mathf.Lerp(currentPitch, 0f, Damp());
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

            if (!wasComplete)
            {
                wasComplete = true;
                cardPresentedPosition = card.localPosition;
                cardPresentedRotation = card.localRotation;
                if (packShell != null)
                {
                    shellPresentedPosition = packShell.localPosition;
                    shellPresentedRotation = packShell.localRotation;
                }
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
                desiredYaw = Mathf.Clamp(
                    desiredYaw + delta.x * dragSensitivity,
                    -maximumYaw,
                    maximumYaw);
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
            Vector3 pivot = card.position;
            Vector3 baseOffset = cameraStartPosition - pivot;
            Quaternion orbit = Quaternion.Euler(
                (currentPitch + idlePitch) * 0.12f,
                (currentYaw + idleYaw) * 0.12f,
                0f);
            Vector3 orbitPosition = pivot + orbit * baseOffset;
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                orbitPosition,
                Damp());
            Quaternion lookRotation = Quaternion.LookRotation(
                pivot - targetCamera.transform.position,
                Vector3.up);
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                lookRotation,
                Damp());

            card.localPosition = cardPresentedPosition +
                new Vector3(0f, Mathf.Sin(time * 1.15f) * 0.035f, 0f);
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
    }
}
