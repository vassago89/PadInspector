using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class CameraStatisticsTests
{
    private static StatisticsService CreateService(int maxHistory = 100)
    {
        var settings = Options.Create(new InspectionSettings { MaxResultHistory = maxHistory });
        return new StatisticsService(settings);
    }

    [Fact]
    public void AddResult_TracksCameraStats()
    {
        var svc = CreateService();

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
        var svc = CreateService();
        var stats = svc.GetCameraStats("UNKNOWN");
        Assert.Equal(0, stats.TotalCount);
    }

    [Fact]
    public void Reset_ClearsCameraStats()
    {
        var svc = CreateService();
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
