using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Rippies.Reveal
{
    public sealed class SwipeTearInteractor : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Collider touchPlane;
        [SerializeField] private TearGuide tearGuide;
        [SerializeField] private PackRipController controller;
        [SerializeField, Range(0f, 0.4f)] private float maximumStartProgress = 0.25f;
        [SerializeField, Range(0f, 0.1f)] private float allowedBacktrack = 0.025f;
        [SerializeField] private float smoothing = 18f;

        private float displayedProgress;
        private float furthestProgress;
        private float gestureStartProjected;
        private float gestureStartProgress;
        private bool pointerActive;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            pointerActive = false;
        }

        private void Update()
        {
            if (controller == null || !controller.AcceptsTearInput)
            {
                pointerActive = false;
                return;
            }

            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    TryBeginPointer(touch.screenPosition);
                }
                else if (touch.isInProgress && pointerActive)
                {
                    UpdatePointer(touch.screenPosition);
                }
                else
                {
                    pointerActive = false;
                }
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryBeginPointer(Mouse.current.position.ReadValue());
            }

            if (pointerActive && Mouse.current.leftButton.isPressed)
            {
                UpdatePointer(Mouse.current.position.ReadValue());
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                pointerActive = false;
            }
        }

        private void TryBeginPointer(Vector2 screenPoint)
        {
            if (!TryProject(screenPoint, out float projected) || projected > maximumStartProgress)
            {
                pointerActive = false;
                return;
            }

            pointerActive = true;
            gestureStartProjected = projected;
            gestureStartProgress = furthestProgress;
        }

        private void UpdatePointer(Vector2 screenPoint)
        {
            if (!TryProject(screenPoint, out float projected))
            {
                return;
            }

            float span = Mathf.Max(0.01f, 1f - gestureStartProjected);
            float gestureProgress = Mathf.Clamp01((projected - gestureStartProjected) / span);
            float target = Mathf.Clamp01(gestureStartProgress + gestureProgress * (1f - gestureStartProgress));
            target = Mathf.Max(target, furthestProgress - allowedBacktrack);
            furthestProgress = Mathf.Max(furthestProgress, target);
            displayedProgress = Mathf.Lerp(
                displayedProgress,
                target,
                1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

            controller.SetTearProgress(displayedProgress);
        }

        private bool TryProject(Vector2 screenPoint, out float projected)
        {
            projected = 0f;
            if (sceneCamera == null || touchPlane == null || tearGuide == null)
            {
                return false;
            }

            Ray ray = sceneCamera.ScreenPointToRay(screenPoint);
            if (!touchPlane.Raycast(ray, out RaycastHit hit, 20f))
            {
                return false;
            }

            Vector3 local = tearGuide.transform.InverseTransformPoint(hit.point);
            projected = tearGuide.ProjectToNormalizedDistance(local);
            return true;
        }

        public void ResetInteraction()
        {
            displayedProgress = 0f;
            furthestProgress = 0f;
            gestureStartProjected = 0f;
            gestureStartProgress = 0f;
            pointerActive = false;
        }
    }
}