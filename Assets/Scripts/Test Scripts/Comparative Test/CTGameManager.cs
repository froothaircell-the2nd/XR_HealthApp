using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using CoreResources.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTGameManager : DestroyableMonoSingleton<CTGameManager>
{
    #region Serialized Fields
    [SerializeField]
    private CT_GameObjectPool _pool1, _pool2;

    [Space(5)]

    [Header("Game Set - Application 1")]

    [SerializeField]
    private GameObject _gameSet1;
    [SerializeField]
    private GameObject _gameBoard;
    [SerializeField]
    private Transform _gameGizmosPosition1, 
        _spawnCenter1;
    [SerializeField]
    private float _minSpawnRadius1 = 0.01f, 
        _maxSpawnRadius1 = 3f, 
        _minSpawnDelay1 = 0.8f, 
        _maxSpawnDelay1 = 5f;

    [SerializeField]
    private bool _showGizmos1 = false;

    [Space(5)]

    [Header("Game Set - Application 2")]

    [SerializeField]
    private GameObject _gameSet2;
    [SerializeField]
    private Transform _gameGizmosPosition2,
        _spawnCenter2;
    [SerializeField]
    private float _minSpawnRadius2 = 0.01f,
        _maxSpawnRadius2 = 3f,
        _minSpawnDelay2 = 0.8f,
        _maxSpawnDelay2 = 5f;

    [SerializeField]
    private bool _showGizmos2 = false;

    [Space(5)]

    [SerializeField]
    private LayerMask _collisionLayerMask;
    [SerializeField]
    private float _maxRaycastDistance = 50;
    [SerializeField]
    private Transform _camHMD;
    #endregion

    #region Private Fields
    private Vector3 _defaultSpawnPosition1;
    private Vector3 _defaultSpawnPosition2;
    private Quaternion _defaultSpawnRotation1;
    private Quaternion _defaultSpawnRotation2;
    private Coroutine _spawnCoroutine;
    private const float GIZMO_DISK_THICKNESS = 0.01f;
    #endregion

    #region Events
    /// <summary>
    /// Event to invoke when launching application 
    /// variant, the parameter should be true for 
    /// application 1 and vice versa
    /// </summary>
    public static Action<bool> OnPlayEvent;
    public static Action OnExitEvent;
    #endregion

    #region Overrides
    public override void OnInit()
    {
        _defaultSpawnPosition1 = _spawnCenter1.position;
        _defaultSpawnPosition2 = _spawnCenter2.position;
        _defaultSpawnRotation1 = _spawnCenter1.rotation;
        _defaultSpawnRotation2 = _spawnCenter2.rotation;

        OnPlayEvent += OnPlay;
        OnExitEvent += OnExit;
    }

    public override void OnDeInit()
    {
        OnPlayEvent -= OnPlay;
        OnExitEvent -= OnExit;

        ResetGame();
    }

    private void OnDrawGizmos()
    {
        if (_showGizmos1)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(0.47f, 0.208f, 1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(_gameGizmosPosition1.position, _gameGizmosPosition1.rotation, new Vector3(1, GIZMO_DISK_THICKNESS, 1));
            Gizmos.DrawSphere(Vector3.zero, _maxSpawnRadius1);
            Gizmos.matrix = oldMatrix;

            
        }
        if (_showGizmos2)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.color = new Color(0.47f, 0.208f, 1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(_gameGizmosPosition2.position, _gameGizmosPosition2.rotation, new Vector3(1, GIZMO_DISK_THICKNESS, 1));
            Gizmos.DrawSphere(Vector3.zero, _maxSpawnRadius2);
            Gizmos.matrix = oldMatrix;
        }

        //var pos = _camHMD.position;
        //var rot = _camHMD.forward;

        //Gizmos.DrawLine(pos, (pos + rot) * 10);
    }
    #endregion

    #region Private Methods
    private void OnPlay(bool isApp1)
    {
        if (InputManager.IsInstantiated)
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += OnTriggerClick;
        
        if (isApp1)
        {
            _gameSet1.SetActive(true);

            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            _pool1.UnlockPool();
            _spawnCoroutine = StartCoroutine(SpawnCoroutine_App1());
        }
        else
        {
            _gameSet2.SetActive(true);

            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            _pool2.UnlockPool();
            _spawnCoroutine = StartCoroutine(SpawnCoroutine_App2());
        }
    }


    private void OnExit()
    {
        if (InputManager.IsInstantiated)
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed -= OnTriggerClick;

        ResetGame();
    }

    private void ResetGame()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        _spawnCenter1.localPosition = _defaultSpawnPosition1;
        _spawnCenter2.localPosition = _defaultSpawnPosition2;
        _spawnCenter1.rotation = _defaultSpawnRotation1;
        _spawnCenter2.rotation = _defaultSpawnRotation2;
        
        _gameSet1.SetActive(false);
        _gameSet2.SetActive(false);

        _pool1.CleanPool();
        _pool2.CleanPool();
    }

    private IEnumerator SpawnCoroutine_App1()
    {
        while (true)
        {
            var angleRad = UnityEngine.Random.Range(0f, 360f).ToRadians();
            var radius = UnityEngine.Random.Range(_minSpawnRadius1, _maxSpawnRadius1);
            var delay = UnityEngine.Random.Range(_minSpawnDelay1, _maxSpawnDelay1);

            yield return new WaitForSecondsRealtime(delay);

            // spawn on a random location within a radius range and angle range
            _spawnCenter1.localPosition = Vector3.zero;
            _spawnCenter1.localPosition = new Vector3(radius * Mathf.Cos(angleRad), radius * Mathf.Sin(angleRad), _spawnCenter1.localPosition.z);
            _pool1.SpawnItem(_spawnCenter1.position, _spawnCenter1.rotation);
        }
    }

    private IEnumerator SpawnCoroutine_App2()
    {
        while (true)
        {
            var angleRad = UnityEngine.Random.Range(0f, 360f).ToRadians();
            var radius = UnityEngine.Random.Range(_minSpawnRadius2, _maxSpawnRadius2);
            var delay = UnityEngine.Random.Range(_minSpawnDelay2, _maxSpawnDelay2);

            yield return new WaitForSecondsRealtime(delay);

            // spawn on a random location within a radius range and angle range
            _spawnCenter2.localPosition = Vector3.zero;
            _spawnCenter2.localPosition = new Vector3(radius * Mathf.Cos(angleRad), radius * Mathf.Sin(angleRad), _spawnCenter2.localPosition.z);
            _spawnCenter2.LookAt(_camHMD);
            _pool2.SpawnItem(_spawnCenter2.position, _spawnCenter2.rotation);
        }
    }
    #endregion

    #region Input Listeners
    private void OnTriggerClick(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        var pos = _camHMD.position;
        var rot = _camHMD.forward;
        // var pos = InputManager.InputActions.XRIHead.Position.ReadValue<Vector3>();
        // var rot = InputManager.InputActions.XRIHead.Rotation.ReadValue<Quaternion>();

        if (Physics.Raycast(pos, rot, out var hit, _maxRaycastDistance, _collisionLayerMask.value))
        {
            hit.collider.GetComponent<CT_PooledItem>().ReturnToPool();
        }
    }
    #endregion
}
