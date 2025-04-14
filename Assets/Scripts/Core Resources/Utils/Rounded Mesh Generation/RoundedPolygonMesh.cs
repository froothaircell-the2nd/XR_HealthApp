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

        private Mesh mesh;

        //private void Start()
        //{
        //    StartCoroutine(Test_RandomSpawnPoints());
        //}

        //private void Update()
        //{
        //    GenerateMesh();
        //}

        public void GenerateMesh()
        {
            if (_points.Count < 3)
            {
                Debug.LogError("Need at least 3 points.");
                return;
            }

            mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Vector3 center = Vector3.zero;
            vertices.Add(center); // index 0

            // Generate rounded arc points
            List<Vector3> arcPoints = new List<Vector3>();
            for (int i = 0; i < _points.Count; i++)
            {
                Vector3 prev = _points[(i - 1 + _points.Count) % _points.Count].localPosition;
                Vector3 current = _points[i].localPosition;
                Vector3 next = _points[(i + 1) % _points.Count].localPosition;

                Vector3 dirA = (prev - current).normalized;
                Vector3 dirB = (next - current).normalized;

                Vector3 start = current + dirA * _cornerRadius;
                Vector3 end = current + dirB * _cornerRadius;

                // Create arc using Bezier between start-current-end
                for (int j = 0; j < _cornerResolution; j++)
                {
                    float t = j / (float)(_cornerResolution - 1);
                    Vector3 arcPoint = Bezier(start, current, end, t);
                    arcPoints.Add(arcPoint);
                }
            }

            // Add arc points to mesh vertices
            vertices.AddRange(arcPoints);

            int arcPointCount = arcPoints.Count;

            // Add triangles from center to each arc segment
            for (int i = 0; i < arcPointCount; i++)
            {
                int a = 0; // center
                int b = 1 + i;
                int c = 1 + ((i + 1) % arcPointCount);
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().mesh = mesh;
        }

        public Vector3 GetRandomPointOnMesh()
        {
            if (mesh == null)
            {
                Debug.LogError("Mesh not assigned.");
                return Vector3.zero;
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

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