using System.Collections.Generic;
using UnityEngine;

public class Katana : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;

    private Player _player;
    private Collider _collider;
    // 한 번 휘두르는 동안 이미 맞은 대상 (중복 타격 방지)
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _collider = GetComponent<Collider>();
        _collider.enabled = false;
    }

    // 휘두르기 애니메이션에서 타격 구간이 시작될 때 호출한다.
    public void BeginSwing()
    {
        _hitTargets.Clear();
        _collider.enabled = true;
    }

    // 타격 구간이 끝날 때 호출한다.
    public void EndSwing()
    {
        _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 자기 자신(몸 콜라이더 포함)은 타격하지 않는다.
        if (other.transform.IsChildOf(_player.transform)) return;
        if (!other.TryGetComponent<IDamageable>(out IDamageable damageable)) return;
        if (!_hitTargets.Add(damageable)) return;

        DamageInfo damageInfo = new DamageInfo();
        damageInfo.Value = _damage;
        Vector3 hitpoint = other.ClosestPoint(_player.transform.position);
        damageInfo.HitPoint = hitpoint;
        damageInfo.HitNormal = (_player.transform.position - hitpoint).normalized;

        damageable.TakeDamage(damageInfo);
    }
}
