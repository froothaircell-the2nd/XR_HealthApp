using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Pooling
{
    public interface IPooledItem
    {
        public bool IsPooled { get; }

        public void InitializePooledItem(ObjectPool parentPool);
        public void SpawnItem(Vector3 position, Quaternion rotation);
        public void ReturnToPool();
    }

    public class PooledItem : MonoBehaviour, IPooledItem
    {
        private ObjectPool _parentPool;

        protected bool _isPooled = false;
        protected bool _isInitialized = false;

        public bool IsPooled => _isInitialized && _isPooled;

        public void InitializePooledItem(ObjectPool parentPool)
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
}