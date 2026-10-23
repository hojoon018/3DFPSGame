using UnityEngine;
using System.Collections;

// FSM(유한 상태 머신) : 유한 개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태" : 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이" : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다
    Patrol, // 순찰 : 순찰 지점을 순서대로 이동한다
    Trace, // 추격 : 플레이어를 쫓아간다
    Comeback, // 복귀 : 처음 자리로 돌아간다
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다
    Death, // 죽음 : 쓰러지고 잠시 뒤 사라진다
}

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [SerializeField] private float _comebackDistance = 15; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackDamage = 10f; // 공격 데미지
    [SerializeField] private float _attackInterval = 1f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5; // 이동속도
    [SerializeField] private float _maxHealth = 30;
    [SerializeField] private float _health;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    [Header("순찰 설정")]
    [SerializeField] private Transform[] _waypoints; // 순찰할 지점들 (2개 이상 등록 가능)
    [SerializeField] private float _idleWaitTime = 2f; // 지점 도착 후 대기 시간
    [SerializeField] private float _patrolSpeed = 2.5f; // 순찰 이동 속도

    private int _currentWaypointIndex = 0;
    private float _idleTimer = 0f;
    private float _attackTimer = 0f;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;

    public float MaxHealth => _maxHealth;
    public float Health => _health;


    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;

        // 게임을 시작하면 플레이어를 찾는다
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;

            case ZombieState.Patrol:
                Patrol();
                break;

            case ZombieState.Trace:
                Trace();
                break;

            case ZombieState.Comeback:
                ComeBack();
                break;

            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    // 대미지를 받는다
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;

        StopAllCoroutines();

        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 스프레이 실행하고 싶다
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, gameObject.transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            // 잠깐 멈칫 하고 싶다
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f);

        // 맞았으니 쫓아온다
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        _controller.enabled = false;
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 대기 : 제자리에서 플레이어를 기다린다

        // 플레이어 가까워지면 Trace 상태로 전이한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            _idleTimer = 0f;
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        // 순찰 지점이 지정되어 있다면 일정 시간 대기 후 순찰을 시작한다
        if (_waypoints != null && _waypoints.Length > 0)
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleWaitTime)
            {
                _idleTimer = 0f;
                Debug.Log($"{_state} -> Patrol");
                _state = ZombieState.Patrol;
            }
        }
    }

    // 순찰 : 지정된 순찰 지점을 순서대로 이동한다
    private void Patrol()
    {
        // Todo. Walk 애니메이션 실행

        // 플레이어가 감지 거리 안에 들어오면 즉시 추격한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }

        // 등록된 순찰 지점이 없다면 Idle 상태로 유지한다
        if (_waypoints == null || _waypoints.Length == 0)
        {
            _state = ZombieState.Idle;
            return;
        }

        Transform target = _waypoints[_currentWaypointIndex];
        if (target == null)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        // 목표 지점에 도착했으면 다음 지점으로 변경하고 Idle 상태로 전이한다
        if (direction.magnitude < 0.5f)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            Debug.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        // 목표 지점을 향해 회전하고 이동한다
        direction.Normalize();
        transform.forward = direction;
        _controller.SimpleMove(direction * _patrolSpeed);
    }

    // 추격 : 플레이어를 쫓아간다
    private void Trace()
    {
        // Todo. Run 애니메이션 실행

        // 처음 자리에서 너무 멀어지면 돌아간다
        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 범위 안이면 공격한다
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void ComeBack()
    {
        // 처음 자리에 도착하면 Idle
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        // 처음 자리로 돌아간다
        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    // 공격 : 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        // 플레이어 가까워지면 Trace 상태로 전이한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행

            DamageInfo info = new DamageInfo()
            {
                Value = _attackDamage,
                HitPoint = _player.transform.position,
                HitNormal = (transform.position - _player.transform.position).normalized,
            };
            _player.GetComponent<IDamageable>().TakeDamage(info);

            _attackTimer = 0f;
        }
    }
}