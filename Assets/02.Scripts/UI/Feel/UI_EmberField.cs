using UnityEngine;
using UnityEngine.UI;

// 화면 아래에서 위로 흔들리며 떠오르는 불티를 UI 이미지로 그린다.
// 불티는 처음에 한 번만 만들고, 화면 밖으로 나가면 아래에서 다시 사용한다.
[RequireComponent(typeof(RectTransform))]
public class UI_EmberField : MonoBehaviour
{
    [SerializeField] private Sprite _emberSprite;
    [SerializeField] private int _emberCount = 45;

    [Header("크기와 속도")]
    [SerializeField] private Vector2 _sizeRange = new Vector2(3f, 9f);
    [SerializeField] private Vector2 _riseSpeedRange = new Vector2(25f, 80f);
    [SerializeField] private Vector2 _swayAmountRange = new Vector2(10f, 40f);
    [SerializeField] private Vector2 _swaySpeedRange = new Vector2(0.4f, 1.5f);

    [Header("색")]
    [SerializeField] private Color _hotColor = new Color(1f, 0.75f, 0.35f, 1f);
    [SerializeField] private Color _coolColor = new Color(0.8f, 0.12f, 0.05f, 1f);
    [SerializeField] private float _flickerSpeed = 9f;

    private struct Ember
    {
        public RectTransform Rect;
        public Image Image;
        public float BaseX;
        public float Y;
        public float RiseSpeed;
        public float SwayAmount;
        public float SwaySpeed;
        public float Seed;
    }

    private RectTransform _field;
    private Ember[] _embers;

    private void Awake()
    {
        _field = (RectTransform)transform;
        _embers = new Ember[_emberCount];

        for (int i = 0; i < _emberCount; i++)
        {
            GameObject go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            Image image = go.GetComponent<Image>();
            image.sprite = _emberSprite;
            image.raycastTarget = false;

            _embers[i].Rect = (RectTransform)go.transform;
            _embers[i].Image = image;
            // 시작할 때는 화면 전체에 흩어 두어 처음부터 불티가 떠 있게 한다.
            Respawn(ref _embers[i], Random.Range(0f, 1f));
        }
    }

    private void Update()
    {
        Rect rect = _field.rect;
        float deltaTime = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        for (int i = 0; i < _embers.Length; i++)
        {
            ref Ember ember = ref _embers[i];
            ember.Y += ember.RiseSpeed * deltaTime;

            float normalizedHeight = ember.Y / rect.height;
            if (normalizedHeight > 1f)
            {
                Respawn(ref ember, 0f);
                continue;
            }

            float x = ember.BaseX + Mathf.Sin(time * ember.SwaySpeed + ember.Seed) * ember.SwayAmount;
            ember.Rect.anchoredPosition = new Vector2(rect.xMin + x, rect.yMin + ember.Y);

            // 올라갈수록 식으며 붉어지고, 위쪽에서 서서히 사라진다.
            Color color = Color.Lerp(_hotColor, _coolColor, normalizedHeight);
            float flicker = 0.6f + 0.4f * Mathf.PerlinNoise(ember.Seed, time * _flickerSpeed);
            color.a = flicker * (1f - normalizedHeight);
            ember.Image.color = color;
        }
    }

    private void Respawn(ref Ember ember, float normalizedHeight)
    {
        Rect rect = _field.rect;
        float size = Random.Range(_sizeRange.x, _sizeRange.y);

        ember.Rect.anchorMin = ember.Rect.anchorMax = new Vector2(0.5f, 0.5f);
        ember.Rect.sizeDelta = new Vector2(size, size);
        ember.BaseX = Random.Range(0f, rect.width);
        ember.Y = normalizedHeight * rect.height;
        ember.RiseSpeed = Random.Range(_riseSpeedRange.x, _riseSpeedRange.y);
        ember.SwayAmount = Random.Range(_swayAmountRange.x, _swayAmountRange.y);
        ember.SwaySpeed = Random.Range(_swaySpeedRange.x, _swaySpeedRange.y);
        ember.Seed = Random.Range(0f, 100f);
        ember.Image.color = Color.clear;
    }
}
