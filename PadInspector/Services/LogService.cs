using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Extensions.Options;
using PadInspector.Configs;

namespace PadInspector.Services;

public class LogService : ILogService
{
    private readonly LogSettings _settings;
    private readonly SynchronizationContext? _syncContext;
    private readonly object _fileLock = new();
    private StreamWriter? _writer;
    private string? _currentDate;

    public ObservableCollection<string> Messages { get; } = new();

    public LogService(IOptions<LogSettings> options)
    {
        _settings = options.Value;
        _syncContext = SynchronizationContext.Current;
    }

    public void Log(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";

        if (_syncContext == null || SynchronizationContext.Current == _syncContext)
            AddLine(line);
        else
            _syncContext.Send(_ => AddLine(line), null);

        if (_settings.EnableFileLog)
            WriteToFile(line);
    }

    private void AddLine(string line)
    {
        Messages.Add(line);
        if (Messages.Count > _settings.MaxLogLines)
            Messages.RemoveAt(0);
    }

    private void WriteToFile(string line)
    {
        lock (_fileLock)
        {
            try
            {
                EnsureWriter();
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
            catch
            {
                // 파일 로깅 실패 무시 (UI 로그에는 영향 없음)
            }
        }
    }

    private void EnsureWriter()
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        if (_currentDate == date && _writer != null) return;

        _writer?.Dispose();
        Directory.CreateDirectory(_settings.LogDirectory);
        var path = Path.Combine(_settings.LogDirectory, $"Log_{date}.txt");
        _writer = new StreamWriter(path, append: true);
        _currentDate = date;
    }

    public void Clear()
    {
        Messages.Clear();
    }

    public void Dispose()
    {
        lock (_fileLock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
