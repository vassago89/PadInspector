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
    private readonly object _lock = new();
    private readonly object _timerLock = new();
    private Timer? _autoTriggerTimer;
    private volatile bool _isRunning;

    public bool IsRunning => _isRunning;

    public VirtualIOService(IOptions<IOSettings> options)
    {
        _settings = options.Value;
        _inputs = new bool[_settings.ChannelCount];
        _outputs = new bool[_settings.ChannelCount];
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

    public void FireTrigger(int channel = 0)
    {
        if (!_isRunning) return;
        if (channel < 0 || channel >= _inputs.Length) return;

        lock (_lock)
        {
            _inputs[channel] = true;
        }

        TriggerReceived?.Invoke(this, new IOSignal
        {
            Channel = channel,
            IsOn = true
        });

        _ = ResetInputAfterDelayAsync(channel);
    }

    private async Task ResetInputAfterDelayAsync(int channel)
    {
        try
        {
            await Task.Delay(_settings.SignalResetDelayMs);
            lock (_lock)
            {
                _inputs[channel] = false;
            }
        }
        catch (ObjectDisposedException)
        {
            // 종료 중 무시
        }
    }

    public void StartAutoTrigger(int intervalMs = 0)
    {
        if (intervalMs <= 0) intervalMs = _settings.DefaultTriggerIntervalMs;
        lock (_timerLock)
        {
            _autoTriggerTimer?.Dispose();
            _autoTriggerTimer = new Timer(_ =>
            {
                foreach (var ch in _settings.AutoTriggerChannels)
                    FireTrigger(ch);
            }, null, 0, intervalMs);
        }
    }

    public void StopAutoTrigger()
    {
        lock (_timerLock)
        {
            _autoTriggerTimer?.Dispose();
            _autoTriggerTimer = null;
        }
    }

    public void SetOutput(int channel, bool value)
    {
        lock (_lock)
        {
            if (channel >= 0 && channel < _outputs.Length)
                _outputs[channel] = value;
        }
    }

    public bool GetInput(int channel)
    {
        lock (_lock)
        {
            return channel >= 0 && channel < _inputs.Length && _inputs[channel];
        }
    }

    public bool GetOutput(int channel)
    {
        lock (_lock)
        {
            return channel >= 0 && channel < _outputs.Length && _outputs[channel];
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
