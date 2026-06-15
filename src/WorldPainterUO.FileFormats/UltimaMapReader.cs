using System;
using System.Collections.Generic;
using System.IO;
using Ultima;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Reads Ultima Online map files into a <see cref="WorldMap"/> using the
/// Ultima SDK (Ultima.dll).  All block layout, column-major ordering, and
/// UOP unpacking is handled by the SDK — we simply walk the tile array it
/// exposes and copy values into our internal model.
/// </summary>
public sealed class UltimaMapReader
{
    // Map index → (Width, Height, Facet)
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

    /// <summary>Detects map dimensions from filename, then file size, then block count estimate.</summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException("Map file not found.", filePath);

        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
        {
            if (baseName.Contains($"map{i}") && KnownByIndex.TryGetValue(i, out var byIdx))
                return new MapDimensions(byIdx.Width, byIdx.Height, byIdx.Facet);
        }

        if (KnownBySize.TryGetValue(info.Length, out var bySize))
            return new MapDimensions(bySize.Width, bySize.Height, bySize.Facet);

        var blockCount = info.Length / 196;
        var side = (int)Math.Sqrt(blockCount * 64);
        side = (side / 8) * 8;
        return new MapDimensions(side, side, "Unknown");
    }

    /// <summary>
    /// Reads a map file using the Ultima SDK and returns a populated <see cref="WorldMap"/>.
    /// Works for both .mul and .uop files — the SDK handles both transparently.
    /// </summary>
    public WorldMap Read(string filePath, MapDimensions dims)
    {
        var dataDir = Path.GetDirectoryName(filePath) ?? string.Empty;

        // Point the Ultima SDK at the folder containing the map file.
        // This sets the global Files.RootDir used by all SDK accessors.
        Files.SetMulPath(dataDir);

        // Determine the map index from dims.Facet so the SDK loads the right map.
        var mapIndex = FacetToMapIndex(dims.Facet, filePath);

        // Ultima.Map exposes a fully-decoded tile array — block order,
        // UOP unpacking, and header skipping are all handled internally.
        var sdkMap = new Ultima.Map(mapIndex, dims.Width, dims.Height);

        var worldMap = WorldMap.Create(dims.Width, dims.Height, dims.Facet, SourceFileType.Mul);

        for (var x = 0; x < dims.Width; x++)
        for (var y = 0; y < dims.Height; y++)
        {
            // Ultima.Map.Tiles[x, y] returns a HuedTile with .ID and .Z
            var tile = sdkMap.Tiles.GetLandTile(x, y);
            worldMap.Terrain[x, y] = (ushort)tile.ID;
            worldMap.Height [x, y] = (sbyte)tile.Z;
        }

        return worldMap;
    }

    private static int FacetToMapIndex(string facet, string filePath)
    {
        // Try to extract the index from the filename first (most reliable).
        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
            if (baseName.Contains($"map{i}")) return i;

        // Fallback: match by facet name.
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
