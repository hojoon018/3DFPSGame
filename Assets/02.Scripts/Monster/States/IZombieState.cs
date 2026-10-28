// 좀비 상태 하나가 지켜야 하는 약속
public interface IZombieState
{
    void Enter();   // 이 상태로 들어올 때 한 번
    void Execute(); // 이 상태인 동안 매 프레임 (Zombie.Update가 호출)
    void Exit();    // 이 상태에서 나갈 때 한 번
}
