using System.IO;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

public class CsvResultLogService : IResultLogService, IDisposable
{
    private readonly CsvLogSettings _settings;
    private readonly object _lock = new();
    private string? _currentDate;
    private StreamWriter? _writer;
    private int _consecutiveFailures;

    public int ConsecutiveWriteFailures => _consecutiveFailures;
    public event Action<string>? WriteError;

    public CsvResultLogService(IOptions<CsvLogSettings> options)
    {
        _settings = options.Value;
    }

    public void Log(InspectionResult result)
    {
        if (!_settings.Enabled) return;

        lock (_lock)
        {
            try
            {
                EnsureWriter(result.Timestamp);
                _writer?.WriteLine(string.Join(",",
                    result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    result.Id,
                    result.CameraName,
                    result.IsPass ? "PASS" : "FAIL",
                    result.Score,
                    $"\"{result.Description}\"",
                    $"\"{result.ImagePath}\""));
                _writer?.Flush();
                _consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                WriteError?.Invoke($"CSV 로그 기록 실패 ({_consecutiveFailures}회 연속): {ex.Message}");
            }
        }
    }

    private void EnsureWriter(DateTime timestamp)
    {
        var date = timestamp.ToString("yyyyMMdd");
        if (_currentDate == date && _writer != null) return;

        _writer?.Dispose();
        Directory.CreateDirectory(_settings.BasePath);
        var path = Path.Combine(_settings.BasePath, $"Result_{date}.csv");
        var isNew = !File.Exists(path);
        _writer = new StreamWriter(path, append: true);
        _currentDate = date;

        if (isNew)
        {
            _writer.WriteLine("DateTime,Id,Camera,Result,Score,Description,ImagePath");
            _writer.Flush();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
