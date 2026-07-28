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
        [SerializeField, Range(0.2f, 0.6f)] private float verticalDragScreenFraction = 0.34f;
        [SerializeField] private float smoothing = 18f;

        private float displayedProgress;
        private float furthestProgress;
        private float gestureStartProjected;
        private float gestureStartScreenY;
        private float gestureStartProgress;
        private Vector2 lastPointerScreenPoint;
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
                    FinishPointer(touch.screenPosition);
                }
                return;
            }

            if (Mouse.current == null)
            {
                FinishPointer(lastPointerScreenPoint);
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
                FinishPointer(Mouse.current.position.ReadValue());
            }
            else if (pointerActive && !Mouse.current.leftButton.isPressed)
            {
                FinishPointer(lastPointerScreenPoint);
            }
        }

        private void TryBeginPointer(Vector2 screenPoint)
        {
            lastPointerScreenPoint = screenPoint;
            if (controller.UsesVerticalTearGesture)
            {
                if (!HitsTouchPlane(screenPoint))
                {
                    pointerActive = false;
                    return;
                }

                pointerActive = true;
                gestureStartScreenY = screenPoint.y;
                gestureStartProgress = furthestProgress;
                return;
            }

            if (!TryProject(screenPoint, out float projected) ||
                projected > maximumStartProgress)
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
            lastPointerScreenPoint = screenPoint;
            float gestureProgress;
            if (controller.UsesVerticalTearGesture)
            {
                float requiredDistance = Mathf.Max(
                    1f,
                    Screen.height * verticalDragScreenFraction);
                gestureProgress = Mathf.Clamp01(
                    (gestureStartScreenY - screenPoint.y) / requiredDistance);
            }
            else
            {
                if (!TryProject(screenPoint, out float projected))
                {
                    return;
                }

                float span = Mathf.Max(0.01f, 1f - gestureStartProjected);
                gestureProgress = Mathf.Clamp01(
                    (projected - gestureStartProjected) / span);
            }

            float target = Mathf.Clamp01(gestureStartProgress + gestureProgress * (1f - gestureStartProgress));
            target = Mathf.Max(target, furthestProgress - allowedBacktrack);
            furthestProgress = Mathf.Max(furthestProgress, target);
            displayedProgress = Mathf.Lerp(
                displayedProgress,
                target,
                1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

            controller.SetTearProgress(displayedProgress);
        }

        private void FinishPointer(Vector2 screenPoint)
        {
            if (!pointerActive)
            {
                return;
            }

            UpdatePointer(screenPoint);
            // Apply the furthest sampled point immediately on release. Without
            // this, a quick valid pull can end while the smoothed display value
            // is still below the reveal commit threshold.
            displayedProgress = furthestProgress;
            controller.SetTearProgress(displayedProgress);
            pointerActive = false;
        }

        private bool HitsTouchPlane(Vector2 screenPoint)
        {
            if (sceneCamera == null || touchPlane == null)
            {
                return false;
            }

            Ray ray = sceneCamera.ScreenPointToRay(screenPoint);
            return touchPlane.Raycast(ray, out _, 20f);
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
            gestureStartScreenY = 0f;
            gestureStartProgress = 0f;
            lastPointerScreenPoint = Vector2.zero;
            pointerActive = false;
        }
    }
}
