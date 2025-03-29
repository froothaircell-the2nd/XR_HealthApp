using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CoreResources.Utils;
using CoreResources.Managers.InputManagement;
using Wave.Essence.Raycast;
using UnityEngine.InputSystem;

public class BasicGameSimulator : MonoBehaviour
{
    [SerializeField]
    private Button _playButton;
    [SerializeField]
    private Button _exitButton;

    [SerializeField]
    private GameObjectPool _pool;
    [SerializeField]
    private GameObject _gameSet;
    [SerializeField] 
    private Transform _gameGizmosPosition;
    [SerializeField]
    private Transform _spawnCenter;
    [SerializeField]
    private float _minSpawnRadius = 0.01f;
    [SerializeField]
    private float _maxSpawnRadius = 3f;
    [SerializeField]
    private float _minSpawnDelay = 0.8f;
    [SerializeField]
    private float _maxSpawnDelay = 5f;

    [SerializeField]
    private bool _showGizmos = false;
    [SerializeField]
    private LayerMask _collisionLayerMask;
    [SerializeField]
    private float _maxRaycastDistance = 50;
    [SerializeField]
    private Transform HMD_Cam;

    private Vector3 _defaultSpawnPosition;
    private Coroutine _spawnCoroutine;
    private const float GIZMO_DISK_THICKNESS = 0.01f;

    private void Awake()
    {
        _playButton.onClick.AddListener(OnPlay);
        _exitButton.onClick.AddListener(OnExit);

        _defaultSpawnPosition = _spawnCenter.position;

        ResetGame();
    }

    private void OnDestroy()
    {
        ResetGame();
    }

    private void OnDrawGizmos()
    {
        if (_showGizmos)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(0.47f, 0.208f, 1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(_gameGizmosPosition.position, _gameGizmosPosition.rotation, new Vector3(1, GIZMO_DISK_THICKNESS, 1));
            Gizmos.DrawSphere(Vector3.zero, _maxSpawnRadius);
            Gizmos.matrix = oldMatrix;

            var pos = HMD_Cam.position;
            var rot = HMD_Cam.forward;

            Gizmos.DrawLine(pos, (pos + rot) * 10);
        }
    }

    private void OnPlay()
    {
        // Start the game simulation
        _playButton.gameObject.SetActive(false);
        _exitButton.gameObject.SetActive(true);
        _gameSet.SetActive(true);

        if (InputManager.IsInstantiated)
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += Activate_performed;


        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        _spawnCoroutine = StartCoroutine(SpawnCoroutine());
    }


    private void OnExit()
    {
        if (InputManager.IsInstantiated)
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed -= Activate_performed;

        ResetGame();
    }

    private void ResetGame()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        _playButton.gameObject.SetActive(true);
        _exitButton.gameObject.SetActive(false);
        _spawnCenter.localPosition = _defaultSpawnPosition;
        _gameSet.SetActive(false);
        // _pool.gameObject.SetActive(true);
        _pool.CleanPool();
    }

    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            var angleRad = UnityEngine.Random.Range(0f, 360f).ToRadians();
            var radius = UnityEngine.Random.Range(_minSpawnRadius, _maxSpawnRadius);
            var delay = UnityEngine.Random.Range(_minSpawnDelay, _maxSpawnDelay);

            yield return new WaitForSecondsRealtime(delay);

            // spawn on a random location within a radius range and angle range
            _spawnCenter.localPosition = new Vector3(radius * Mathf.Cos(angleRad), radius * Mathf.Sin(angleRad), _spawnCenter.localPosition.z);
            _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation);
            _spawnCenter.localPosition = Vector3.zero;
        }
    }


    private void Activate_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        var pos = HMD_Cam.position;
        var rot = HMD_Cam.forward;


        if (Physics.Raycast(pos, rot, out var hit, _maxRaycastDistance, _collisionLayerMask.value))
        {
            hit.collider.GetComponent<PooledItem>().ReturnToPool();
        }
    }
}
