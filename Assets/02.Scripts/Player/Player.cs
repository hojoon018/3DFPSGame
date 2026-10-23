using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private void Start()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;
        Debug.Log($"플레이어가 맞았다 데미지 {damageInfo.Value}, 남은 체력{_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다");
        }
    }
}