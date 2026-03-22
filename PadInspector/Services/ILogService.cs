using System.Collections.ObjectModel;

namespace PadInspector.Services;

public interface ILogService : IDisposable
{
    ObservableCollection<string> Messages { get; }
    void Log(string level, string message);
    void Clear();
}
