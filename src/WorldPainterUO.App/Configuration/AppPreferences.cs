using System;
using System.IO;
using System.Text.Json;

namespace WorldPainterUO.App.Configuration;

/// <summary>
/// User preferences stored as JSON in the OS user config directory.
/// </summary>
public sealed class AppPreferences
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Autosave interval in seconds (default 300 = 5 min).</summary>
    public int AutosaveIntervalSeconds { get; set; } = 300;

    /// <summary>Default map width for new maps.</summary>
    public int DefaultMapWidth { get; set; } = 128;

    /// <summary>Default map height for new maps.</summary>
    public int DefaultMapHeight { get; set; } = 128;

    /// <summary>Default facet name for new maps.</summary>
    public string DefaultFacet { get; set; } = "Felucca";

    /// <summary>Path to UO data files (tiledata.mul, radarcol.mul, etc.).</summary>
    public string? UoDataPath { get; set; }

    /// <summary>UI theme: "Light" or "Dark".</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Gets the preferences directory path.</summary>
    public static string DirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WorldPainterUO");

    /// <summary>Gets the full preferences file path.</summary>
    public static string FilePath => Path.Combine(DirectoryPath, "preferences.json");

    /// <summary>Loads preferences from disk, or returns defaults if not found.</summary>
    public static AppPreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppPreferences();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppPreferences>(json, JsonOptions) ?? new AppPreferences();
        }
        catch
        {
            return new AppPreferences();
        }
    }

    /// <summary>Saves preferences to disk.</summary>
    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
