using UnityEngine;

// 카메라를 숨 쉬듯 천천히 흔들어 정지 화면 같은 느낌을 없앤다. (로그인 화면 배경용)
public class CameraIdleSway : MonoBehaviour
{
    [SerializeField] private float _positionAmount = 0.04f;
    [SerializeField] private float _rotationAmount = 0.8f;
    [SerializeField] private float _speed = 0.35f;
    [SerializeField] private float _breathAmount = 0.015f;
    [SerializeField] private float _breathSpeed = 1.6f;

    private Vector3 _basePosition;
    private Quaternion _baseRotation;

    private void Awake()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float time = Time.time * _speed;

        // 축마다 다른 노이즈를 써서 규칙적이지 않은 흔들림을 만든다. (-1 ~ 1)
        float noiseX = Mathf.PerlinNoise(time, 0f) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(0f, time) * 2f - 1f;
        float noiseZ = Mathf.PerlinNoise(time, time) * 2f - 1f;
        float breath = Mathf.Sin(Time.time * _breathSpeed) * _breathAmount;

        transform.localPosition = _basePosition + new Vector3(noiseX * _positionAmount, noiseY * _positionAmount + breath, 0f);
        transform.localRotation = _baseRotation * Quaternion.Euler(noiseY * _rotationAmount, noiseX * _rotationAmount, noiseZ * _rotationAmount * 0.5f);
    }
}
