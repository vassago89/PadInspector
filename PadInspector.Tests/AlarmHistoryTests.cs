using PadInspector.Services;

namespace PadInspector.Tests;

public class AlarmHistoryTests
{
    [Fact]
    public void AlarmTriggered_AddsToHistory()
    {
        var svc = TestHelper.CreateAlarmService(threshold: 2);

        svc.CheckResult("CAM1", false);
        svc.CheckResult("CAM1", false);

        Assert.Single(svc.AlarmHistory);
        Assert.Equal("CAM1", svc.AlarmHistory[0].CameraName);
        Assert.Equal(2, svc.AlarmHistory[0].ConsecutiveNgCount);
    }

    [Fact]
    public void Clear_SetsClearedTimestamp()
    {
        var svc = TestHelper.CreateAlarmService(threshold: 1);
        svc.CheckResult("CAM1", false);
        Assert.Null(svc.AlarmHistory[0].ClearedAt);

        svc.Clear();
        Assert.NotNull(svc.AlarmHistory[0].ClearedAt);
    }

    [Fact]
    public void Reset_ClearsHistory()
    {
        var svc = TestHelper.CreateAlarmService(threshold: 1);
        svc.CheckResult("CAM1", false);
        Assert.Single(svc.AlarmHistory);

        svc.Reset();
        Assert.Empty(svc.AlarmHistory);
    }

    [Fact]
    public void MultipleAlarms_TracksSeparately()
    {
        var svc = TestHelper.CreateAlarmService(threshold: 1);

        svc.CheckResult("CAM1", false);
        svc.Clear();
        svc.CheckResult("CAM2", false);

        Assert.Equal(2, svc.AlarmHistory.Count);
        Assert.Equal("CAM2", svc.AlarmHistory[0].CameraName);
        Assert.Equal("CAM1", svc.AlarmHistory[1].CameraName);
    }
}
