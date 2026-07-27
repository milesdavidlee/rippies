using System.Collections.Generic;
using UnityEngine;

namespace Rippies.Reveal
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FoilPackDeformer : MonoBehaviour
    {
        [Header("Sealed pack")]
        [SerializeField, Min(1f)] private float width = 3.2f;
        [SerializeField, Min(1f)] private float height = 4.7f;
        [SerializeField] private float seamY = 1.72f;
        [SerializeField, Min(0.01f)] private float edgeThickness = 0.055f;
        [SerializeField, Min(0f)] private float centerBulge = 0.34f;
        [SerializeField, Range(8, 64)] private int columns = 32;
        [SerializeField, Range(8, 64)] private int bodyRows = 30;
        [SerializeField, Range(3, 20)] private int stripRows = 8;

        [Header("Material character")]
        [SerializeField, Range(0f, 0.15f)] private float wrinkleDepth = 0.045f;
        [SerializeField, Range(0f, 0.2f)] private float crimpDepth = 0.08f;
        [SerializeField, Range(0f, 1f)] private float tearProgress;
        [SerializeField, Range(0f, 1f)] private float stripRelease;


        private Mesh mesh;
        private Vector3[] sealedVertices;
        private Vector3[] deformedVertices;
        private VertexInfo[] vertexInfo;

        private struct VertexInfo
        {
            public bool IsStrip;
            public bool IsFront;
            public float X01;
            public float Y;
        }

        public float TearProgress => tearProgress;
        public float StripRelease => stripRelease;


        private void OnEnable()
        {
            Rebuild();
        }

        private void OnValidate()
        {
            seamY = Mathf.Clamp(seamY, -height * 0.1f, height * 0.45f);
            columns = Mathf.Max(8, columns);
            bodyRows = Mathf.Max(8, bodyRows);
            stripRows = Mathf.Max(3, stripRows);
            Rebuild();
        }

        [ContextMenu("Rebuild Foil Pack")]
        public void Rebuild()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null)
            {
                return;
            }

            if (mesh == null)
            {
                mesh = new Mesh { name = "Rippies_CohesiveFoilPack" };
                mesh.MarkDynamic();
            }
            else
            {
                mesh.Clear();
            }

            var vertices = new List<Vector3>(5000);
            var uvs = new List<Vector2>(5000);
            var infos = new List<VertexInfo>(5000);
            var triangles = new List<int>(10000);

            AddShell(vertices, uvs, infos, triangles, false, -height * 0.5f, seamY, bodyRows);
            AddShell(vertices, uvs, infos, triangles, true, seamY, height * 0.5f, stripRows);

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(width * 2f, height * 2f, 4f));

            sealedVertices = vertices.ToArray();
            deformedVertices = new Vector3[sealedVertices.Length];
            vertexInfo = infos.ToArray();
            filter.sharedMesh = mesh;
            ApplyTearProgress(tearProgress);
        }

public void ApplyTearProgress(float value)
        {
            tearProgress = Mathf.Clamp01(value);
            if (mesh == null || sealedVertices == null || vertexInfo == null)
            {
                Rebuild();
                return;
            }

            float eased = tearProgress * tearProgress * (3f - 2f * tearProgress);
            float released = stripRelease * stripRelease * (3f - 2f * stripRelease);
            for (int i = 0; i < sealedVertices.Length; i++)
            {
                Vector3 vertex = sealedVertices[i];
                VertexInfo info = vertexInfo[i];
                float tearStart = info.X01 * 0.92f - 0.08f;
                float tearEnd = info.X01 * 0.92f + 0.14f;
                float tearAtColumn = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(tearStart, tearEnd, tearProgress));

                if (info.IsStrip)
                {
                    float distanceAboveSeam = info.Y - JaggedSeam(info.X01);
                    float curlDegrees = tearAtColumn * (72f - 28f * info.X01) * eased;
                    float radians = curlDegrees * Mathf.Deg2Rad;
                    float originalZ = vertex.z;
                    float frontDirection = info.IsFront ? -1f : 1f;

                    vertex.y = JaggedSeam(info.X01)
                        + Mathf.Cos(radians) * distanceAboveSeam
                        + tearAtColumn * (0.42f - 0.16f * info.X01) * eased;
                    vertex.z = Mathf.Sin(radians) * distanceAboveSeam
                        + originalZ
                        + frontDirection * tearAtColumn * 0.055f
                        - tearAtColumn * (0.55f - 0.2f * info.X01) * eased;
                    vertex.x += tearAtColumn * (0.08f + 0.12f * info.X01) * eased;

                    if (released > 0f)
                    {
                        Vector3 pivot = new Vector3(0f, seamY + 0.2f, 0f);
                        Quaternion flingRotation = Quaternion.Euler(0f, 0f, -24f * released);
                        vertex = pivot + flingRotation * (vertex - pivot);
                        vertex += new Vector3(5.2f, 4.9f, -1.8f) * released;
                    }
                }
                else
                {
                    float seamDistance = Mathf.Clamp01(
                        1f - (JaggedSeam(info.X01) - info.Y) / 0.72f);
                    float mouthOpen = tearAtColumn * seamDistance * eased;
                    float direction = info.IsFront ? -1f : 1f;
                    vertex.z += direction * mouthOpen * 0.3f;
                    vertex.y -= mouthOpen * 0.085f;
                }

                deformedVertices[i] = vertex;
            }

            mesh.vertices = deformedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.bounds = new Bounds(
                new Vector3(1.2f, 1.2f, -0.3f),
                new Vector3(width * 3.5f, height * 3f, 7f));
        }

