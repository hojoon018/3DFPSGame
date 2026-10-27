using UnityEngine;
using TMPro;

public class UI_WaveInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _waveText;
    [SerializeField] private TextMeshProUGUI _remainingText;
    [SerializeField] private TextMeshProUGUI _aliveText;

    private void Awake()
    {
        // 첫 알림이 오기 전(준비 화면)에는 빈 값을 보여 준다
        _waveText.text = "WAVE -";
        _remainingText.text = string.Empty;
        _aliveText.text = string.Empty;
    }

    private void ShowWave(int waveNumber)
    {
        _waveText.text = $"WAVE {waveNumber}";
    }

    private void ShowStatus(WaveStatus status)
    {
        _remainingText.text = $"남은 좀비 {status.RemainingCount} / {status.WaveTotalCount}";
        _aliveText.text = $"필드 {status.AliveCount} / {status.MaxAliveCount}";
    }
}
