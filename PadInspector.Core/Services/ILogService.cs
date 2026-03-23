using System.Collections.ObjectModel;
using PadInspector.Models;

namespace PadInspector.Services;

public interface ILogService : IDisposable
{
    ObservableCollection<string> Messages { get; }
    ObservableCollection<LogEntry> LogEntries { get; }
    void Log(string level, string message);
    void Clear();
}
