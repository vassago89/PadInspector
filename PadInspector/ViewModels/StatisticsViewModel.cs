using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class StatisticsViewModel : ObservableObject, IDisposable
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogService _logService;

    public int TotalCount => _statisticsService.TotalCount;
    public int PassCount => _statisticsService.PassCount;
    public int FailCount => _statisticsService.FailCount;
    public double PassRate => _statisticsService.PassRate;
    public ObservableCollection<InspectionResult> Results => _statisticsService.Results;
    public ObservableCollection<double> YieldTrend => _statisticsService.YieldTrend;

    public ICollectionView FilteredResults { get; }

    [ObservableProperty] private string _filter = "All";
    [ObservableProperty] private string _cameraFilter = "All";

    public IReadOnlyDictionary<string, CameraStatistics> CameraStats => _statisticsService.CameraStats;

    public StatisticsViewModel(IStatisticsService statisticsService, ILogService logService)
    {
        _statisticsService = statisticsService;
        _logService = logService;
        _statisticsService.Updated += OnStatisticsUpdated;
        FilteredResults = CollectionViewSource.GetDefaultView(_statisticsService.Results);
    }

    private void OnStatisticsUpdated()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PassCount));
        OnPropertyChanged(nameof(FailCount));
        OnPropertyChanged(nameof(PassRate));
        OnPropertyChanged(nameof(CameraStats));
    }

    partial void OnFilterChanged(string value) => ApplyFilters();
    partial void OnCameraFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        FilteredResults.Filter = obj =>
        {
            if (obj is not InspectionResult result) return false;

            // 카메라 필터
            if (CameraFilter != "All" && result.CameraName != CameraFilter)
                return false;

            // Pass/Fail 필터
            return Filter switch
            {
                "Pass" => result.IsPass,
                "Fail" => !result.IsPass,
                _ => true
            };
        };
    }

    [RelayCommand]
    private void SetFilter(string filter) => Filter = filter;

    [RelayCommand]
    private void SetCameraFilter(string cameraName) => CameraFilter = cameraName;

    [RelayCommand]
    private void ExportReport()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files|*.csv",
            FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            using var writer = new StreamWriter(dialog.FileName);
            writer.WriteLine($"# Pad Inspector Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"# Total: {TotalCount}, Pass: {PassCount}, Fail: {FailCount}, Yield: {PassRate}%");
            writer.WriteLine();
            writer.WriteLine("Id,Camera,Result,Score,Pads,Time,Description,ImagePath");
            foreach (var r in Results)
            {
                writer.WriteLine(string.Join(",",
                    r.Id, r.CameraName, r.IsPass ? "PASS" : "FAIL",
                    r.Score, r.PadCount, r.Timestamp.ToString("HH:mm:ss.fff"),
                    EscapeCsv(r.Description), EscapeCsv(r.ImagePath)));
            }
            _logService.Log("INFO", $"리포트 내보내기: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"리포트 내보내기 실패: {ex.Message}");
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return $"\"{value}\"";
    }

    public void AddResult(InspectionResult result) => _statisticsService.AddResult(result);

    public void Reset() => _statisticsService.Reset();

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _statisticsService.Updated -= OnStatisticsUpdated;
    }
}
