using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using GameResources.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace GameResources.Gameplay
{
    public enum ProjectileMode
    {
        CenteringReticle = 0,
        Phase2Projectile = 1,   // Projectiles that get shot towards user in phase 2
        Phase3Projectile = 2,   // Projectile that will move on a fixed route in phase 3
    }

    public class ProjectileController : PooledItem, ICursorInteractable
    {
        [SerializeField]
        private float _targetVelocity = 10f;
        [SerializeField, Range(0f, 1f)]
        private float _velocityBlendStrength = 0.5f;
        [SerializeField, Range(0f, 10f)]
        private float _centeringReticleResizingDuration;
        [SerializeField, Range(0f, 2f)]
        private float _centeringReticleResettingDuration;
        [SerializeField]
        private Rigidbody _rb;
        [SerializeField]
        private LayerMask _validCollisionLayers;
        [SerializeField, ColorUsage(true, true)]
        private Color _centeringReticleColor, 
            _phase2ProjectileColor, 
            _phase3ProjectileColor;

        #region Private Properties
        private Material _projectileMaterial;
        private ProjectileMode _currentProjectileMode = ProjectileMode.Phase2Projectile;
        private Vector3 _currentVelocity = Vector3.zero;
        private bool _isInteractable = false;
        private bool _centeringReticleContracting = false;
        private float _originalScale = 1.2f, _finalScale = 0.35f;

        private TweenerCore<Vector3, Vector3, VectorOptions> _centeringReticleResizingTween = null;
        #endregion

        #region Public Properties
        public Action OnCenteringReticleDespawned = null;

        public bool IsInteractable
        {
            get => _isInteractable;
            private set => _isInteractable = value;
        }

        public ProjectileMode CurrentProjectileMode
        {
            get { return _currentProjectileMode; }
            private set
            {
                // Only allow property to be set while the object is pooled
                if (_isPooled)
                    _currentProjectileMode = value;
            }
        }

        public bool CenteringReticleContracting
        {
            get { return _centeringReticleContracting; }
            set
            {
                if (!_isPooled && CurrentProjectileMode == ProjectileMode.CenteringReticle)
                {
                    _centeringReticleContracting = value;
                }
            }
        }
        #endregion

        #region Overrides
        protected override void OnSpawn()
        {
            _currentVelocity = Vector3.zero;
            _originalScale = transform.localScale.x; // Only taking one dimension since the dimensions will be equal

            if (_projectileMaterial == null)
                _projectileMaterial = gameObject.GetComponent<MeshRenderer>().material;

            switch (CurrentProjectileMode)
            {
                case ProjectileMode.CenteringReticle:
                    _projectileMaterial.SetColor("_EmissionColor", _centeringReticleColor);
                    break;
                case ProjectileMode.Phase2Projectile:
                default:
                    _projectileMaterial.SetColor("_EmissionColor", _phase2ProjectileColor);
                    break;
                case ProjectileMode.Phase3Projectile:
                    _projectileMaterial.SetColor("_EmissionColor", _phase3ProjectileColor);
                    break;
            }
        }

        protected override void OnDespawn()
        {
            _currentVelocity = Vector3.zero;

            transform.localScale = Vector3.one;
            transform.localScale = Vector3.zero;
            transform.rotation = Quaternion.identity;

            _isInteractable = false;
            _centeringReticleContracting = false;
        }

        private void Update()
        {
            switch (_currentProjectileMode)
            {
                case ProjectileMode.CenteringReticle:
                    SimulateCenteringReticle();
                    break;
                case ProjectileMode.Phase2Projectile:
                    SimulatePhase2Projectile();
                    break;
                case ProjectileMode.Phase3Projectile:
                    SimulatePhase3Projectile();
                    break;
                default:
                    // SimulatePhase2Projectile();
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && (_validCollisionLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                ReturnToPool();
            }
        }
        #endregion

        #region Private Methods
        private void SimulateCenteringReticle()
        {
            if (!IsPooled)
            {
                if (_centeringReticleContracting && _centeringReticleResizingTween == null)
                {
                    _centeringReticleResizingTween = transform.DOScale(_finalScale, _centeringReticleResizingDuration).OnComplete(() => DespawnCenteringReticle());
                }
                else if (!_centeringReticleContracting && _centeringReticleResizingTween != null)
                {
                    _centeringReticleResizingTween.Kill();
                    _centeringReticleResizingTween = null;

                    transform.DOScale(_originalScale, _centeringReticleResettingDuration);
                }
            }
        }

        private void SimulatePhase2Projectile()
        {
            if (!IsPooled)
            {
                var targetVelocity = transform.forward * _targetVelocity;
                var currentVelocity = _rb.velocity;
                // _currentVelocity += _velocityBlendStrength * Time.deltaTime * (targetVelocity - _currentVelocity);

                // transform.Translate(_currentVelocity);
                _rb.AddForce((targetVelocity - currentVelocity) * _velocityBlendStrength);
            }
        }

        private void SimulatePhase3Projectile()
        {

        }

        private void DespawnCenteringReticle()
        {
            OnCenteringReticleDespawned?.Invoke();
            OnCenteringReticleDespawned = null;

            ReturnToPool();
        }
        #endregion

        #region Public Methods
        public void InitializeItem(ProjectileMode mode)
        {
            CurrentProjectileMode = mode;
            IsInteractable = true;
        }

        public void HandleSelectionEnter()
        {
            if (CurrentProjectileMode == ProjectileMode.Phase2Projectile)
            {
                GameplayHandler.OnPhase2Hit?.Invoke();
                ReturnToPool();
            }
        }
        #endregion
    }
}