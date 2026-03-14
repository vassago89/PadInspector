using OpenCvSharp;

namespace PadInspector.Services;

public interface ICameraService : IDisposable
{
    event EventHandler<Mat>? ImageGrabbed;

    bool IsConnected { get; }
    bool IsGrabbing { get; }

    Task<bool> ConnectAsync();
    void Disconnect();
    Task StartGrabAsync();
    void StopGrab();
    Task<Mat?> GrabSingleAsync();
}
