using Lean.Pool;
using UnityEngine;

// 총알처럼 날아가는 레이저 선: 머리가 목표를 향해 날아가고 꼬리가 뒤따라가며, 꼬리가 목표에 닿으면 풀에 반납된다
public class LaserTracer : MonoBehaviour
{
    [SerializeField] private LineRenderer _line;
    [SerializeField] private float _speed = 150f; // 날아가는 속도 (m/s)
    [SerializeField] private float _length = 2f; // 선의 길이

    private Vector3 _start;
    private Vector3 _direction;
    private float _totalDistance;
    private float _traveled;

    // 시작점에서 끝점을 향해 날아가기 시작한다
    public void Fire(Vector3 start, Vector3 end)
    {
        _start = start;
        _direction = (end - start).normalized;
        _totalDistance = Vector3.Distance(start, end);
        _traveled = 0f;

        _line.useWorldSpace = true;
        _line.positionCount = 2;
        UpdateLine();
    }

    private void Update()
    {
        _traveled += _speed * Time.deltaTime;

        // 꼬리까지 목표를 지나갔으면 사라진다
        if (_traveled - _length >= _totalDistance)
        {
            LeanPool.Despawn(gameObject);
            return;
        }

        UpdateLine();
    }

    private void UpdateLine()
    {
        float headDistance = Mathf.Min(_traveled, _totalDistance);
        float tailDistance = Mathf.Max(_traveled - _length, 0f);

        _line.SetPosition(0, _start + _direction * tailDistance);
        _line.SetPosition(1, _start + _direction * headDistance);
    }
}
