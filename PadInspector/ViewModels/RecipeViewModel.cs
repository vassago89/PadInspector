using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class RecipeViewModel : ObservableObject
{
    private readonly IRecipeService _recipeService;
    private readonly ILogService _logService;
    private bool _isInternalSelection;

    [ObservableProperty] private string? _selectedName;
    [ObservableProperty] private string _recipeName = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private double _threshold = 128;
    [ObservableProperty] private double _minArea = 0.01;
    [ObservableProperty] private double _maxArea = 0.5;
    [ObservableProperty] private double _passScore = 5.0;
    [ObservableProperty] private int _exposure = 500;
    [ObservableProperty] private int _gain = 0;
    [ObservableProperty] private int _triggerInterval = 2000;

    // Camera1 ROI
    [ObservableProperty] private double _cam1RoiX = 0;
    [ObservableProperty] private double _cam1RoiY = 0;
    [ObservableProperty] private double _cam1RoiW = 1;
    [ObservableProperty] private double _cam1RoiH = 1;

    // Camera2 ROI
    [ObservableProperty] private double _cam2RoiX = 0;
    [ObservableProperty] private double _cam2RoiY = 0;
    [ObservableProperty] private double _cam2RoiW = 1;
    [ObservableProperty] private double _cam2RoiH = 1;

    public ObservableCollection<string> Names { get; } = new();

    public event Action<Recipe>? RecipeChanged;

    public RecipeViewModel(IRecipeService recipeService, ILogService logService)
    {
        _recipeService = recipeService;
        _logService = logService;
        RefreshList();
        ApplyToUI(_recipeService.CurrentRecipe);
        _isInternalSelection = true;
        SelectedName = _recipeService.CurrentRecipe.Name;
        _isInternalSelection = false;
    }

    partial void OnSelectedNameChanged(string? value)
    {
        if (string.IsNullOrEmpty(value) || _isInternalSelection) return;
        _recipeService.Load(value);
        var recipe = _recipeService.CurrentRecipe;
        ApplyToUI(recipe);
        RecipeChanged?.Invoke(recipe);
        _logService.Log("RECIPE", $"레시피 변경: {recipe.Name} (Threshold={recipe.ThresholdValue}, PassScore={recipe.PassScoreThreshold})");
    }

    [RelayCommand]
    private void Save()
    {
        var recipe = BuildFromUI();
        _recipeService.Save(recipe);
        RecipeChanged?.Invoke(recipe);
        _logService.Log("RECIPE", $"레시피 저장: {recipe.Name}");
        RefreshList();
        SetSelectedSilently(recipe.Name);
    }

    [RelayCommand]
    private void SaveAs()
    {
        var newName = RecipeName.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        var recipe = BuildFromUI();
        _recipeService.SaveAs(newName, recipe);
        RecipeChanged?.Invoke(recipe);
        _logService.Log("RECIPE", $"레시피 다른이름 저장: {newName}");
        RefreshList();
        SetSelectedSilently(newName);
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedName == null || Names.Count <= 1) return;

        var name = SelectedName;
        _recipeService.Delete(name);
        _logService.Log("RECIPE", $"레시피 삭제: {name}");
        RefreshList();
        SelectedName = Names.FirstOrDefault();
    }

    private void RefreshList()
    {
        Names.Clear();
        foreach (var name in _recipeService.RecipeNames)
            Names.Add(name);
    }

    private void ApplyToUI(Recipe recipe)
    {
        RecipeName = recipe.Name;
        Description = recipe.Description;
        Threshold = recipe.ThresholdValue;
        MinArea = recipe.MinAreaRatio;
        MaxArea = recipe.MaxAreaRatio;
        PassScore = recipe.PassScoreThreshold;
        Exposure = recipe.ExposureTimeUs;
        Gain = recipe.GainDb;
        TriggerInterval = recipe.TriggerIntervalMs;
        Cam1RoiX = recipe.Camera1Roi.X;
        Cam1RoiY = recipe.Camera1Roi.Y;
        Cam1RoiW = recipe.Camera1Roi.Width;
        Cam1RoiH = recipe.Camera1Roi.Height;
        Cam2RoiX = recipe.Camera2Roi.X;
        Cam2RoiY = recipe.Camera2Roi.Y;
        Cam2RoiW = recipe.Camera2Roi.Width;
        Cam2RoiH = recipe.Camera2Roi.Height;
    }

    private Recipe BuildFromUI()
    {
        return new Recipe
        {
            Name = RecipeName,
            Description = Description,
            ThresholdValue = Threshold,
            MinAreaRatio = MinArea,
            MaxAreaRatio = MaxArea,
            PassScoreThreshold = PassScore,
            ExposureTimeUs = Exposure,
            GainDb = Gain,
            TriggerChannel = 0,
            TriggerIntervalMs = TriggerInterval,
            Camera1Roi = new RoiRect { X = Cam1RoiX, Y = Cam1RoiY, Width = Cam1RoiW, Height = Cam1RoiH },
            Camera2Roi = new RoiRect { X = Cam2RoiX, Y = Cam2RoiY, Width = Cam2RoiW, Height = Cam2RoiH },
            CreatedAt = _recipeService.CurrentRecipe.CreatedAt
        };
    }

    private void SetSelectedSilently(string name)
    {
        _isInternalSelection = true;
        SelectedName = name;
        _isInternalSelection = false;
    }
}
