using UnityEngine;

// Animator가 붙은 오브젝트에서 애니메이션 이벤트를 받아 Player 쪽 컴포넌트로 전달한다.
public class PlayerAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerBombFire _bombFire;

    private void Awake()
    {
        if (_bombFire == null)
        {
            _bombFire = GetComponentInParent<PlayerBombFire>();
        }
    }

    // GrenadeThrow 클립에서 손이 폭탄을 놓는 프레임에 호출된다.
    public void OnThrowRelease()
    {
        _bombFire.OnThrowRelease();
    }
}
