using BezierSolution;
using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using CoreResources.Utils;
using GameResources.Gameplay.VRController;
using GameResources.Pooling;
using GameResources.StateMachine;
using GameResources.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameResources.Gameplay
{
    public enum AppPhase
    {
        MainMenu = 0,
        Warmup = 1,
        Phase1 = 2,
        Phase2 = 3,
        Phase3 = 4,
        Phase4 = 5,
    }

    public class GameplayHandler : DestroyableMonoSingleton<GameplayHandler>
    {
        #region Serialized Fields
        [SerializeField]
        private float _appCalibrationDistance = 15f;
        [SerializeField]
        private ObjectPool _pool;
        [SerializeField]
        private PhysiologicalDataHandler _dataHandler;

        [Space(5)]
        
        [Header("Game Set - Application Warmup")]
        [SerializeField]
        private GameObject _gameSetWarmup;
        [SerializeField]
        private Transform[] _defaultTransforms;

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

        [Space(5)]

        [Header("Game Set - Application Phase 4")]
        [SerializeField, Range(0, 8)]
        private int _phase4TargetSpawnCount = 2; 
        #endregion

        #region Private Fields
        private Vector3 _defaultSpawnPosition;
        private Quaternion _defaultSpawnRotation;
        private Coroutine _spawnCoroutine;
        private int _spawnCount, _warmupSelectedCount;
        private bool _triggerPressed = false,
            _inputsAssigned = false,
            _centeringReticleDespawned = false,
            _phase2ProjectileDespawned = false,
            _phase3NextProjectileRequested = false,
            _phase4TargetRequested = false,
            _phase4TargetDespawned = false,
            _phase4HeadRecentered = false;
        private AppPhase _phase;

        private ProjectileController _cachedProjectile_Interaction = null;
        private ProjectileController _cachedProjectile_Selection = null;

        private const string P4_BLACKOUT_TEXT = "Please recenter your head position and then press the right grip button";
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
        public static Action OnWarmupComplete;
        public static Action OnPhase2Hit;
        public static Action OnPhase2Complete;
        public static Action OnEnablePhase3NextButton;
        public static Action OnPhase3NextItem;
        public static Action OnPhase3Complete;
        public static Action OnEnablePhase4NextButton;
        public static Action OnPhase4NextItem;
        public static Action OnPhase4Complete;
        #endregion

        #region Overrides
        public override void OnInit()
        {
            _defaultSpawnPosition = _spawnCenter.position;
            _defaultSpawnRotation = _spawnCenter.rotation;

            _dataHandler.InitSingleton(_camHMD);
            _lookAreaGenerator.InitSingleton(_camHMD);

            OnPlayEvent += OnPlay;
            OnExitEvent += OnExit;
            OnPhase2Hit += OnHitPerformed_AppP2;
            OnPhase3NextItem += OnNextProjectileRequested_AppP3;
            OnPhase4NextItem += OnNextProjectileRequested_AppP4;

            _inputsAssigned = false;

            StartCoroutine(WaitForInputSystem());
        }

        public override void OnDeInit()
        {
            OnPlayEvent = null;
            OnExitEvent = null;
            OnWarmupComplete = null;
            OnPhase2Hit = null;
            OnPhase2Complete = null;
            OnEnablePhase3NextButton = null;
            OnPhase3NextItem = null;
            OnPhase3Complete = null;
            OnPhase4NextItem = null;
            OnPhase4Complete = null;

            _dataHandler.CleanSingleton();

            ResetGame();
        }
        #endregion

        #region Public Methods
        public void CalibrateSceneToCameraOrientation()
        {
            // Set Positions of relevant 
            Vector3 forward = _camHMD.forward;
            _camHMD.GetPositionAndRotation(out Vector3 origin, out Quaternion rotation);
            Vector3 finalPosition = origin + forward * _appCalibrationDistance;

            _gameSetWarmup.transform.position = finalPosition;
            _gameSetWarmup.transform.rotation = rotation;
            
            _lookAreaGenerator.transform.position = finalPosition;
            _lookAreaGenerator.transform.rotation = rotation;
            _lookAreaGenerator.RecalibrateInteractables();
            
            _gameSetPhase2.transform.position = finalPosition;
            _gameSetPhase2.transform.rotation = rotation;
            _defaultSpawnPosition = _spawnCenter.position;
            _defaultSpawnRotation = _spawnCenter.rotation;
            
            _gameSetPhase3.transform.position = finalPosition;
            _gameSetPhase3.transform.rotation = rotation;

            UIMediator.Instance.SetMenuPositions(_camHMD, origin, forward, rotation);
        }
        #endregion

        #region Event Listeners
        private void OnPlay(int appPhase)
        {
            if (CursorHandler.IsInstantiated)
            {
                CursorHandler.Instance.OnValidSelectionPerformed += OnValidSelection;
                CursorHandler.Instance.OnValidSelectionCancelled += OnValidSelectionCancelled;
                CursorHandler.Instance.OnValidInteractionStarted += OnValidInteractionStarted;
                CursorHandler.Instance.OnValidInteractionPerformed += OnValidInteractionPerformed;
                CursorHandler.Instance.OnValidInteractionCancelled += OnValidInteractionCancelled;
            }

            switch (appPhase)
            {
                case 1:
                    // Initialization for warmup
                    _gameSetWarmup.SetActive(true);
                    _lookAreaGenerator.gameObject.SetActive(false);

                    if (_spawnCoroutine != null)
                    {
                        StopCoroutine(_spawnCoroutine);
                        _spawnCoroutine = null;
                    }

                    _pool.UnlockPool();
                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_Warmup());

                    _phase = (AppPhase)appPhase;
                    break;
                case 2:
                    // ResetGame(false);
                    _gameSetWarmup.SetActive(false);
                    _gameSetPhase2.SetActive(true);
                    _lookAreaGenerator.gameObject.SetActive(true);
                    _lookAreaGenerator.ResetInteractables();
                    _lookAreaGenerator.AllowLookAreaModification();

                    if (_spawnCoroutine != null)
                    {
                        StopCoroutine(_spawnCoroutine);
                        _spawnCoroutine = null;
                    }

                    _phase = (AppPhase)appPhase;
                    break;
                case 3:
                    _lookAreaGenerator.RestrictLookAreaModification();

                    _phase2ProjectileDespawned = false;
                    _centeringReticleDespawned = false;

                    _pool.UnlockPool();
                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_AppP2());

                    _phase = (AppPhase)appPhase;
                    break;
                case 4:
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
                case 5:
                    // Initialization for proprioception test
                    _gameSetPhase3.SetActive(false);
                    _lookAreaGenerator.RestrictLookAreaModification();
                    _lookAreaGenerator.gameObject.SetActive(true);
                    // _gameSetPhase2and4.SetActive(true);

                    foreach (var item in _bezierSplines)
                    {
                        item.gameObject.SetActive(false);
                    }

                    _spawnCoroutine = StartCoroutine(SpawnCoroutine_AppP4());

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
                CursorHandler.Instance.OnValidSelectionPerformed -= OnValidSelection;
                CursorHandler.Instance.OnValidSelectionCancelled -= OnValidSelectionCancelled;
                CursorHandler.Instance.OnValidInteractionStarted -= OnValidInteractionStarted;
                CursorHandler.Instance.OnValidInteractionPerformed -= OnValidInteractionPerformed;
                CursorHandler.Instance.OnValidInteractionCancelled -= OnValidInteractionCancelled;
            }

            ResetGame(false);
        }

        private void RightGripPressed(InputAction.CallbackContext obj)
        {
            switch (_phase)
            {
                case AppPhase.Phase4:
                    if (_phase4TargetDespawned && !_phase4HeadRecentered)
                        _phase4HeadRecentered = true;
                    break;
                default:
                    break;
            }
        }

        private void OnWarmupTargetSelected()
        {
            ++_warmupSelectedCount;
            // Debug.LogError($"Warmup Count incremented. Current Count: {_warmupSelectedCount}");
        }

        private void OnHitPerformed_AppP2()
        {
            _phase2ProjectileDespawned = true;
        }

        private void OnNextProjectileRequested_AppP3()
        {
            _phase3NextProjectileRequested = true;
        }

        private void OnNextProjectileRequested_AppP4()
        {
            _phase4TargetRequested = true;
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

        private void OnValidInteractionStarted(Transform transform, Collider collider, Vector3 _)
        {
            var currProj = transform.GetComponent<ProjectileController>();

            if (_cachedProjectile_Interaction != null && _cachedProjectile_Interaction != currProj)
            {
                _cachedProjectile_Interaction.HandleInteractionExit();
            }

            _cachedProjectile_Interaction = currProj;

            if (_cachedProjectile_Interaction == null)
                return;

            if ((Phase == AppPhase.Phase2 || Phase == AppPhase.Phase3) && !_centeringReticleDespawned)
            {
                _cachedProjectile_Interaction.HandleInteractionEnter();
            }
            else if (Phase == AppPhase.Warmup || Phase == AppPhase.Phase4)
            {
                _cachedProjectile_Interaction.HandleInteractionEnter();
            }
        }

        private void OnValidInteractionPerformed(Transform transform, Collider collider, Vector3 _)
        {
            
        }

        private void OnValidInteractionCancelled()
        {
            // var currProjectile = transform.GetComponent<ProjectileController>();

            if (_cachedProjectile_Interaction != null && !_centeringReticleDespawned)
            {
                _cachedProjectile_Interaction.CenteringReticleContracting = false;
            }
        }
        #endregion

        #region Spawn Coroutines
        private IEnumerator SpawnCoroutine_Warmup()
        {
            _spawnCount = 0;
            _warmupSelectedCount = 0;

            int maxCount = _defaultTransforms.Length;

            while (_spawnCount < maxCount)
            {
                var currTrnsfrm = _defaultTransforms[_spawnCount];

                ProjectileController item = (ProjectileController)_pool.SpawnItem(currTrnsfrm.position, currTrnsfrm.rotation, InitializeWarmupTarget);

                item.OnWarmupTargetSelected += OnWarmupTargetSelected;

                ++_spawnCount;
            }

            yield return new WaitUntil(() => _warmupSelectedCount >= maxCount);

            OnWarmupComplete?.Invoke();
        }

        private IEnumerator SpawnCoroutine_AppP2()
        {
            _spawnCount = 0;

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
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, InitializeCenteringReticle);

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
            _spawnCount = 0;

            while (_spawnCount < _bezierSplines.Length)
            {
                var currSpline = _bezierSplines[_spawnCount];
                currSpline.gameObject.SetActive(true);

                _spawnCenter.position = currSpline.GetPoint(0);
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, (item) => { InitializePhase3Projectile(item, currSpline); });
                ++_spawnCount;

                yield return new WaitUntil(() => _phase3NextProjectileRequested);

                currSpline.gameObject.SetActive(false);
                _phase3NextProjectileRequested = false;
            }

            ResetGame(false);

            OnPhase3Complete?.Invoke();
            OnEnablePhase3NextButton?.Invoke();
        }

        private IEnumerator SpawnCoroutine_AppP4()
        {
            for (int i = 0; i < _phase4TargetSpawnCount; i++)
            {
                _spawnCenter.localPosition = Vector3.zero;
                _spawnCenter.localRotation = Quaternion.identity;
                _pool.SpawnItem(_spawnCenter.position, _spawnCenter.rotation, InitializeCenteringReticle);

                yield return new WaitUntil(() => _centeringReticleDespawned);

                var pos = _lookAreaGenerator.GetEdgePoint(i);
                _pool.SpawnItem(pos, Quaternion.identity, InitializePhase4Target);

                yield return new WaitUntil(() => _phase4TargetDespawned);

                BlackoutScreenHandler.Instance.SetBlackoutScreen(true, P4_BLACKOUT_TEXT);
                _lookAreaGenerator.SetMeshInteraction(true);

                yield return new WaitUntil(() => _phase4HeadRecentered);

                _lookAreaGenerator.DisplayTargetDistanceFromOrigin_AppP4();
                _lookAreaGenerator.SetMeshInteraction(false);

                BlackoutScreenHandler.Instance.SetBlackoutScreen(false);
                OnEnablePhase4NextButton?.Invoke();

                // Display the current position of the head pointer on the look area with respect to the "true center"

                yield return new WaitUntil(() => _phase4TargetRequested);

                _lookAreaGenerator.ClearDisplay_AppP4();
                _centeringReticleDespawned = false;
                _phase4TargetRequested = false;
                _phase4TargetDespawned = false;
                _phase4HeadRecentered = false;
            }


            ResetGame(false);
            
            OnPhase4Complete?.Invoke();
            OnEnablePhase4NextButton?.Invoke();
        }
        #endregion

        #region Private Methods
        private IEnumerator WaitForInputSystem()
        {
            yield return new WaitUntil(() => InputManager.IsInstantiated);

            InputManager.InputActions.XRIRightHandInteraction.Select.performed += RightGripPressed;

            _inputsAssigned = true;
        }

        private void InitializeWarmupTarget(PooledItem item)
        {
            var res = (ProjectileController)item;

            res.InitializeItem(ProjectileMode.WarmupTarget);
        }

        private void InitializeCenteringReticle(PooledItem item)
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

        private void InitializePhase4Target(PooledItem item)
        {
            var res = (ProjectileController)item;

            res.InitializeItem(ProjectileMode.Phase4Target);

            res.OnPhase4TargetDespawned += () =>
            {
                _phase4TargetDespawned = true;
            };
        }

        private void ResetGame(bool hardRest = true)
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            if (_cachedProjectile_Interaction != null)
            {
                _cachedProjectile_Interaction.ReturnToPool();
                _cachedProjectile_Interaction = null;
            }

            _triggerPressed = false;
            _inputsAssigned = false;
            _centeringReticleDespawned = false;
            _phase2ProjectileDespawned = false;
            _phase4TargetRequested = false;
            _phase3NextProjectileRequested = false;
            _phase4TargetDespawned = false;
            _phase4HeadRecentered = false;

            _spawnCenter.localPosition = _defaultSpawnPosition;
            _spawnCenter.rotation = _defaultSpawnRotation;

            _interactionPanel_AppP3.DeInitializePanel();

            if (hardRest)
            {
                _phase = 0;

                _gameSetWarmup.SetActive(false);
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
            _warmupSelectedCount = 0;
        }
        #endregion
    }
}