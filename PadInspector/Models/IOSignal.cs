namespace PadInspector.Models;

public class IOSignal
{
    public int Channel { get; set; }
    public bool IsOn { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
