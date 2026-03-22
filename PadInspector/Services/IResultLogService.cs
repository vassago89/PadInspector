using PadInspector.Models;

namespace PadInspector.Services;

public interface IResultLogService : IDisposable
{
    void Log(InspectionResult result);
}
