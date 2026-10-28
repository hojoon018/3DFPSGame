using UnityEngine;

// 공격: 일정 시간마다 플레이어를 때린다
public class ZombieAttackState : IZombieState
{
    private readonly Zombie _zombie;
    private float _timer = 0f; // Zombie._attackTimer가 이사온 곳

    public ZombieAttackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _timer = 0f;
        _zombie.StopMoving();
    }

    public void Execute()
    {
        if (_zombie.DistanceToPlayer() > _zombie.AttackRange)
        {
            _zombie.PlayAnimation("AttackMove");
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _zombie.AttackInterval)
        {
            _zombie.PlayAnimation("StartAttack"); // 실제 데미지는 애니메이션 이벤트 AttackAction()에서
            _timer = 0f;
        }
    }

    public void Exit()
    {
    }
}
