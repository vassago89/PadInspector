using PadInspector.Models;

namespace PadInspector.Services;

public interface IIOService : IDisposable
{
    event EventHandler<IOSignal>? TriggerReceived;

    bool IsRunning { get; }
    void Start();
    void Stop();
    void SetOutput(int channel, bool value);
    void FireTrigger(int channel = 0);
    void StartAutoTrigger(int intervalMs = 2000);
    void StopAutoTrigger();
}
