# PadInspector

WPF .NET 10 factory pad inspection application using HIK (Hikvision) MVS SDK line scan cameras.

## Prerequisites

WPF is Windows-only. .NET 10 Preview SDK is required:

```powershell
winget install Microsoft.DotNet.SDK.Preview
```

## Build & Run

```bash
dotnet build
dotnet run --project PadInspector
```

- Target: `net10.0-windows` (preview SDK required)
- Solution: `PadInspector.slnx`
- Clean build (0 warnings, 0 errors)

## Architecture

MVVM with CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection.

### Project Structure

```
PadInspector/
├── Configs/          # IOptions<T> settings classes (9 files)
│   ├── CameraSettings.cs      CamerasSettings, CameraConfig
│   ├── InspectionSettings.cs
│   ├── IOSettings.cs           output channel mapping, pulse duration
│   ├── ImageSaveSettings.cs
│   ├── CsvLogSettings.cs
│   ├── AlarmSettings.cs
│   ├── ModbusSettings.cs       connection + address settings only
│   ├── LogSettings.cs
│   └── RecipeSettings.cs       BasePath, DefaultRecipeName
├── Converters/       # BoolToColor, BoolToPassFail, InverseBool
├── Models/           # Recipe, InspectionResult, IOSignal, RoiRect
├── Services/         # Interfaces + implementations (12 pairs)
│   ├── ICameraService / HikCameraService         (CancellationToken grab loop)
│   ├── ICameraServiceFactory / HikCameraServiceFactory
│   ├── IIOService / VirtualIOService / ModbusTcpIOService
│   ├── IIOOutputService / IOOutputService         (channel mapping + pulse)
│   ├── IInspectionService / InspectionService
│   ├── IRecipeService / RecipeService
│   ├── IImageSaveService / ImageSaveService
│   ├── IResultLogService / CsvResultLogService
│   ├── ILogService / LogService                   (SynchronizationContext-based)
│   ├── IAlarmService / AlarmService               (consecutive NG tracking)
│   ├── IStatisticsService / StatisticsService     (counts, trend, history)
│   └── ITestImageService / TestImageService       (dummy image for virtual mode)
├── ViewModels/       # MainViewModel + sub-VMs
│   ├── MainViewModel.cs        Orchestrator (SynchronizationContext for UI dispatch)
│   ├── CameraViewModel.cs      Accepts ICameraServiceFactory (not concrete)
│   ├── RecipeViewModel.cs      Injected via DI (IRecipeService + ILogService)
│   └── StatisticsViewModel.cs  Thin wrapper over IStatisticsService (IDisposable)
├── Views/            # CameraView, RecipeView, StatisticsView, YieldChart, ZoomPanImage
├── App.xaml(.cs)     # DI composition root, implicit DataTemplates, disposal chain
├── MainWindow.xaml   # Shell layout (Fluent dark theme)
└── appsettings.json  # All configuration
```

### IoC / DI

All services and ViewModels are resolved through the DI container in `App.xaml.cs`.

- **Factory pattern**: `ICameraServiceFactory` creates `ICameraService` instances per camera config; factory injects `ILogService` into each instance
- **Sub-ViewModel injection**: `RecipeViewModel` and `StatisticsViewModel` registered as singletons in DI; `CameraViewModel` created via factory delegate in `MainViewModel`
- **Conditional registration**: `IIOService` resolves to `ModbusTcpIOService` or `VirtualIOService` based on `Modbus:Enabled` config
- **IOptions<T>** pattern for all settings (9 config classes)
- **Disposal chain**: `ServiceProvider.Dispose()` called in `App.OnExit()`, cascading to `ILogService`, `IResultLogService`, `IIOService`
- **Implicit DataTemplates**: `App.xaml` maps ViewModel types to View UserControls (CameraView, RecipeView, StatisticsView)
- **No service locator**: No `public static IServiceProvider`

### Key Patterns

- **Event-based communication**: Sub-VMs communicate via events (RecipeChanged, ImageAcquired) wired in MainViewModel
- **SynchronizationContext**: LogService and MainViewModel use `SynchronizationContext` instead of WPF `Dispatcher` directly for UI thread dispatch
- **Service extraction**: Business logic split into dedicated services (AlarmService, IOOutputService, StatisticsService, TestImageService) — ViewModels are thin orchestrators
- **Thread-safe IO**: VirtualIOService uses `lock` + `async/await` reset; ModbusTcpIOService uses `ReadExact` for reliable stream reads + Modbus FC error validation
- **CancellationToken**: HikCameraService GrabLoop supports cooperative cancellation via `CancellationTokenSource`
- **Dispose guard**: MainViewModel uses `_disposed` flag to prevent double-dispose
- **IO channel mapping**: Camera pass/fail output channels configured in `IOSettings`, consumed by `IOOutputService`
- **Camera**: HikCameraService wraps MvCameraControl.Net SDK. Finds cameras by SerialNumber. IO trigger: Line0 RisingEdge
- **ROI**: Ratio-based (0~1) coordinates in Recipe. Applied in InspectionService.CropRoi()
- **Theme**: WPF .NET 10 Fluent dark (`ThemeMode="Dark"`). Uses `{DynamicResource ControlStrokeColorDefaultBrush}`

### Dependencies

- CommunityToolkit.Mvvm 8.4.0
- OpenCvSharp4 4.13.0
- Microsoft.Extensions.Configuration/DI/Options
- MvCameraControl.Net.dll (native, from MVS SDK install path)

### Configuration (appsettings.json)

Sections: `Cameras`, `Inspection`, `IO`, `ImageSave`, `CsvLog`, `Alarm`, `Recipe`, `Modbus`, `Log`

## Features

- Dual HIK line scan camera support with IO trigger
- OpenCV-based pad inspection (threshold + contour)
- NG image save, CSV result logging
- Modbus TCP IO with response validation (or virtual IO for testing)
- Per-camera ROI with ratio-based coordinates
- Recipe management (save/load/delete)
- Zoom/pan image viewer, canvas-based yield chart
- Consecutive NG alarm with configurable threshold (AlarmService)
- Statistics tracking with yield trend (StatisticsService)
- File logging (optional, via LogSettings.EnableFileLog)
