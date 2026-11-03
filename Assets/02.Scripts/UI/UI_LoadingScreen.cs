using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임 씬을 비동기로 불러오면서 진행 바, 퍼센트, 회전하는 크로스헤어, 조작 팁을 보여준다.
// 로딩이 너무 빨리 끝나도 연출이 보이도록 최소 표시 시간을 둔다.
public class UI_LoadingScreen : MonoBehaviour
{
    // AsyncOperation.progress는 allowSceneActivation이 false면 0.9에서 멈춘다.
    private const float LoadReadyProgress = 0.9f;

    [SerializeField] private string _targetSceneName = "GameScene";
    [SerializeField] private float _minLoadingTime = 3.5f;

    [Header("참조")]
    [SerializeField] private UI_ScreenImpact _impact;
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private Image _progressFill;
    [SerializeField] private RectTransform _progressHead;
    [SerializeField] private TextMeshProUGUI _percentText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private RectTransform _spinner;
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private CanvasGroup _tipGroup;

    [Header("진행 바")]
    [SerializeField] private float _progressSmoothSpeed = 1.2f;
    [SerializeField] private float _headPulseSpeed = 8f;
    [SerializeField] private float _headPulseAmount = 0.35f;

    [Header("크로스헤어")]
    [SerializeField] private float _spinnerSpeed = 180f;
    [SerializeField] private float _spinnerPulseSpeed = 4f;
    [SerializeField] private float _spinnerPulseAmount = 0.12f;

    [Header("상태 텍스트")]
    [SerializeField] private string _loadingLabel = "LOADING";
    [SerializeField] private string _completeLabel = "READY";
    [SerializeField] private float _dotInterval = 0.35f;

    [Header("팁")]
    [SerializeField, TextArea] private string[] _tips;
    [SerializeField] private float _tipDuration = 2.4f;
    [SerializeField] private float _tipFadeDuration = 0.25f;
    [SerializeField] private float _tipSlideOffset = 30f;

    [Header("완료 연출")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private Color _completeFlashColor = new Color(0.8f, 0.05f, 0.05f, 0.5f);
    [SerializeField] private float _completeFlashDuration = 0.4f;
    [SerializeField] private float _completeShakeStrength = 16f;
    [SerializeField] private float _completeHoldTime = 0.6f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    [Header("효과음")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _completeClip;
    [SerializeField, Range(0f, 1f)] private float _completeVolume = 0.7f;

    private float _displayedProgress;
    private bool _isComplete;
    private Vector2 _tipBasePosition;

    private void Start()
    {
        _tipBasePosition = _tipText.rectTransform.anchoredPosition;

        StartCoroutine(Load_Coroutine());
        StartCoroutine(Tips_Coroutine());
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        _spinner.localEulerAngles = new Vector3(0f, 0f, -time * _spinnerSpeed);
        _spinner.localScale = Vector3.one * (1f + Mathf.Sin(time * _spinnerPulseSpeed) * _spinnerPulseAmount);

        _progressHead.localScale = Vector3.one * (1f + Mathf.Sin(time * _headPulseSpeed) * _headPulseAmount);

        if (!_isComplete)
        {
            int dotCount = Mathf.FloorToInt(time / _dotInterval) % 4;
            _statusText.text = _loadingLabel + new string('.', dotCount);
        }
    }

    private IEnumerator Load_Coroutine()
    {
        _fade.alpha = 1f;
        SetProgress(0f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(_targetSceneName);
        operation.allowSceneActivation = false;

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            _fade.alpha = 1f - elapsed / _fadeInDuration;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _fade.alpha = 0f;

        float loadingTime = 0f;
        while (_displayedProgress < 1f)
        {
            loadingTime += Time.unscaledDeltaTime;

            // 실제 로딩 진행도와 최소 표시 시간 중 느린 쪽을 따라간다.
            float realProgress = Mathf.Clamp01(operation.progress / LoadReadyProgress);
            float timeProgress = Mathf.Clamp01(loadingTime / _minLoadingTime);
            float target = Mathf.Min(realProgress, timeProgress);

            SetProgress(Mathf.MoveTowards(_displayedProgress, target, _progressSmoothSpeed * Time.unscaledDeltaTime));
            yield return null;
        }

        _isComplete = true;
        _statusText.text = _completeLabel;
        if (_audioSource != null && _completeClip != null)
        {
            _audioSource.PlayOneShot(_completeClip, _completeVolume);
        }
        _impact.Flash(_completeFlashColor, _completeFlashDuration);
        _impact.Shake(_completeFlashDuration, _completeShakeStrength);

        yield return new WaitForSecondsRealtime(_completeHoldTime);

        elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            _fade.alpha = elapsed / _fadeOutDuration;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _fade.alpha = 1f;

        operation.allowSceneActivation = true;
    }

    private void SetProgress(float progress)
    {
        _displayedProgress = progress;
        _progressFill.fillAmount = progress;
        _percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";

        // 빛나는 머리 부분을 채워진 끝 위치에 붙인다.
        RectTransform bar = (RectTransform)_progressFill.transform;
        _progressHead.anchoredPosition = new Vector2(bar.rect.xMin + bar.rect.width * progress, 0f);
    }

    private IEnumerator Tips_Coroutine()
    {
        if (_tips == null || _tips.Length == 0) yield break;

        int index = Random.Range(0, _tips.Length);
        while (true)
        {
            _tipText.text = _tips[index];
            yield return FadeTip(true);
            yield return new WaitForSecondsRealtime(_tipDuration);
            yield return FadeTip(false);

            index = (index + 1) % _tips.Length;
        }
    }

    private IEnumerator FadeTip(bool isShowing)
    {
        float elapsed = 0f;
        while (elapsed < _tipFadeDuration)
        {
            float t = elapsed / _tipFadeDuration;
            float visible = isShowing ? t : 1f - t;
            _tipGroup.alpha = visible;
            // 들어올 때는 오른쪽에서, 나갈 때는 왼쪽으로 미끄러진다.
            float direction = isShowing ? 1f : -1f;
            _tipText.rectTransform.anchoredPosition = _tipBasePosition + Vector2.right * direction * _tipSlideOffset * (1f - visible);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _tipGroup.alpha = isShowing ? 1f : 0f;
        _tipText.rectTransform.anchoredPosition = _tipBasePosition;
    }
}
