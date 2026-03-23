namespace PadInspector.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FormattedMessage => $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message}";
}
