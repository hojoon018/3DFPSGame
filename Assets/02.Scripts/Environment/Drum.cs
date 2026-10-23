using System.Collections;
using UnityEngine;


// 체력이 0이 되면 위로 튀어오르면 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _damage = 100f;
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destroyDelay = 3f;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _explosionDelay = 0.2f;

    private Rigidbody _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0)
        {
            return;
        }

        _health -= damageInfo.Value;

        if (_health <= 0)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 0. 폭발하기 전에 대기
        yield return new WaitForSeconds(_explosionDelay);

        // 1. 폭발 이펙트를 생성한다
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 2. 위로 튀어오르며 빙글빙글 돈다
        _rigidBody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidBody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 주위에 폭발을 전달한다 (데미지를 준다)
        ApplyAreaDamage();

        // 3. 딜레이 후 사라진다
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        // 1. 드럼통들에게 데미지를 준다
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