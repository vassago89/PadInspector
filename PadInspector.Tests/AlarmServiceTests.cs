using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

public class AlarmServiceTests
{
    private static AlarmService CreateService(int threshold = 3, bool enabled = true)
    {
        var settings = Options.Create(new AlarmSettings
        {
            Enabled = enabled,
            ConsecutiveNgThreshold = threshold
        });
        var log = TestHelper.CreateLogService();
        return new AlarmService(settings, log);
    }

    [Fact]
    public void ConsecutiveNg_ReachesThreshold_TriggersAlarm()
    {
        var svc = CreateService(threshold: 3);

        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);
        Assert.False(svc.IsAlarm);

        svc.CheckResult("CAM1", false);
        Assert.True(svc.IsAlarm);
        Assert.Contains("CAM1", svc.AlarmMessage);
    }

    [Fact]
    public void PassResult_ResetsConsecutiveCount()
    {
        var svc = CreateService(threshold: 3);

        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", true); // reset
        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);

        Assert.False(svc.IsAlarm);
    }

    [Fact]
    public void Clear_ResetsAlarmState()
    {
        var svc = CreateService(threshold: 1);
        svc.CheckResult("CAM1", false);
        Assert.True(svc.IsAlarm);

        svc.Clear();
        Assert.False(svc.IsAlarm);
        Assert.Equal("", svc.AlarmMessage);
    }

    [Fact]
    public void Disabled_DoesNotTrigger()
    {
        var svc = CreateService(threshold: 1, enabled: false);
        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);
        Assert.False(svc.IsAlarm);
    }

    [Fact]
    public void MultipleCameras_TrackSeparately()
    {
        var svc = CreateService(threshold: 2);
        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM2", false);
        Assert.False(svc.IsAlarm);

        svc.CheckResult("CAM1", false); // CAM1: 2 consecutive
        Assert.True(svc.IsAlarm);
    }

    [Fact]
    public void AlarmStateChanged_EventFires()
    {
        var svc = CreateService(threshold: 1);
        bool eventFired = false;
        svc.AlarmStateChanged += (isAlarm, msg) => eventFired = true;

        svc.CheckResult("CAM1", false);
        Assert.True(eventFired);
    }
}
