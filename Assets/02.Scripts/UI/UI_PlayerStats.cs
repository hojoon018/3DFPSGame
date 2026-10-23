using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _playerHealthBar;
    [SerializeField] private Slider _playerStaminaBar;

    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private Player _player;

    private void Update()
    {
        _playerHealthBar.value = _player.Health / _player.MaxHealth;
        _playerStaminaBar.value = _playerMove.Stamina / PlayerMove.MaxStamina;
    }
}