using PadInspector.Configs;

namespace PadInspector.Services;

public class HikCameraServiceFactory : ICameraServiceFactory
{
    private readonly ILogService _logService;

    public HikCameraServiceFactory(ILogService logService)
    {
        _logService = logService;
    }

    public ICameraService Create(CameraConfig config) => new HikCameraService(config, _logService);
}
