namespace PadInspector.Configs;

public class CameraSettings
{
    public string PixelFormat { get; set; } = "Mono8";
    public string TriggerMode { get; set; } = "On";
    public string TriggerSource { get; set; } = "Line0";
    public int GrabTimeoutMs { get; set; } = 1000;
    public int SingleGrabTimeoutMs { get; set; } = 3000;
}
