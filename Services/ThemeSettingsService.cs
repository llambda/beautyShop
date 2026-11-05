using System.IO;
using System.Text.Json;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace BeautyStudio.Services;

public sealed class ThemeSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        BeautyStudio.Data.AppDbContext.DataDirectory, "settings.json");

    public ThemeSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save(ThemeSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Apply(string primaryHex, string secondaryHex, bool isDark)
    {
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        theme.SetPrimaryColor((Color)ColorConverter.ConvertFromString(primaryHex));
        theme.SetSecondaryColor((Color)ColorConverter.ConvertFromString(secondaryHex));
        helper.SetTheme(theme);
    }
}

public sealed class ThemeSettings
{
    public string PrimaryColor { get; set; } = "#FFE91E63";
    public string SecondaryColor { get; set; } = "#FFFFC107";
    public bool IsDark { get; set; }
}
