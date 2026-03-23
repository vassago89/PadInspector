namespace PadInspector.Configs;

public class CameraConfig
{
    public string Name { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string PixelFormat { get; set; } = "Mono8";
    public string TriggerMode { get; set; } = "On";
    public string TriggerSource { get; set; } = "Line0";
    public string TriggerActivation { get; set; } = "RisingEdge";
    public int GrabTimeoutMs { get; set; } = 1000;
}

public class CamerasSettings
{
    public CameraConfig Camera1 { get; set; } = new() { Name = "Camera1" };
    public CameraConfig Camera2 { get; set; } = new() { Name = "Camera2" };
}
