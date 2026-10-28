using UnityEngine;

// 순찰: 순찰 경로의 다음 지점으로 이동하고, 도착하면 잠깐 쉰다
public class ZombiePatrolState : IZombieState
{
    private readonly Zombie _zombie;
    private int _waypointIndex = 0; // 상태 객체가 순찰 순서를 기억한다

    public ZombiePatrolState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        Vector3 target = _zombie.PatrolRoute.GetPoint(_waypointIndex);
        _zombie.MoveTo(target, _zombie.PatrolSpeed);
    }

    public void Execute()
    {
        // 플레이어를 발견하면 즉시 추격한다
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        if (!_zombie.HasArrived()) return;

        // 다음 지점을 정해 두고 잠깐 쉬러 간다
        _waypointIndex = (_waypointIndex + 1) % _zombie.PatrolRoute.Count;
        _zombie.PlayAnimation("MoveToIdle");
        _zombie.ChangeState(ZombieState.Idle);
    }

    public void Exit()
    {
    }
}
