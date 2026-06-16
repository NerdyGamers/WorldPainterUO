using System;
using System.Collections.Generic;
using System.IO;
using WorldPainterUO.Core;

// 'using Ultima' is intentionally omitted — all SDK access goes through
// UltimaSDKBridge to avoid the WorldPainterUO.FileFormats.Ultima.* namespace
// collision. See UltimaSDKBridge.cs for details.

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Reads Ultima Online map files into a <see cref="WorldMap"/> using the
/// Ultima SDK via <see cref="UltimaSDKBridge"/>. Block layout, column-major
/// ordering, and UOP unpacking are all handled by the SDK automatically.
/// </summary>
public sealed class UltimaMapReader
{
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
    /// Detects map dimensions from filename, then file size, then block-count estimate.
    /// </summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException("Map file not found.", filePath);

        // 1. Try filename (most reliable — "map0", "map1", etc.)
        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
            if (baseName.Contains($"map{i}") && KnownByIndex.TryGetValue(i, out var byIdx))
                return new MapDimensions(byIdx.Width, byIdx.Height, byIdx.Facet);

        // 2. Try file size (only works for the three facets with unique sizes)
        if (KnownBySize.TryGetValue(info.Length, out var bySize))
            return new MapDimensions(bySize.Width, bySize.Height, bySize.Facet);

        // 3. Square estimate for unknown custom maps
        var blockCount = info.Length / 196;
        var side = (int)Math.Sqrt(blockCount * 64);
        side = (side / 8) * 8;
        return new MapDimensions(side, side, "Unknown");
    }

    /// <summary>
    /// Reads a map file using the Ultima SDK (via <see cref="UltimaSDKBridge"/>)
    /// and returns a populated <see cref="WorldMap"/>.
    /// Works for both .mul and .uop files transparently.
    ///
    /// The map file directory is registered as a FALLBACK data path only.
    /// If the user has already configured a UO data path in Settings, that
    /// path is used instead and the map directory is ignored for SDK lookups.
    /// </summary>
    public WorldMap Read(string filePath, MapDimensions dims)
    {
        var dataDir = Path.GetDirectoryName(filePath) ?? string.Empty;

        // Register map directory as fallback ONLY — will be ignored if a
        // Settings path is already active. See UltimaSDKBridge for priority rules.
        UltimaSDKBridge.InitializeFromMapDirectory(dataDir);

        var mapIndex = FacetToMapIndex(dims.Facet, filePath);

        var worldMap = WorldMap.Create(dims.Width, dims.Height, dims.Facet, SourceFileType.Mul);

        UltimaSDKBridge.ReadMapTiles(mapIndex, worldMap);

        return worldMap;
    }

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
