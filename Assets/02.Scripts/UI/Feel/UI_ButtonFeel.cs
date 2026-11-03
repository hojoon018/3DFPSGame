using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼에 스프링 기반 크기 반응(hover 확대, press 압축, release 튕김)과 색 강조, 효과음을 준다.
// 색은 직접 바꾸므로 Button의 Transition은 None으로 둔다.
[RequireComponent(typeof(Button))]
public class UI_ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // 프레임이 크게 튀어도 스프링이 발산하지 않도록 한 번에 적분할 최대 시간
    private const float MaxDeltaTime = 1f / 30f;

    [Header("크기 반응")]
    [SerializeField] private float _hoverScale = 1.06f;
    [SerializeField] private float _pressScale = 0.92f;
    [SerializeField] private float _releaseKick = 3f;
    [SerializeField] private float _stiffness = 420f;
    [SerializeField] private float _damping = 16f;

    [Header("색 강조")]
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private Color _normalBackgroundColor = Color.white;
    [SerializeField] private Color _hoverBackgroundColor = new Color(0.72f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color _normalLabelColor = new Color(0.196f, 0.196f, 0.196f, 1f);
    [SerializeField] private Color _hoverLabelColor = Color.white;
    [SerializeField] private float _colorSpeed = 12f;

    [Header("효과음")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hoverClip;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField, Range(0f, 1f)] private float _hoverVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _clickVolume = 0.8f;

    private Button _button;
    private float _scale = 1f;
    private float _velocity;
    private float _highlight;
    private bool _isHovered;
    private bool _isPressed;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        // 팝업이 닫히는 등 비활성화될 때 눌린 상태로 굳지 않도록 초기화한다.
        _isHovered = false;
        _isPressed = false;
        _scale = 1f;
        _velocity = 0f;
        _highlight = 0f;
        transform.localScale = Vector3.one;
        ApplyColor();
    }

    private void Update()
    {
        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime);

        float targetScale = _isPressed ? _pressScale : (_isHovered ? _hoverScale : 1f);
        float acceleration = (targetScale - _scale) * _stiffness - _velocity * _damping;
        _velocity += acceleration * deltaTime;
        _scale += _velocity * deltaTime;
        transform.localScale = Vector3.one * _scale;

        float targetHighlight = _isHovered && _button.interactable ? 1f : 0f;
        _highlight = Mathf.MoveTowards(_highlight, targetHighlight, _colorSpeed * deltaTime);
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_background != null)
        {
            _background.color = Color.Lerp(_normalBackgroundColor, _hoverBackgroundColor, _highlight);
        }

        if (_label != null)
        {
            _label.color = Color.Lerp(_normalLabelColor, _hoverLabelColor, _highlight);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        _isHovered = true;
        PlaySound(_hoverClip, _hoverVolume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable || eventData.button != PointerEventData.InputButton.Left) return;

        _isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed) return;

        _isPressed = false;
        if (!_isHovered) return;

        // 뗄 때 위로 튀어 오르는 속도를 더해 탄성 있게 돌아오게 한다.
        _velocity += _releaseKick;
        PlaySound(_clickClip, _clickVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null) return;

        _audioSource.PlayOneShot(clip, volume);
    }
}
