using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine.AI;

// FSM(유한 상태 머신) : 유한 개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태" : 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이" : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다
    Patrol, // 순찰 : 순찰 지점을 순서대로 이동한다
    Roaming, // 배회 : 시작 지점 반경 안의 무작위 지점으로 이동한다 (Step 5에서 구현)
    Trace, // 추격 : 플레이어를 쫓아간다
    Comeback, // 복귀 : 처음 자리로 돌아간다
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다
    Death, // 죽음 : 쓰러지고 잠시 뒤 사라진다
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable, IPoolable
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

    [Header("로밍 설정")]
    [SerializeField] private float _roamRadius = 6f; // 시작 지점에서 배회할 반경

    private Dictionary<ZombieState, IZombieState> _states;
    private IZombieState _currentState;

    private int _currentWaypointIndex = 0;
    private float _idleTimer = 0f;
    private float _attackTimer = 0f;
    private Vector3 _startPosition;
    private GameObject _player;
    private Animator _animator;

    private NavMeshAgent _agent;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // 상태들이 읽는 값
    public float DetectDistance => _detectDistance;
    public float ComebackDistance => _comebackDistance;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float MoveSpeed => _moveSpeed;
    public float PatrolSpeed => _patrolSpeed;
    public float IdleWaitTime => _idleWaitTime;
    public float RoamRadius => _roamRadius;
    public Vector3 StartPosition => _startPosition;
    public Vector3 PlayerPosition => _player.transform.position;

    public event Action OnHealthChanged;
    public event Action<Zombie> OnDied;


    private ZombieState _state = ZombieState.Idle;

    private void Awake()
    {
        // 풀에서 처음 꺼낼 때 OnSpawn이 Start보다 먼저 불리므로 Awake에서 캐싱한다
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        ResetState();
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _agent.speed = _moveSpeed;
    }

    public void OnSpawn()
    {
        ResetState();
        ResetAnimator();
    }

    public void OnDespawn()
    {
    }

    private void ResetState()
    {
        _health = _maxHealth;
        _state = ZombieState.Idle;
        _startPosition = transform.position;
        _currentWaypointIndex = 0;
        _idleTimer = 0f;
        _attackTimer = 0f;
        OnHealthChanged?.Invoke();
    }

    // 재사용되는 좀비가 이전 애니메이션 상태(Death 등)와 남은 트리거를 물려받지 않도록 초기화한다
    private void ResetAnimator()
    {
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                _animator.ResetTrigger(parameter.name);
            }
        }

        _animator.Rebind(); // 기본 상태(Idle)로 되돌린다
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

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

    // 상태들이 시키는 행동
    public float DistanceTo(Vector3 position)
    {
        return Vector3.Distance(transform.position, position);
    }

    public float DistanceToPlayer()
    {
        return DistanceTo(PlayerPosition);
    }

    // 속도를 항상 함께 지정해서, 이전 상태의 속도가 남지 않게 한다
    public void MoveTo(Vector3 destination, float speed)
    {
        _agent.speed = speed;
        _agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        if (!_agent.isOnNavMesh) return; // 내비메시 위에 올라가기 전에는 멈출 경로도 없다

        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
    }

    // 목적지에 도착했는가
    public bool HasArrived()
    {
        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
    }

    public void PlayAnimation(string triggerName)
    {
        _animator.SetTrigger(triggerName);
    }

    public void Despawn()
    {
        LeanPool.Despawn(gameObject);
    }

    // 상태 전이 (모든 전이는 여기를 거친다)
    public void ChangeState(ZombieState next)
    {
        Debug.Log($"{_state} -> {next}");

        _currentState?.Exit();
        _state = next;
        _currentState = _states[next];
        _currentState.Enter();
    }

    // 대미지를 받는다
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;

        OnHealthChanged?.Invoke();

        StopAllCoroutines();

        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 스프레이 실행하고 싶다
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, gameObject.transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;

            // 잠깐 멈칫 하고 싶다
            Debug.Log($"{_state} -> Hit");
            _animator.SetTrigger("Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _animator.SetTrigger("Death");
            _state = ZombieState.Death;
            OnDied?.Invoke(this);
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(0.5f);

        // 맞았으니 쫓아온다
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        yield return new WaitForSeconds(2f);
        LeanPool.Despawn(gameObject);
    }

    private void Idle()
    {
        StopAgent();

        // 대기 : 제자리에서 플레이어를 기다린다

        // 플레이어 가까워지면 Trace 상태로 전이한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            _idleTimer = 0f;
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("IdleToMove");
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
                _animator.SetTrigger("IdleToMove");
                _state = ZombieState.Patrol;
            }
        }
    }

    private void StopAgent()
    {
        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해 가속도 0
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
            _animator.SetTrigger("MoveToIdle");
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
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
            return;
        }

        // 목표 지점을 향해 회전하고 이동한다
        _agent.speed = _patrolSpeed;
        _agent.SetDestination(target.position);
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
            _animator.SetTrigger("MoveToAttackDelay");
            _state = ZombieState.Attack;
            return;
        }

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 X
        // - 한 번 세팅하면 내부에서 계속해서 이동
        _agent.SetDestination(_player.transform.position);
    }

    private void ComeBack()
    {
        // 처음 자리에 도착하면 Idle
        if (Vector3.Distance(transform.position, _startPosition) < _agent.stoppingDistance)
        {
            Debug.Log($"{_state} -> Idle");
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
            return;
        }

        // 처음 자리로 돌아간다
        _agent.SetDestination(_startPosition);
    }

    // 공격 : 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        StopAgent();

        // 플레이어 가까워지면 Trace 상태로 전이한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("AttackMove");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            _animator.SetTrigger("StartAttack");

            _attackTimer = 0f;
        }
    }

    public void AttackAction()
    {
        DamageInfo info = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized,
        };
        _player.GetComponent<IDamageable>().TakeDamage(info);
    }
}