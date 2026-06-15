using System;
using System.Collections.Generic;
using System.IO;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Reads Ultima Online map0..5.mul / mapXLegacyMUL.uop files into a <see cref="WorldMap"/>.
/// Each map is stored as a series of 8×8 tile blocks (196 bytes each).
/// Block layout: 4-byte header + 64 × (ushort tileId, sbyte z) = 4 + 192 = 196 bytes.
/// </summary>
public sealed class UltimaMapReader
{
    // Map index → (Width, Height, Facet)
    // Malas (map3) and TerMur (map5) have IDENTICAL file sizes (1280×4096 vs 2560×2048
    // both yield 81,920 blocks × 196 bytes = 16,056,320 bytes), so size-based detection
    // is ambiguous for those two.  We therefore key on map index extracted from the filename.
    private static readonly Dictionary<int, (int Width, int Height, string Facet)> KnownByIndex = new()
    {
        { 0, (6144, 4096, "Felucca")  },   // map0.mul / map0LegacyMUL.uop
        { 1, (6144, 4096, "Trammel")  },   // map1.mul / map1LegacyMUL.uop
        { 2, (2304, 1600, "Ilshenar") },   // map2.mul
        { 3, (2560, 2048, "Malas")    },   // map3.mul
        { 4, (1448, 1448, "Tokuno")   },   // map4.mul
        { 5, (1280, 4096, "TerMur")   },   // map5.mul
    };

    // Fallback: unique file sizes for maps that don’t collide.
    // Malas and TerMur are intentionally absent — use filename detection instead.
    private static readonly Dictionary<long, (int Width, int Height, string Facet)> KnownBySize = new()
    {
        { FileSize(6144, 4096), (6144, 4096, "Felucca")  },
        { FileSize(2304, 1600), (2304, 1600, "Ilshenar") },
        { FileSize(1448, 1448), (1448, 1448, "Tokuno")   },
    };

    private static long FileSize(int width, int height)
        => (long)(width / 8) * (height / 8) * 196L;

    /// <summary>
    /// Attempts to identify map dimensions and facet name.
    /// Detection order: (1) map index from filename, (2) unique file size, (3) square estimate.
    /// </summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException("Map file not found.", filePath);

        // 1. Try filename: match "map0", "map1" … "map5" anywhere in the base name.
        var baseName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        for (var i = 0; i <= 5; i++)
        {
            if (baseName.Contains($"map{i}") && KnownByIndex.TryGetValue(i, out var byIdx))
                return new MapDimensions(byIdx.Width, byIdx.Height, byIdx.Facet);
        }

        // 2. Fall back to file size for non-ambiguous cases.
        if (KnownBySize.TryGetValue(info.Length, out var bySize))
            return new MapDimensions(bySize.Width, bySize.Height, bySize.Facet);

        // 3. Unknown — estimate square from block count.
        var blockCount = info.Length / 196;
        var side = (int)Math.Sqrt(blockCount * 64);
        side = (side / 8) * 8;
        return new MapDimensions(side, side, "Unknown");
    }

    /// <summary>Reads a .mul map file into a <see cref="WorldMap"/>.</summary>
    public WorldMap Read(string filePath, MapDimensions dims)
    {
        var map = WorldMap.Create(dims.Width, dims.Height, dims.Facet, SourceFileType.Mul);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 65536, useAsync: false);
        using var br = new BinaryReader(fs);

        var blocksX = dims.Width  / 8;
        var blocksY = dims.Height / 8;

        for (var by = 0; by < blocksY; by++)
        for (var bx = 0; bx < blocksX; bx++)
        {
            br.ReadUInt32(); // 4-byte block header (unused)

            for (var ty = 0; ty < 8; ty++)
            for (var tx = 0; tx < 8; tx++)
            {
                var tileId = br.ReadUInt16();
                var z      = br.ReadSByte();

                map.Terrain[bx * 8 + tx, by * 8 + ty] = tileId;
                map.Height [bx * 8 + tx, by * 8 + ty] = z;
            }
        }

        return map;
    }
}
