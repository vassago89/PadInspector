using System.Collections.ObjectModel;
using PadInspector.Models;

namespace PadInspector.Services;

public interface IStatisticsService
{
    int TotalCount { get; }
    int PassCount { get; }
    int FailCount { get; }
    double PassRate { get; }
    ObservableCollection<InspectionResult> Results { get; }
    ObservableCollection<double> YieldTrend { get; }
    IReadOnlyDictionary<string, CameraStatistics> CameraStats { get; }
    event Action? Updated;
    void AddResult(InspectionResult result);
    void Reset();
    CameraStatistics GetCameraStats(string cameraName);
}
