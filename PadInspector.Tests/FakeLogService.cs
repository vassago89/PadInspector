using System.Collections.ObjectModel;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class FakeLogService : ILogService
{
    public ObservableCollection<string> Messages { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public List<string> Logs { get; } = new();

    public void Log(string level, string message)
    {
        Logs.Add($"[{level}] {message}");
        LogEntries.Add(new LogEntry { Timestamp = DateTime.Now, Level = level, Message = message });
    }

    public void Clear()
    {
        Messages.Clear();
        LogEntries.Clear();
        Logs.Clear();
    }

    public void Dispose() { }
}
