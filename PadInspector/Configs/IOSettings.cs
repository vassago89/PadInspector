namespace PadInspector.Configs;

public class IOSettings
{
    public int ChannelCount { get; set; } = 8;
    public int DefaultTriggerChannel { get; set; } = 0;
    public int DefaultTriggerIntervalMs { get; set; } = 2000;
    public int SignalResetDelayMs { get; set; } = 50;
}
