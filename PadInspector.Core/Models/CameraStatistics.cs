namespace PadInspector.Models;

public class CameraStatistics
{
    public string CameraName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public double PassRate => TotalCount > 0 ? Math.Round((double)PassCount / TotalCount * 100, 1) : 0;

    public void AddResult(bool isPass)
    {
        TotalCount++;
        if (isPass) PassCount++;
        else FailCount++;
    }

    public void Reset()
    {
        TotalCount = 0;
        PassCount = 0;
        FailCount = 0;
    }
}
