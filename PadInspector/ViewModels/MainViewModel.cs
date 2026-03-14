using System.Collections.ObjectModel;
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
    private readonly ICameraService _cameraService;
    private readonly IIOService _ioService;
    private readonly IInspectionService _inspectionService;
    private readonly IRecipeService _recipeService;
    private readonly LogSettings _logSettings;
    private readonly InspectionSettings _inspectionSettings;

    [ObservableProperty] private BitmapSource? _currentImage;
    [ObservableProperty] private bool _isCameraConnected;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusMessage = "대기 중";
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _passCount;
    [ObservableProperty] private int _failCount;
    [ObservableProperty] private double _passRate;
    [ObservableProperty] private bool _autoTrigger;
    [ObservableProperty] private int _triggerInterval = 2000;
    [ObservableProperty] private InspectionResult? _lastResult;

    // 레시피
    [ObservableProperty] private string? _selectedRecipeName;
    [ObservableProperty] private string _recipeName = "";
    [ObservableProperty] private string _recipeDescription = "";
    [ObservableProperty] private double _recipeThreshold = 128;
    [ObservableProperty] private double _recipeMinArea = 0.01;
    [ObservableProperty] private double _recipeMaxArea = 0.5;
    [ObservableProperty] private double _recipePassScore = 5.0;
    [ObservableProperty] private int _recipeExposure = 500;
    [ObservableProperty] private int _recipeGain = 0;

    public ObservableCollection<InspectionResult> Results { get; } = new();
    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<string> RecipeNames { get; } = new();

    public MainViewModel(
        ICameraService cameraService,
        IIOService ioService,
        IInspectionService inspectionService,
        IRecipeService recipeService,
        IOptions<LogSettings> logOptions,
        IOptions<InspectionSettings> inspectionOptions)
    {
        _cameraService = cameraService;
        _ioService = ioService;
        _inspectionService = inspectionService;
        _recipeService = recipeService;
        _logSettings = logOptions.Value;
        _inspectionSettings = inspectionOptions.Value;

        _ioService.TriggerReceived += OnTriggerReceived;
        _cameraService.ImageGrabbed += OnImageGrabbed;

        // 레시피 목록 로드
        RefreshRecipeList();
        ApplyRecipeToUI(_recipeService.CurrentRecipe);
        SelectedRecipeName = _recipeService.CurrentRecipe.Name;

        Log("INFO", $"시스템 초기화 완료 | 레시피: {_recipeService.CurrentRecipe.Name}");
    }

    private void RefreshRecipeList()
    {
        RecipeNames.Clear();
        foreach (var name in _recipeService.RecipeNames)
            RecipeNames.Add(name);
    }

    private void ApplyRecipeToUI(Recipe recipe)
    {
        RecipeName = recipe.Name;
        RecipeDescription = recipe.Description;
        RecipeThreshold = recipe.ThresholdValue;
        RecipeMinArea = recipe.MinAreaRatio;
        RecipeMaxArea = recipe.MaxAreaRatio;
        RecipePassScore = recipe.PassScoreThreshold;
        RecipeExposure = recipe.ExposureTimeUs;
        RecipeGain = recipe.GainDb;
        TriggerInterval = recipe.TriggerIntervalMs;
    }

    private Recipe BuildRecipeFromUI()
    {
        return new Recipe
        {
            Name = RecipeName,
            Description = RecipeDescription,
            ThresholdValue = RecipeThreshold,
            MinAreaRatio = RecipeMinArea,
            MaxAreaRatio = RecipeMaxArea,
            PassScoreThreshold = RecipePassScore,
            ExposureTimeUs = RecipeExposure,
            GainDb = RecipeGain,
            TriggerChannel = 0,
            TriggerIntervalMs = TriggerInterval,
            CreatedAt = _recipeService.CurrentRecipe.CreatedAt
        };
    }

    partial void OnSelectedRecipeNameChanged(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        _recipeService.Load(value);
        var recipe = _recipeService.CurrentRecipe;
        ApplyRecipeToUI(recipe);
        _inspectionService.ApplyRecipe(recipe);
        Log("RECIPE", $"레시피 변경: {recipe.Name} (Threshold={recipe.ThresholdValue}, PassScore={recipe.PassScoreThreshold})");
    }

    [RelayCommand]
    private void SaveRecipe()
    {
        var recipe = BuildRecipeFromUI();
        _recipeService.Save(recipe);
        _inspectionService.ApplyRecipe(recipe);
        RefreshRecipeList();
        SelectedRecipeName = recipe.Name;
        Log("RECIPE", $"레시피 저장: {recipe.Name}");
    }

    [RelayCommand]
    private void SaveRecipeAs()
    {
        var newName = RecipeName.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        var recipe = BuildRecipeFromUI();
        _recipeService.SaveAs(newName, recipe);
        _inspectionService.ApplyRecipe(recipe);
        RefreshRecipeList();
        SelectedRecipeName = newName;
        Log("RECIPE", $"레시피 다른이름 저장: {newName}");
    }

    [RelayCommand]
    private void DeleteRecipe()
    {
        if (SelectedRecipeName == null || RecipeNames.Count <= 1) return;

        var name = SelectedRecipeName;
        _recipeService.Delete(name);
        RefreshRecipeList();
        SelectedRecipeName = RecipeNames.FirstOrDefault();
        Log("RECIPE", $"레시피 삭제: {name}");
    }

    private void Log(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
        {
            LogMessages.Add(line);
            if (LogMessages.Count > _logSettings.MaxLogLines)
                LogMessages.RemoveAt(0);
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LogMessages.Add(line);
                if (LogMessages.Count > _logSettings.MaxLogLines)
                    LogMessages.RemoveAt(0);
            });
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
        Log("INFO", "로그 초기화");
    }

    [RelayCommand]
    private async Task ConnectCameraAsync()
    {
        Log("INFO", "카메라 연결 시도...");
        StatusMessage = "카메라 연결 중...";
        IsCameraConnected = await _cameraService.ConnectAsync();
        if (IsCameraConnected)
        {
            StatusMessage = "카메라 연결됨";
            Log("INFO", "카메라 연결 성공");
        }
        else
        {
            StatusMessage = "카메라 연결 실패";
            Log("WARN", "카메라 연결 실패 - 더미 이미지 모드로 동작합니다");
        }
    }

    [RelayCommand]
    private void DisconnectCamera()
    {
        _cameraService.Disconnect();
        IsCameraConnected = false;
        StatusMessage = "카메라 연결 해제됨";
        Log("INFO", "카메라 연결 해제");
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        _ioService.Start();
        Log("INFO", "IO 서비스 시작 (Virtual)");

        if (IsCameraConnected)
        {
            await _cameraService.StartGrabAsync();
            Log("INFO", "카메라 그랩 시작");
        }

        IsRunning = true;
        StatusMessage = "검사 실행 중";
        Log("INFO", "검사 시작");

        if (AutoTrigger)
        {
            _ioService.StartAutoTrigger(TriggerInterval);
            Log("INFO", $"자동 트리거 시작 (주기: {TriggerInterval}ms)");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        IsRunning = false;
        _ioService.Stop();
        _cameraService.StopGrab();
        StatusMessage = "정지됨";
        Log("INFO", "검사 정지");
    }

    [RelayCommand]
    private void ManualTrigger()
    {
        if (!IsRunning) return;
        _ioService.FireTrigger(0);
        Log("TRIG", "수동 트리거 발생 (CH0)");
    }

    [RelayCommand]
    private void ResetCount()
    {
        TotalCount = 0;
        PassCount = 0;
        FailCount = 0;
        PassRate = 0;
        Results.Clear();
        LastResult = null;
        Log("INFO", "카운트 초기화");
    }

    partial void OnAutoTriggerChanged(bool value)
    {
        if (IsRunning)
        {
            if (value)
            {
                _ioService.StartAutoTrigger(TriggerInterval);
                Log("INFO", $"자동 트리거 ON (주기: {TriggerInterval}ms)");
            }
            else
            {
                _ioService.StopAutoTrigger();
                Log("INFO", "자동 트리거 OFF");
            }
        }
    }

    private void OnTriggerReceived(object? sender, IOSignal signal)
    {
        if (!IsRunning) return;

        if (!IsCameraConnected)
        {
            var dummyImage = GenerateTestImage();
            ProcessImage(dummyImage);
        }
    }

    private void OnImageGrabbed(object? sender, Mat image)
    {
        ProcessImage(image);
    }

    private void ProcessImage(Mat image)
    {
        try
        {
            var bitmapSource = image.ToBitmapSource();
            bitmapSource.Freeze();

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentImage = bitmapSource;

                var result = _inspectionService.Inspect(image);
                LastResult = result;

                TotalCount++;
                if (result.IsPass)
                    PassCount++;
                else
                    FailCount++;

                PassRate = TotalCount > 0 ? Math.Round((double)PassCount / TotalCount * 100, 1) : 0;

                Results.Insert(0, result);
                if (Results.Count > _inspectionSettings.MaxResultHistory)
                    Results.RemoveAt(Results.Count - 1);

                StatusMessage = $"#{result.Id} {(result.IsPass ? "PASS" : "FAIL")} - {result.Description}";

                Log(result.IsPass ? "PASS" : "FAIL",
                    $"#{result.Id} Score={result.Score}% | {result.Description} | Total={TotalCount} Pass={PassCount} Fail={FailCount} Rate={PassRate}%");
            });
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"처리 오류: {ex.Message}";
                Log("ERR", $"이미지 처리 오류: {ex.Message}");
            });
        }
        finally
        {
            if (!IsCameraConnected)
                image.Dispose();
        }
    }

    private static Mat GenerateTestImage()
    {
        var random = Random.Shared;
        var mat = new Mat(480, 640, MatType.CV_8UC1, Scalar.All(30));

        int padCount = random.Next(1, 6);
        for (int i = 0; i < padCount; i++)
        {
            int x = random.Next(50, 550);
            int y = random.Next(50, 400);
            int w = random.Next(30, 80);
            int h = random.Next(30, 80);
            Cv2.Rectangle(mat, new Rect(x, y, w, h), Scalar.All(200), -1);
        }

        var noise = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.Randn(noise, Scalar.All(0), Scalar.All(15));
        Cv2.Add(mat, noise, mat);

        return mat;
    }

    public void Dispose()
    {
        _ioService.TriggerReceived -= OnTriggerReceived;
        _cameraService.ImageGrabbed -= OnImageGrabbed;
        _ioService.Dispose();
        _cameraService.Dispose();
    }
}
