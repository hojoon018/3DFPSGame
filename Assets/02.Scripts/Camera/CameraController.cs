using System.Reflection;
using UnityEngine;

/// <summary>
/// 시점 설정(CameraViewSetting)에 따라 카메라의 위치와 회전을 계산하고 적용하는 컨트롤러입니다.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Tooltip("카메라가 추적할 타겟 (CameraPosition 또는 Player)")]
    [SerializeField] private Transform _target;

    private CameraRotate _cameraRotate;
    private CameraFollow _cameraFollow;
    private CameraViewSetting _currentSetting;

    private FieldInfo _accumulationXField;
    private FieldInfo _accumulationYField;

    public Transform Target
    {
        get => _target;
        set => _target = value;
    }

    private void Awake()
    {
        _cameraRotate = GetComponent<CameraRotate>();
        _cameraFollow = GetComponent<CameraFollow>();

        // CameraController가 위치 제어를 전담하므로 기존 CameraFollow는 비활성화
        if (_cameraFollow != null)
        {
            _cameraFollow.enabled = false;
        }

        if (_target == null)
        {
            FindDefaultTarget();
        }

        // CameraRotate의 누적 각도 동기화를 위한 리플렉션 캐싱
        if (_cameraRotate != null)
        {
            _accumulationXField = typeof(CameraRotate).GetField("_accumulationX", BindingFlags.NonPublic | BindingFlags.Instance);
            _accumulationYField = typeof(CameraRotate).GetField("_accumulationY", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    private void LateUpdate()
    {
        if (_target == null || _currentSetting == null)
        {
            return;
        }

        UpdatePosition();
        UpdateRotation();
    }

    /// <summary>
    /// 외부(CameraViewSwitcher 등)로부터 새 시점 설정을 전달받아 적용합니다.
    /// </summary>
    public void ApplyViewSetting(CameraViewSetting setting)
    {
        if (setting == null)
        {
            return;
        }

        _currentSetting = setting;

        // 마우스 회전 컴포넌트(CameraRotate) 활성화/비활성화 처리
        if (_cameraRotate != null)
        {
            _cameraRotate.enabled = _currentSetting.UseMouseRotation;

            // 마우스 회전을 사용하는 시점으로 진입할 때 플레이어 정면 각도와 카메라 각도 동기화
            if (_currentSetting.UseMouseRotation && _target != null)
            {
                _accumulationXField?.SetValue(_cameraRotate, _target.eulerAngles.y);
                _accumulationYField?.SetValue(_cameraRotate, _currentSetting.RotationEuler.x);
            }
        }

        // 고정 각도 시점인 경우 즉시 회전 각도 1회 반영
        if (!_currentSetting.UseMouseRotation)
        {
            ApplyInstantRotation();
        }
    }

    /// <summary>
    /// 타겟 위치와 설정된 오프셋을 바탕으로 카메라 위치를 갱신합니다.
    /// </summary>
    private void UpdatePosition()
    {
        if (_currentSetting.FollowTargetRotation)
        {
            // 타겟(플레이어)의 수평 회전(Y축)에 맞춰 오프셋을 회전하여 위치 결정
            Quaternion targetYawRotation = Quaternion.Euler(0f, _target.eulerAngles.y, 0f);
            transform.position = _target.position + (targetYawRotation * _currentSetting.PositionOffset);
        }
        else
        {
            // 월드 좌표계 기준 고정 오프셋 적용
            transform.position = _target.position + _currentSetting.PositionOffset;
        }
    }

    /// <summary>
    /// 설정된 각도를 바탕으로 카메라 바라보는 방향을 갱신합니다.
    /// </summary>
    private void UpdateRotation()
    {
        // 마우스 회전 모드가 아닐 때만 카메라 각도를 직접 고정 제어
        if (!_currentSetting.UseMouseRotation)
        {
            ApplyInstantRotation();
        }
    }

    /// <summary>
    /// 현재 설정에 맞는 회전값을 카메라 transform에 즉각 반영합니다.
    /// </summary>
    private void ApplyInstantRotation()
    {
        if (_currentSetting.FollowTargetRotation && _target != null)
        {
            transform.rotation = Quaternion.Euler(
                _currentSetting.RotationEuler.x,
                _target.eulerAngles.y + _currentSetting.RotationEuler.y,
                _currentSetting.RotationEuler.z
            );
        }
        else
        {
            transform.rotation = Quaternion.Euler(_currentSetting.RotationEuler);
        }
    }

    /// <summary>
    /// 씬에서 카메라가 추적할 타겟을 자동으로 탐색합니다.
    /// </summary>
    private void FindDefaultTarget()
    {
        if (_cameraFollow != null)
        {
            FieldInfo targetField = typeof(CameraFollow).GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField != null)
            {
                _target = targetField.GetValue(_cameraFollow) as Transform;
            }
        }

        if (_target == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                Transform camPos = playerObj.transform.Find("CameraPosition");
                _target = camPos != null ? camPos : playerObj.transform;
            }
        }

        if (_target == null)
        {
            Debug.LogWarning("[CameraController] 추적할 타겟(_target)을 찾지 못했습니다! 인스펙터에서 타겟을 지정해주세요.");
        }
    }
}
