using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledItem : MonoBehaviour
{
    [SerializeField]
    private float _targetVelocity = 10f;
    [SerializeField, Range(0f, 1f)]
    private float _velocityBlendStrength = 0.5f;
    [SerializeField]
    private Rigidbody _rb;
    [SerializeField]
    private LayerMask _validCollisionLayers;

    private GameObjectPool _parentPool;

    private bool _isPooled = false;
    private bool _isInitialized = false;

    public bool IsPooled => _isInitialized && _isPooled;

    private void Update()
    {
        if (_isInitialized && !_isPooled)
        {
            var targetVelocity = transform.forward * _targetVelocity;
            var currentVelocity = _rb.velocity;

            _rb.AddForce((targetVelocity - currentVelocity) * _velocityBlendStrength);
        }
    }

    public void InitializePooledItem(GameObjectPool parentPool)
    {
        _isPooled = false;
        _isInitialized = true;
        _parentPool = parentPool;
        gameObject.SetActive(false);
        _isPooled = true;
    }

    public void SpawnItem(Vector3 position, Quaternion rotation)
    {
        if (_isInitialized && _isPooled)
        {
            transform.position = position;
            transform.rotation = rotation;
            gameObject.SetActive(true);
            _isPooled = false;
        }
    }

    public void ReturnToPool()
    {
        if (_isInitialized && !_isPooled)
        {
            gameObject.SetActive(false);
            _parentPool.ReturnItemToPool(this);
            _isPooled = true;
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
