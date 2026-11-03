using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 팝업을 열 때 배경을 어둡게 깔며 패널이 튀어나오고, 닫을 때 줄어들며 사라지게 한다.
// Button OnClick에 Open/Close를 연결해서 사용한다.
public class UI_PopupFeel : MonoBehaviour
{
    // easeOutBack의 튀어나가는 정도
    private const float BackOvershoot = 1.70158f;

    [SerializeField] private Image _dim;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _openDuration = 0.35f;
    [SerializeField] private float _closeDuration = 0.18f;
    [SerializeField] private float _startScale = 0.6f;

    [Header("효과음")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openClip;
    [SerializeField, Range(0f, 1f)] private float _openVolume = 0.6f;

    private float _dimAlpha;
    private Coroutine _animation;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        _dimAlpha = _dim.color.a;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);

        if (_audioSource != null && _openClip != null)
        {
            _audioSource.PlayOneShot(_openClip, _openVolume);
        }

        Play(Open_Coroutine());
    }

    public void Close()
    {
        if (!gameObject.activeSelf) return;

        Play(Close_Coroutine());
    }

    private void Play(IEnumerator routine)
    {
        if (_animation != null)
        {
            StopCoroutine(_animation);
        }

        _animation = StartCoroutine(routine);
    }

    private IEnumerator Open_Coroutine()
    {
        float elapsed = 0f;
        while (elapsed < _openDuration)
        {
            float t = elapsed / _openDuration;
            SetDimAlpha(Mathf.Lerp(0f, _dimAlpha, t));
            _panel.localScale = Vector3.one * Mathf.LerpUnclamped(_startScale, 1f, EaseOutBack(t));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetDimAlpha(_dimAlpha);
        _panel.localScale = Vector3.one;
        _animation = null;
    }

    private IEnumerator Close_Coroutine()
    {
        float startAlpha = _dim.color.a;
        Vector3 startScale = _panel.localScale;

        float elapsed = 0f;
        while (elapsed < _closeDuration)
        {
            float t = elapsed / _closeDuration;
            SetDimAlpha(Mathf.Lerp(startAlpha, 0f, t));
            _panel.localScale = Vector3.Lerp(startScale, Vector3.one * _startScale, t * t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _animation = null;
        gameObject.SetActive(false);
    }

    private void SetDimAlpha(float alpha)
    {
        Color color = _dim.color;
        color.a = alpha;
        _dim.color = color;
    }

    private static float EaseOutBack(float t)
    {
        float c3 = BackOvershoot + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + BackOvershoot * p * p;
    }
}
