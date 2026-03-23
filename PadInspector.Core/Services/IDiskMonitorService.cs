namespace PadInspector.Services;

public interface IDiskMonitorService : IDisposable
{
    long FreeSpaceBytes { get; }
    long FreeSpaceMB { get; }
    bool IsLowDiskSpace { get; }
    event Action<long>? DiskSpaceLow;
    void Start();
    void Stop();
}
