using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _playerHealthBar;
    [SerializeField] private Slider _playerStaminaBar;

    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Player _player;

    private void Start()
    {
        // 플레이어 공격 이벤트 구독
        _player.OnDamaged += RefreshHealth;
        RefreshHealth();
    }

    private void Update()
    {
        // RefreshHealth();    // 폴링

        _playerStaminaBar.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        // 문제 : 체력값이 변하지 않았음에도 초당 60회 이상 캔버스를 불필요하게 다시 그린다 -> 성능 낭비
        // 이거 하나 정도는 괜찮아 보이지만.. 이런 불필요한 연산이 다른 UI에서 누적될수록 느려진다
        // 해결 : 값이 바뀌었을 때만 캔버스 슬라이더 갱신을 한다

        // 이때 쓰는 게 옵저버 패턴
        // 유튜브 구독 패턴은 유튜버가 영상을 올릴 때 이를 구독 중인 여러 구독자에게 알려주는 패턴이다
        // C#에서는 이것을 Action으로 구현할 수 있다

        _playerHealthBar.value = _player.Health / _player.MaxHealth;
    }
}