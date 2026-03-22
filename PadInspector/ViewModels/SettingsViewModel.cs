using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    private readonly ILogService _logService;

    // ImageSave
    [ObservableProperty] private bool _imageSaveEnabled;
    [ObservableProperty] private bool _saveOk;
    [ObservableProperty] private bool _saveNg;
    [ObservableProperty] private string _imageSavePath = "";
    [ObservableProperty] private string _imageFormat = "bmp";
    [ObservableProperty] private int _maxDaysToKeep;

    // Alarm
    [ObservableProperty] private bool _alarmEnabled;
    [ObservableProperty] private int _consecutiveNgThreshold;

    // Log
    [ObservableProperty] private int _maxLogLines;
    [ObservableProperty] private bool _enableFileLog;

    // CsvLog
    [ObservableProperty] private bool _csvLogEnabled;
    [ObservableProperty] private string _csvLogPath = "";

    public SettingsViewModel(
        IOptions<ImageSaveSettings> imageSaveOptions,
        IOptions<AlarmSettings> alarmOptions,
        IOptions<LogSettings> logOptions,
        IOptions<CsvLogSettings> csvLogOptions,
        ILogService logService)
    {
        _logService = logService;

        var img = imageSaveOptions.Value;
        ImageSaveEnabled = img.Enabled;
        SaveOk = img.SaveOk;
        SaveNg = img.SaveNg;
        ImageSavePath = img.BasePath;
        ImageFormat = img.Format;
        MaxDaysToKeep = img.MaxDaysToKeep;

        var alarm = alarmOptions.Value;
        AlarmEnabled = alarm.Enabled;
        ConsecutiveNgThreshold = alarm.ConsecutiveNgThreshold;

        var log = logOptions.Value;
        MaxLogLines = log.MaxLogLines;
        EnableFileLog = log.EnableFileLog;

        var csv = csvLogOptions.Value;
        CsvLogEnabled = csv.Enabled;
        CsvLogPath = csv.BasePath;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonNode.Parse(json) ?? new JsonObject();

            doc["ImageSave"]!["Enabled"] = ImageSaveEnabled;
            doc["ImageSave"]!["SaveOk"] = SaveOk;
            doc["ImageSave"]!["SaveNg"] = SaveNg;
            doc["ImageSave"]!["BasePath"] = ImageSavePath;
            doc["ImageSave"]!["Format"] = ImageFormat;
            doc["ImageSave"]!["MaxDaysToKeep"] = MaxDaysToKeep;

            doc["Alarm"]!["Enabled"] = AlarmEnabled;
            doc["Alarm"]!["ConsecutiveNgThreshold"] = ConsecutiveNgThreshold;

            doc["Log"]!["MaxLogLines"] = MaxLogLines;
            doc["Log"]!["EnableFileLog"] = EnableFileLog;

            doc["CsvLog"]!["Enabled"] = CsvLogEnabled;
            doc["CsvLog"]!["BasePath"] = CsvLogPath;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var content = doc.ToJsonString(options);

            // 원자적 쓰기: 임시 파일에 쓴 후 교체 (손상 방지)
            var tempPath = SettingsPath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Copy(tempPath, SettingsPath, overwrite: true);
            File.Delete(tempPath);

            _logService.Log("INFO", "설정 저장 완료 (재시작 후 적용)");
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"설정 저장 실패: {ex.Message}");
        }
    }
}
