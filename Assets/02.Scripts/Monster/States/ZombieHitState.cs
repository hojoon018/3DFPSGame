using UnityEngine;

// 피격: 잠깐 멈칫한 뒤 플레이어를 쫓아간다
public class ZombieHitState : IZombieState
{
    private const float StunDuration = 0.5f; // 멈칫하는 시간

    private readonly Zombie _zombie;
    private float _timer = 0f;

    public ZombieHitState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving();
        _zombie.PlayAnimation("Hit");
    }

    public void Execute()
    {
        _timer += Time.deltaTime;
        if (_timer < StunDuration) return;

        // 맞았으니 쫓아온다
        _zombie.ChangeState(ZombieState.Trace);
    }

    public void Exit()
    {
    }
}
