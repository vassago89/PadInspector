namespace PadInspector.Models;

public class AlarmRecord
{
    public DateTime Timestamp { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public int ConsecutiveNgCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ClearedAt { get; set; }
    public TimeSpan? Duration => ClearedAt.HasValue ? ClearedAt.Value - Timestamp : null;
}
