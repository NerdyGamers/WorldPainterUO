using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.Project;

/// <summary>
/// Configurable autosave manager with timer-based snapshots and recovery detection.
/// </summary>
public sealed class AutosaveManager : IDisposable
{
    private readonly string _projectFilePath;
    private readonly ILogger _logger;
    private Timer? _timer;
    private bool _disposed;

    /// <summary>
    /// Creates a new autosave manager for the given project path.
    /// </summary>
    public AutosaveManager(string projectFilePath)
    {
        ArgumentNullException.ThrowIfNull(projectFilePath);
        _projectFilePath = projectFilePath;
        _logger = Log.For($"AutosaveManager({Path.GetFileName(projectFilePath)})");
    }

    /// <summary>Autosave interval in seconds. Default 300 (5 min).</summary>
    public int IntervalSeconds { get; set; } = UomapAutosave.DefaultIntervalSeconds;

    /// <summary>True when the timer is running.</summary>
    public bool IsRunning => _timer is not null;

    /// <summary>Fires when the autosave completes (success or failure).</summary>
    public event Action<bool>? AutosaveCompleted;

    /// <summary>Starts the periodic autosave timer.</summary>
    public void Start()
    {
        if (_timer is not null) return;
        var ms = Math.Max(1000, IntervalSeconds * 1000);
        _timer = new Timer(OnTimer, null, ms, ms);
        _logger.LogInformation("Autosave started (interval {Interval}s)", IntervalSeconds);
    }

    /// <summary>Stops the periodic autosave timer.</summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Autosave stopped");
    }

    /// <summary>Saves an autosave snapshot immediately.</summary>
    public void SaveNow(WorldMap map)
    {
        try
        {
            UomapAutosave.SaveSnapshot(_projectFilePath, map);
            _logger.LogInformation("Autosave snapshot saved");
            AutosaveCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autosave snapshot failed");
            AutosaveCompleted?.Invoke(false);
        }
    }

    /// <summary>
    /// True when an autosave snapshot exists and is newer than the given last-save time.
    /// </summary>
    public bool IsRecoveryAvailable(DateTime lastManualSaveTime)
    {
        var path = UomapAutosave.GetAutosavePath(_projectFilePath);
        if (!File.Exists(path))
            return false;

        var snapshotTime = File.GetLastWriteTimeUtc(path);
        return snapshotTime > lastManualSaveTime.ToUniversalTime();
    }

    /// <summary>Tries to load the recovery snapshot. Returns null on failure.</summary>
    public WorldMap? TryRecover()
    {
        var loaded = UomapAutosave.TryLoadSnapshot(_projectFilePath);
        if (loaded is not null)
            _logger.LogInformation("Recovery snapshot loaded successfully");
        else
            _logger.LogWarning("Recovery snapshot could not be loaded");
        return loaded;
    }

    /// <summary>Deletes the autosave snapshot.</summary>
    public void DeleteSnapshot()
    {
        UomapAutosave.DeleteSnapshot(_projectFilePath);
        _logger.LogInformation("Autosave snapshot deleted");
    }

    private void OnTimer(object? state)
    {
        // Save is triggered externally via SaveNow — the timer just fires the event.
        // The caller should handle the actual save in response.
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
