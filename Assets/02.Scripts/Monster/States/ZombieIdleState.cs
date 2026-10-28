using UnityEngine;

// 대기: 제자리에서 플레이어를 기다린다
public class ZombieIdleState : IZombieState
{
    private readonly Zombie _zombie;
    private float _timer = 0f; // Zombie._idleTimer가 이사온 곳

    public ZombieIdleState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving(); // 기존에는 매 프레임 호출했지만, 들어올 때 한 번이면 된다
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.PlayAnimation("IdleToMove");
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 일정 시간 쉬고 나면 순찰/배회를 시작한다
        _timer += Time.deltaTime;
        if (_timer < _zombie.IdleWaitTime) return;

        // 순찰 경로가 있으면 순찰, 없으면 배회
        ZombieState next = _zombie.HasPatrolRoute ? ZombieState.Patrol : ZombieState.Roaming;
        _zombie.PlayAnimation("IdleToMove");
        _zombie.ChangeState(next);
    }

    public void Exit()
    {
    }
}
