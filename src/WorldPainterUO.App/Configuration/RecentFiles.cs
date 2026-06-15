using System.Text.Json;

namespace WorldPainterUO.App.Configuration;

public sealed class RecentFiles
{
    private const int MaxEntries = 10;

    public List<RecentFileEntry> Entries { get; set; } = [];

    public static string FilePath => Path.Combine(AppPreferences.DirectoryPath, "recent.json");

    public static RecentFiles Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new RecentFiles();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<RecentFiles>(json) ?? new RecentFiles();
        }
        catch
        {
            return new RecentFiles();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(AppPreferences.DirectoryPath);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    public void Add(string path)
    {
        Entries.RemoveAll(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        Entries.Insert(0, new RecentFileEntry(path, DateTime.UtcNow));
        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);
        Save();
    }
}

public sealed record RecentFileEntry(string Path, DateTime LastOpened);
