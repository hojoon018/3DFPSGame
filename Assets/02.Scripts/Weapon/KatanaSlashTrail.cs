using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 칼날의 밑동(_base)과 칼끝(_tip)이 지나간 자리를 리본 메시로 그려 검기 궤적을 남긴다.
public class KatanaSlashTrail : MonoBehaviour
{
    // 궤적의 한 지점: 그 순간의 밑동/칼끝 위치와 기록된 시각
    private struct TrailSample
    {
        public Vector3 Base;
        public Vector3 Tip;
        public float Time;
    }

    [Header("칼날 기준점")]
    [SerializeField] private Transform _base;
    [SerializeField] private Transform _tip;

    [Header("크기")]
    [SerializeField] private float _tipExtension = 1.8f; // 밑동→칼끝 길이에 곱하는 배율 (1 = 실제 칼 길이, 클수록 큰 초승달)
    [SerializeField] private float _baseExtension = 0.3f; // 밑동을 칼끝 쪽으로 당기는 비율 (0 = 그대로, 손잡이 근처를 비워 초승달 모양을 만든다)
    [SerializeField, Range(0f, 1f)] private float _tailTaper = 0.85f; // 오래된 쪽(꼬리)으로 갈수록 폭을 좁히는 정도 (0 = 일정, 1 = 꼬리 끝에서 폭 0)

    [Header("궤적")]
    [SerializeField] private Material _material;
    [SerializeField] private float _lifetime = 0.4f; // 한 지점이 사라지기까지 걸리는 시간 (초)
    [SerializeField] private float _minEmitDuration = 0.2f; // 타격 구간이 짧아도 최소 이 시간(초)만큼은 궤적을 그린다
    [SerializeField] private int _maxSamples = 48;
    [SerializeField] private int _subdivisions = 4; // 샘플 사이를 곡선으로 나누는 개수 (빠르게 휘둘러도 각지지 않게)

    private readonly List<TrailSample> _samples = new List<TrailSample>();
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector2> _uvs = new List<Vector2>();
    private readonly List<int> _triangles = new List<int>();

    private Mesh _mesh;
    private RenderParams _renderParams;
    private bool _isEmitting;
    private bool _isStopRequested;
    private float _minEmitEndTime;

    private void Awake()
    {
        if (_material == null || _base == null || _tip == null)
        {
            Debug.LogError($"{name}: KatanaSlashTrail의 Base/Tip/Material을 지정해야 합니다.");
            enabled = false;
            return;
        }

        _mesh = new Mesh();
        _mesh.MarkDynamic();

        _renderParams = new RenderParams(_material);
        _renderParams.shadowCastingMode = ShadowCastingMode.Off;
        _renderParams.receiveShadows = false;
        _renderParams.layer = gameObject.layer;
    }

    private void OnDisable()
    {
        // 무기를 바꿨다가 다시 꺼낼 때 이전 궤적이 남아 보이지 않게 비운다.
        _samples.Clear();
        _isEmitting = false;
        _isStopRequested = false;
    }

    private void OnDestroy()
    {
        if (_mesh != null)
        {
            Destroy(_mesh);
        }
    }

    // 휘두르기 시작: 이 시점부터 칼날 위치를 기록한다.
    public void StartTrail()
    {
        if (!enabled) return;

        _samples.Clear();
        _isEmitting = true;
        _isStopRequested = false;
        _minEmitEndTime = Time.time + _minEmitDuration;
        AddSample();
    }

    // 휘두르기 끝: 최소 기록 시간이 지나면 기록을 멈추고, 남은 궤적은 수명이 다할 때까지 사라지며 보인다.
    public void StopTrail()
    {
        _isStopRequested = true;
    }

    // 애니메이션으로 칼이 움직인 뒤의 위치를 기록해야 하므로 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (_isEmitting && _isStopRequested && Time.time >= _minEmitEndTime)
        {
            _isEmitting = false;
        }

