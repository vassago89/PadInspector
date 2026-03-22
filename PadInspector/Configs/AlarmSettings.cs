namespace PadInspector.Configs;

public class AlarmSettings
{
    public bool Enabled { get; set; } = true;
    public int ConsecutiveNgThreshold { get; set; } = 5;
}
