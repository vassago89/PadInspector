namespace PadInspector.Models;

public class InspectionResult
{
    public int Id { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsPass { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Score { get; set; }
    public int PadCount { get; set; }
    public string ImagePath { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Id}] {CameraName} {(IsPass ? "PASS" : "FAIL")} Score={Score}% Pads={PadCount}";
}
