using UnityEngine;
using UnityEngine.UI;

// 배경을 천천히 확대/이동시키고, 가장자리 비네트를 심장박동처럼 맥동시킨다.
public class UI_BackgroundFeel : MonoBehaviour
{
    [Header("배경 움직임")]
    [SerializeField] private RectTransform _background;
    [SerializeField] private float _baseScale = 1.08f;
    [SerializeField] private float _zoomAmount = 0.03f;
    [SerializeField] private float _zoomPeriod = 18f;
    [SerializeField] private Vector2 _panAmount = new Vector2(20f, 8f);
    [SerializeField] private float _panPeriod = 26f;

    [Header("비네트 맥동")]
    [SerializeField] private Image _vignette;
    [SerializeField, Range(0f, 1f)] private float _vignetteBaseAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float _vignettePulseAlpha = 0.25f;
    [SerializeField] private float _heartbeatPeriod = 1.4f;
    [SerializeField] private float _secondBeatDelay = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _secondBeatStrength = 0.6f;
    [SerializeField] private float _beatSharpness = 120f;

    private void Update()
    {
        float time = Time.unscaledTime;

        if (_background != null)
        {
            float zoom = _baseScale + Mathf.Sin(time * Mathf.PI * 2f / _zoomPeriod) * _zoomAmount;
            float panPhase = time * Mathf.PI * 2f / _panPeriod;
            _background.localScale = Vector3.one * zoom;
            _background.anchoredPosition = new Vector2(Mathf.Sin(panPhase) * _panAmount.x, Mathf.Cos(panPhase) * _panAmount.y);
        }

        if (_vignette != null)
        {
            // 쿵-쿵 두 번 뛰고 쉬는 심장박동 패턴
            float phase = Mathf.Repeat(time, _heartbeatPeriod);
            float firstBeat = Mathf.Exp(-_beatSharpness * phase * phase);
            float secondPhase = phase - _secondBeatDelay;
            float secondBeat = Mathf.Exp(-_beatSharpness * secondPhase * secondPhase) * _secondBeatStrength;

            Color color = _vignette.color;
            color.a = _vignetteBaseAlpha + (firstBeat + secondBeat) * _vignettePulseAlpha;
            _vignette.color = color;
        }
    }
}
