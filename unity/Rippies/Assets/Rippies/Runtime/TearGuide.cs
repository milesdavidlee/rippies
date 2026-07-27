using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class TearGuide : MonoBehaviour
    {
        [SerializeField] private Transform[] controlPoints;

        public float ProjectToNormalizedDistance(Vector3 localPoint)
        {
            if (controlPoints == null || controlPoints.Length < 2)
            {
                return Mathf.InverseLerp(-1.5f, 1.5f, localPoint.x);
            }

            float totalLength = 0f;
            float bestDistance = float.PositiveInfinity;
            float bestAlong = 0f;
            float accumulated = 0f;

            for (int index = 0; index < controlPoints.Length - 1; index++)
            {
                Vector3 a = transform.InverseTransformPoint(controlPoints[index].position);
                Vector3 b = transform.InverseTransformPoint(controlPoints[index + 1].position);
                Vector3 segment = b - a;
                float length = segment.magnitude;
                if (length <= Mathf.Epsilon)
                {
                    continue;
                }

                float t = Mathf.Clamp01(Vector3.Dot(localPoint - a, segment) / segment.sqrMagnitude);
                Vector3 closest = a + segment * t;
                float distance = (localPoint - closest).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestAlong = accumulated + length * t;
                }

                accumulated += length;
                totalLength += length;
            }

            return totalLength <= Mathf.Epsilon ? 0f : Mathf.Clamp01(bestAlong / totalLength);
        }

        public void Configure(Transform[] points)
        {
            controlPoints = points;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (controlPoints == null || controlPoints.Length < 2)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.95f, 1f, 0.9f);
            for (int index = 0; index < controlPoints.Length - 1; index++)
            {
                if (controlPoints[index] != null && controlPoints[index + 1] != null)
                {
                    Gizmos.DrawLine(controlPoints[index].position, controlPoints[index + 1].position);
                }
            }
        }
#endif
    }
}