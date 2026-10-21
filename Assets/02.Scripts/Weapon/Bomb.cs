using System;
using Lean.Pool;
using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 폭탄
public class Bomb : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem _explositionEffectPrefab;
    [SerializeField] private Rigidbody _rigidbody;

    public Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : (_rigidbody = GetComponent<Rigidbody>());

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    }

    // 풀에서 꺼내질 때마다 호출되어 이전 물리 속도(선속도, 각속도)를 초기화
    public void OnSpawn()
    {
        if (Rigidbody != null)
        {
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void OnDespawn()
    {
    }

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        ParticleSystem effect = Instantiate(_explositionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        Debug.Log(collision.gameObject.name);

        // 폭탄을 Destroy 대신 LeanPool에 반환
        LeanPool.Despawn(gameObject);
    }
}