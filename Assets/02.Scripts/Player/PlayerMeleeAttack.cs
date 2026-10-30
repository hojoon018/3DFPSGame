using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    private const int SlashVariationCount = 2;

    [SerializeField] private Collider _katanaCollider;

    private Player _player;
    private Animator _animator;
    private Katana _katana;

    private void Awake()
    {
        // 이 컴포넌트는 Animator가 붙은 모델 오브젝트에 있고, Player는 부모에 있다.
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
        _katana = _katanaCollider.GetComponent<Katana>();
    }

    // 휘두르기 클립의 타격 시작 프레임에서 애니메이션 이벤트로 호출된다.
    public void OnSlashHitStart()
    {
        _katana.BeginSwing();
    }

    // 휘두르기 클립의 타격 종료 프레임에서 애니메이션 이벤트로 호출된다.
    public void OnSlashHitEnd()
    {
        _katana.EndSwing();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire2"))
        {
            // 0: Outward, 1: Overhead (Melee Layer의 SlashIndex 전환 조건과 일치)
            _animator.SetInteger("SlashIndex", Random.Range(0, SlashVariationCount));
            _animator.SetTrigger("Slash");
        }
    }
}
