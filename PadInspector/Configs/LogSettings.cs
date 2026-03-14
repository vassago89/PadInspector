namespace PadInspector.Configs;

public class LogSettings
{
    public int MaxLogLines { get; set; } = 500;
    public string LogDirectory { get; set; } = "Logs";
    public bool EnableFileLog { get; set; } = false;
}
