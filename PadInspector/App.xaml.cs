using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;
using PadInspector.ViewModels;

namespace PadInspector;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ValidateConfiguration(_serviceProvider);
        WireServiceEvents(_serviceProvider);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    public static void SetLanguage(string lang)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri($"Resources/Strings.{lang}.xaml", UriKind.Relative)
        };

        var merged = Current.Resources.MergedDictionaries;
        if (merged.Count > 0)
            merged[0] = dict;
        else
            merged.Insert(0, dict);
    }

    private static void ValidateConfiguration(IServiceProvider sp)
    {
        var logService = sp.GetRequiredService<ILogService>();
        var camSettings = sp.GetRequiredService<IOptions<CamerasSettings>>().Value;

        if (string.IsNullOrEmpty(camSettings.Camera1.SerialNumber))
            logService.Log("WARN", "CAM1 시리얼번호 미설정 - 더미 모드로 동작합니다");
        if (string.IsNullOrEmpty(camSettings.Camera2.SerialNumber))
            logService.Log("WARN", "CAM2 시리얼번호 미설정 - 더미 모드로 동작합니다");

        var ioSettings = sp.GetRequiredService<IOptions<IOSettings>>().Value;
        if (ioSettings.OutputPulseMs <= 0)
            logService.Log("WARN", "IO OutputPulseMs 값이 0 이하입니다");
    }

    private static void WireServiceEvents(IServiceProvider sp)
    {
        var logService = sp.GetRequiredService<ILogService>();
        var resultLogService = sp.GetRequiredService<IResultLogService>();
        var recipeService = sp.GetRequiredService<IRecipeService>();

        resultLogService.WriteError += msg => logService.Log("ERR", msg);
        recipeService.Error += msg => logService.Log("ERR", msg);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Options
        services.Configure<CamerasSettings>(configuration.GetSection("Cameras"));
        services.Configure<InspectionSettings>(configuration.GetSection("Inspection"));
        services.Configure<IOSettings>(configuration.GetSection("IO"));
        services.Configure<LogSettings>(configuration.GetSection("Log"));
        services.Configure<ImageSaveSettings>(configuration.GetSection("ImageSave"));
        services.Configure<CsvLogSettings>(configuration.GetSection("CsvLog"));
        services.Configure<AlarmSettings>(configuration.GetSection("Alarm"));
        services.Configure<RecipeSettings>(configuration.GetSection("Recipe"));

        // Services
        services.AddSingleton<IIOService, VirtualIOService>();

        services.AddSingleton<ICameraServiceFactory, HikCameraServiceFactory>();
        services.AddSingleton<IInspectionService, InspectionService>();
        services.AddSingleton<IRecipeService, RecipeService>();
        services.AddSingleton<IImageSaveService, ImageSaveService>();
        services.AddSingleton<IResultLogService, CsvResultLogService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IAlarmService, AlarmService>();
        services.AddSingleton<IIOOutputService, IOOutputService>();
        services.AddSingleton<IStatisticsService, StatisticsService>();
        services.AddSingleton<ITestImageService, TestImageService>();
        services.AddSingleton<IImageCleanupService, ImageCleanupService>();
        services.AddSingleton<IDiskMonitorService, DiskMonitorService>();

        // ViewModels
        services.AddSingleton<RecipeViewModel>();
        services.AddSingleton<StatisticsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }
}
