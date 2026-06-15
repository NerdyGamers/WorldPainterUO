using WorldPainterUO.Core;

namespace WorldPainterUO.Project;

public static class UomapAutosave
{
    /// <summary>
    /// Default autosave interval in seconds.
    /// </summary>
    public const int DefaultIntervalSeconds = 300;

    /// <summary>
    /// Gets the autosave file path for a given project path.
    /// </summary>
    public static string GetAutosavePath(string projectFilePath)
    {
        ArgumentNullException.ThrowIfNull(projectFilePath);
        return projectFilePath + ".autosave";
    }

    /// <summary>
    /// Saves an autosave snapshot. Uses a write-then-rename strategy
    /// to avoid corrupting a previous snapshot on failure.
    /// </summary>
    public static void SaveSnapshot(string projectFilePath, WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(projectFilePath);
        ArgumentNullException.ThrowIfNull(map);

        var autosavePath = GetAutosavePath(projectFilePath);
        var tempPath = autosavePath + ".tmp";

        UomapSerializer.Save(tempPath, map);

        if (File.Exists(autosavePath))
        {
            File.Delete(autosavePath);
        }

        File.Move(tempPath, autosavePath);
    }

    /// <summary>
    /// Attempts to load an autosave snapshot. Returns null if no snapshot exists
    /// or if it cannot be read.
    /// </summary>
    public static WorldMap? TryLoadSnapshot(string projectFilePath)
    {
        ArgumentNullException.ThrowIfNull(projectFilePath);

        var autosavePath = GetAutosavePath(projectFilePath);

        if (!File.Exists(autosavePath))
            return null;

        try
        {
            return UomapSerializer.Load(autosavePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes an existing autosave snapshot if present.
    /// </summary>
    public static void DeleteSnapshot(string projectFilePath)
    {
        var autosavePath = GetAutosavePath(projectFilePath);
        if (File.Exists(autosavePath))
        {
            File.Delete(autosavePath);
        }
    }

    /// <summary>
    /// Returns true if an autosave snapshot exists for the given project path.
    /// </summary>
    public static bool SnapshotExists(string projectFilePath)
    {
        return File.Exists(GetAutosavePath(projectFilePath));
    }
}
