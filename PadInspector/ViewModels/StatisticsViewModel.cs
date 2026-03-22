using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class StatisticsViewModel : ObservableObject, IDisposable
{
    private readonly IStatisticsService _statisticsService;

    public int TotalCount => _statisticsService.TotalCount;
    public int PassCount => _statisticsService.PassCount;
    public int FailCount => _statisticsService.FailCount;
    public double PassRate => _statisticsService.PassRate;
    public ObservableCollection<InspectionResult> Results => _statisticsService.Results;
    public ObservableCollection<double> YieldTrend => _statisticsService.YieldTrend;

    public StatisticsViewModel(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _statisticsService.Updated += OnStatisticsUpdated;
    }

    private void OnStatisticsUpdated()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PassCount));
        OnPropertyChanged(nameof(FailCount));
        OnPropertyChanged(nameof(PassRate));
    }

    public void AddResult(InspectionResult result) => _statisticsService.AddResult(result);

    public void Reset() => _statisticsService.Reset();

    public void Dispose()
    {
        _statisticsService.Updated -= OnStatisticsUpdated;
    }
}
