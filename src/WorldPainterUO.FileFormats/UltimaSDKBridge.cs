// Do NOT add 'using Ultima' anywhere in this file or files that use this bridge.
// The compiler resolves 'Ultima.X' as 'WorldPainterUO.FileFormats.Ultima.X' due to
// namespace prefix collision. All SDK access is via global::Ultima.* here only.
using System;
using System.IO;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Central bridge to the Ultima SDK (Ultima.dll / Ultima source project).
/// All SDK calls in WorldPainterUO go through this class — no other file should
/// reference global::Ultima.* directly. This keeps the namespace collision issue
/// contained in one place and makes future SDK swaps trivial.
/// </summary>
public static class UltimaSDKBridge
{
    private static string? _loadedPath;
    private static bool _initialized;

    // ── Pre-built SDK map instances (stateful singletons in the SDK) ──────────
    // These are the correct entry points — do NOT call new Ultima.Map(...)
    // as the constructor arguments differ across SDK versions and the static
    // instances carry correct block counts, cache state, and UOP support.
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
    /// Points the Ultima SDK at <paramref name="dataPath"/> and forces it to
    /// initialize its file table. Safe to call multiple times — reinitializes
    /// only when the path changes.
    /// </summary>
    public static void Initialize(string dataPath)
    {
        if (_initialized && _loadedPath == dataPath)
            return;

        global::Ultima.Files.SetMulPath(dataPath);
        _loadedPath = dataPath;
        _initialized = true;
    }

    /// <summary>
    /// Reads every land tile in the map at <paramref name="mapIndex"/> into
    /// <paramref name="worldMap"/>. The SDK handles column-major block ordering,
    /// UOP decompression, and MUL seeking automatically.
    /// </summary>
    public static void ReadMapTiles(int mapIndex, WorldMap worldMap)
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "UltimaSDKBridge.Initialize(dataPath) must be called before reading map tiles.");

        var sdk = SDKMap(mapIndex);
        var w = worldMap.Width;
        var h = worldMap.Height;

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
    /// Indexes directly into radarcol.mul via the SDK.
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

    // Force the SDK to load radarcol.mul on first radar-color access.
    private static bool _radarColLoaded;
    private static void EnsureRadarCol()
    {
        if (_radarColLoaded) return;
        // Touch index 0 to trigger lazy file load inside the SDK.
        _ = global::Ultima.RadarCol.GetLandColor(0);
        _radarColLoaded = true;
    }
}
