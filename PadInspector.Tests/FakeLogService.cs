using System.Collections.ObjectModel;
using PadInspector.Services;

namespace PadInspector.Tests;

public class FakeLogService : ILogService
{
    public ObservableCollection<string> Messages { get; } = new();
    public List<string> LogEntries { get; } = new();

    public void Log(string level, string message)
    {
        LogEntries.Add($"[{level}] {message}");
    }

    public void Clear()
    {
        Messages.Clear();
        LogEntries.Clear();
    }

    public void Dispose() { }
}
