using UnityEngine;

public interface IWaveObserver
{
    void OnWaveStarted(int waveNumber);
    void OnWaveStatusChanged(WaveStatus status);
}
