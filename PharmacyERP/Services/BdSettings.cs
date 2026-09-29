using System.Text.Json;

namespace PharmacyERP.Services;

/// <summary>Shop profile + Single/Multi branch + language/theme. Stored in shopsettings.json.</summary>
public class BdShopSettings
{
    public string PharmacyName { get; set; } = "My Pharmacy";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? DgdaLicense { get; set; }
    public string? TradeLicense { get; set; }
    public string Mode { get; set; } = "Single"; // Single | Multi
    public int? CurrentBranchId { get; set; }
    public string? CurrentBranchName { get; set; }
    public string Language { get; set; } = "en"; // en | bn
    public string Theme { get; set; } = "Light"; // Light | Dark
    public bool BigFont { get; set; }
    public string Currency { get; set; } = "BDT";
}

public static class BdSettings
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "shopsettings.json");
    public static BdShopSettings Current { get; private set; } = new();

    static BdSettings() => Load();

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<BdShopSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* keep defaults */ }
    }

    public static void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true })); }
        catch { /* ignore */ }
    }

    public static bool IsMulti => string.Equals(Current.Mode, "Multi", StringComparison.OrdinalIgnoreCase);
}
