using UnityEngine;

// 대기: 제자리에서 플레이어를 기다린다
public class ZombieIdleState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieIdleState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.StopMoving(); // 기존에는 매 프레임 호출했지만, 들어올 때 한 번이면 된다
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.PlayAnimation("IdleToMove");
            _zombie.ChangeState(ZombieState.Trace);
        }

        // Step 4에서 "기다렸다가 순찰/배회로 넘어가기"를 추가한다
    }

    public void Exit()
    {
    }
}
