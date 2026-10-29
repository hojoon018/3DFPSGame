using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite[] _modeIcons;
    [SerializeField] private TextMeshProUGUI _currentBulletCountText;
    [SerializeField] private TextMeshProUGUI _currentBombCountText;

    [SerializeField] private Player _player;
    [SerializeField] private PlayerBombFire _playerBombFire;
    [SerializeField] private PlayerGunFire _playerGunFire;

    private void Start()
    {
        RefreshMode(_player.WeaponMode);
        RefreshBomb(_playerBombFire.CurrentBombCount);
        RefreshBullet(_playerGunFire.BulletCount, _playerGunFire.MaxBulletCount);
    }

    private void OnEnable()
    {
        _player.OnWeaponModeChanged += RefreshMode;
        _playerBombFire.OnBombChanged += RefreshBomb;
        _playerGunFire.OnBulletChanged += RefreshBullet;
    }

    private void OnDisable()
    {
        _player.OnWeaponModeChanged -= RefreshMode;
        _playerBombFire.OnBombChanged -= RefreshBomb;
        _playerGunFire.OnBulletChanged -= RefreshBullet;
    }

    private void RefreshMode(WeaponMode mode)
    {
        _weaponIcon.sprite = _modeIcons[(int)mode];
    }

    private void RefreshBullet(int bulletCount, int maxBulletCount)
    {
        _currentBulletCountText.text = $"{bulletCount} / {maxBulletCount}";
    }

    private void RefreshBomb(int count)
    {
        _currentBombCountText.text = $"폭탄 {count}개";
    }
}