using OpenCvSharp;

namespace PadInspector.Services;

public interface ICameraService : IDisposable
{
    event EventHandler<Mat>? ImageGrabbed;

    string Name { get; }
    bool IsConnected { get; }
    bool IsGrabbing { get; }

    Task<bool> ConnectAsync();
    void Disconnect();
    Task StartGrabAsync();
    void StopGrab();
    void SetExposure(double exposureUs);
    void SetGain(double gainDb);
}
