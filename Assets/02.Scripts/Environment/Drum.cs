using System.Collections;
using UnityEngine;


// 체력이 0이 되면 위로 튀어오르면 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destroyDelay = 3f;

    private Rigidbody _rigidBody;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void Damage(float damage)
    {
        if (_health <= 0)
        {
            return;
        }

        _health -= damage;

        if (_health <= 0)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 1. 폭발 이펙트를 생성한다
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 2. 위로 튀어오르며 빙글빙글 돈다
        _rigidBody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidBody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 사라진다
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}