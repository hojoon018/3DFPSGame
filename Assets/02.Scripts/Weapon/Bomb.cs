using System;
using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 폭탄
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explositionEffectPrefab;

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        ParticleSystem effect = Instantiate(_explositionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        Debug.Log(collision.gameObject.name);

        // Todo. 오브젝트 풀링 적용
        // 폭탄을 삭제한다
        Destroy(gameObject);
    }
}