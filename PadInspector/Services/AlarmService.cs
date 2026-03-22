using Microsoft.Extensions.Options;
using PadInspector.Configs;

namespace PadInspector.Services;

public class AlarmService : IAlarmService
{
    private readonly AlarmSettings _settings;
    private readonly ILogService _logService;
    private readonly Dictionary<string, int> _consecutiveNgCounts = new();

    public bool IsAlarm { get; private set; }
    public string AlarmMessage { get; private set; } = "";

    public event Action<bool, string>? AlarmStateChanged;

    public AlarmService(IOptions<AlarmSettings> alarmOptions, ILogService logService)
    {
        _settings = alarmOptions.Value;
        _logService = logService;
    }

    public void CheckResult(string cameraName, bool isPass)
    {
        if (!_settings.Enabled) return;

        _consecutiveNgCounts.TryAdd(cameraName, 0);

        if (isPass)
        {
            _consecutiveNgCounts[cameraName] = 0;
        }
        else
        {
            _consecutiveNgCounts[cameraName]++;
            if (_consecutiveNgCounts[cameraName] >= _settings.ConsecutiveNgThreshold)
            {
                IsAlarm = true;
                AlarmMessage = $"[{cameraName}] 연속 NG {_consecutiveNgCounts[cameraName]}회 발생!";
                _logService.Log("ALARM", AlarmMessage);
                AlarmStateChanged?.Invoke(IsAlarm, AlarmMessage);
            }
        }
    }

    public void Clear()
    {
        IsAlarm = false;
        AlarmMessage = "";
        foreach (var key in _consecutiveNgCounts.Keys)
            _consecutiveNgCounts[key] = 0;
        AlarmStateChanged?.Invoke(false, "");
    }

    public void Reset() => Clear();
}
