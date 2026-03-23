using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

public class AlarmService : IAlarmService
{
    private readonly AlarmSettings _settings;
    private readonly ILogService _logService;
    private readonly Dictionary<string, int> _consecutiveNgCounts = new();

    public bool IsAlarm { get; private set; }
    public string AlarmMessage { get; private set; } = "";
    public ObservableCollection<AlarmRecord> AlarmHistory { get; } = new();

    public event Action<bool, string>? AlarmStateChanged;

    public AlarmService(IOptions<AlarmSettings> alarmOptions, ILogService logService)
    {
        _settings = alarmOptions.Value;
        _logService = logService;
    }

    public void CheckResult(string cameraName, bool isPass)
    {
        if (!_settings.Enabled || _settings.ConsecutiveNgThreshold <= 0) return;

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

                AlarmHistory.Insert(0, new AlarmRecord
                {
                    Timestamp = DateTime.Now,
                    CameraName = cameraName,
                    ConsecutiveNgCount = _consecutiveNgCounts[cameraName],
                    Message = AlarmMessage
                });

                // 최대 100개 이력 유지
                if (AlarmHistory.Count > 100)
                    AlarmHistory.RemoveAt(AlarmHistory.Count - 1);

                AlarmStateChanged?.Invoke(IsAlarm, AlarmMessage);
            }
        }
    }

    public void Clear()
    {
        if (IsAlarm && AlarmHistory.Count > 0)
        {
            // 현재 알람에 해제 시간 기록
            AlarmHistory[0].ClearedAt = DateTime.Now;
        }

        IsAlarm = false;
        AlarmMessage = "";
        foreach (var key in _consecutiveNgCounts.Keys)
            _consecutiveNgCounts[key] = 0;
        AlarmStateChanged?.Invoke(false, "");
    }

    public void Reset()
    {
        Clear();
        AlarmHistory.Clear();
    }
}
