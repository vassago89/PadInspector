using PadInspector.Models;

namespace PadInspector.Services;

public interface IRecipeService
{
    event EventHandler<Recipe>? RecipeChanged;
    event Action<string>? Error;
    Recipe CurrentRecipe { get; }
    IReadOnlyList<string> RecipeNames { get; }
    IReadOnlyList<RecipeAuditEntry> AuditLog { get; }
    void Load(string name);
    void Save(Recipe recipe);
    void SaveAs(string name, Recipe recipe);
    void Delete(string name);
    void Refresh();
}
