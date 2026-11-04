using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인 화면의 시작 연출과 입력을 담당한다.
// 계정 검증은 UI_Login이 하며, 검증을 통과하면 PlayLoginTransition()으로 연출 후 로딩 씬으로 넘어간다.
public class UI_LoginScreen : MonoBehaviour
{
    [SerializeField] private string _loadingSceneName = "LoadingScene";
    [SerializeField] private UI_Login _login;

    [Header("참조")]
    [SerializeField] private UI_ScreenImpact _impact;
    [SerializeField] private CanvasGroup _fade;
    [SerializeField] private RectTransform _title;
    [SerializeField] private CanvasGroup _titleGroup;
    [SerializeField] private RectTransform _loginPanel;
    [SerializeField] private CanvasGroup _loginPanelGroup;
    [SerializeField] private RectTransform[] _panelItems;
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;

    [Header("시작 연출")]
    [SerializeField] private float _fadeInDuration = 0.8f;
    [SerializeField] private float _titleDropDuration = 0.22f;
    [SerializeField] private float _titleStartScale = 2.6f;
    [SerializeField] private float _titleShakeDuration = 0.4f;
    [SerializeField] private float _titleShakeStrength = 22f;
    [SerializeField] private Color _titleFlashColor = new Color(0.8f, 0.05f, 0.05f, 0.45f);
    [SerializeField] private float _titleFlashDuration = 0.35f;
    [SerializeField] private float _panelRiseDuration = 0.45f;
    [SerializeField] private float _panelRiseOffset = 90f;
    [SerializeField] private float _itemPopDuration = 0.25f;
    [SerializeField] private float _itemInterval = 0.06f;

    [Header("로그인 연출")]
    [SerializeField] private Color _loginFlashColor = new Color(1f, 0.9f, 0.8f, 0.6f);
    [SerializeField] private float _loginFlashDuration = 0.25f;
    [SerializeField] private float _loginShakeStrength = 14f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    [Header("효과음")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _titleSlamClip;
    [SerializeField] private AudioClip _loginClip;
    [SerializeField, Range(0f, 1f)] private float _titleSlamVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _loginVolume = 0.6f;

    private bool _isReady;
    private bool _isLoggingIn;

    private void Start()
    {
        StartCoroutine(Intro_Coroutine());
    }

    private void Update()
    {
        if (!_isReady || _isLoggingIn) return;

        // 비밀번호 확인 칸이 보이면 회원가입 모드로 본다.
        bool isRegisterMode = _passwordConfirmInput.gameObject.activeInHierarchy;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GetNextInput(isRegisterMode).Select();
        }
        else if (!isRegisterMode && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            _login.Login();
        }
    }

    // 아이디 → 비밀번호 → (회원가입 모드면) 비밀번호 확인 → 아이디 순으로 순환한다.
    private TMP_InputField GetNextInput(bool isRegisterMode)
    {
        if (_idInput.isFocused) return _passwordInput;
        if (_passwordInput.isFocused && isRegisterMode) return _passwordConfirmInput;

        return _idInput;
    }

    private IEnumerator Intro_Coroutine()
    {
        _fade.alpha = 1f;
        _titleGroup.alpha = 0f;
        _loginPanelGroup.alpha = 0f;
        _loginPanelGroup.interactable = false;
        foreach (RectTransform item in _panelItems)
        {
            item.localScale = Vector3.zero;
        }

        Vector2 panelPosition = _loginPanel.anchoredPosition;

        // 1. 검은 화면이 걷힌다.
        yield return Animate(_fadeInDuration, t => _fade.alpha = 1f - t);
        _fade.blocksRaycasts = false;

        // 2. 제목이 크게 내려꽂힌다.
        yield return Animate(_titleDropDuration, t =>
        {
            float eased = t * t;
            _title.localScale = Vector3.one * Mathf.Lerp(_titleStartScale, 1f, eased);
            _titleGroup.alpha = t;
        });
        PlaySound(_titleSlamClip, _titleSlamVolume);
        _impact.Shake(_titleShakeDuration, _titleShakeStrength);
        _impact.Flash(_titleFlashColor, _titleFlashDuration);

        // 3. 로그인 패널이 아래에서 올라오고, 입력칸과 버튼이 차례로 튀어나온다.
        yield return Animate(_panelRiseDuration, t =>
        {
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            _loginPanel.anchoredPosition = panelPosition + Vector2.down * _panelRiseOffset * (1f - eased);
            _loginPanelGroup.alpha = t;
        });

        for (int i = 0; i < _panelItems.Length; i++)
        {
            StartCoroutine(PopItem_Coroutine(_panelItems[i], i * _itemInterval));
        }

        yield return new WaitForSecondsRealtime(_panelItems.Length * _itemInterval + _itemPopDuration);

        _loginPanelGroup.interactable = true;
        _isReady = true;
        _idInput.Select();
    }

    private IEnumerator PopItem_Coroutine(RectTransform item, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        yield return Animate(_itemPopDuration, t =>
        {
            // 살짝 크게 튀어나왔다가 제자리로 돌아온다.
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.15f;
            item.localScale = Vector3.one * (t + overshoot);
        });
        item.localScale = Vector3.one;
    }

    // 로그인 검증 통과 후 UI_Login이 호출한다.
    public void PlayLoginTransition()
    {
        if (!_isReady || _isLoggingIn) return;

        _isLoggingIn = true;
        StartCoroutine(Login_Coroutine());
    }

    private IEnumerator Login_Coroutine()
    {
        _loginPanelGroup.interactable = false;
        _fade.blocksRaycasts = true;

        PlaySound(_loginClip, _loginVolume);
        _impact.Flash(_loginFlashColor, _loginFlashDuration);
        _impact.Shake(_loginFlashDuration, _loginShakeStrength);

        yield return Animate(_fadeOutDuration, t => _fade.alpha = t);

        SceneLoader.Load("GameScene");
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null) return;

        _audioSource.PlayOneShot(clip, volume);
    }

    // 0~1로 진행하는 t를 duration 동안 매 프레임 전달한다.
    private static IEnumerator Animate(float duration, System.Action<float> step)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            step(elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        step(1f);
    }
}