        if (_isEmitting)
        {
            AddSample();
        }

        RemoveOldSamples();
        if (_samples.Count < 2) return;

        BuildMesh();
        _renderParams.worldBounds = _mesh.bounds;
        // 정점을 월드 좌표로 넣었으므로 단위 행렬로 그린다.
        Graphics.RenderMesh(_renderParams, _mesh, 0, Matrix4x4.identity);
    }

    private void AddSample()
    {
        // 칼날 방향을 따라 밑동과 칼끝을 늘려 실제 칼보다 큰 검기를 그린다.
        Vector3 basePosition = _base.position;
        Vector3 blade = _tip.position - basePosition;

        TrailSample sample = new TrailSample();
        sample.Base = basePosition + blade * _baseExtension;
        sample.Tip = basePosition + blade * _tipExtension;
        sample.Time = Time.time;
        _samples.Add(sample);
    }

    private void RemoveOldSamples()
    {
        while (_samples.Count > 0 && Time.time - _samples[0].Time > _lifetime)
        {
            _samples.RemoveAt(0);
        }

        while (_samples.Count > _maxSamples)
        {
            _samples.RemoveAt(0);
        }
    }

    private void BuildMesh()
    {
        _vertices.Clear();
        _uvs.Clear();
        _triangles.Clear();

        float now = Time.time;
        int last = _samples.Count - 1;

        for (int i = 0; i < last; i++)
        {
            TrailSample p0 = _samples[Mathf.Max(i - 1, 0)];
            TrailSample p1 = _samples[i];
            TrailSample p2 = _samples[i + 1];
            TrailSample p3 = _samples[Mathf.Min(i + 2, last)];

            // 마지막 구간에서만 끝점(t = 1)까지 넣어 점이 중복되지 않게 한다.
            int steps = (i == last - 1) ? _subdivisions + 1 : _subdivisions;
            for (int step = 0; step < steps; step++)
            {
                float t = (float)step / _subdivisions;
                Vector3 basePosition = CatmullRom(p0.Base, p1.Base, p2.Base, p3.Base, t);
                Vector3 tipPosition = CatmullRom(p0.Tip, p1.Tip, p2.Tip, p3.Tip, t);
                float sampleTime = Mathf.Lerp(p1.Time, p2.Time, t);

                // uv.x = 나이(0: 방금 지나감, 1: 곧 사라짐), uv.y = 칼날 위치(0: 밑동, 1: 칼끝)
                float age = Mathf.Clamp01((now - sampleTime) / _lifetime);

                // 꼬리로 갈수록 밑동을 칼끝 쪽으로 끌어당겨 폭을 좁힌다. 바깥(칼끝) 선은 그대로라 초승달 끝이 날카로워진다.
                basePosition = Vector3.Lerp(basePosition, tipPosition, age * _tailTaper);
                _vertices.Add(basePosition);
                _uvs.Add(new Vector2(age, 0f));
                _vertices.Add(tipPosition);
                _uvs.Add(new Vector2(age, 1f));
            }
        }

        // 이웃한 (밑동, 칼끝) 두 쌍을 사각형(삼각형 2개)으로 잇는다. 셰이더가 Cull Off라 감는 방향은 상관없다.
        int pointCount = _vertices.Count / 2;
        for (int i = 0; i < pointCount - 1; i++)
        {
            int v = i * 2;
            _triangles.Add(v);
            _triangles.Add(v + 1);
            _triangles.Add(v + 2);
            _triangles.Add(v + 2);
            _triangles.Add(v + 1);
            _triangles.Add(v + 3);
        }

        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetTriangles(_triangles, 0);
        _mesh.RecalculateBounds();
    }

    // Catmull-Rom 스플라인: p1~p2 사이를 p0, p3를 참고해 부드러운 곡선으로 보간한다 (표준 공식).
    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1
            + (p2 - p0) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }
}
