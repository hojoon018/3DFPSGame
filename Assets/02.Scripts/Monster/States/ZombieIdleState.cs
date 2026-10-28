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

        // 일정 시간 쉬고 나면 순찰을 시작한다
        _timer += Time.deltaTime;
        if (_timer < _zombie.IdleWaitTime) return;
        if (!_zombie.HasPatrolRoute) return; // Step 5에서 로밍으로 바꾼다

        _zombie.PlayAnimation("IdleToMove");
        _zombie.ChangeState(ZombieState.Patrol);
    }

    public void Exit()
    {
    }
}
