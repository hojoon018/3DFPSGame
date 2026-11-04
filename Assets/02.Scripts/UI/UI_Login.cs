using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
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
    [SerializeField] private string _gameSceneName = "GameScene";

    private Mode _mode = Mode.Login;

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

    private void Login()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호가 입력해주세요.";
            return;
        }

        SceneManager.LoadScene("GameScene");
    }

    public void Register()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        // Todo. ID와 비밀번호 규칙
        // ID: 이메일
        // 비밀번호: 영어만 되며 특수문자 대문자 하나 이상을 포함한 7~15글자
        // 규칙이 여기서만 쓰일까?

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해주세요.";
            return;
        }

        if (password != passwordConfirm)
        {
            _messageText.text = "비밀번호 확인이 맞지 않습니다.";
            return;
        }

        // Todo. 중복 ID 검사
        // Todo. 실제 회원가입 데이터 저장

        GotoLogin();
        _messageText.text = "가입했습니다. 로그인 해주세요.";
    }
}