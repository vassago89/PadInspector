using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
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
        services.Configure<ModbusSettings>(configuration.GetSection("Modbus"));
        services.Configure<RecipeSettings>(configuration.GetSection("Recipe"));

        // Services
        var modbusEnabled = configuration.GetValue<bool>("Modbus:Enabled");
        if (modbusEnabled)
            services.AddSingleton<IIOService, ModbusTcpIOService>();
        else
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

        // ViewModels
        services.AddSingleton<RecipeViewModel>();
        services.AddSingleton<StatisticsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }
}
