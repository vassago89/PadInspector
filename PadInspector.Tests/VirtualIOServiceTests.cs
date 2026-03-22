using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class VirtualIOServiceTests
{
    private static VirtualIOService CreateService(int channelCount = 8)
    {
        var settings = Options.Create(new IOSettings
        {
            ChannelCount = channelCount,
            AutoTriggerChannels = [0, 1],
            DefaultTriggerIntervalMs = 2000,
            SignalResetDelayMs = 50
        });
        return new VirtualIOService(settings);
    }

    [Fact]
    public void FireTrigger_WhenRunning_RaisesEvent()
    {
        var svc = CreateService();
        svc.Start();

        IOSignal? received = null;
        svc.TriggerReceived += (_, signal) => received = signal;

        svc.FireTrigger(0);

        Assert.NotNull(received);
        Assert.Equal(0, received.Channel);
        Assert.True(received.IsOn);
    }

    [Fact]
    public void FireTrigger_WhenNotRunning_DoesNotRaiseEvent()
    {
        var svc = CreateService();

        IOSignal? received = null;
        svc.TriggerReceived += (_, signal) => received = signal;

        svc.FireTrigger(0);

        Assert.Null(received);
    }

    [Fact]
    public void FireTrigger_InvalidChannel_DoesNotRaiseEvent()
    {
        var svc = CreateService(channelCount: 4);
        svc.Start();

        IOSignal? received = null;
        svc.TriggerReceived += (_, signal) => received = signal;

        svc.FireTrigger(5); // out of bounds

        Assert.Null(received);
    }

    [Fact]
    public void SetOutput_GetOutput_ReturnsCorrectState()
    {
        var svc = CreateService();

        svc.SetOutput(2, true);
        Assert.True(svc.GetOutput(2));
        Assert.False(svc.GetOutput(3));

        svc.SetOutput(2, false);
        Assert.False(svc.GetOutput(2));
    }

    [Fact]
    public void GetOutput_InvalidChannel_ReturnsFalse()
    {
        var svc = CreateService(channelCount: 4);
        Assert.False(svc.GetOutput(10));
        Assert.False(svc.GetOutput(-1));
    }

    [Fact]
    public void Stop_StopsAutoTrigger()
    {
        var svc = CreateService();
        svc.Start();
        svc.StartAutoTrigger(100);
        svc.Stop();

        Assert.False(svc.IsRunning);
    }
}
