using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class StatisticsServiceTests
{
    private static StatisticsService CreateService(int maxHistory = 100)
    {
        var settings = Options.Create(new InspectionSettings { MaxResultHistory = maxHistory });
        return new StatisticsService(settings);
    }

    [Fact]
    public void AddResult_IncrementsCounts()
    {
        var svc = CreateService();

        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = false });
        svc.AddResult(new InspectionResult { IsPass = true });

        Assert.Equal(3, svc.TotalCount);
        Assert.Equal(2, svc.PassCount);
        Assert.Equal(1, svc.FailCount);
    }

    [Fact]
    public void PassRate_CalculatesCorrectly()
    {
        var svc = CreateService();

        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = false });

        Assert.Equal(66.7, svc.PassRate);
    }

    [Fact]
    public void Results_RespectMaxHistory()
    {
        var svc = CreateService(maxHistory: 5);

        for (int i = 0; i < 10; i++)
            svc.AddResult(new InspectionResult { IsPass = true });

        Assert.Equal(5, svc.Results.Count);
        Assert.Equal(10, svc.TotalCount);
    }

    [Fact]
    public void Reset_ClearsAll()
    {
        var svc = CreateService();
        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = false });

        svc.Reset();

        Assert.Equal(0, svc.TotalCount);
        Assert.Equal(0, svc.PassCount);
        Assert.Equal(0, svc.FailCount);
        Assert.Equal(0, svc.PassRate);
        Assert.Empty(svc.Results);
        Assert.Empty(svc.YieldTrend);
    }

    [Fact]
    public void YieldTrend_AddsEntries()
    {
        var svc = CreateService();
        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = false });

        Assert.Equal(2, svc.YieldTrend.Count);
    }

    [Fact]
    public void Updated_EventFires()
    {
        var svc = CreateService();
        int fireCount = 0;
        svc.Updated += () => fireCount++;

        svc.AddResult(new InspectionResult { IsPass = true });
        svc.AddResult(new InspectionResult { IsPass = false });

        Assert.Equal(2, fireCount);
    }
}
