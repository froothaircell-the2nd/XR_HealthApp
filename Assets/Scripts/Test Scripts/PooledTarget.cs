using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledTarget : CT_PooledItem
{
    [SerializeField]
    private float _minTargetLifetime = 0.01f, _maxTargetLifetime = 5f;

    private float _startTime = 0f;
    private float _countdownTimer = 0f;
    private float _countdownTimerLimit = 0f;
    private Coroutine _despawnCoroutine;

    protected override void OnSpawn()
    {
        _startTime = Time.time;
        _countdownTimer = _startTime;

        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }

        _countdownTimerLimit = Random.Range(_minTargetLifetime, _maxTargetLifetime);
    }

    protected override void OnDespawn()
    {
        _startTime = 0;
        _countdownTimer = 0f;
        _countdownTimerLimit = 0f;

        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }
    }

    private IEnumerator DespawnCoroutine()
    {
        _countdownTimer += Time.deltaTime;
        if (_countdownTimer > _countdownTimerLimit)
        {
            ReturnToPool();
        }

        yield return null;
    }
}
