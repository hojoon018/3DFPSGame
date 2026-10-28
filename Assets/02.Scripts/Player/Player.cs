using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료형 : 함수(들)를 저장할 수 있다
    public event Action OnDamaged = null;

    private Animator _animator;

    private int _injuredLayerIndex;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured Layer");

        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;

        // 체력에 따라 Injured Layer의 가중치를 바꿔준다
        float healthPercent = _health / _maxHealth; // 0 ~ 1
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);

        OnDamaged?.Invoke();

        Debug.Log($"플레이어가 맞았다 데미지 {damageInfo.Value}, 남은 체력{_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다");
            GameManager.Instance.GameOver();
        }
    }
}