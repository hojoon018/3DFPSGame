// 복귀: 처음 자리로 돌아간다
public class ZombieComebackState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieComebackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        // 목적지가 고정이므로 들어올 때 한 번만 설정하면 된다
        _zombie.MoveTo(_zombie.StartPosition, _zombie.MoveSpeed);
    }

    public void Execute()
    {
        if (!_zombie.HasArrived()) return;

        _zombie.PlayAnimation("MoveToIdle");
        _zombie.ChangeState(ZombieState.Idle);
    }

    public void Exit()
    {
    }
}
