using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

public class StatisticsService : IStatisticsService
{
    private readonly int _maxResultHistory;
    private const int ChartWindowSize = 50;
    private readonly Queue<bool> _recentResults = new();
    private readonly ConcurrentDictionary<string, CameraStatistics> _cameraStats = new();

    public int TotalCount { get; private set; }
    public int PassCount { get; private set; }
    public int FailCount { get; private set; }
    public double PassRate { get; private set; }

    public ObservableCollection<InspectionResult> Results { get; } = new();
    public ObservableCollection<double> YieldTrend { get; } = new();
    public IReadOnlyDictionary<string, CameraStatistics> CameraStats => _cameraStats;

    public event Action? Updated;

    public StatisticsService(IOptions<InspectionSettings> inspectionOptions)
    {
        _maxResultHistory = inspectionOptions.Value.MaxResultHistory;
    }

    public void AddResult(InspectionResult result)
    {
        TotalCount++;
        if (result.IsPass)
            PassCount++;
        else
            FailCount++;

        PassRate = TotalCount > 0 ? Math.Round((double)PassCount / TotalCount * 100, 1) : 0;

        // 카메라별 통계 (thread-safe)
        var camStats = _cameraStats.GetOrAdd(result.CameraName,
            name => new CameraStatistics { CameraName = name });
        camStats.AddResult(result.IsPass);

        Results.Insert(0, result);
        if (Results.Count > _maxResultHistory)
            Results.RemoveAt(Results.Count - 1);

        // 이동평균 수율 계산
        _recentResults.Enqueue(result.IsPass);
        if (_recentResults.Count > ChartWindowSize)
            _recentResults.Dequeue();

        double windowRate = Math.Round((double)_recentResults.Count(x => x) / _recentResults.Count * 100, 1);
        YieldTrend.Add(windowRate);
        if (YieldTrend.Count > 200)
            YieldTrend.RemoveAt(0);

        Updated?.Invoke();
    }

    public CameraStatistics GetCameraStats(string cameraName)
    {
        if (_cameraStats.TryGetValue(cameraName, out var stats))
            return stats;
        return new CameraStatistics { CameraName = cameraName };
    }

    public void Reset()
    {
        TotalCount = 0;
        PassCount = 0;
        FailCount = 0;
        PassRate = 0;
        Results.Clear();
        YieldTrend.Clear();
        _recentResults.Clear();
        _cameraStats.Clear();
        Updated?.Invoke();
    }
}
