using UnityEngine;

/// <summary>
/// 지원하는 6가지 시점 모드 열거형
/// </summary>
public enum CameraViewMode
{
    FirstPerson, // 1인칭
    ThirdPerson, // 3인칭
    Shoulder,    // 숄더뷰
    QuarterView, // 쿼터뷰
    TopDown,     // 탑다운
    SideView     // 사이드뷰
}

/// <summary>
/// 각 시점의 위치 오프셋, 회전 각도 및 동작 옵션을 정의하는 데이터 클래스입니다.
/// </summary>
[System.Serializable]
public class CameraViewSetting
{
    [Tooltip("시점 이름 (인스펙터 및 로그 구분용)")]
    [SerializeField] private string _viewName;

    [Tooltip("타겟 기준 카메라 위치 오프셋 (X: 좌우, Y: 상하, Z: 전후)")]
    [SerializeField] private Vector3 _positionOffset;

    [Tooltip("카메라 고정 회전 각도 (X: Pitch, Y: Yaw, Z: Roll)")]
    [SerializeField] private Vector3 _rotationEuler;

    [Tooltip("타겟의 Y축 회전을 따라갈지 여부 (true: 타겟 로컬 기준, false: 월드 고정)")]
    [SerializeField] private bool _followTargetRotation;

    [Tooltip("마우스 시점 회전(CameraRotate) 활성화 여부")]
    [SerializeField] private bool _useMouseRotation;

    public string ViewName => _viewName;
    public Vector3 PositionOffset => _positionOffset;
    public Vector3 RotationEuler => _rotationEuler;
    public bool FollowTargetRotation => _followTargetRotation;
    public bool UseMouseRotation => _useMouseRotation;

    public CameraViewSetting(string viewName, Vector3 positionOffset, Vector3 rotationEuler, bool followTargetRotation, bool useMouseRotation)
    {
        _viewName = viewName;
        _positionOffset = positionOffset;
        _rotationEuler = rotationEuler;
        _followTargetRotation = followTargetRotation;
        _useMouseRotation = useMouseRotation;
    }
}
