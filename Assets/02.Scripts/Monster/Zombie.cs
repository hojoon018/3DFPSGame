using System;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using Debug = UnityEngine.Debug;

// FSM(유한 상태 머신) : 유한 개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태" : 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이" : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다
    Trace, // 추격 : 플레이어를 쫓아간다
    Comeback, // 복귀 : 처음 자리로 돌아간다
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다
    Death, // 죽음 : 쓰러지고 잠시 뒤 사라진다
}

public class Zombie : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [SerializeField] private float _comebackDistance = 15; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _moveSpeed = 5; // 이동속도

    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;


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

            case ZombieState.Trace:
                Trace();
                break;

            case ZombieState.Comeback:
                ComeBack();
                break;

            case ZombieState.Attack:
                Attack();
                break;

            case ZombieState.Hit:
                Hit();
                break;

            case ZombieState.Death:
                Death();
                break;
        }
    }

    private void Idle()
    {
        // Todo. Idle 애니메이션 실행

        // 대기 : 제자리에서 플레이어를 기다린다

        // 플레이어 가까워지면 Trace 상태로 전이한다
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _state = ZombieState.Trace;
            return;
        }
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

    private void Attack()
    {
    }

    private void Hit()
    {
    }

    private void Death()
    {
    }
}