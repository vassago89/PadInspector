using System.IO;
using Microsoft.Extensions.Options;
using PadInspector.Configs;

namespace PadInspector.Services;

public class ImageCleanupService : IImageCleanupService
{
    private readonly ImageSaveSettings _settings;
    private readonly ILogService _logService;
    private Timer? _timer;
    private readonly string _basePath;

    public ImageCleanupService(IOptions<ImageSaveSettings> options, ILogService logService)
    {
        _settings = options.Value;
        _logService = logService;
        _basePath = Path.IsPathRooted(_settings.BasePath)
            ? _settings.BasePath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.BasePath);
    }

    public void Start()
    {
        // 시작 시 한번 실행, 이후 24시간 간격
        _timer = new Timer(_ => CleanupOldImages(), null, TimeSpan.Zero, TimeSpan.FromHours(24));
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public int CleanupOldImages()
    {
        if (_settings.MaxDaysToKeep <= 0) return 0;
        if (!Directory.Exists(_basePath)) return 0;

        var deletedCount = 0;
        var cutoffDate = DateTime.Now.AddDays(-_settings.MaxDaysToKeep);

        try
        {
            foreach (var dateDir in Directory.GetDirectories(_basePath))
            {
                var dirName = Path.GetFileName(dateDir);
                if (DateTime.TryParseExact(dirName, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out var dirDate))
                {
                    if (dirDate < cutoffDate)
                    {
                        var fileCount = Directory.GetFiles(dateDir, "*", SearchOption.AllDirectories).Length;
                        Directory.Delete(dateDir, recursive: true);
                        deletedCount += fileCount;
                        _logService.Log("CLEANUP", $"오래된 이미지 폴더 삭제: {dirName} ({fileCount}개 파일)");
                    }
                }
            }

            if (deletedCount > 0)
                _logService.Log("INFO", $"이미지 정리 완료: {deletedCount}개 파일 삭제 (보관기간: {_settings.MaxDaysToKeep}일)");
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"이미지 정리 실패: {ex.Message}");
        }

        return deletedCount;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
