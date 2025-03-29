using CoreResources.Singleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : MonoBehaviour
{
    [SerializeField]
    private GameObject _pooledPrefab;

    private bool _poolInitialized = false;

    private const int INITIALIZED_COUNT = 45;

    private List<PooledItem> _pool = new List<PooledItem>(INITIALIZED_COUNT);
    private List<PooledItem> _spawnedItems = new List<PooledItem>();

    private void Awake()
    {
        InitializePool();
    }

    private void OnDisable()
    {
        CleanPool();
    }

    public void InitializePool()
    {
        if (_poolInitialized) return;

        for (int i = 0; i < INITIALIZED_COUNT; i++)
        {
            var obj = Instantiate(_pooledPrefab, transform.position, transform.rotation, transform);
            _pool.Add(obj.GetComponent<PooledItem>());
            _pool[i].InitializePooledItem(this);
        }

        _poolInitialized = true;
    }

    public void CleanPool()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            _spawnedItems[i].ReturnToPool();
        }
    }

    public PooledItem SpawnItem(Vector3 position, Quaternion rotation)
    {
        if (_pool == null || _pool.Count == 0)
            return null;

        var item = _pool[0];
        _pool.RemoveAt(0);
        _spawnedItems.Add(item);

        item.transform.parent = null;
        item.SpawnItem(position, rotation);

        return item;
    }

    public void ReturnItemToPool(PooledItem item)
    {
        _spawnedItems.Remove(item);
        _pool.Add(item);
        item.transform.parent = transform;
        item.transform.localPosition = Vector3.zero;
    }
}
