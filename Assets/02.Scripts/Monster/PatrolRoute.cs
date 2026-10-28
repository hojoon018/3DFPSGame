using UnityEngine;

// 순찰 경로: 등록된 지점들을 순서대로 순찰한다. 스폰 지점에 붙여 쓴다
public class PatrolRoute : MonoBehaviour
{
    [SerializeField] private Transform[] _waypoints;

    public int Count => _waypoints.Length;

    public Vector3 GetPoint(int index)
    {
        return _waypoints[index].position;
    }

    // 씬 뷰에서 경로를 선으로 보여 준다 (게임에는 영향 없음)
    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            Vector3 from = _waypoints[i].position;
            Vector3 to = _waypoints[(i + 1) % _waypoints.Length].position;
            Gizmos.DrawLine(from, to);
        }
    }
}
