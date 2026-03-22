using System.Text.Json;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class RecipeServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RecipeService _svc;

    public RecipeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PadInspectorRecipeTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = Options.Create(new RecipeSettings
        {
            BasePath = _tempDir,
            DefaultRecipeName = "Default"
        });
        _svc = new RecipeService(settings);
    }

    [Fact]
    public void Constructor_CreatesDefaultRecipe()
    {
        Assert.Contains("Default", _svc.RecipeNames);
        Assert.Equal("Default", _svc.CurrentRecipe.Name);
    }

    [Fact]
    public void Save_CreatesJsonFile()
    {
        var recipe = new Recipe { Name = "TestRecipe", Description = "Test" };
        _svc.Save(recipe);

        var path = Path.Combine(_tempDir, "TestRecipe.json");
        Assert.True(File.Exists(path));
        Assert.Contains("TestRecipe", _svc.RecipeNames);
    }

    [Fact]
    public void Save_CreatesBackup()
    {
        var recipe = new Recipe { Name = "BackupTest", Description = "V1" };
        _svc.Save(recipe);

        recipe.Description = "V2";
        _svc.Save(recipe);

        var bakPath = Path.Combine(_tempDir, "BackupTest.json.bak");
        Assert.True(File.Exists(bakPath));

        var bakContent = File.ReadAllText(bakPath);
        Assert.Contains("V1", bakContent);
    }

    [Fact]
    public void Load_RestoresRecipe()
    {
        var recipe = new Recipe { Name = "LoadTest", ThresholdValue = 200 };
        _svc.Save(recipe);

        _svc.Load("LoadTest");
        Assert.Equal("LoadTest", _svc.CurrentRecipe.Name);
        Assert.Equal(200, _svc.CurrentRecipe.ThresholdValue);
    }

    [Fact]
    public void Load_NonExistent_KeepsCurrent()
    {
        var before = _svc.CurrentRecipe.Name;
        _svc.Load("NonExistentRecipe");
        Assert.Equal(before, _svc.CurrentRecipe.Name);
    }

    [Fact]
    public void Delete_RemovesRecipe()
    {
        var recipe = new Recipe { Name = "ToDelete" };
        _svc.Save(recipe);
        Assert.Contains("ToDelete", _svc.RecipeNames);

        _svc.Delete("ToDelete");
        Assert.DoesNotContain("ToDelete", _svc.RecipeNames);
        Assert.False(File.Exists(Path.Combine(_tempDir, "ToDelete.json")));
    }

    [Fact]
    public void SaveAs_CreatesNewRecipe()
    {
        var recipe = new Recipe { ThresholdValue = 150 };
        _svc.SaveAs("NewName", recipe);

        Assert.Contains("NewName", _svc.RecipeNames);
        Assert.Equal("NewName", _svc.CurrentRecipe.Name);
    }

    [Fact]
    public void Refresh_UpdatesRecipeList()
    {
        // 직접 파일 추가
        var json = JsonSerializer.Serialize(new Recipe { Name = "External" });
        File.WriteAllText(Path.Combine(_tempDir, "External.json"), json);

        _svc.Refresh();
        Assert.Contains("External", _svc.RecipeNames);
    }

    [Fact]
    public void Error_EventFires_OnCorruptFile()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Corrupt.json"), "NOT_JSON");
        _svc.Refresh();

        string? error = null;
        _svc.Error += msg => error = msg;

        _svc.Load("Corrupt");
        Assert.NotNull(error);
        Assert.Contains("Corrupt", error);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
