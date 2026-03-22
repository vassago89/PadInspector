using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IIOService _ioService;
    private readonly IInspectionService _inspectionService;
    private readonly IImageSaveService _imageSaveService;
    private readonly IResultLogService _resultLogService;
    private readonly ILogService _logService;
    private readonly IAlarmService _alarmService;
    private readonly IIOOutputService _ioOutputService;
    private readonly ITestImageService _testImageService;
    private readonly IImageCleanupService _imageCleanupService;
    private readonly IDiskMonitorService _diskMonitorService;
    private readonly SynchronizationContext? _syncContext;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private bool _disposed;

    public CameraViewModel Camera1 { get; }
    public CameraViewModel Camera2 { get; }
    public RecipeViewModel Recipe { get; }
    public StatisticsViewModel Statistics { get; }
    public SettingsViewModel Settings { get; }
    public ILogService LogService => _logService;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusMessage = "대기 중";
    [ObservableProperty] private bool _autoTrigger;

    // 알람
    [ObservableProperty] private bool _isAlarm;
    [ObservableProperty] private string _alarmMessage = "";

    public MainViewModel(
        IIOService ioService,
        IInspectionService inspectionService,
        IImageSaveService imageSaveService,
        IResultLogService resultLogService,
        ILogService logService,
        IAlarmService alarmService,
        IIOOutputService ioOutputService,
        ITestImageService testImageService,
        IImageCleanupService imageCleanupService,
        IDiskMonitorService diskMonitorService,
        IOptions<CamerasSettings> camerasOptions,
        ICameraServiceFactory cameraFactory,
        RecipeViewModel recipeViewModel,
        StatisticsViewModel statisticsViewModel,
        SettingsViewModel settingsViewModel)
    {
        _ioService = ioService;
        _inspectionService = inspectionService;
        _imageSaveService = imageSaveService;
        _resultLogService = resultLogService;
        _logService = logService;
        _alarmService = alarmService;
        _ioOutputService = ioOutputService;
        _testImageService = testImageService;
        _imageCleanupService = imageCleanupService;
        _diskMonitorService = diskMonitorService;
        _syncContext = SynchronizationContext.Current;

        // Sub ViewModels (DI resolved)
        var camSettings = camerasOptions.Value;
        Camera1 = new CameraViewModel(camSettings.Camera1, cameraFactory);
        Camera2 = new CameraViewModel(camSettings.Camera2, cameraFactory);
        Recipe = recipeViewModel;
        Statistics = statisticsViewModel;
        Settings = settingsViewModel;

        // Event wiring
        Camera1.ImageAcquired += OnImageAcquired;
        Camera2.ImageAcquired += OnImageAcquired;
        Recipe.RecipeChanged += OnRecipeChanged;
        _ioService.TriggerReceived += OnTriggerReceived;
        _alarmService.AlarmStateChanged += OnAlarmStateChanged;

        // 초기 레시피 적용
        _currentRecipe = GetCurrentRecipeFromVM();
        _inspectionService.ApplyRecipe(_currentRecipe);

        // 백그라운드 서비스 시작
        _imageCleanupService.Start();
        _diskMonitorService.Start();
        _diskMonitorService.DiskSpaceLow += OnDiskSpaceLow;

        _logService.Log("INFO", $"시스템 초기화 완료 (듀얼 카메라) | 레시피: {Recipe.RecipeName}");
    }

    private void OnDiskSpaceLow(long freeMb)
    {
        PostToUiThread(() =>
        {
            StatusMessage = $"디스크 공간 부족! 남은 공간: {freeMb}MB";
            ShowError($"디스크 공간이 부족합니다.\n남은 공간: {freeMb}MB\n이미지 저장에 문제가 발생할 수 있습니다.");
        });
    }

    private Recipe _currentRecipe = new();

    private Recipe GetCurrentRecipeFromVM() => new()
    {
        Name = Recipe.RecipeName,
        ThresholdValue = Recipe.Threshold,
        MinAreaRatio = Recipe.MinArea,
        MaxAreaRatio = Recipe.MaxArea,
        PassScoreThreshold = Recipe.PassScore,
        ExposureTimeUs = Recipe.Exposure,
        GainDb = Recipe.Gain,
        TriggerIntervalMs = Recipe.TriggerInterval,
        Camera1Roi = new RoiRect { X = Recipe.Cam1RoiX, Y = Recipe.Cam1RoiY, Width = Recipe.Cam1RoiW, Height = Recipe.Cam1RoiH },
        Camera2Roi = new RoiRect { X = Recipe.Cam2RoiX, Y = Recipe.Cam2RoiY, Width = Recipe.Cam2RoiW, Height = Recipe.Cam2RoiH }
    };

    private void OnRecipeChanged(Recipe recipe)
    {
        _currentRecipe = recipe;
        _inspectionService.ApplyRecipe(recipe);
        ApplyCameraParameters();
    }

    private void OnAlarmStateChanged(bool isAlarm, string message)
    {
        IsAlarm = isAlarm;
        AlarmMessage = message;
    }

    private void ApplyCameraParameters()
    {
        if (Camera1.IsConnected)
        {
            Camera1.ApplyExposure(_currentRecipe.ExposureTimeUs);
            Camera1.ApplyGain(_currentRecipe.GainDb);
        }
        if (Camera2.IsConnected)
        {
            Camera2.ApplyExposure(_currentRecipe.ExposureTimeUs);
            Camera2.ApplyGain(_currentRecipe.GainDb);
        }
    }

    private void PostToUiThread(Action action)
    {
        if (_syncContext == null || SynchronizationContext.Current == _syncContext)
            action();
        else
            _syncContext.Post(_ => action(), null);
    }

    #region 카메라 제어

    [RelayCommand]
    private async Task ConnectCamerasAsync()
    {
        try
        {
            _logService.Log("INFO", "카메라 연결 시도...");
            StatusMessage = "카메라 연결 중...";

            var results = await Task.WhenAll(Camera1.ConnectAsync(), Camera2.ConnectAsync());

            _logService.Log("INFO", $"{Camera1.Name} {(results[0] ? "연결 성공" : "연결 실패 - 더미 모드")}");
            _logService.Log("INFO", $"{Camera2.Name} {(results[1] ? "연결 성공" : "연결 실패 - 더미 모드")}");
            StatusMessage = $"CAM1={CamStatus(results[0])} CAM2={CamStatus(results[1])}";

            ApplyCameraParameters();
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"카메라 연결 실패: {ex.Message}");
            StatusMessage = "카메라 연결 오류";
            ShowError($"카메라 연결 중 오류가 발생했습니다.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void DisconnectCameras()
    {
        Camera1.Disconnect();
        Camera2.Disconnect();
        StatusMessage = "카메라 연결 해제됨";
        _logService.Log("INFO", "모든 카메라 연결 해제");
    }

    private static string CamStatus(bool connected) => connected ? "연결" : "미연결";

    #endregion

    #region 실행 제어

    [RelayCommand]
    private async Task StartAsync()
    {
        try
        {
            _ioService.Start();

            if (Camera1.IsConnected)
            {
                await Camera1.StartGrabAsync();
                _logService.Log("INFO", $"{Camera1.Name} 그랩 시작 (IO 트리거 대기)");
            }
            if (Camera2.IsConnected)
            {
                await Camera2.StartGrabAsync();
                _logService.Log("INFO", $"{Camera2.Name} 그랩 시작 (IO 트리거 대기)");
            }

            IsRunning = true;
            StatusMessage = "검사 실행 중 - IO 트리거 대기";
            _logService.Log("INFO", "검사 시작");

            if (AutoTrigger)
            {
                _ioService.StartAutoTrigger(Recipe.TriggerInterval);
                _logService.Log("INFO", $"자동 트리거 시작 (주기: {Recipe.TriggerInterval}ms)");
            }
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"검사 시작 실패: {ex.Message}");
            StatusMessage = "시작 오류";
            ShowError($"검사 시작 중 오류가 발생했습니다.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        IsRunning = false;
        _ioService.Stop();
        Camera1.StopGrab();
        Camera2.StopGrab();
        StatusMessage = "정지됨";
        _logService.Log("INFO", "검사 정지");
    }

    [RelayCommand]
    private void ManualTrigger()
    {
        if (!IsRunning) return;
        _ioService.FireTrigger(0);
        _ioService.FireTrigger(1);
        _logService.Log("TRIG", "수동 트리거 (CH0, CH1)");
    }

    [RelayCommand]
    private void ResetCount()
    {
        var result = System.Windows.MessageBox.Show(
            "모든 검사 통계를 초기화하시겠습니까?",
            "초기화 확인",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        Statistics.Reset();
        Camera1.LastResult = null;
        Camera2.LastResult = null;
        _alarmService.Reset();
        _logService.Log("INFO", "카운트 초기화");
    }

    partial void OnAutoTriggerChanged(bool value)
    {
        if (!IsRunning) return;

        if (value)
        {
            _ioService.StartAutoTrigger(Recipe.TriggerInterval);
            _logService.Log("INFO", $"자동 트리거 ON (주기: {Recipe.TriggerInterval}ms)");
        }
        else
        {
            _ioService.StopAutoTrigger();
            _logService.Log("INFO", "자동 트리거 OFF");
        }
    }

    #endregion

    #region 이미지 처리

    private void OnTriggerReceived(object? sender, IOSignal signal)
    {
        if (!IsRunning) return;

        if (signal.Channel == 0 && !Camera1.IsConnected)
            ProcessImage(_testImageService.Generate(), Camera1);
        else if (signal.Channel == 1 && !Camera2.IsConnected)
            ProcessImage(_testImageService.Generate(), Camera2);
    }

    private void OnImageAcquired(object? sender, Mat image)
    {
        if (sender is CameraViewModel camera)
            ProcessImage(image, camera);
    }

    private void ProcessImage(Mat image, CameraViewModel camera)
    {
        if (!_processLock.Wait(0))
        {
            _logService.Log("WARN", $"[{camera.Name}] 프레임 드롭 (처리 중)");
            image.Dispose();
            return;
        }

        try
        {
            // ── Background thread: inspection + overlay ──
            var roi = camera == Camera1 ? _currentRecipe.Camera1Roi : _currentRecipe.Camera2Roi;
            var (result, overlay) = _inspectionService.Inspect(image, roi);
            result.CameraName = camera.Name;

            // ── Background: convert overlay to frozen BitmapSource ──
            var bitmapSource = overlay.ToBitmapSource();
            bitmapSource.Freeze();
            overlay.Dispose();

            // ── Background: save original image + CSV log ──
            var savedPath = _imageSaveService.Save(image, result);
            if (savedPath != null)
                result.ImagePath = savedPath;
            _resultLogService.Log(result);

            // ── Background: IO output ──
            var cameraIndex = camera == Camera1 ? 0 : 1;
            _ioOutputService.OutputResult(cameraIndex, result.IsPass);

            // ── UI thread only (non-blocking) ──
            PostToUiThread(() =>
            {
                camera.UpdateDisplay(bitmapSource, result);
                Statistics.AddResult(result);
                _alarmService.CheckResult(camera.Name, result.IsPass);
                StatusMessage = $"[{camera.Name}] #{result.Id} {(result.IsPass ? "PASS" : "FAIL")} - {result.Description}";
                _logService.Log(result.IsPass ? "PASS" : "FAIL",
                    $"[{camera.Name}] #{result.Id} Score={result.Score}% Pads={result.PadCount} | Total={Statistics.TotalCount} P={Statistics.PassCount} F={Statistics.FailCount} Rate={Statistics.PassRate}%");
            });
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"[{camera.Name}] {ex.Message}");
            PostToUiThread(() => StatusMessage = $"[{camera.Name}] 처리 오류: {ex.Message}");
        }
        finally
        {
            _processLock.Release();
            image.Dispose();
        }
    }

    #endregion

    #region 알람

    [RelayCommand]
    private void ClearAlarm()
    {
        _alarmService.Clear();
    }

    #endregion

    #region 로그

    [RelayCommand]
    private void ClearLog()
    {
        _logService.Clear();
        _logService.Log("INFO", "로그 초기화");
    }

    #endregion

    #region 유틸

    private void ShowError(string message)
    {
        PostToUiThread(() =>
            System.Windows.MessageBox.Show(message, "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { Camera1.ImageAcquired -= OnImageAcquired; } catch { }
        try { Camera2.ImageAcquired -= OnImageAcquired; } catch { }
        try { Recipe.RecipeChanged -= OnRecipeChanged; } catch { }
        try { _ioService.TriggerReceived -= OnTriggerReceived; } catch { }
        try { _alarmService.AlarmStateChanged -= OnAlarmStateChanged; } catch { }
        try { _diskMonitorService.DiskSpaceLow -= OnDiskSpaceLow; } catch { }
        try { _ioService.Dispose(); } catch { }
        try { Camera1.Dispose(); } catch { }
        try { Camera2.Dispose(); } catch { }
        try { Statistics.Dispose(); } catch { }
        try { _imageCleanupService.Dispose(); } catch { }
        try { _diskMonitorService.Dispose(); } catch { }
        try { _processLock.Dispose(); } catch { }
    }
}
