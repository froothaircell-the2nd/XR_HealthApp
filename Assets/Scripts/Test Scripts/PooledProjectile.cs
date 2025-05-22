using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledProjectile : CT_PooledItem
{
    [SerializeField]
    private float _targetVelocity = 10f;
    [SerializeField, Range(0f, 1f)]
    private float _velocityBlendStrength = 0.5f;
    [SerializeField]
    private Rigidbody _rb;
    [SerializeField]
    private LayerMask _validCollisionLayers;

    private Vector3 _currentVelocity = Vector3.zero;

    protected override void OnSpawn()
    {
        _currentVelocity = Vector3.zero;
    }

    protected override void OnDespawn()
    {
        _currentVelocity = Vector3.zero;
    }


    private void Update()
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

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && (_validCollisionLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            ReturnToPool();
        }
    }
}
