using System;
using Lean.Pool;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 폭탄을 던질 준비를 하고, 떼면 던지는 모션을 재생한다.
// 폭탄은 모션에서 손이 폭탄을 놓는 순간(애니메이션 이벤트)에 시선이 바라보는 방향으로 던져진다.
public class PlayerBombFire : MonoBehaviour
{
    private const int MaxBombCount = 10;

    private static readonly int ChargingHash = Animator.StringToHash("Charging");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    [SerializeField] private Transform _firePoint; // 오른손 본을 찾지 못했을 때 쓰는 대체 발사 위치
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private float _minThrowTimer = 1f;
    [SerializeField] private float _maxThrowTimer = 3f;

    [Header("던지기 모션")]
    [SerializeField] private float _throwTimeout = 1.5f; // 애니메이션 이벤트가 오지 않을 때 던지기 상태를 강제로 해제하는 시간

    private int _currentBombCount = 0;
    private float _timer = 0f;
    private bool _isCharging = false;

    private bool _isThrowing = false;
    private float _throwTimer = 0f; // 버튼을 뗀 순간 확정된 던지는 힘 배율
    private float _throwElapsed = 0f;

    private Animator _animator;
    private Transform _rightHand;

    private void Awake()
    {
        _currentBombCount = MaxBombCount;

        _animator = GetComponentInChildren<Animator>();
        _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            CancelThrow();
            return;
        }

        if (_isThrowing)
        {
            _throwElapsed += Time.deltaTime;
            if (_throwElapsed >= _throwTimeout)
            {
                CancelThrow();
            }

            return;
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2") && _currentBombCount > 0)
        {
            _isCharging = true;
            _timer = _minThrowTimer;
            _animator.SetBool(ChargingHash, true);
        }

        if (Input.GetButton("Fire2") && _isCharging)
        {
            _timer += Time.deltaTime;
            _timer = Mathf.Min(_timer, _maxThrowTimer);
        }

        // 2. 버튼을 떼면 던지는 힘을 확정하고 던지기 모션을 재생한다.
        if (Input.GetButtonUp("Fire2") && _isCharging)
        {
            _isCharging = false;
            _isThrowing = true;
            _throwTimer = _timer;
            _throwElapsed = 0f;

            _animator.SetBool(ChargingHash, false);
            _animator.SetTrigger(ThrowHash);
        }
    }

    // 던지기 모션에서 손이 폭탄을 놓는 프레임에 애니메이션 이벤트로 호출된다.
    public void OnThrowRelease()
    {
        if (!_isThrowing) return;

        Shoot();
        _isThrowing = false;
    }

    private void CancelThrow()
    {
        if (!_isCharging && !_isThrowing) return;

        _isCharging = false;
        _isThrowing = false;
        _timer = 0f;

        _animator.SetBool(ChargingHash, false);
        _animator.ResetTrigger(ThrowHash);
    }

    private void Shoot()
    {
        // 3. 오른손 위치에서 수류탄 스폰하기 (LeanPool 활용)
        Vector3 spawnPosition = _rightHand != null ? _rightHand.position : _firePoint.position;
        Bomb bomb = LeanPool.Spawn(_bombPrefab, spawnPosition, Quaternion.identity);

        // 4. 생성된 수류탄을 시선 방향으로 던지기
        bomb.Rigidbody.AddForce(Camera.main.transform.forward * _throwPower * _throwTimer, ForceMode.Impulse);
        _currentBombCount--;
        _timer = 0f;
    }
}
