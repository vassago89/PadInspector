using PadInspector.Configs;

namespace PadInspector.Services;

public interface ICameraServiceFactory
{
    ICameraService Create(CameraConfig config);
}
