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
dotnet test PadInspector.Tests
```

- Target: `net10.0-windows` (preview SDK required)
- Solution: `PadInspector.slnx`
- Clean build (0 warnings, 0 errors)
- Tests: 18 passing (AlarmService, StatisticsService, InspectionService)

## Architecture

MVVM with CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection.

### Project Structure

```
PadInspector/
├── Configs/          # IOptions<T> settings classes (8 files)
│   ├── CameraSettings.cs      CamerasSettings, CameraConfig
│   ├── InspectionSettings.cs
│   ├── IOSettings.cs           output channel mapping, pulse duration
│   ├── ImageSaveSettings.cs
│   ├── CsvLogSettings.cs
│   ├── AlarmSettings.cs
│   ├── LogSettings.cs
│   └── RecipeSettings.cs       BasePath, DefaultRecipeName
├── Converters/       # BoolToColor, BoolToPassFail, InverseBool
├── Models/           # Recipe, InspectionResult, IOSignal, RoiRect
├── Resources/        # Localization
│   ├── Strings.ko.xaml         Korean strings
│   └── Strings.en.xaml         English strings
├── Services/         # Interfaces + implementations (12 pairs)
│   ├── ICameraService / HikCameraService         (CancellationToken grab loop)
│   ├── ICameraServiceFactory / HikCameraServiceFactory
│   ├── IIOService / VirtualIOService
│   ├── IIOOutputService / IOOutputService         (channel mapping + pulse)
│   ├── IInspectionService / InspectionService     (overlay drawing)
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
│   ├── StatisticsViewModel.cs  Thin wrapper over IStatisticsService (IDisposable)
│   └── SettingsViewModel.cs    Runtime config editor (appsettings.json)
├── Views/            # CameraView, RecipeView, StatisticsView, SettingsView, YieldChart, ZoomPanImage
├── App.xaml(.cs)     # DI composition root, implicit DataTemplates, disposal chain, language switching
├── MainWindow.xaml   # Shell layout (Fluent dark/light theme)
└── appsettings.json  # All configuration
```

### IoC / DI

All services and ViewModels are resolved through the DI container in `App.xaml.cs`.

- **Factory pattern**: `ICameraServiceFactory` creates `ICameraService` instances per camera config; factory injects `ILogService` into each instance
- **Sub-ViewModel injection**: `RecipeViewModel`, `StatisticsViewModel`, `SettingsViewModel` registered as singletons in DI; `CameraViewModel` created via factory delegate in `MainViewModel`
- **IOptions<T>** pattern for all settings (8 config classes)
- **Disposal chain**: `MainViewModel.Dispose()` cascades to Camera1/2, Statistics; `ServiceProvider.Dispose()` in `App.OnExit()` handles ILogService, IResultLogService, IIOService
- **Implicit DataTemplates**: `App.xaml` maps ViewModel types to View UserControls (CameraView, RecipeView, StatisticsView, SettingsView)
- **No service locator**: No `public static IServiceProvider`

### Key Patterns

- **Event-based communication**: Sub-VMs communicate via events (RecipeChanged, ImageAcquired) wired in MainViewModel
- **SynchronizationContext**: LogService and MainViewModel use `SynchronizationContext` instead of WPF `Dispatcher` directly for UI thread dispatch
- **Background processing**: Inspection, image save, CSV log, IO output run on background thread; only UI updates via `Post` (non-blocking)
- **Image overlay**: InspectionService returns `(InspectionResult, Mat overlay)` with contours + ROI drawn; overlay converted to frozen BitmapSource on background thread
- **Service extraction**: Business logic split into dedicated services (AlarmService, IOOutputService, StatisticsService, TestImageService) — ViewModels are thin orchestrators
- **Thread-safe IO**: VirtualIOService uses `lock` + `async/await` reset + channel bounds check
- **CancellationToken**: HikCameraService GrabLoop supports cooperative cancellation via `CancellationTokenSource`; `_isGrabbing` is `volatile`
- **Dispose guard**: MainViewModel, CameraViewModel, StatisticsViewModel use `_disposed` flag to prevent double-dispose
- **IO channel mapping**: Camera pass/fail output channels configured in `IOSettings`, consumed by `IOOutputService`
- **Camera**: HikCameraService wraps MvCameraControl.Net SDK. Finds cameras by SerialNumber. IO trigger: Line0 RisingEdge. Real-time Exposure/Gain control via `SetExposure`/`SetGain`
- **ROI**: Ratio-based (0~1) coordinates in Recipe. Applied in InspectionService.CropRoi()
- **Theme**: WPF .NET 10 Fluent dark/light toggle (`ThemeMode`). Uses `{DynamicResource ControlStrokeColorDefaultBrush}`
- **Localization**: KO/EN runtime switch via `App.SetLanguage()`, ResourceDictionary with `{DynamicResource S.xxx}` keys
- **Result filtering**: StatisticsViewModel uses `ICollectionView` with filter predicate (All/Pass/Fail)
- **Recipe import/export**: JSON file dialog in RecipeViewModel
- **Settings UI**: SettingsViewModel edits appsettings.json directly (restart required for reload)
- **Error handling**: MainViewModel shows MessageBox on camera connect / inspection start failures
- **Frozen brushes**: YieldChart uses `static readonly` + `Freeze()` brushes to avoid per-redraw allocations

### Dependencies

- CommunityToolkit.Mvvm 8.4.0
- OpenCvSharp4 4.13.0
- Microsoft.Extensions.Configuration/DI/Options
- MvCameraControl.Net.dll (native, from MVS SDK install path)

### Testing

- xUnit test project: `PadInspector.Tests` (net10.0-windows)
- FakeLogService for mocking ILogService
- Tests cover: AlarmService (6), StatisticsService (6), InspectionService (6) with OpenCV Mat

### Configuration (appsettings.json)

Sections: `Cameras`, `Inspection`, `IO`, `ImageSave`, `CsvLog`, `Alarm`, `Recipe`, `Log`

## Features

- Dual HIK line scan camera support with IO trigger
- OpenCV-based pad inspection (threshold + contour) with overlay visualization
- Real-time camera Exposure/Gain control
- NG image save, CSV result logging
- Virtual IO for testing (trigger simulation, auto trigger)
- Per-camera ROI with ratio-based coordinates
- Recipe management (save/load/delete/import/export)
- Result filtering (All/Pass/Fail) with ICollectionView
- Statistics report CSV export
- Zoom/pan image viewer, canvas-based yield chart (frozen brushes)
- Consecutive NG alarm with configurable threshold (AlarmService)
- Statistics tracking with yield trend (StatisticsService)
- File logging (optional, via LogSettings.EnableFileLog)
- Korean/English localization with runtime switching
- Dark/light theme toggle
- Settings UI for runtime config editing (ImageSave, Alarm, Log, CsvLog)
- Error handling with MessageBox notifications
