using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

/// <summary>
/// 가상 IO 서비스 - 실제 하드웨어 없이 IO 신호를 시뮬레이션
/// </summary>
public class VirtualIOService : IIOService
{
    public event EventHandler<IOSignal>? TriggerReceived;

    private readonly IOSettings _settings;
    private readonly bool[] _inputs;
    private readonly bool[] _outputs;
    private Timer? _autoTriggerTimer;
    private bool _isRunning;
    private bool _autoTriggerEnabled;
    private int _triggerIntervalMs;

    public bool IsRunning => _isRunning;
    public bool AutoTriggerEnabled => _autoTriggerEnabled;
    public int TriggerIntervalMs => _triggerIntervalMs;

    public VirtualIOService(IOptions<IOSettings> options)
    {
        _settings = options.Value;
        _inputs = new bool[_settings.ChannelCount];
        _outputs = new bool[_settings.ChannelCount];
        _triggerIntervalMs = _settings.DefaultTriggerIntervalMs;
    }

    public void Start()
    {
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
        StopAutoTrigger();
    }

    /// <summary>
    /// 수동으로 트리거 신호 발생
    /// </summary>
    public void FireTrigger(int channel = 0)
    {
        if (!_isRunning) return;

        _inputs[channel] = true;
        var signal = new IOSignal
        {
            Channel = channel,
            IsOn = true,
            Name = $"TRIGGER_IN_{channel}",
            Timestamp = DateTime.Now
        };
        TriggerReceived?.Invoke(this, signal);

        // 설정된 딜레이 후 신호 리셋
        Task.Delay(_settings.SignalResetDelayMs).ContinueWith(_ => _inputs[channel] = false);
    }

    /// <summary>
    /// 자동 트리거 시작 (주기적으로 신호 발생)
    /// </summary>
    public void StartAutoTrigger(int intervalMs = 0)
    {
        if (intervalMs <= 0) intervalMs = _settings.DefaultTriggerIntervalMs;
        _triggerIntervalMs = intervalMs;
        _autoTriggerEnabled = true;
        _autoTriggerTimer = new Timer(_ => FireTrigger(_settings.DefaultTriggerChannel), null, 0, intervalMs);
    }

    /// <summary>
    /// 자동 트리거 정지
    /// </summary>
    public void StopAutoTrigger()
    {
        _autoTriggerEnabled = false;
        _autoTriggerTimer?.Dispose();
        _autoTriggerTimer = null;
    }

    public void SetOutput(int channel, bool value)
    {
        if (channel >= 0 && channel < _outputs.Length)
        {
            _outputs[channel] = value;
        }
    }

    public bool GetInput(int channel) => channel >= 0 && channel < _inputs.Length && _inputs[channel];
    public bool GetOutput(int channel) => channel >= 0 && channel < _outputs.Length && _outputs[channel];

    public void Dispose()
    {
        Stop();
        _autoTriggerTimer?.Dispose();
    }
}
