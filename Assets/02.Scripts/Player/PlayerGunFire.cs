using System;
using UnityEngine;
using System.Collections;
using Lean.Pool;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("레이저")]
    [SerializeField] private LaserTracer _tracerPrefab; // 총알처럼 날아가는 레이저 선
    [SerializeField] private Transform _muzzle; // 레이저 시작 위치(총구). 비어 있으면 카메라 위치

    [Header("스탯")]
    [SerializeField] private int _maxBulletCount = 30;
    [SerializeField] private float _reloadingTime = 3f;
    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; //발사 속도

    private float _fireTimer = 0f;
    private int _bulletCount = 0;
    private Coroutine _reloadingCoroutine = null;

    private Animator _animator;

    public int MaxBulletCount => _maxBulletCount;
    public int BulletCount => _bulletCount;

    public event Action<int, int> OnBulletChanged;

    private void Awake()
    {
        _bulletCount = _maxBulletCount;
    }

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        if (!isMouseVisible && Input.GetButton("Fire1") && _fireTimer <= 0f && _bulletCount > 0)
        {
            _bulletCount--;
            OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);
            _fireTimer = _fireInterval;
            Shoot();

            _animator.SetBool("Fire", true);

            // 이 방식은 : 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            // StopAllCoroutines();

            if (_reloadingCoroutine != null)
            {
                StopCoroutine(_reloadingCoroutine);
                Debug.Log("재장전 취소!");
                _reloadingCoroutine = null;
            }
        }
        else
        {
            _animator.SetBool("Fire", false);
        }

        // 2. R키 누르면 총알 재장전
        if (Input.GetKey(KeyCode.R) && _reloadingCoroutine == null)
        {
            _reloadingCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadingTime);

        _bulletCount = _maxBulletCount;
        OnBulletChanged?.Invoke(_bulletCount, _maxBulletCount);

        Debug.Log("재장전 완료!");
        _reloadingCoroutine = null;
    }

    // 총구에서 목표 지점을 향해 날아가는 레이저를 하나 쏜다
    private void ShowLaser(Vector3 targetPoint)
    {
        if (_tracerPrefab == null) return;

        Vector3 startPoint = _muzzle != null ? _muzzle.position : Camera.main.transform.position;
        LaserTracer tracer = LeanPool.Spawn(_tracerPrefab, startPoint, Quaternion.identity);
        tracer.Fire(startPoint, targetPoint);
    }

    private void Shoot()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // Ray : 레이저(시작위치, 날아갈 방향)
        // Physics.RayCast : 레이저를 발사
        // RayCastHit : 레이저가 물체와 부딪히면 그 정보를 저장하는 구조체

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪히는 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만약 부딪힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);

        // 맞았으면 맞은 자리, 아니면 사거리 끝을 향해 레이저를 쏜다
        Vector3 targetPoint = isHit ? hitInfo.point : ray.origin + ray.direction * _range;
        ShowLaser(targetPoint);

        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알을 맞았다. 대미지 : {_damage}");

            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();

            // 데미지 구조체 만들기
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);

            // 맞은 대상이 좀비라면 TakeDamage
            /*if (hitInfo.collider.TryGetComponent(out Zombie zombie))
            {
                zombie.TakeDamage(_damage);
            }
            hitInfo.collider.GetComponent<Drum>()?.TakeDamage(_damage);
            hitInfo.collider.GetComponent<Box>()?.TakeDamage(_damage);*/
        }
    }
}