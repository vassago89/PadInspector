using PadInspector.Models;

namespace PadInspector.Services;

public interface IIOService : IDisposable
{
    event EventHandler<IOSignal>? TriggerReceived;

    bool IsRunning { get; }
    void Start();
    void Stop();
    void SetOutput(int channel, bool value);
    bool GetOutput(int channel);
    bool GetInput(int channel);
    void FireTrigger(int channel = 0);
    void StartAutoTrigger(int intervalMs = 2000);
    void StopAutoTrigger();
}
