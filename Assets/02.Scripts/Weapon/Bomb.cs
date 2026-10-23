using System;
using Lean.Pool;
using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 폭탄
public class Bomb : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem _explositionEffectPrefab;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100;

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

        ApplyAreaDamage();

        // 폭탄을 Destroy 대신 LeanPool에 반환
        LeanPool.Despawn(gameObject);
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSpere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        // 1. 드럼통과 좀비들에게 대미지를 준다.
        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new()
            {
                Value = _damage,
                HitPoint = col.ClosestPoint(transform.position),
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized
            };

            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}