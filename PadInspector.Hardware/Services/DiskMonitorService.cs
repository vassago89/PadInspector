using System.IO;
using Microsoft.Extensions.Options;
using PadInspector.Configs;

namespace PadInspector.Services;

public class DiskMonitorService : IDiskMonitorService
{
    private const long LowDiskThresholdMB = 1024; // 1GB
    private readonly ILogService _logService;
    private readonly string _monitorPath;
    private readonly object _timerLock = new();
    private Timer? _timer;
    private bool _alerted;

    public long FreeSpaceBytes { get; private set; }
    public long FreeSpaceMB => FreeSpaceBytes / (1024 * 1024);
    public bool IsLowDiskSpace { get; private set; }

    public event Action<long>? DiskSpaceLow;

    public DiskMonitorService(IOptions<ImageSaveSettings> imageOptions, ILogService logService)
    {
        _logService = logService;
        var basePath = imageOptions.Value.BasePath;
        _monitorPath = Path.IsPathRooted(basePath)
            ? basePath
            : AppDomain.CurrentDomain.BaseDirectory;
    }

    public void Start()
    {
        lock (_timerLock)
        {
            _timer?.Dispose();
            CheckDiskSpace();
            _timer = new Timer(_ => CheckDiskSpace(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
    }

    public void Stop()
    {
        lock (_timerLock)
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void CheckDiskSpace()
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_monitorPath) ?? "C");
            FreeSpaceBytes = driveInfo.AvailableFreeSpace;
            IsLowDiskSpace = FreeSpaceMB < LowDiskThresholdMB;

            if (IsLowDiskSpace && !_alerted)
            {
                _alerted = true;
                _logService.Log("WARN", $"디스크 공간 부족! 남은 공간: {FreeSpaceMB}MB ({driveInfo.Name})");
                DiskSpaceLow?.Invoke(FreeSpaceMB);
            }
            else if (!IsLowDiskSpace)
            {
                _alerted = false;
            }
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"디스크 공간 확인 실패: {ex.Message}");
        }
    }

    public void Dispose()
    {
        lock (_timerLock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
