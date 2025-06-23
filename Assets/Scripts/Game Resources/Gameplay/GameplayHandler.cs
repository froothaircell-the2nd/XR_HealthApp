using BezierSolution;
using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using CoreResources.Utils;
using GameResources.Gameplay.VRController;
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

        [Header("Game Set - Application Phase 1")]
        [SerializeField]
        private LookAreaGenerator _lookAreaGenerator;

        [Space(5)]
        
        [Header("Game Set - Application Phase 2")]
        [SerializeField]
        private GameObject _gameSetPhase2;
        [SerializeField]
        private Transform _spawnCenter;
        [SerializeField]
        private float _minSpawnDelay = 0.8f, 
        _maxSpawnDelay = 5f,
        _maxSpawnDistance = 10f;
        [SerializeField]
        private LayerMask _collisionLayerMask;
        [SerializeField]
        private int _phase2SpawnCount = 15;
        [SerializeField]
        private Transform _camHMD;

        [Space(5)]

        [Header("Game Set - Application Phase 3")]
        [SerializeField]
        private GameObject _gameSetPhase3;
        [SerializeField]
        private BezierSpline[] _bezierSplines;
        [SerializeField]
        private InteractionPanel _interactionPanel_AppP3;

        #endregion

        #region Private Fields
        private Vector3 _defaultSpawnPosition;
        private Quaternion _defaultSpawnRotation;
        private Coroutine _spawnCoroutine;
        private int _spawnCount;
        private bool _triggerPressed,
            _centeringReticleDespawned = false,
            _phase2ProjectileDespawned = false,
            _phase3NextProjectileRequested = false;
        private AppPhase _phase;

        private ProjectileController _cachedProjectile = null;
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
        public static Action OnPhase2Hit;
        public static Action OnPhase2Complete;
        public static Action OnEnablePhase3NextButton;
        public static Action OnPhase3NextItem;
        public static Action OnPhase3Complete;
        #endregion

        #region Overrides
        public override void OnInit()
        {
            _defaultSpawnPosition = _spawnCenter.position;
            _defaultSpawnRotation = _spawnCenter.rotation;

            _dataHandler.InitSingleton();

            OnPlayEvent += OnPlay;
            OnExitEvent += OnExit;
            OnPhase2Hit += OnHitPerformed_AppP2;
            OnPhase3NextItem += OnNextProjectileRequested_AppP3;
        }

        public override void OnDeInit()
        {
            OnPlayEvent = null;
            OnExitEvent = null;
            OnPhase2Hit = null;
            OnPhase2Complete = null;
            OnPhase3NextItem = null;
            OnPhase3Complete = null;
            OnEnablePhase3NextButton = null;

            _dataHandler.CleanSingleton();

            ResetGame();
        }
        #endregion

        #region Private Methods
        private void InitializeCenterCursor(PooledItem item)
        {
            var res = (ProjectileController) item;

            res.InitializeItem(ProjectileMode.CenteringReticle);
            res.OnCenteringReticleDespawned += () => { _centeringReticleDespawned = true; };
        }

        private void InitalizePhase2Projectile(PooledItem item)
        {
            var res = (ProjectileController) item;

            res.InitializeItem(ProjectileMode.Phase2Projectile);
        }

        private void InitializePhase3Projectile(PooledItem item, BezierSpline spline)
        {
            var res = (ProjectileController) item;

            res.InitializeItem(ProjectileMode.Phase3Projectile);
            res.InjectSpline(spline);
        }

        private void ResetGame(bool hardRest = true)
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            if (_cachedProjectile != null)
            {
                _cachedProjectile.ReturnToPool();
                _cachedProjectile = null;
            }

            _phase = 0;

            _centeringReticleDespawned = false;
            _phase2ProjectileDespawned = false;

            _spawnCenter.localPosition = _defaultSpawnPosition;
            _spawnCenter.rotation = _defaultSpawnRotation;

            _interactionPanel_AppP3.DeInitializePanel();

            if (hardRest)
            {
                _gameSetPhase2.SetActive(false);
                _gameSetPhase3.SetActive(false);

                foreach (var item in _bezierSplines)
                {
                    item.gameObject.SetActive(false);
                }

                _lookAreaGenerator.RestrictLookAreaModification();
                _lookAreaGenerator.gameObject.SetActive(false);

                _dataHandler.ResetMetrics();
            }

            _pool.CleanPool();
            _spawnCount = 0;
        }

        private IEnumerator SpawnCoroutine_AppP2()
        {
            while (_spawnCount < _phase2SpawnCount)
            {
                // var angleRad = UnityEngine.Random.Range(0f, 360f).ToRadians();
                // var radius = UnityEngine.Random.Range(_minSpawnRadius2, _maxSpawnRadius2);
                var pos = _lookAreaGenerator.GetRandomPointOnMesh();
                var dir = (pos - _camHMD.position).normalized;
                pos += (dir * UnityEngine.Random.Range(0f, _maxSpawnDistance));

                // Access the look area handler and use the spawn function
                // var delay = UnityEngine.Random.Range(_minSpawnDelay, _maxSpawnDelay);

                // yield return new WaitForSecondsRealtime(delay);
                _spawnCenter.localPosition = Vector3.zero;
                _spawnCenter.localRotation = Quaternion.identity;
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, InitializeCenterCursor);

                yield return new WaitUntil(() => _centeringReticleDespawned);

                // spawn on a random location within a radius range and angle range
                _spawnCenter.position = pos;
                _spawnCenter.LookAt(_camHMD);
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, InitalizePhase2Projectile);
                ++_spawnCount;

                _centeringReticleDespawned = false;

                yield return new WaitUntil(() => _phase2ProjectileDespawned);

                _phase2ProjectileDespawned = false;
            }

            ResetGame(false);

            OnPhase2Complete?.Invoke();
        }

        private IEnumerator SpawnCoroutine_AppP3()
        {
            while (_spawnCount < _bezierSplines.Length)
            {
                var currSpline = _bezierSplines[_spawnCount];
                currSpline.gameObject.SetActive(true);

                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, (item) => { InitializePhase3Projectile(item, currSpline); });
                ++_spawnCount;

                yield return new WaitUntil(() => _phase3NextProjectileRequested);

                currSpline.gameObject.SetActive(false);
                _phase3NextProjectileRequested = false;
            }

            ResetGame(false);

            OnPhase3Complete?.Invoke();
        }
        #endregion

        #region Event Listeners
        private void OnPlay(int appPhase)
        {
            if (CursorHandler.IsInstantiated)
            {
                CursorHandler.Instance.OnValidSelection += OnValidSelection;
                CursorHandler.Instance.OnValidCancellation += OnValidSelectionCancelled;
                CursorHandler.Instance.OnValidInteractionStarted += OnValidInteractionStarted;
                CursorHandler.Instance.OnValidInteractionPerformed += OnValidInteractionPerformed;
                CursorHandler.Instance.OnValidInteractionCancelled += OnValidInteractionCancelled;
            }

            switch (appPhase)
            {
                case 1:
                    _gameSetPhase2.SetActive(true);
                    _lookAreaGenerator.gameObject.SetActive(true);
                    _lookAreaGenerator.AllowLookAreaModification();

                    if (_spawnCoroutine != null)
                    {
                        StopCoroutine(_spawnCoroutine);
                        _spawnCoroutine = null;
                    }

                    _phase = (AppPhase)appPhase;
                    break;
                case 2:
                    _lookAreaGenerator.RestrictLookAreaModification();

                    _phase2ProjectileDespawned = false;
                    _centeringReticleDespawned = false;

                    _pool.UnlockPool();
                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_AppP2());

                    _phase = (AppPhase)appPhase;
                    break;
                case 3:
                    _gameSetPhase3.SetActive(true);
                    _lookAreaGenerator.RestrictLookAreaModification();
                    _lookAreaGenerator.gameObject.SetActive(false);
                    _gameSetPhase2.SetActive(false);

                    _interactionPanel_AppP3.InitializePanel();    

                    _pool.UnlockPool();

                    if (_spawnCoroutine != null)
                    {
                        StopCoroutine(_spawnCoroutine);
                        _spawnCoroutine = null;
                    }

                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_AppP3());

                    _phase = (AppPhase)appPhase;
                    break;
                default:
                    break;
            }
        }

        private void OnExit()
        {
            if (CursorHandler.IsInstantiated)
            {
                CursorHandler.Instance.OnValidSelection -= OnValidSelection;
                CursorHandler.Instance.OnValidCancellation -= OnValidSelectionCancelled;
                CursorHandler.Instance.OnValidInteractionPerformed -= OnValidInteractionPerformed;
            }

            ResetGame();
        }

        private void OnHitPerformed_AppP2()
        {
            _phase2ProjectileDespawned = true;
        }

        private void OnNextProjectileRequested_AppP3()
        {
            _phase3NextProjectileRequested = true;
        }

        private void OnValidSelection(Transform objTransform, Collider objCollider)
        {
            if (!_triggerPressed && (_collisionLayerMask.value & (1 << objCollider.gameObject.layer)) > 0)
            {
                var currSelection = objCollider.GetComponent<ProjectileController>();
                currSelection.HandleSelectionEnter();
                
                // objCollider.GetComponent<ProjectileController>().ReturnToPool();
                // OnHit?.Invoke();
                _triggerPressed = true;
            }
        }

        private void OnValidSelectionCancelled()
        {
            _triggerPressed = false;
        }

        private void OnValidInteractionStarted(Transform transform, Collider collider)
        {
            var currProj = transform.GetComponent<ProjectileController>();

            if (_cachedProjectile != null && _cachedProjectile != currProj)
            {
                _cachedProjectile.CenteringReticleContracting = false;
            }

            _cachedProjectile = currProj;
            
            if (_cachedProjectile != null && !_centeringReticleDespawned)
            {
                _cachedProjectile.CenteringReticleContracting = true;
            }
        }

        private void OnValidInteractionPerformed(Transform transform, Collider collider)
        {
            // Debug.Log("Interaction Scripts working");
            
        }

        private void OnValidInteractionCancelled()
        {
            // var currProjectile = transform.GetComponent<ProjectileController>();

            if (_cachedProjectile != null && !_centeringReticleDespawned)
            {
                _cachedProjectile.CenteringReticleContracting = false;
            }
        }
        #endregion
    }
}