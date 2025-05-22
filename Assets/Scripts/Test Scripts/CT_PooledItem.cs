using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface CT_IPooledItem
{
    public bool IsPooled { get; }

    public void InitializePooledItem(CT_GameObjectPool parentPool);
    public void SpawnItem(Vector3 position, Quaternion rotation);
    public void ReturnToPool();
}

public class CT_PooledItem : MonoBehaviour, CT_IPooledItem
{
    private CT_GameObjectPool _parentPool;

    protected bool _isPooled = false;
    protected bool _isInitialized = false;

    public bool IsPooled => _isInitialized && _isPooled;

    public void InitializePooledItem(CT_GameObjectPool parentPool)
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

            OnSpawn();
        }
    }

    public void ReturnToPool()
    {
        if (_isInitialized && !_isPooled)
        {
            OnDespawn();

            gameObject.SetActive(false);
            _parentPool.ReturnItemToPool(this);
            _isPooled = true;
        }
    }

    protected virtual void OnSpawn()
    {

    }

    protected virtual void OnDespawn()
    {

    }
}
