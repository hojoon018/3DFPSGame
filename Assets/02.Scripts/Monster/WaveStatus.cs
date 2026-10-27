public struct WaveStatus
{
    public int RemainingCount; // 이번 웨이브에서 아직 처치하지 않은 수
    public int WaveTotalCount; // 이번 웨이브 총 수
    public int AliveCount; // 지금 살아 있는 수
    public int MaxAliveCount; // 동시에 살아 있을 수 있는 최대 수
}