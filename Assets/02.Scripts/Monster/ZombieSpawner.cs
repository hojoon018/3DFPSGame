using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;
using Lean.Pool;

[Serializable]
public class ZombieWave
{
    [SerializeField] private int _zombieCount = 5; // 이번 웨이브에 나오는 총 좀비 수
    [SerializeField] private float _spawnInterval = 1f; // 스폰 간격

    public int ZombieCount => _zombieCount;
    public float SpawnInterval => _spawnInterval;
}

public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _maxAliveCount = 3; // 동시에 살아 있을 수 있는 최대 좀비 수

    [Header("웨이브 설정")]
    [SerializeField] private ZombieWave[] _waves;
    [SerializeField] private float _timeBetweenWaves = 3f; // 웨이브 사이 휴식 시간

    private int _aliveCount = 0;

    private void Start()
    {
        StartCoroutine(WaveLoop_Coroutine());
    }

    private void SpawnZombie()
    {
        int index = Random.Range(0, _spawnPoints.Length);
        Transform spawnPoint = _spawnPoints[index];

        Zombie zombie = LeanPool.Spawn(_zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombie.OnDied += HandleZombieDied;
        _aliveCount++;
    }

    private IEnumerator WaveLoop_Coroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.State == GameState.Playing);

        for (int i = 0; i < _waves.Length; i++)
        {
            Debug.Log($"웨이브 {i + 1} 시작");
            yield return StartCoroutine(RunWave_Coroutine(_waves[i]));

            if (GameManager.Instance.State != GameState.Playing) yield break;

            Debug.Log($"웨이브 {i + 1} 클리어");
            yield return new WaitForSeconds(_timeBetweenWaves);
        }

        Debug.Log("모든 웨이브 클리어");
    }

    private IEnumerator RunWave_Coroutine(ZombieWave wave)
    {
        // 1. 이번 웨이브 좀비를 모두 스폰한다
        for (int spawned = 0; spawned < wave.ZombieCount; spawned++)
        {
            yield return new WaitUntil(() => _aliveCount < _maxAliveCount);
            if (GameManager.Instance.State != GameState.Playing) yield break;

            SpawnZombie();
            yield return new WaitForSeconds(wave.SpawnInterval);
        }

        yield return new WaitUntil(() => _aliveCount == 0);
    }

    private void HandleZombieDied(Zombie zombie)
    {
        zombie.OnDied -= HandleZombieDied;
        _aliveCount--;
    }
}