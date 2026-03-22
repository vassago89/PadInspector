using System.IO;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

public class ImageSaveService : IImageSaveService
{
    private readonly ImageSaveSettings _settings;
    private readonly ILogService _logService;

    private readonly string _basePath;

    public ImageSaveService(IOptions<ImageSaveSettings> options, ILogService logService)
    {
        _settings = options.Value;
        _logService = logService;
        _basePath = Path.IsPathRooted(_settings.BasePath)
            ? _settings.BasePath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.BasePath);
    }

    public string? Save(Mat image, InspectionResult result)
    {
        if (!_settings.Enabled) return null;
        if (result.IsPass && !_settings.SaveOk) return null;
        if (!result.IsPass && !_settings.SaveNg) return null;

        try
        {
            var date = result.Timestamp.ToString("yyyyMMdd");
            var status = result.IsPass ? "OK" : "NG";
            var dir = Path.Combine(_basePath, date, result.CameraName, status);
            Directory.CreateDirectory(dir);

            var fileName = $"{result.Timestamp:HHmmss_fff}_{result.Id}.{_settings.Format}";
            var filePath = Path.Combine(dir, fileName);
            Cv2.ImWrite(filePath, image);
            return filePath;
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"이미지 저장 실패: {ex.Message}");
            return null;
        }
    }
}
