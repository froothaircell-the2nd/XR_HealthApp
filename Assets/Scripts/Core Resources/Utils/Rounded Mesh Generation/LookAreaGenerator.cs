using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using GameResources.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CoreResources.Utils
{
    [RequireComponent(typeof(MeshFilter))]
    public class LookAreaGenerator : DestroyableMonoSingleton<LookAreaGenerator>
    {
        [SerializeField] private Transform _camHMD;
        [SerializeField] private List<Transform> _points = new List<Transform>(8);
        [SerializeField] private float _cornerRadius = 0.1f;
        [SerializeField] private int _cornerResolution = 4;
        [SerializeField] private Transform _spawnPos;
        [SerializeField] private float _borderThickness = 0.1f, _maxRaycastDistance = 50f, _spherecastRadius = 1f;
        [SerializeField] private LayerMask _collisionLayerMask;

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

        // Handling user input for handling movement
        private bool _lookAreaModificationAllowed = false;
        private bool _triggerPressed = false;
        private List<LookAreaInteractable> _interactables = new List<LookAreaInteractable>();
        private LookAreaInteractable _currentInteractable = null;

        #region Overrides
        public override void OnInit()
        {
            if (_interactables.Count == 0)
            {
                for (int i = 0; i < _points.Count; i++)
                {
                    var currItem = _points[i].GetComponent<LookAreaInteractable>();
                    _interactables.Add(currItem);
                    currItem.InitializeInteractable();
                }
            }
        }

        public override void OnDeInit()
        {
        }
        #endregion

        private void Update()
        {
            GenerateMeshes();

            if (_lookAreaModificationAllowed && _triggerPressed && _currentInteractable != null)
            {
                ProcessMovementInput();
            }
        }

        #region Public Methods
        public void GenerateMeshes()
        {
            if (_points.Count < 3)
            {
                UnityEngine.Debug.LogError("Need at least 3 points.");
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

        public void AllowLookAreaModification()
        {
            for (int i = 0; i < _interactables.Count; i++)
            {
                _interactables[i].EnableInteraction();
            }

            _lookAreaModificationAllowed = true;
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += OnTriggerClick;
        }

        public void RestrictLookAreaModification()
        {
            for (int i = 0; i < _interactables.Count; i++)
            {
                _interactables[i].DisableInteraction();
            }

            _lookAreaModificationAllowed = false;
        }
        #endregion

        private void ProcessMovementInput()
        {
            if (InputManager.InputActions.XRILeftHandInteraction.UIPress.WasReleasedThisFrame())
            {
                if (_currentInteractable != null)
                    _currentInteractable.SetHighlight(false);

                _triggerPressed = false;
                _currentInteractable = null;

                return;
            }

            var pos = _camHMD.position;
            var rot = _camHMD.forward;
            // var pos = InputManager.InputActions.XRIHead.Position.ReadValue<Vector3>();
            // var rot = InputManager.InputActions.XRIHead.Rotation.ReadValue<Quaternion>();

            if (Physics.SphereCast(pos, _spherecastRadius, rot, out var hit, _maxRaycastDistance, _collisionLayerMask.value))
            {
                _currentInteractable.UpdatePosition(hit.point);
            }
        }

        private List<Vector3> GenerateArcPoints(List<Transform> points, float radius, int resolution)
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
                UnityEngine.Debug.LogError("Mesh not assigned.");
                return Vector3.zero;
            }

            Vector3[] vertices = _fillMesh.vertices;
            int[] triangles = _fillMesh.triangles;

            // Pick a random triangle
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;

            Vector3 a = transform.TransformPoint(vertices[triangles[triIndex]]);
            Vector3 b = transform.TransformPoint(vertices[triangles[triIndex + 1]]);
            Vector3 c = transform.TransformPoint(vertices[triangles[triIndex + 2]]);

            var pos = GetRandomPointInTriangle(a, b, c);

            return pos;
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

        #region Event Listeners
        private void OnTriggerClick(InputAction.CallbackContext obj)
        {
            if (obj.performed)
            {
                var pos = _camHMD.position;
                var rot = _camHMD.forward;
                // var pos = InputManager.InputActions.XRIHead.Position.ReadValue<Vector3>();
                // var rot = InputManager.InputActions.XRIHead.Rotation.ReadValue<Quaternion>();

                if (Physics.SphereCast(pos, _spherecastRadius, rot, out var hit, _maxRaycastDistance, _collisionLayerMask.value))
                {
                    var interactable = hit.collider.GetComponent<LookAreaInteractable>();
                    if (interactable != null)
                    {
                        _currentInteractable = interactable;
                        _currentInteractable.SetHighlight(true);
                        _triggerPressed = true;
                    }
                    // OnHit?.Invoke();
                }
            }
        }
        #endregion
    }
}