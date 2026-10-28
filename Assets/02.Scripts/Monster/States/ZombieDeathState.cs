using UnityEngine;

// 죽음: 쓰러지는 애니메이션을 보여 주고 풀에 반납된다
public class ZombieDeathState : IZombieState
{
    private const float DespawnDelay = 2f; // 쓰러진 뒤 사라지기까지의 시간

    private readonly Zombie _zombie;
    private float _timer = 0f;

    public ZombieDeathState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving();
        _zombie.PlayAnimation("Death");
    }

    public void Execute()
    {
        _timer += Time.deltaTime;
        if (_timer < DespawnDelay) return;

        _zombie.Despawn();
    }

    public void Exit()
    {
    }
}
