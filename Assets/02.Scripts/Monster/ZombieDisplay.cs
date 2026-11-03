using UnityEngine;

// 로그인 화면 배경에 세워 두는 연출용 좀비. AI 없이 Idle 모션을 반복하다가 가끔 공격 모션을 섞는다.
// ZombieDisplayAC(Idle ↔ Attack)와 함께 사용한다.
[RequireComponent(typeof(Animator))]
public class ZombieDisplay : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");

    [SerializeField] private Vector2 _attackIntervalRange = new Vector2(4f, 10f);
    [SerializeField] private Vector2 _speedRange = new Vector2(0.85f, 1.1f);

    private Animator _animator;
    private float _attackTimer;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 여러 마리가 같은 박자로 움직이지 않도록 시작 지점과 속도를 다르게 한다.
        _animator.Play(IdleStateHash, 0, Random.value);
        _animator.speed = Random.Range(_speedRange.x, _speedRange.y);
        ScheduleNextAttack();
    }

    private void Update()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f) return;

        _animator.SetTrigger(AttackHash);
        ScheduleNextAttack();
    }

    // Z_Attack 클립의 애니메이션 이벤트 수신용. 연출용 좀비라 데미지 처리는 하지 않는다.
    public void HitEvent()
    {
    }

    private void ScheduleNextAttack()
    {
        _attackTimer = Random.Range(_attackIntervalRange.x, _attackIntervalRange.y);
    }
}
