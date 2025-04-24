using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreResources.Utils
{
    [RequireComponent(typeof(MeshFilter))]
    public class RoundedPolygonMesh : MonoBehaviour
    {
        [SerializeField] private List<Transform> _points = new List<Transform>(8);
        [SerializeField] private float _cornerRadius = 0.1f;
        [SerializeField] private int _cornerResolution = 4;
        [SerializeField] private Transform _spawnPos;
        [SerializeField] private float _borderThickness = 0.1f;

        [Header("Materials")]
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private Material _borderMaterial;

        [Header("Meshes")]
        [SerializeField] private MeshFilter _fillMeshFilter;
        [SerializeField] private MeshRenderer _fillMeshRenderer;
        [SerializeField] private MeshFilter _borderMeshFilter;
        [SerializeField] private MeshRenderer _borderMeshRenderer;

        private Mesh _fillMesh;
        private Mesh _borderMesh;

        private void Start()
        {
            StartCoroutine(Test_RandomSpawnPoints());
        }

        private void Update()
        {
            GenerateMeshes();
        }

        public void GenerateMeshes()
        {
            if (_points.Count < 3)
            {
                Debug.LogError("Need at least 3 points.");
                return;
            }
            
            _fillMesh = new Mesh();
            _borderMesh = new Mesh();

            List<Vector3> innerArc = GenerateArcPoints(_points, _cornerRadius, _cornerResolution);

            // Generate inner fan mesh
            _fillMesh = CreateFanMesh(Vector3.zero, innerArc);
            _fillMeshFilter.mesh = _fillMesh;
            _fillMeshRenderer.material = _fillMaterial;

            // Generate outer ring mesh
            _borderMesh = CreateRingMesh(innerArc, _borderThickness);
            _borderMeshFilter.mesh = _borderMesh;
            _borderMeshRenderer.material = _borderMaterial;
        }

        List<Vector3> GenerateArcPoints(List<Transform> points, float radius, int resolution)
        {
            List<Vector3> arcPoints = new List<Vector3>();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 prev = points[(i - 1 + points.Count) % points.Count].localPosition;
                Vector3 current = points[i].localPosition;
                Vector3 next = points[(i + 1) % points.Count].localPosition;

                Vector3 dirA = (prev - current).normalized;
                Vector3 dirB = (next - current).normalized;

                Vector3 start = current + dirA * radius;
                Vector3 end = current + dirB * radius;

                for (int j = 0; j < resolution; j++)
                {
                    float t = j / (float)(resolution - 1);
                    Vector3 arcPoint = Bezier(start, current, end, t);
                    arcPoints.Add(arcPoint);
                }
            }

            return arcPoints;
        }

        private Mesh CreateFanMesh(Vector3 center, List<Vector3> arcPoints)
        {
            Mesh mesh = new Mesh();

            List<Vector3> vertices = new List<Vector3> { center };
            vertices.AddRange(arcPoints);

            List<int> triangles = new List<int>();
            int count = arcPoints.Count;
            for (int i = 0; i < count; i++)
            {
                triangles.Add(0);
                triangles.Add(1 + i);
                triangles.Add(1 + (i + 1) % count);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateRingMesh(List<Vector3> innerArc, float thickness)
        {
            Mesh mesh = new Mesh();

            List<Vector3> outerArc = new List<Vector3>();
            foreach (var pt in innerArc)
            {
                Vector3 dir = (pt - Vector3.zero).normalized;
                outerArc.Add(pt + dir * thickness);
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            int count = innerArc.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 innerA = innerArc[i];
                Vector3 innerB = innerArc[(i + 1) % count];
                Vector3 outerA = outerArc[i];
                Vector3 outerB = outerArc[(i + 1) % count];

                int index = vertices.Count;

                vertices.Add(innerA); // 0
                vertices.Add(innerB); // 1
                vertices.Add(outerA); // 2
                vertices.Add(outerB); // 3

                // Tri 1
                triangles.Add(index + 0);
                triangles.Add(index + 2);
                triangles.Add(index + 3);

                // Tri 2
                triangles.Add(index + 0);
                triangles.Add(index + 3);
                triangles.Add(index + 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public Vector3 GetRandomPointOnMesh()
        {
            if (_fillMesh == null)
            {
                Debug.LogError("Mesh not assigned.");
                return Vector3.zero;
            }

            Vector3[] vertices = _fillMesh.vertices;
            int[] triangles = _fillMesh.triangles;

            // Pick a random triangle
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;

            Vector3 a = transform.TransformPoint(vertices[triangles[triIndex]]);
            Vector3 b = transform.TransformPoint(vertices[triangles[triIndex + 1]]);
            Vector3 c = transform.TransformPoint(vertices[triangles[triIndex + 2]]);

            return GetRandomPointInTriangle(a, b, c);
        }

        private Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            // Quadratic Bezier curve
            return (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
        }

        private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            // Generate random barycentric coordinates
            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;

            Vector3 point = (1 - r1) * a + r1 * (1 - r2) * b + r1 * r2 * c;
            return point;
        }


        private IEnumerator Test_RandomSpawnPoints()
        {
            while (true)
            {
                yield return new WaitForSeconds(2.5f);
                var pos = GetRandomPointOnMesh();
                _spawnPos.position = pos;
            }
        }
    }
}