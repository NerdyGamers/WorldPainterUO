using System;
using System.Collections.Generic;
using System.IO;
using Ultima;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Reads Ultima Online map files into a <see cref="WorldMap"/> using the
/// Ultima SDK directly. Block layout, column-major ordering, and UOP
/// unpacking are all handled by the SDK automatically.
///
/// PATH PRIORITY RULES:
/// 1. Settings path (set via <see cref="SetDataPath"/>) always wins.
///    This is the user's UO client data folder (radarcol.mul, tiledata.mul, etc.).
/// 2. Map file directory is used as a fallback ONLY when no Settings path is set.
///    This handles the simple case where map files live next to client data.
/// </summary>
public sealed class UltimaMapReader
{
    // ── Path-priority state ─────────────────────────────────────────────────

    private static string? _settingsPath;  // explicitly set by the user in Settings
    private static string? _fallbackPath;  // map file directory, lower priority

    /// <summary>
    /// Whether a user-configured UO data path has been set.
    /// When true, map-file-directory fallback is ignored.
    /// </summary>
    public static bool HasSettingsPath => !string.IsNullOrWhiteSpace(_settingsPath);

    /// <summary>
    /// The active data path the SDK is currently pointing at.
    /// </summary>
    public static string? ActivePath => HasSettingsPath ? _settingsPath : _fallbackPath;

    /// <summary>
    /// Sets the UO data path from user Settings. This path always takes priority
    /// over the map-file-directory fallback. Passing null or whitespace clears
    /// the Settings path and falls back to the map directory (if any).
    /// </summary>
    public static void SetDataPath(string? dataPath)
    {
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            _settingsPath = null;
            if (_fallbackPath != null)
                Files.SetMulPath(_fallbackPath);
            return;
        }

        _settingsPath = dataPath;
        Files.SetMulPath(dataPath);
    }

    // ── Dimension detection ─────────────────────────────────────────────────

    // Map index -> (Width, Height, Facet)
    private static readonly Dictionary<int, (int Width, int Height, string Facet)> KnownByIndex = new()
    {
        { 0, (6144, 4096, "Felucca")  },
        { 1, (6144, 4096, "Trammel")  },
        { 2, (2304, 1600, "Ilshenar") },
        { 3, (2560, 2048, "Malas")    },
        { 4, (1448, 1448, "Tokuno")   },
        { 5, (1280, 4096, "TerMur")   },
    };

    private static readonly Dictionary<long, (int Width, int Height, string Facet)> KnownBySize = new()
    {
        { FileSize(6144, 4096), (6144, 4096, "Felucca")  },
        { FileSize(2304, 1600), (2304, 1600, "Ilshenar") },
        { FileSize(1448, 1448), (1448, 1448, "Tokuno")   },
    };

    private static long FileSize(int width, int height)
        => (long)(width / 8) * (height / 8) * 196L;

    /// <summary>
    /// Detects map dimensions from the filename, then file size, then a
    /// block-count square estimate for unknown custom maps.
    /// </summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException("Map file not found.", filePath);

        // 1. Filename match (most reliable — "map0", "map1", etc.)
        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
            if (baseName.Contains($"map{i}") && KnownByIndex.TryGetValue(i, out var byIdx))
                return new MapDimensions(byIdx.Width, byIdx.Height, byIdx.Facet);

        // 2. File size (unique for three facets)
        if (KnownBySize.TryGetValue(info.Length, out var bySize))
            return new MapDimensions(bySize.Width, bySize.Height, bySize.Facet);

        // 3. Square estimate for unknown custom maps
        var blockCount = info.Length / 196;
        var side = (int)Math.Sqrt(blockCount * 64);
        side = (side / 8) * 8;
        return new MapDimensions(side, side, "Unknown");
    }

    // ── Map reading ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a map file using the Ultima SDK and returns a populated
    /// <see cref="WorldMap"/>. Works transparently for both .mul and .uop files.
    ///
    /// The map file directory is registered as a FALLBACK data path only —
    /// it is ignored when the user has already set a path via
    /// <see cref="SetDataPath"/>.
    ///
    /// For non-standard filenames, the exact file path is registered with the
    /// SDK under the standard key (e.g. "map0legacymul.uop") so the SDK's
    /// TileMatrix can resolve it regardless of the actual file name.
    /// </summary>
    public WorldMap Read(string filePath, MapDimensions dims)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (filePath.Length == 0)
            throw new ArgumentException("Path must not be empty.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Map file not found.", filePath);
        var isMul = filePath.EndsWith(".mul", StringComparison.OrdinalIgnoreCase);
        if (isMul)
        {
            var requiredBytes = (long)(dims.Width >> 3) * (dims.Height >> 3) * 196;
            var actualBytes = new FileInfo(filePath).Length;
            if (actualBytes < requiredBytes)
                throw new MulFormatException(
                    $"MUL file is too short: {actualBytes} bytes, need {requiredBytes} bytes.");
        }

        var mapIndex = FacetToMapIndex(dims.Facet, filePath);
        var isUop = filePath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase);

        // ── Isolate from stale SDK static state ──────────────────────────────
        // Reset stale entries for this map index so SetMulPath doesn't skip them
        // (absolute paths are left unchanged by SetMulPath's continue branch).
        // Setting to empty string forces a fresh evaluation of the file's existence.
        foreach (var key in new[]
        {
            $"map{mapIndex}.mul",
            $"map{mapIndex}legacymul.uop",
            $"staidx{mapIndex}.mul",
            $"statics{mapIndex}.mul",
        })
        {
            if (Files.MulPath.ContainsKey(key))
                Files.MulPath[key] = string.Empty;
        }

        // Register the map directory as a low-priority fallback.
        var dataDir = Path.GetDirectoryName(filePath) ?? string.Empty;
        _fallbackPath = dataDir;
        if (!HasSettingsPath)
            Files.SetMulPath(dataDir);

        // Register the exact file path under the key the SDK expects.
        var standardKey = isUop
            ? $"map{mapIndex}legacymul.uop"
            : $"map{mapIndex}.mul";
        Files.SetMulPath(filePath, standardKey);

        // Create a fresh Map with the correct dimensions.
        // (Uses Files.MulPath to resolve the file when _path is null.)
        var sdkMap = new Map(null, mapIndex, mapIndex, dims.Width, dims.Height);
        var worldMap = WorldMap.Create(dims.Width, dims.Height, dims.Facet,
            isUop ? SourceFileType.Uop : SourceFileType.Mul);

        var w = dims.Width;
        var h = dims.Height;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            var tile = sdkMap.Tiles.GetLandTile(x, y);
            worldMap.Terrain[x, y] = (ushort)tile.Id;
            worldMap.Height [x, y] = (sbyte) tile.Z;
        }

        worldMap.Terrain.MarkAllClean();
        worldMap.Height.MarkAllClean();

        return worldMap;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static int FacetToMapIndex(string facet, string filePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
            if (baseName.Contains($"map{i}"))
                return i;

        return facet.ToLowerInvariant() switch
        {
            "felucca"  => 0,
            "trammel"  => 1,
            "ilshenar" => 2,
            "malas"    => 3,
            "tokuno"   => 4,
            "termur"   => 5,
            _          => 0,
        };
    }
}
