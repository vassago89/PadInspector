using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

public class ImageCleanupServiceTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"PadInspectorTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static ImageCleanupService CreateService(string basePath, int maxDays = 7)
    {
        var settings = Options.Create(new ImageSaveSettings
        {
            BasePath = basePath,
            MaxDaysToKeep = maxDays
        });
        return new ImageCleanupService(settings, new FakeLogService());
    }

    [Fact]
    public void CleanupOldImages_DeletesOldFolders()
    {
        var basePath = CreateTempDir();
        try
        {
            // 오래된 폴더 생성 (10일 전)
            var oldDate = DateTime.Now.AddDays(-10).ToString("yyyyMMdd");
            var oldDir = Path.Combine(basePath, oldDate, "CAM1", "NG");
            Directory.CreateDirectory(oldDir);
            File.WriteAllText(Path.Combine(oldDir, "test.bmp"), "dummy");

            // 최근 폴더 생성 (오늘)
            var todayDate = DateTime.Now.ToString("yyyyMMdd");
            var todayDir = Path.Combine(basePath, todayDate, "CAM1", "OK");
            Directory.CreateDirectory(todayDir);
            File.WriteAllText(Path.Combine(todayDir, "test.bmp"), "dummy");

            var svc = CreateService(basePath, maxDays: 7);
            var deleted = svc.CleanupOldImages();

            Assert.Equal(1, deleted);
            Assert.False(Directory.Exists(Path.Combine(basePath, oldDate)));
            Assert.True(Directory.Exists(Path.Combine(basePath, todayDate)));
        }
        finally
        {
            Directory.Delete(basePath, true);
        }
    }

    [Fact]
    public void CleanupOldImages_KeepsRecentFolders()
    {
        var basePath = CreateTempDir();
        try
        {
            var recentDate = DateTime.Now.AddDays(-2).ToString("yyyyMMdd");
            var recentDir = Path.Combine(basePath, recentDate);
            Directory.CreateDirectory(recentDir);
            File.WriteAllText(Path.Combine(recentDir, "test.bmp"), "dummy");

            var svc = CreateService(basePath, maxDays: 7);
            var deleted = svc.CleanupOldImages();

            Assert.Equal(0, deleted);
            Assert.True(Directory.Exists(recentDir));
        }
        finally
        {
            Directory.Delete(basePath, true);
        }
    }

    [Fact]
    public void CleanupOldImages_NonExistentPath_ReturnsZero()
    {
        var svc = CreateService("/nonexistent/path", maxDays: 7);
        var deleted = svc.CleanupOldImages();
        Assert.Equal(0, deleted);
    }

    [Fact]
    public void CleanupOldImages_MaxDaysZero_ReturnsZero()
    {
        var basePath = CreateTempDir();
        try
        {
            var svc = CreateService(basePath, maxDays: 0);
            var deleted = svc.CleanupOldImages();
            Assert.Equal(0, deleted);
        }
        finally
        {
            Directory.Delete(basePath, true);
        }
    }
}
