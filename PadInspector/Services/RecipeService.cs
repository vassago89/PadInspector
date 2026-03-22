using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

public class RecipeService : IRecipeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public event EventHandler<Recipe>? RecipeChanged;

    private readonly string _recipeDir;
    private readonly string _defaultName;
    private readonly List<string> _recipeNames = [];

    public Recipe CurrentRecipe { get; private set; } = new();
    public IReadOnlyList<string> RecipeNames => _recipeNames;

    public RecipeService(IOptions<RecipeSettings> options)
    {
        var settings = options.Value;
        _recipeDir = Path.IsPathRooted(settings.BasePath)
            ? settings.BasePath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settings.BasePath);
        _defaultName = settings.DefaultRecipeName;
        Directory.CreateDirectory(_recipeDir);

        Refresh();

        if (_recipeNames.Count == 0)
        {
            Save(new Recipe { Name = _defaultName, Description = "기본 레시피" });
            Refresh();
        }

        Load(_recipeNames[0]);
    }

    public void Refresh()
    {
        _recipeNames.Clear();
        foreach (var file in Directory.GetFiles(_recipeDir, "*.json").OrderBy(f => f))
        {
            _recipeNames.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    public void Load(string name)
    {
        var path = GetPath(name);
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
        var recipe = JsonSerializer.Deserialize<Recipe>(json, JsonOptions);
        if (recipe == null) return;

        CurrentRecipe = recipe;
        RecipeChanged?.Invoke(this, recipe);
    }

    public void Save(Recipe recipe)
    {
        if (recipe.CreatedAt == default)
            recipe.CreatedAt = DateTime.Now;
        recipe.ModifiedAt = DateTime.Now;
        var path = GetPath(recipe.Name);
        var json = JsonSerializer.Serialize(recipe, JsonOptions);
        File.WriteAllText(path, json);

        if (!_recipeNames.Contains(recipe.Name))
        {
            _recipeNames.Add(recipe.Name);
            _recipeNames.Sort();
        }

        CurrentRecipe = recipe;
    }

    public void SaveAs(string name, Recipe recipe)
    {
        recipe.Name = name;
        recipe.CreatedAt = DateTime.Now;
        Save(recipe);
    }

    public void Delete(string name)
    {
        var path = GetPath(name);
        if (File.Exists(path))
            File.Delete(path);

        _recipeNames.Remove(name);
    }

    private string GetPath(string name) => Path.Combine(_recipeDir, $"{name}.json");
}
