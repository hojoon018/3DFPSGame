using UnityEngine;
using UnityEngine.AI;

// 배회: 시작 지점 반경 안의 무작위 지점으로 걸어가고, 도착하면 잠깐 쉰다
public class ZombieRoamingState : IZombieState
{
    private readonly Zombie _zombie;
    private bool _hasDestination = false;

    public ZombieRoamingState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _hasDestination = TryGetRandomPoint(out Vector3 destination);
        if (_hasDestination)
        {
            _zombie.MoveTo(destination, _zombie.PatrolSpeed);
        }
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 아직 가는 중이면 계속 간다
        if (_hasDestination && !_zombie.HasArrived()) return;

        // 도착했거나 갈 곳을 못 찾았으면 잠깐 쉬었다가 다시 고른다
        _zombie.PlayAnimation("MoveToIdle");
        _zombie.ChangeState(ZombieState.Idle);
    }

    public void Exit()
    {
    }

    // 시작 지점 반경 안에서 NavMesh 위의 무작위 지점을 고른다
    private bool TryGetRandomPoint(out Vector3 result)
    {
        Vector3 randomPoint = _zombie.StartPosition + Random.insideUnitSphere * _zombie.RoamRadius;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _zombie.RoamRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = _zombie.StartPosition;
        return false;
    }
}
