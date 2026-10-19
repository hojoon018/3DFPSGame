using UnityEngine;

/// <summary>
/// V 키 입력을 감지하여 6가지 시점(1인칭 → 3인칭 → 숄더뷰 → 쿼터뷰 → 탑다운 → 사이드뷰)을 순차적으로 전환하는 스위처입니다.
/// </summary>
[RequireComponent(typeof(CameraController))]
public class CameraViewSwitcher : MonoBehaviour
{
    [Header("단축키 및 상태 설정")]
    [Tooltip("시점 전환 단축키")]
    [SerializeField] private KeyCode _switchKey = KeyCode.V;

    [Tooltip("현재 활성화된 시점 모드")]
    [SerializeField] private CameraViewMode _currentViewMode = CameraViewMode.FirstPerson;

    [Header("시점별 프리셋 설정 (인스펙터 조정 가능)")]
    // [1인칭 시점]
    // - 위치: 캐릭터의 눈 위치(CameraPosition)와 일치 (오프셋 Vector3.zero)
    // - 회전: 마우스로 상하좌우 자유 회전 (CameraRotate 활성화)
    // - 변화: 플레이어의 시선과 완전히 일치하는 기본 FPS 시점
    [SerializeField]
    private CameraViewSetting _firstPersonView = new CameraViewSetting(
        "1인칭 (First Person)",
        Vector3.zero,
        Vector3.zero,
        followTargetRotation: true,
        useMouseRotation: true);

    // [3인칭 시점]
    // - 위치: 플레이어 뒤쪽 상단으로 후퇴 (오프셋: Y +1.0m, Z -3.5m)
    // - 회전: 플레이어 후방에서 전방을 향해 12도 아래로 내려다봄 (Pitch 12도)
    // - 변화: 플레이어가 회전할 때 등 뒤를 유지하도록 공전하며 캐릭터 전신과 전방 시야 확보
    [SerializeField]
    private CameraViewSetting _thirdPersonView = new CameraViewSetting(
        "3인칭 (Third Person)",
        new Vector3(0f, 1.0f, -3.5f),
        new Vector3(12f, 0f, 0f),
        followTargetRotation: true,
        useMouseRotation: false);

    // [숄더뷰] - 정통 TPS 스타일로 개선
    // - 위치: 캐릭터 우측 어깨 바로 뒤 밀착 (오프셋: X +0.6m, Y 0.0m, Z -1.3m)
    //   * 타겟(CameraPosition: 눈 높이) 기준 Y를 0.0으로 두어 머리 위로 붕 뜨지 않고 어깨 너머로 조준선 정렬
    //   * 캐릭터가 화면 좌측 1/3 지점에 배치되어 전방 시야가 시원하게 트임
    // - 회전: 전방 수평 주시 (Euler 0, 0, 0)
    // - 변화: TPS 조준 게임처럼 마우스 회전(CameraRotate)을 활성화하여 자유롭게 조준 가능
    [SerializeField]
    private CameraViewSetting _shoulderView = new CameraViewSetting(
        "숄더뷰 (Shoulder View)",
        new Vector3(0.6f, 0.0f, -1.3f),
        Vector3.zero,
        followTargetRotation: true,
        useMouseRotation: true);

    // [쿼터뷰]
    // - 위치: 플레이어 대각선 상공 (월드 기준 오프셋: Y +8m, Z -7m)
    // - 회전: 대각선 아래로 45도 내려다봄 (Pitch 45도)
    // - 변화: 월드 좌표계 기준으로 고정된 45도 시야각을 유지하며 이동만 추적 (RPG/전략 시점)
    [SerializeField]
    private CameraViewSetting _quarterView = new CameraViewSetting(
        "쿼터뷰 (Quarter View)",
        new Vector3(0f, 8f, -7f),
        new Vector3(45f, 0f, 0f),
        followTargetRotation: false,
        useMouseRotation: false);

    // [탑다운 뷰]
    // - 위치: 플레이어 정수리 수직 상공 (월드 기준 오프셋: Y +10m, Z -0.01m)
    // - 회전: 수직 아래로 90도 내려다봄 (Pitch 90도)
    // - 변화: 월드 기준 고정 수직 시야로 전환하여 사방의 지형과 구조를 평면적으로 파악
    [SerializeField]
    private CameraViewSetting _topDownView = new CameraViewSetting(
        "탑다운 (Top-Down View)",
        new Vector3(0f, 10f, -0.01f),
        new Vector3(90f, 0f, 0f),
        followTargetRotation: false,
        useMouseRotation: false);

    // [사이드뷰]
    // - 위치: 플레이어 우측 측면 (월드 기준 오프셋: X +8m, Y +0.5m, Z 0m)
    // - 회전: 왼쪽 플레이어를 향해 수평으로 -90도 회전 (Yaw -90도)
    // - 변화: 월드 기준 고정 측면 시야로 2.5D 횡스크롤 플랫포머 느낌 제공
    [SerializeField]
    private CameraViewSetting _sideView = new CameraViewSetting(
        "사이드뷰 (Side View)",
        new Vector3(8f, 0.5f, 0f),
        new Vector3(0f, -90f, 0f),
        followTargetRotation: false,
        useMouseRotation: false);

    private CameraController _cameraController;

    private void Awake()
    {
        _cameraController = GetComponent<CameraController>();
    }

    private void Start()
    {
        ApplyCurrentView();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_switchKey))
        {
            SwitchToNextView();
        }
    }

    /// <summary>
    /// 다음 시점으로 순환 전환합니다 (1인칭 → 3인칭 → 숄더뷰 → 쿼터뷰 → 탑다운 → 사이드뷰 → 1인칭).
    /// </summary>
    public void SwitchToNextView()
    {
        int totalModes = System.Enum.GetValues(typeof(CameraViewMode)).Length;
        int nextIndex = ((int)_currentViewMode + 1) % totalModes;
        SetViewMode((CameraViewMode)nextIndex);
    }

    /// <summary>
    /// 지정한 시점 모드로 변경하고 컨트롤러에 전달합니다.
    /// </summary>
    public void SetViewMode(CameraViewMode newMode)
    {
        _currentViewMode = newMode;
        ApplyCurrentView();
    }

    private void ApplyCurrentView()
    {
        CameraViewSetting currentSetting = GetViewSetting(_currentViewMode);
        _cameraController.ApplyViewSetting(currentSetting);
        Debug.Log($"[CameraViewSwitcher] 시점 전환: {currentSetting.ViewName}");
    }

    /// <summary>
    /// 시점 모드 열거형에 해당하는 설정을 반환합니다.
    /// </summary>
    public CameraViewSetting GetViewSetting(CameraViewMode mode)
    {
        switch (mode)
        {
            case CameraViewMode.FirstPerson:
                return _firstPersonView;
            case CameraViewMode.ThirdPerson:
                return _thirdPersonView;
            case CameraViewMode.Shoulder:
                return _shoulderView;
            case CameraViewMode.QuarterView:
                return _quarterView;
            case CameraViewMode.TopDown:
                return _topDownView;
            case CameraViewMode.SideView:
                return _sideView;
            default:
                return _firstPersonView;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && _cameraController != null)
        {
            ApplyCurrentView();
        }
    }
#endif
}
