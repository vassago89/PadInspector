using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

public class AlarmHistoryTests
{
    private static AlarmService CreateService(int threshold = 3)
    {
        var settings = Options.Create(new AlarmSettings
        {
            Enabled = true,
            ConsecutiveNgThreshold = threshold
        });
        return new AlarmService(settings, new FakeLogService());
    }

    [Fact]
    public void AlarmTriggered_AddsToHistory()
    {
        var svc = CreateService(threshold: 2);

        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);

        Assert.Single(svc.AlarmHistory);
        Assert.Equal("CAM1", svc.AlarmHistory[0].CameraName);
        Assert.Equal(2, svc.AlarmHistory[0].ConsecutiveNgCount);
    }

    [Fact]
    public void Clear_SetsClearedTimestamp()
    {
        var svc = CreateService(threshold: 1);
        svc.CheckResult("CAM1", false);
        Assert.Null(svc.AlarmHistory[0].ClearedAt);

        svc.Clear();
        Assert.NotNull(svc.AlarmHistory[0].ClearedAt);
    }

    [Fact]
    public void Reset_ClearsHistory()
    {
        var svc = CreateService(threshold: 1);
        svc.CheckResult("CAM1", false);
        Assert.Single(svc.AlarmHistory);

        svc.Reset();
        Assert.Empty(svc.AlarmHistory);
    }

    [Fact]
    public void MultipleAlarms_TracksSeparately()
    {
        var svc = CreateService(threshold: 1);

        svc.CheckResult("CAM1", false);
        svc.Clear();
        svc.CheckResult("CAM2", false);

        Assert.Equal(2, svc.AlarmHistory.Count);
        Assert.Equal("CAM2", svc.AlarmHistory[0].CameraName);
        Assert.Equal("CAM1", svc.AlarmHistory[1].CameraName);
    }
}
