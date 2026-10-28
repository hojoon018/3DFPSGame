// 추격: 플레이어를 쫓아간다
public class ZombieTraceState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieTraceState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        // 처음 자리에서 너무 멀어지면 돌아간다
        if (_zombie.DistanceTo(_zombie.StartPosition) > _zombie.ComebackDistance)
        {
            _zombie.ChangeState(ZombieState.Comeback);
            return;
        }

        // 플레이어가 공격 범위 안이면 공격한다
        if (_zombie.DistanceToPlayer() <= _zombie.AttackRange)
        {
            _zombie.PlayAnimation("MoveToAttackDelay");
            _zombie.ChangeState(ZombieState.Attack);
            return;
        }

        // 플레이어가 움직이므로 목적지를 매 프레임 갱신한다
        _zombie.MoveTo(_zombie.PlayerPosition, _zombie.MoveSpeed);
    }

    public void Exit()
    {
    }
}
