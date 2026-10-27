using System.Collections.ObjectModel;
using System.Text.Json;
using RelayDesk.Models;

namespace RelayDesk.Services;

public static class ConfigStore
{
    public static string DirectoryPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RelayDesk");

    private static string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(SettingsPath)) return new AppSettings();
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions)
               ?? new AppSettings();
    }

    public static void Save(IEnumerable<Destination> targets, VideoEncoder encoder)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(new AppSettings
        {
            Destinations = targets.ToList(), Encoder = encoder.ToString()
        }, JsonOptions);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, SettingsPath, overwrite: true);
    }
}