public void ApplyStripRelease(float value)
        {
            stripRelease = Mathf.Clamp01(value);
            ApplyTearProgress(tearProgress);
        }


        private void AddShell(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<VertexInfo> infos,
            List<int> triangles,
            bool isStrip,
            float minY,
            float maxY,
            int rows)
        {
            int frontStart = AddPanel(vertices, uvs, infos, isStrip, true, minY, maxY, rows);
            int backStart = AddPanel(vertices, uvs, infos, isStrip, false, minY, maxY, rows);
            int stride = columns + 1;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int f0 = frontStart + row * stride + column;
                    int f1 = f0 + 1;
                    int f2 = f0 + stride;
                    int f3 = f2 + 1;
                    triangles.Add(f0); triangles.Add(f2); triangles.Add(f1);
                    triangles.Add(f1); triangles.Add(f2); triangles.Add(f3);

                    int b0 = backStart + row * stride + column;
                    int b1 = b0 + 1;
                    int b2 = b0 + stride;
                    int b3 = b2 + 1;
                    triangles.Add(b0); triangles.Add(b1); triangles.Add(b2);
                    triangles.Add(b1); triangles.Add(b3); triangles.Add(b2);
                }
            }

            for (int row = 0; row < rows; row++)
            {
                Bridge(triangles, frontStart + row * stride, frontStart + (row + 1) * stride,
                    backStart + row * stride, backStart + (row + 1) * stride, true);
                Bridge(triangles, frontStart + row * stride + columns, backStart + row * stride + columns,
                    frontStart + (row + 1) * stride + columns, backStart + (row + 1) * stride + columns, true);
            }

            for (int column = 0; column < columns; column++)
            {
                if (!isStrip)
                {
                    Bridge(triangles, frontStart + column, backStart + column,
                        frontStart + column + 1, backStart + column + 1, false);
                }

                if (isStrip)
                {
                    int topFront = frontStart + rows * stride + column;
                    int topBack = backStart + rows * stride + column;
                    Bridge(triangles, topFront, topFront + 1, topBack, topBack + 1, false);
                }
            }
        }

        private int AddPanel(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<VertexInfo> infos,
            bool isStrip,
            bool isFront,
            float minY,
            float maxY,
            int rows)
        {
            int start = vertices.Count;
            for (int row = 0; row <= rows; row++)
            {
                float row01 = row / (float)rows;
                for (int column = 0; column <= columns; column++)
                {
                    float x01 = column / (float)columns;
                    float y = Mathf.Lerp(minY, maxY, row01);
                    if (!isStrip && row == rows)
                    {
                        y = JaggedSeam(x01);
                    }
                    else if (isStrip && row == 0)
                    {
                        y = JaggedSeam(x01) - 0.1f;
                    }

                    vertices.Add(SealedPosition(x01, y, isFront));
                    uvs.Add(new Vector2(x01, Mathf.InverseLerp(-height * 0.5f, height * 0.5f, y)));
                    infos.Add(new VertexInfo { IsStrip = isStrip, IsFront = isFront, X01 = x01, Y = y });
                }
            }

            return start;
        }

        private Vector3 SealedPosition(float x01, float y, bool isFront)
        {
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, x01);
            float y01 = Mathf.InverseLerp(-height * 0.5f, height * 0.5f, y);
            float xEnvelope = Mathf.Pow(Mathf.Sin(Mathf.PI * x01), 0.62f);
            float yEnvelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * y01)), 0.6f);
            float envelope = xEnvelope * yEnvelope;
            float corrugationZone =
                Mathf.SmoothStep(0.66f, 0.96f, Mathf.Abs(y) / (height * 0.5f));
            float corrugation = Mathf.Sin(x01 * Mathf.PI * columns) * crimpDepth * corrugationZone;
            float diagonalWrinkle =
                (Mathf.Sin(x * 8.3f + y * 3.1f) + 0.45f * Mathf.Sin(x * 17f - y * 5.7f))
                * wrinkleDepth * envelope;
            float depth = edgeThickness + centerBulge * envelope + corrugation + diagonalWrinkle;
            float sign = isFront ? -1f : 1f;
            return new Vector3(x, y, sign * depth);
        }

        private float JaggedSeam(float x01)
        {
            return seamY
                + Mathf.Sin(x01 * 17.3f) * 0.045f
                + Mathf.Sin(x01 * 41.7f + 0.8f) * 0.022f;
        }

        private static void Bridge(
            List<int> triangles,
            int a,
            int b,
            int c,
            int d,
            bool flip)
        {
            if (flip)
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
            else
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }
    }
}