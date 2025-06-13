using GameResources.Pooling;
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
        [SerializeField]
        private Rigidbody _rb;
        [SerializeField]
        private LayerMask _validCollisionLayers;

        private ProjectileMode _currentProjectileMode = ProjectileMode.Phase2Projectile;
        private Vector3 _currentVelocity = Vector3.zero;
        private bool _isInteractable = false;

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

        #region Overrides
        protected override void OnSpawn()
        {
            _currentVelocity = Vector3.zero;
        }

        protected override void OnDespawn()
        {
            _currentVelocity = Vector3.zero;
            _isInteractable = false;
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
        #endregion

        #region Public Methods
        public void InitializeItem(ProjectileMode mode)
        {
            CurrentProjectileMode = mode;
            IsInteractable = true;
        }
        #endregion
    }
}