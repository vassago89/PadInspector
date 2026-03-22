namespace PadInspector.Configs;

public class IOSettings
{
    public int ChannelCount { get; set; } = 8;
    public List<int> AutoTriggerChannels { get; set; } = [0, 1];
    public int DefaultTriggerIntervalMs { get; set; } = 2000;
    public int SignalResetDelayMs { get; set; } = 50;

    // 카메라별 출력 채널 매핑
    public int Camera1PassChannel { get; set; } = 0;
    public int Camera1FailChannel { get; set; } = 1;
    public int Camera2PassChannel { get; set; } = 2;
    public int Camera2FailChannel { get; set; } = 3;
    public int OutputPulseMs { get; set; } = 200;
}
