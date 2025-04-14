using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : MonoBehaviour
{
    [SerializeField]
    private GameObject _pooledPrefab;
    [SerializeField]
    private int _initializedCount = 45;

    private bool _poolInitialized = false;
    private bool _poolLocked = true; // Ensures the spawn function isn't called right after a clean call


    private List<PooledItem> _pool = new List<PooledItem>();
    private List<PooledItem> _spawnedItems = new List<PooledItem>();



    public void Init()
    {
        InitializePool();
    }

    public void DeInit()
    {
        CleanPool();
    }

    public void InitializePool()
    {
        if (_poolInitialized) return;

        for (int i = 0; i < _initializedCount; i++)
        {
            var obj = Instantiate(_pooledPrefab, transform.position, transform.rotation, transform);
            _pool.Add(obj.GetComponent<PooledItem>());
            _pool[i].InitializePooledItem(this);
        }

        _poolInitialized = true;
    }

    public void UnlockPool()
    {
        _poolLocked = false;
    }

    public void CleanPool()
    {
        _poolLocked = true;

        while (_spawnedItems.Count > 0)
        {
            _spawnedItems[0].ReturnToPool();
        }
    }

    public PooledItem SpawnItem(Vector3 position, Quaternion rotation)
    {
        if (!_poolLocked && (_pool == null || _pool.Count == 0))
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
