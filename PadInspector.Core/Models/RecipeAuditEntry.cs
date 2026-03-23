namespace PadInspector.Models;

public class RecipeAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty; // Save, Delete, Load, Import, Export
    public string RecipeName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
