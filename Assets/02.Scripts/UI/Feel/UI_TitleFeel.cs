using TMPro;
using UnityEngine;

// 제목 글자를 한 글자씩 물결처럼 움직이고, 가끔 고장 난 네온사인처럼 지직거리게 한다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_TitleFeel : MonoBehaviour
{
    [Header("물결")]
    [SerializeField] private float _waveHeight = 6f;
    [SerializeField] private float _waveSpeed = 2.2f;
    [SerializeField] private float _waveCharacterOffset = 0.6f;

    [Header("지직거림")]
    [SerializeField] private float _glitchIntervalMin = 2f;
    [SerializeField] private float _glitchIntervalMax = 5f;
    [SerializeField] private float _glitchDuration = 0.18f;
    [SerializeField] private float _glitchJitter = 10f;
    [SerializeField] private Color _glitchColor = new Color(1f, 0.15f, 0.1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _glitchMinAlpha = 0.25f;

    private TextMeshProUGUI _text;
    private Color32 _baseColor;
    private float _glitchTimer;
    private float _glitchRemaining;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _baseColor = _text.color;
        ScheduleNextGlitch();
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        _glitchTimer -= deltaTime;
        if (_glitchTimer <= 0f)
        {
            _glitchRemaining = _glitchDuration;
            ScheduleNextGlitch();
        }

        bool isGlitching = _glitchRemaining > 0f;
        _glitchRemaining -= deltaTime;

        ApplyVertexEffect(isGlitching);
    }

    private void ScheduleNextGlitch()
    {
        _glitchTimer = Random.Range(_glitchIntervalMin, _glitchIntervalMax);
    }

    private void ApplyVertexEffect(bool isGlitching)
    {
        // 매 프레임 원래 메시를 다시 만든 뒤 글자별로 정점을 옮긴다.
        _text.ForceMeshUpdate();
        TMP_TextInfo textInfo = _text.textInfo;
        float time = Time.unscaledTime;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            Vector3 offset = new Vector3(0f, Mathf.Sin(time * _waveSpeed + i * _waveCharacterOffset) * _waveHeight, 0f);
            Color32 color = _baseColor;

            if (isGlitching)
            {
                offset.x += Random.Range(-_glitchJitter, _glitchJitter);
                Color glitch = Random.value > 0.5f ? _glitchColor : (Color)_baseColor;
                glitch.a = Random.Range(_glitchMinAlpha, 1f);
                color = glitch;
            }

            for (int v = 0; v < 4; v++)
            {
                vertices[vertexIndex + v] += offset;
                colors[vertexIndex + v] = color;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}
