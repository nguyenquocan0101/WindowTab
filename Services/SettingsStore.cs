using System.Text.Json;
using WindowTab.Models;

namespace WindowTab.Services;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string SettingsPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowTab",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null)
                {
                    settings.Normalize();
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Start with usable defaults when a stale or unreadable settings file is present.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
