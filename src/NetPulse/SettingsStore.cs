using System.Text.Json;

namespace NetPulse;

public sealed class AppSettings
{
    public string? SelectedAdapterId { get; set; }
    public int WidgetX { get; set; } = -1;
    public int WidgetY { get; set; } = -1;
    public bool ShowTaskbarStrip { get; set; } = true;
    public bool UseMegabits { get; set; }
    public bool LaunchAtStartup { get; set; }
}

public sealed class SettingsStore
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetPulse", "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }
}
