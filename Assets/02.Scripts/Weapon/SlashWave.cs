using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

// 앞으로 날아가며 닿는 적을 관통해 베는 초승달 검기 (월아천충 느낌)
// 모양은 코드로 만든 초승달 띠 메시가, 색과 불꽃과 바깥 기운은 SlashWave 셰이더가 그린다.
// 띠는 앞날 바깥의 기운 영역(_auraWidth)부터 안쪽 불꽃이 그려질 공간(_bandWidthMeters)까지를 덮는다.
// 초승달은 진행 방향을 품은 평면(로컬 XZ)에 누워 있다. 앞날 꼭대기가 앞(+Z)을 향하고 불꽃은 뒤로 끌린다.
// 그래서 쏘는 사람에게는 날이 옆면으로 보여 "/" 모양이 되고, 옆에서 봐야 초승달과 불꽃이 드러난다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SlashWave : MonoBehaviour, IPoolable
{
    private const int MaxOverlapCount = 32;
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int AuraWidthId = Shader.PropertyToID("_AuraWidth");
    private static readonly int BandWidthId = Shader.PropertyToID("_BandWidth");

    [Header("스탯")]
    [SerializeField] private float _damage = 50f;
    [SerializeField] private float _speed = 25f; // 날아가는 속도 (m/s)
    [SerializeField] private float _range = 20f; // 이 거리를 날아가면 사라지기 시작한다

    [Header("판정")]
    [SerializeField] private LayerMask _targetMask; // 데미지를 줄 레이어 (Zombie, Drum 등)
    [SerializeField] private LayerMask _obstacleMask; // 부딪히면 멈추고 사라지는 레이어 (벽 등)

    [Header("모양")]
    [SerializeField] private float _edgeRadius = 3.5f; // 앞날(바깥 호)의 반지름 (m)
    [SerializeField, Range(30f, 180f)] private float _arcAngle = 140f; // 초승달이 펼쳐진 각도
    [SerializeField] private float _bandWidthMeters = 2.4f; // 앞날에서 안쪽으로 몸통과 불꽃이 그려질 공간의 폭 (m, 반지름보다 작아야 한다)
    [SerializeField] private float _auraWidth = 0.6f; // 앞날 바깥으로 기운이 번지는 폭 (m)
    [SerializeField] private float _tipDroop = 0.4f; // 양 끝이 평면 아래로 처지는 정도 (m, 완전히 평평하지 않게)
    [SerializeField] private float _hitThickness = 1.5f; // 판정 상자의 두께 (m, 평면에 수직인 방향)
    [SerializeField] private int _segments = 48; // 호를 나누는 개수 (클수록 매끄럽다)

    [Header("연출")]
    [SerializeField] private float _spawnDuration = 0.08f; // 작게 나타나 원래 크기로 커지는 시간 (초)
    [SerializeField] private float _spawnScale = 0.4f; // 나타나는 순간의 크기 배율
    [SerializeField] private float _fadeDuration = 0.3f; // 양 끝부터 타들어가며 사라지는 시간 (초)

    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private readonly Collider[] _overlapResults = new Collider[MaxOverlapCount];

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private Mesh _mesh;
    private Vector2 _halfSize; // 초승달의 가로(X)/앞뒤(Z) 절반 크기 (판정 상자에 사용)

    private Transform _owner;
    private Vector3 _direction; // 날아가는 방향 (메시가 기울어져 있어도 수평으로 날아가도록 따로 저장)
    private float _age;
    private float _traveled;
    private float _fadeTimer;
    private bool _isFading;
    private bool _isStopped;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();

        _mesh = BuildMesh();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        SetFade(0f);
    }

    private void OnDestroy()
    {
        if (_mesh != null)
        {
            Destroy(_mesh);
        }
    }

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
        _owner = null;
    }

    // 생성 직후 호출해 상태를 초기화하고 direction으로 날아가기 시작한다. owner의 콜라이더는 맞히지 않는다.
    public void Launch(Transform owner, Vector3 direction)
    {
        _owner = owner;
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;
        _hitTargets.Clear();
        _age = 0f;
        _traveled = 0f;
        _fadeTimer = 0f;
        _isFading = false;
        _isStopped = false;

        transform.localScale = Vector3.one * _spawnScale;
        SetFade(0f);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _age += deltaTime;

        // 작게 나타났다가 순식간에 원래 크기로 커진다
        float spawnProgress = _spawnDuration > 0f ? Mathf.Clamp01(_age / _spawnDuration) : 1f;
        transform.localScale = Vector3.one * Mathf.Lerp(_spawnScale, 1f, spawnProgress);

        if (!_isStopped)
        {
            Move(deltaTime);
        }

        if (_isFading)
        {
            _fadeTimer += deltaTime;
            float fade = _fadeDuration > 0f ? Mathf.Clamp01(_fadeTimer / _fadeDuration) : 1f;
            SetFade(fade);

            if (fade >= 1f)
            {
                LeanPool.Despawn(gameObject);
            }
        }
    }

    private void Move(float deltaTime)
    {
        Vector3 start = transform.position;
        Vector3 direction = _direction;
        float distance = _speed * deltaTime;

        // 벽에 부딪히면 그 자리에서 멈춘다
        bool isBlocked = Physics.Raycast(start, direction, out RaycastHit hit, distance, _obstacleMask);
        if (isBlocked)
        {
            distance = hit.distance;
        }

        Vector3 end = start + direction * distance;

        // 사라지는 중에는 더 이상 베지 않는다
        if (!_isFading)
        {
            HitTargets(start, end);
        }

        transform.position = end;
        _traveled += distance;

        if (isBlocked)
        {
            _isStopped = true;
            StartFade();
        }
        else if (_traveled >= _range)
        {
            StartFade();
        }
    }

    // 이번 프레임에 지나간 구간 전체를 상자로 검사해, 빠르게 날아가도 적을 건너뛰지 않게 한다.
    private void HitTargets(Vector3 start, Vector3 end)
    {
        float scale = transform.localScale.x;
        Vector3 center = (start + end) * 0.5f;
        // 상자는 검기와 같이 기울어진다: X = 초승달 폭, Y = 평면 두께, Z = 앞뒤 길이 + 이번 프레임 이동 거리
        Vector3 halfExtents = new Vector3(
            _halfSize.x * scale,
            _hitThickness * 0.5f * scale,
            _halfSize.y * scale + Vector3.Distance(start, end) * 0.5f);

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapResults, transform.rotation, _targetMask);
        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapResults[i];

            // 쏜 사람 자신은 맞히지 않는다
            if (_owner != null && col.transform.IsChildOf(_owner)) continue;
            if (!col.TryGetComponent(out IDamageable damageable)) continue;
            // 관통하므로 같은 대상은 한 번만 벤다
            if (!_hitTargets.Add(damageable)) continue;

            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = col.ClosestPoint(center);
            damageInfo.HitNormal = -_direction;
            damageable.TakeDamage(damageInfo);
        }
    }

    private void StartFade()
    {
        if (_isFading) return;

        _isFading = true;
        _fadeTimer = 0f;
    }

    // 셰이더가 띠 위의 위치를 미터 단위로 계산할 수 있도록 메시 치수도 함께 넣어 준다.
    private void SetFade(float fade)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(FadeId, fade);
        _propertyBlock.SetFloat(AuraWidthId, _auraWidth);
        _propertyBlock.SetFloat(BandWidthId, _bandWidthMeters);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    // 진행 방향을 품은 평면(로컬 XZ)에 누운 초승달 띠 메시를 만든다. 앞날 꼭대기가 +Z(앞)를 향한다.
    // uv.x = 호 위치(0: 한쪽 끝, 1: 다른 쪽 끝), uv.y = 띠 위치(0: 바깥 기운 끝, 1: 띠 안쪽 끝)
    private Mesh BuildMesh()
    {
        int columns = _segments + 1;
        Vector3[] vertices = new Vector3[columns * 2];
        Vector2[] uvs = new Vector2[columns * 2];
        int[] triangles = new int[_segments * 6];

        float halfArc = _arcAngle * 0.5f * Mathf.Deg2Rad;
        float outerRadius = _edgeRadius + _auraWidth;
        float innerRadius = Mathf.Max(_edgeRadius - _bandWidthMeters, 0f);

        // 앞날 꼭대기(z = 0)와 띠 안쪽 양 끝(가장 뒤쪽) 사이의 가운데가 원점이 되도록 뒤로 민다
        float back = innerRadius * Mathf.Cos(halfArc) - _edgeRadius;
        float zOffset = -back * 0.5f;

        for (int i = 0; i < columns; i++)
        {
            float s = (float)i / _segments;
            float angle = Mathf.Lerp(-halfArc, halfArc, s);
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);

            // 양 끝은 평면 아래로 살짝 처진다
            float edge = 2f * s - 1f;
            float y = -_tipDroop * edge * edge;

            vertices[i * 2] = new Vector3(outerRadius * sin, y, outerRadius * cos - _edgeRadius + zOffset);
            vertices[i * 2 + 1] = new Vector3(innerRadius * sin, y, innerRadius * cos - _edgeRadius + zOffset);
            uvs[i * 2] = new Vector2(s, 0f);
            uvs[i * 2 + 1] = new Vector2(s, 1f);
        }

        for (int i = 0; i < _segments; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }

        _halfSize = new Vector2(_edgeRadius * Mathf.Sin(halfArc), zOffset);

        Mesh mesh = new Mesh();
        mesh.name = "SlashWaveMesh";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
