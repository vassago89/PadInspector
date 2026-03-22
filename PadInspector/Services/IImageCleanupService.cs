namespace PadInspector.Services;

public interface IImageCleanupService : IDisposable
{
    void Start();
    void Stop();
    int CleanupOldImages();
}
