namespace PadInspector.Services;

public interface IAlarmService
{
    bool IsAlarm { get; }
    string AlarmMessage { get; }
    event Action<bool, string>? AlarmStateChanged;
    void CheckResult(string cameraName, bool isPass);
    void Clear();
    void Reset();
}
