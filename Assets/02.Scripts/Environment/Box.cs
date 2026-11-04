using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 체력이 0이 되면 작은 나무 조각들로 산산조각 나며 부서지는 상자
[RequireComponent(typeof(BoxCollider))]
public class Box : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _health = 30f;

    [Header("파편")]
    [SerializeField] private int _fragmentDivision = 2; // 상자를 가로/세로/높이로 몇 칸씩 나눌지 (2면 8조각)
    [SerializeField, Range(0.1f, 1f)] private float _fragmentScale = 0.8f; // 칸 크기 대비 파편 크기 (틈이 생겨야 조각나 보인다)
    [SerializeField] private float _pushPower = 6f; // 맞은 방향으로 밀려나는 속도
    [SerializeField] private float _upPower = 3f; // 위로 튀어오르는 속도
    [SerializeField] private float _scatterPower = 2f; // 조각마다 흩어지는 랜덤 속도
    [SerializeField] private float _spinPower = 10f; // 조각마다 랜덤으로 도는 속도

    [Header("사라짐")]
    [SerializeField] private float _fragmentLifetime = 3f; // 파편이 바닥에 남아 있는 시간 (초)
    [SerializeField] private float _shrinkDuration = 0.5f; // 남아 있는 시간이 끝난 뒤 작아지며 사라지는 시간 (초)

    private BoxCollider _collider;
    private bool _isBroken;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_isBroken) return;

        _health -= damageInfo.Value;

        if (_health <= 0f)
        {
            Break(damageInfo);
        }
    }

    private void Break(DamageInfo damageInfo)
    {
        _isBroken = true;

        // 맞은 면의 안쪽 방향으로 밀어낸다. 방향 정보가 없으면 흩어지기만 한다.
        Vector3 pushDirection = -damageInfo.HitNormal;

        List<Transform> fragments = SpawnFragments(pushDirection);

        // 정적 배칭된 오브젝트라 움직이지 않고, 보이지 않게만 한 뒤 파편 정리가 끝나면 지운다
        GetComponent<Renderer>().enabled = false;
        _collider.enabled = false;

        StartCoroutine(Cleanup_Coroutine(fragments));
    }

    // 콜라이더 크기를 칸으로 나눠, 칸마다 상자와 같은 재질의 작은 큐브를 만든다.
    // 정적 배칭되면 MeshFilter의 메시가 합쳐진 메시로 바뀌므로 크기는 콜라이더를 기준으로 한다.
    private List<Transform> SpawnFragments(Vector3 pushDirection)
    {
        int division = Mathf.Max(1, _fragmentDivision);
        Material material = GetComponent<Renderer>().sharedMaterial;

        Vector3 cellSize = _collider.size / division;
        Vector3 fragmentSize = Vector3.Scale(cellSize, transform.lossyScale) * _fragmentScale;
        Vector3 origin = _collider.center - _collider.size * 0.5f + cellSize * 0.5f;

        List<Transform> fragments = new List<Transform>(division * division * division);
        for (int x = 0; x < division; x++)
        {
            for (int y = 0; y < division; y++)
            {
                for (int z = 0; z < division; z++)
                {
                    Vector3 localPosition = origin + Vector3.Scale(cellSize, new Vector3(x, y, z));

                    GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fragment.name = "BoxFragment";
                    fragment.layer = gameObject.layer;
                    fragment.transform.SetPositionAndRotation(transform.TransformPoint(localPosition), transform.rotation);
                    fragment.transform.localScale = fragmentSize;
                    fragment.GetComponent<Renderer>().sharedMaterial = material;

                    Rigidbody body = fragment.AddComponent<Rigidbody>();
                    Vector3 velocity = pushDirection * _pushPower + Vector3.up * _upPower + Random.insideUnitSphere * _scatterPower;
                    body.AddForce(velocity, ForceMode.VelocityChange);
                    body.AddTorque(Random.insideUnitSphere * _spinPower, ForceMode.VelocityChange);

                    fragments.Add(fragment.transform);
                }
            }
        }

        return fragments;
    }

    private IEnumerator Cleanup_Coroutine(List<Transform> fragments)
    {
        yield return new WaitForSeconds(_fragmentLifetime);

        // 파편들이 동시에 작아지며 사라진다
        Vector3[] startScales = new Vector3[fragments.Count];
        for (int i = 0; i < fragments.Count; i++)
        {
            startScales[i] = fragments[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < _shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f - Mathf.Clamp01(elapsed / _shrinkDuration);
            for (int i = 0; i < fragments.Count; i++)
            {
                fragments[i].localScale = startScales[i] * scale;
            }

            yield return null;
        }

        foreach (Transform fragment in fragments)
        {
            Destroy(fragment.gameObject);
        }

        Destroy(gameObject);
    }
}
