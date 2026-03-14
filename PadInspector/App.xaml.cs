using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PadInspector.Configs;
using PadInspector.Services;
using PadInspector.ViewModels;

namespace PadInspector;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
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
        services.Configure<CameraSettings>(configuration.GetSection("Camera"));
        services.Configure<InspectionSettings>(configuration.GetSection("Inspection"));
        services.Configure<IOSettings>(configuration.GetSection("IO"));
        services.Configure<LogSettings>(configuration.GetSection("Log"));

        // Services
        services.AddSingleton<IIOService, VirtualIOService>();
        services.AddSingleton<ICameraService, HikCameraService>();
        services.AddSingleton<IInspectionService, InspectionService>();
        services.AddSingleton<IRecipeService, RecipeService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }
}
