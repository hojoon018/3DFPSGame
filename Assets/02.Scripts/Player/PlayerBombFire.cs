using System;
using Lean.Pool;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다
public class PlayerBombFire : MonoBehaviour
{
    private const int MaxBombCount = 10;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private float _minThrowTimer = 1f;
    [SerializeField] private float _maxThrowTimer = 3f;

    private int _currentBombCount = 0;
    private float _timer = 0f;
    private bool _isCharging = false;

    private void Awake()
    {
        _currentBombCount = MaxBombCount;
    }

    private void Update()
    {
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2") && _currentBombCount > 0)
        {
            _isCharging = true;
            _timer = _minThrowTimer;
        }

        if (Input.GetButton("Fire2") && _isCharging)
        {
            _timer += Time.deltaTime;
            _timer = Mathf.Min(_timer, _maxThrowTimer);
        }

        if (Input.GetButtonUp("Fire2") && _isCharging)
        {
            Shoot();
            _isCharging = false;
        }
    }

    private void Shoot()
    {
        // 2. 발사 위치에서 수류탄 스폰하기 (LeanPool 활용)
        Bomb bomb = LeanPool.Spawn(_bombPrefab, _firePoint.position, Quaternion.identity);

        // 3. 생성된 수류탄을 시선 방향으로 던지기
        bomb.Rigidbody.AddForce(Camera.main.transform.forward * _throwPower * _timer, ForceMode.Impulse);
        _currentBombCount--;
        _timer = 0f;
    }
}