using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using CoreResources.Utils;
using GameResources.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Gameplay
{
    public enum AppPhase
    {
        MainMenu = 0,
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
    }

    public class GameplayHandler : DestroyableMonoSingleton<GameplayHandler>
    {
        #region Serialized Fields
        [SerializeField]
        private ObjectPool _pool;
        [SerializeField]
        private PhysiologicalDataHandler _dataHandler;

        [Space(5)]

        [Header("Game Set")]
        [SerializeField]
        private GameObject _gameSet;
        [SerializeField]
        private Transform _spawnCenter;
        [SerializeField]
        private LookAreaGenerator _lookAreaGenerator;
        [SerializeField]
        private float _minSpawnDelay = 0.8f, 
        _maxSpawnDelay = 5f,
        _maxSpawnDistance = 10f;

        [Space(5)]

        [SerializeField]
        private LayerMask _collisionLayerMask;
        [SerializeField]
        private float _maxRaycastDistance = 50;
        [SerializeField]
        private Transform _camHMD;
        #endregion

        #region Private Fields
        private Vector3 _defaultSpawnPosition;
        private Quaternion _defaultSpawnRotation;
        private Coroutine _spawnCoroutine;
        private int _spawnCount;
        private AppPhase _phase;
        #endregion

        public AppPhase Phase => _phase;

        #region Events
        /// <summary>
        /// Event to invoke when launching application 
        /// variant, the parameter should be the 
        /// corresponding value for the application phase
        /// </summary>
        public static Action<int> OnPlayEvent;
        public static Action OnExitEvent;
        public static Action OnHit;
        #endregion

        #region Overrides
        public override void OnInit()
        {
            _defaultSpawnPosition = _spawnCenter.position;
            _defaultSpawnRotation = _spawnCenter.rotation;

            _dataHandler.InitSingleton();

            OnPlayEvent += OnPlay;
            OnExitEvent += OnExit;
        }

        public override void OnDeInit()
        {
            OnPlayEvent -= OnPlay;
            OnExitEvent -= OnExit;

            _dataHandler.CleanSingleton();

            ResetGame();
        }
        #endregion

        #region Private Methods
        private void OnPlay(int appPhase)
        {
            if (InputManager.IsInstantiated)
                InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += OnTriggerClick;

            switch (appPhase)
            {
                case 1:
                    _gameSet.SetActive(true);
                    _lookAreaGenerator.gameObject.SetActive(true);
                    _lookAreaGenerator.AllowLookAreaModification();

                    if (_spawnCoroutine != null)
                    {
                        StopCoroutine(_spawnCoroutine);
                        _spawnCoroutine = null;
                    }

                    _phase = (AppPhase) appPhase;
                    break;
                case 2:
                    _lookAreaGenerator.RestrictLookAreaModification();

                    _pool.UnlockPool();
                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_AppP1());

                    _phase = (AppPhase)appPhase;
                    break;
                case 3:
                    break;
                default:
                    break;
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

            _phase = 0;

            _spawnCenter.localPosition = _defaultSpawnPosition;
            _spawnCenter.rotation = _defaultSpawnRotation;

            _gameSet.SetActive(false);

            _lookAreaGenerator.RestrictLookAreaModification();
            _lookAreaGenerator.gameObject.SetActive(false);

            _dataHandler.ResetMetrics();

            _pool.CleanPool();
            _spawnCount = 0;
        }

        private IEnumerator SpawnCoroutine_AppP1()
        {
            while (_spawnCount < 15)
            {
                // var angleRad = UnityEngine.Random.Range(0f, 360f).ToRadians();
                // var radius = UnityEngine.Random.Range(_minSpawnRadius2, _maxSpawnRadius2);
                var pos = _lookAreaGenerator.GetRandomPointOnMesh();
                var dir = (pos - _camHMD.position).normalized;
                pos += (dir * UnityEngine.Random.Range(0f, _maxSpawnDistance));

                // Access the look area handler and use the spawn function
                
                var delay = UnityEngine.Random.Range(_minSpawnDelay, _maxSpawnDelay);

                yield return new WaitForSecondsRealtime(delay);

                // spawn on a random location within a radius range and angle range
                _spawnCenter.localPosition = Vector3.zero;
                _spawnCenter.position = pos;
                _spawnCenter.LookAt(_camHMD);
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation);
                _spawnCount++;
            }

            ResetGame();
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
                hit.collider.GetComponent<ProjectileController>().ReturnToPool();
                OnHit?.Invoke();
            }
        }
        #endregion
    }
}