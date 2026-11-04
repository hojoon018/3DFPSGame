using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_Login : MonoBehaviour
{
    private enum Mode
    {
        Login,
        Register,
    }

    [Header("입력 칸")]
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;

    [Header("버튼")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _gotoRegisterButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _gotoLoginButton;

    [Header("기타")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private UI_LoginScreen _loginScreen;

    // 계정 규칙
    // ID: 이메일을 사용한 3~20 글자
    //   (?=.{3,20}$)                  전체 길이 3~20
    //   [A-Za-z0-9_+-]+(\.[...]+)*    로컬 부분 ('.'으로 시작/끝나거나 '..' 불가)
    //   @[A-Za-z0-9-]+(\.[...]+)+     '@' 뒤 도메인, '.'으로 구분된 부분이 하나 이상
    private static readonly Regex IdRegex = new Regex(
        @"^(?=.{3,20}$)[A-Za-z0-9_+-]+(\.[A-Za-z0-9_+-]+)*@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$");

    // 비밀번호: 대문자 소문자 숫자 기호를 포함한 8~30 글자
    //   (?=.*[A-Z]) (?=.*[a-z]) (?=.*[0-9]) (?=.*[^A-Za-z0-9])   각각 하나 이상 포함
    //   [!-~]{8,30}                   공백을 제외한 ASCII 출력 문자(영문/숫자/기호)만 8~30 글자
    private static readonly Regex PasswordRegex = new Regex(
        @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{8,30}$");

    private Mode _mode = Mode.Login;

    private PlayerPrefsAccountRepository _repository = new PlayerPrefsAccountRepository();

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool isLoginMode = _mode == Mode.Login;

        _passwordConfirmInput.gameObject.SetActive(!isLoginMode);

        _loginButton.gameObject.SetActive(isLoginMode);
        _gotoRegisterButton.gameObject.SetActive(isLoginMode);

        _registerButton.gameObject.SetActive(!isLoginMode);
        _gotoLoginButton.gameObject.SetActive(!isLoginMode);
    }

    // 로그인 모드로
    public void GotoLogin()
    {
        _mode = Mode.Login;
        _messageText.text = string.Empty;
        Refresh();
    }

    // 회원가입 모드로
    public void GotoRegister()
    {
        _mode = Mode.Register;
        _messageText.text = string.Empty;
        Refresh();
    }

    public void Login()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호가 입력해주세요.";
            return;
        }

        if (_repository.CheckPassword(id, password))
        {
            // 검증 통과 시 로그인 연출 후 로딩 씬으로 넘어간다.
            _loginScreen.PlayLoginTransition();
        }
        else
        {
            _messageText.text = "아이디와 비밀번호를 확인해주세요.";
            return;
        }
    }

    public void Register()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        // 정규표현식(정규식)
        // 문자열에서 특정한 규칙이나 패턴을 찾기 위해 사용하는 문법
        // - 데이터 검증(이메일 형식, 주민번호 형식, 비밀번호 형식)
        // - 문자열 검색(ToDo라는게 있냐?, 한국어가 있냐?)
        // - 문자열 치환(규칙에 맞는 문자열을 찾아서 다른 문자열로 변경)
        // - 웹 크롤링 & 파싱


        // 규칙이 여기서만 쓰일까?

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해주세요.";
            return;
        }

        if (!IdRegex.IsMatch(id))
        {
            _messageText.text = "아이디는 3~20글자의 이메일 형식이어야 합니다.";
            return;
        }

        if (!PasswordRegex.IsMatch(password))
        {
            _messageText.text = "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자여야 합니다.";
            return;
        }

        if (password != passwordConfirm)
        {
            _messageText.text = "비밀번호 확인이 맞지 않습니다.";
            return;
        }

        // Todo. 중복 ID 검사
        if (_repository.Exists(id))
        {
            _messageText.text = "이미 존재하는 아이디입니다.";
            return;
        }

        // Todo. 실제 회원가입 데이터 저장
        _repository.Register(id, password);

        // PlayerPrefs

        GotoLogin();
        _messageText.text = "가입했습니다. 로그인 해주세요.";
    }
}