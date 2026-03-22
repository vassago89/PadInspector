using System.Collections.ObjectModel;
using PadInspector.Models;

namespace PadInspector.Services;

public interface IAlarmService
{
    bool IsAlarm { get; }
    string AlarmMessage { get; }
    ObservableCollection<AlarmRecord> AlarmHistory { get; }
    event Action<bool, string>? AlarmStateChanged;
    void CheckResult(string cameraName, bool isPass);
    void Clear();
    void Reset();
}
