using UnityEngine;

public enum MeleeHitMode
{
    Collider,
    Fan
}

public class PlayerMeleeAttack : MonoBehaviour
{
    private const int SlashVariationCount = 2;

    [SerializeField] private Collider _katanaCollider;
    [SerializeField] private MeleeHitMode _meleeHitMode = MeleeHitMode.Collider;

    private Player _player;
    private Animator _animator;
    private Katana _katana;

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;

    private void Awake()
    {
        // 이 컴포넌트는 Animator가 붙은 모델 오브젝트에 있고, Player는 부모에 있다.
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
        _katana = _katanaCollider.GetComponent<Katana>();
    }

    public void FanAttack()
    {
        if (_meleeHitMode != MeleeHitMode.Fan) return;

        Vector3 origin = _player.transform.position;

        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders = Physics.OverlapSphere(_player.transform.position, _range, _targetMask);
        foreach (Collider col in colliders)
        {
            // 적의 타격점을 찾는다
            Vector3 hitPoint = col.ClosestPoint(origin);

            // 내 앞인지 뒤에 있는지 방향을 판단한다
            Vector3 direction = (hitPoint - origin);
            direction.y = 0f;
            float angle = Vector3.Angle(_player.transform.forward, direction);
            if (angle > _angle / 2f) continue;

            // 적과 나 사이에 벽이 있으면 때리지 않음
            // if (Physics.Linecast(origin, hitPoint, LayerMask.NameToLayer("Wall"))) continue;

            if (col.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = 25f;
                damageInfo.HitPoint = hitPoint;
                damageInfo.HitNormal = (origin - hitPoint).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 플레이하지 않을 때도 불리므로, Awake에서 찾는 _player 대신 여기서 찾는다.
        Player player = GetComponentInParent<Player>();
        if (player == null) return;

        Vector3 origin = player.transform.position;
        Vector3 forward = player.transform.forward;
        Gizmos.color = Color.yellow;

        // 앞쪽을 y축으로 -Angle/2, +Angle/2만큼 돌린 두 방향이 부채꼴의 양 끝이다.
        Vector3 left = Quaternion.Euler(0f, -_angle / 2f, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, _angle / 2f, 0f) * forward;
        Gizmos.DrawLine(origin, origin + left * _range);
        Gizmos.DrawLine(origin, origin + right * _range);

        // 호: 10도마다 짧은 선을 이어 그린다.
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        Vector3 previous = origin + left * _range;
        for (int i = 1; i <= count; i++)
        {
            float angle = -_angle / 2f + _angle * i / count;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    // 휘두르기 클립의 타격 시작 프레임에서 애니메이션 이벤트로 호출된다.
    public void OnSlashHitStart()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _katana.BeginSwing();
    }

    // 휘두르기 클립의 타격 종료 프레임에서 애니메이션 이벤트로 호출된다.
    public void OnSlashHitEnd()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _katana.EndSwing();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            // 0: Outward, 1: Overhead (Melee Layer의 SlashIndex 전환 조건과 일치)
            _animator.SetInteger("SlashIndex", Random.Range(0, SlashVariationCount));
            _animator.SetTrigger("Slash");
        }
    }
}