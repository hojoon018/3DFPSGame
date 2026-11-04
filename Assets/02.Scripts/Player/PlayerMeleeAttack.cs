using Lean.Pool;
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
    private KatanaSlashTrail _slashTrail; // 없으면 궤적 없이 타격만 한다

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;

    [Header("검기 발사")]
    [SerializeField] private SlashWave _slashWavePrefab;
    [SerializeField] private float _waveCooldown = 2f;
    [SerializeField] private int _waveSlashIndex = 1; // 검기를 쏠 때 재생할 베기 모션 (0: Outward, 1: Overhead)
    [SerializeField] private float _waveSpawnHeight = 1.2f; // 플레이어 발밑 기준 생성 높이
    [SerializeField] private float _waveSpawnForward = 1f; // 플레이어 앞쪽으로 떨어진 생성 거리
    [SerializeField] private float _waveRollAngle = 35f; // 진행 방향을 축으로 검기 평면을 기울이는 각도 (0: 수평, 90: 세로, 음수면 반대 대각선)
    [SerializeField] private float _waveTiltAngle = 20f; // 쏘는 사람 쪽으로 검기 평면을 숙이는 각도 (0이면 날만 보이는 가는 선)

    private float _waveCooldownTimer;
    private bool _isWaveQueued; // 좌클릭으로 베기를 시작했고, 타격 시작 이벤트에서 검기를 쏠 차례인지

    private void Awake()
    {
        // 이 컴포넌트는 Animator가 붙은 모델 오브젝트에 있고, Player는 부모에 있다.
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
        _katana = _katanaCollider.GetComponent<Katana>();
        _slashTrail = _katanaCollider.GetComponent<KatanaSlashTrail>();
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
        if (_isWaveQueued)
        {
            _isWaveQueued = false;
            FireSlashWave();
        }

        // 궤적은 타격 방식(Collider/Fan)과 상관없이 그린다.
        if (_slashTrail != null)
        {
            _slashTrail.StartTrail();
        }

        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _katana.BeginSwing();
    }

    // 휘두르기 클립의 타격 종료 프레임에서 애니메이션 이벤트로 호출된다.
    public void OnSlashHitEnd()
    {
        if (_slashTrail != null)
        {
            _slashTrail.StopTrail();
        }

        if (_meleeHitMode != MeleeHitMode.Collider) return;

        _katana.EndSwing();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _waveCooldownTimer -= Time.deltaTime;

        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            // 일반 베기로 덮어쓰면 대기 중이던 검기는 취소한다.
            _isWaveQueued = false;
            // 0: Outward, 1: Overhead (Melee Layer의 SlashIndex 전환 조건과 일치)
            _animator.SetInteger("SlashIndex", Random.Range(0, SlashVariationCount));
            _animator.SetTrigger("Slash");
        }
        else if (!Cursor.visible && Input.GetButtonDown("Fire1") && _waveCooldownTimer <= 0f && _slashWavePrefab != null)
        {
            // 베기 모션을 재생하고, 실제 검기는 타격 시작 이벤트(OnSlashHitStart)에서 쏜다.
            _waveCooldownTimer = _waveCooldown;
            _isWaveQueued = true;
            _animator.SetInteger("SlashIndex", _waveSlashIndex);
            _animator.SetTrigger("Slash");
        }
    }

    // 플레이어 정면으로 초승달 검기를 날린다.
    private void FireSlashWave()
    {
        Transform playerTransform = _player.transform;
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 position = playerTransform.position + Vector3.up * _waveSpawnHeight + forward * _waveSpawnForward;
        // 진행 방향을 축으로 기울인 뒤, 앞날(앞쪽 끝)을 살짝 들어 올려 쏘는 사람에게 면이 조금 보이게 한다.
        // 날아가는 방향은 forward로 따로 넘기므로, 숙인 각도 때문에 위아래로 날아가지 않는다.
        Quaternion rotation = Quaternion.LookRotation(forward)
            * Quaternion.Euler(0f, 0f, _waveRollAngle)
            * Quaternion.Euler(-_waveTiltAngle, 0f, 0f);

        SlashWave wave = LeanPool.Spawn(_slashWavePrefab, position, rotation);
        wave.Launch(playerTransform, forward);
    }
}