// Do NOT add 'using Ultima' anywhere in this file or files that use this bridge.
// The compiler resolves 'Ultima.X' as 'WorldPainterUO.FileFormats.Ultima.X' due to
// namespace prefix collision. All SDK access is via global::Ultima.* here only.
using System;
using System.IO;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Central bridge to the Ultima SDK (Ultima.dll / Ultima source project).
/// All SDK calls in WorldPainterUO go through this class.
///
/// PATH PRIORITY RULES:
///   1. Settings path (set via <see cref="InitializeFromSettings"/>) always wins.
///      This is the user's UO client data folder (contains radarcol.mul, tiledata.mul, art.mul, etc.).
///   2. Map file directory is used as a fallback ONLY when no Settings path has been set.
///      This handles the simple case where map files live next to client data.
///
/// Calling <see cref="InitializeFromSettings"/> with a new path always re-initializes
/// the SDK and clears the radar color loaded flag so callers can reload colors.
/// </summary>
public static class UltimaSDKBridge
{
    private static string? _settingsPath;   // Set by user via Settings dialog
    private static string? _fallbackPath;   // Map file directory, used only when _settingsPath is null
    private static bool _initialized;
    private static bool _radarColLoaded;

    /// <summary>
    /// Whether the bridge has been pointed at a valid Settings-provided UO data path.
    /// When true, map-file-directory fallback is ignored.
    /// </summary>
    public static bool HasSettingsPath => !string.IsNullOrWhiteSpace(_settingsPath);

    /// <summary>
    /// The active data path the SDK is currently pointing at.
    /// Returns the Settings path if set, otherwise the fallback map-directory path.
    /// </summary>
    public static string? ActivePath => HasSettingsPath ? _settingsPath : _fallbackPath;

    // ── Pre-built SDK map instances ───────────────────────────────────────────
    private static global::Ultima.Map SDKMap(int mapIndex) => mapIndex switch
    {
        0 => global::Ultima.Map.Felucca,
        1 => global::Ultima.Map.Trammel,
        2 => global::Ultima.Map.Ilshenar,
        3 => global::Ultima.Map.Malas,
        4 => global::Ultima.Map.Tokuno,
        5 => global::Ultima.Map.TerMur,
        _ => global::Ultima.Map.Felucca,
    };

    /// <summary>
    /// Called on app startup and whenever the user changes the UO data path in Settings.
    /// This path takes priority over any map-file-directory fallback.
    /// Always re-initializes the SDK so new radar/art/tiledata files are picked up.
    /// </summary>
    public static void InitializeFromSettings(string? dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            _settingsPath = null;
            if (_fallbackPath != null)
                ApplyPath(_fallbackPath);
            return;
        }

        _settingsPath = dataPath;
        _radarColLoaded = false;
        ApplyPath(dataPath);
    }

    /// <summary>
    /// Called by <see cref="UltimaMapReader"/> when opening a map file.
    /// Only takes effect if no Settings path has been configured — never
    /// overrides the user's explicitly chosen UO data folder.
    /// </summary>
    public static void InitializeFromMapDirectory(string mapDirectory)
    {
        _fallbackPath = mapDirectory;

        if (HasSettingsPath)
            return;

        ApplyPath(mapDirectory);
    }

    /// <summary>
    /// Reads every land tile in the map at <paramref name="mapIndex"/> into
    /// <paramref name="worldMap"/>. The SDK handles block ordering and UOP
    /// decompression automatically.
    /// </summary>
    public static void ReadMapTiles(int mapIndex, WorldMap worldMap)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "UltimaSDKBridge must be initialized before reading map tiles. " +
                "Call InitializeFromSettings or InitializeFromMapDirectory first.");

        var sdk = SDKMap(mapIndex);
        var w = worldMap.Dimensions.Width;
        var h = worldMap.Dimensions.Height;

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            var tile = sdk.Tiles.GetLandTile(x, y);
            worldMap.Terrain[x, y] = (ushort)tile.Id;
            worldMap.Height [x, y] = (sbyte) tile.Z;
        }
    }

    /// <summary>
    /// Returns the BGR555 radar color for a land tile ID.
    /// </summary>
    public static ushort GetLandRadarColor(int tileId)
    {
        EnsureRadarCol();
        return global::Ultima.RadarCol.GetLandColor(tileId);
    }

    /// <summary>
    /// Returns the BGR555 radar color for a static tile ID.
    /// radarcol.mul stores statics at offset 0x4000.
    /// </summary>
    public static ushort GetStaticRadarColor(int staticId)
    {
        EnsureRadarCol();
        return global::Ultima.RadarCol.GetLandColor(0x4000 + staticId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void ApplyPath(string path)
    {
        global::Ultima.Files.SetMulPath(path);
        _initialized = true;
    }

    private static void EnsureRadarCol()
    {
        if (_radarColLoaded) return;
        _ = global::Ultima.RadarCol.GetLandColor(0);
        _radarColLoaded = true;
    }
}
