using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class CameraStatisticsTests
{
    [Fact]
    public void AddResult_TracksCameraStats()
    {
        var svc = TestHelper.CreateStatisticsService();

        svc.AddResult(new InspectionResult { CameraName = "CAM1", IsPass = true });
        svc.AddResult(new InspectionResult { CameraName = "CAM1", IsPass = false });
        svc.AddResult(new InspectionResult { CameraName = "CAM2", IsPass = true });

        var cam1 = svc.GetCameraStats("CAM1");
        Assert.Equal(2, cam1.TotalCount);
        Assert.Equal(1, cam1.PassCount);
        Assert.Equal(1, cam1.FailCount);

        var cam2 = svc.GetCameraStats("CAM2");
        Assert.Equal(1, cam2.TotalCount);
        Assert.Equal(1, cam2.PassCount);
    }

    [Fact]
    public void GetCameraStats_Unknown_ReturnsEmpty()
    {
        var svc = TestHelper.CreateStatisticsService();
        var stats = svc.GetCameraStats("UNKNOWN");
        Assert.Equal(0, stats.TotalCount);
    }

    [Fact]
    public void Reset_ClearsCameraStats()
    {
        var svc = TestHelper.CreateStatisticsService();
        svc.AddResult(new InspectionResult { CameraName = "CAM1", IsPass = true });
        svc.Reset();

        Assert.Empty(svc.CameraStats);
    }

    [Fact]
    public void CameraStatistics_PassRate_CalculatesCorrectly()
    {
        var stats = new CameraStatistics { CameraName = "CAM1" };
        stats.AddResult(true);
        stats.AddResult(true);
        stats.AddResult(false);

        Assert.Equal(66.7, stats.PassRate);
    }
}
