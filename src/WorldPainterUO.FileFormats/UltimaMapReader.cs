using System;
using System.Collections.Generic;
using System.IO;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

/// <summary>
/// Reads Ultima Online map0..5.mul files into a <see cref="WorldMap"/>.
/// Each map is stored as a series of 8×8 tile blocks (196 bytes each).
/// Block layout: 4-byte header + 64 × (ushort tileId, sbyte z) = 4 + 192 = 196 bytes.
/// </summary>
public sealed class UltimaMapReader
{
    // Known UO map dimensions keyed by file size (bytes).
    // Used by DetectDimensions to identify which facet a file belongs to.
    private static readonly Dictionary<long, (int Width, int Height, string Facet)> KnownSizes = new()
    {
        // map0/map1 Felucca/Trammel: 6144×4096
        { 6144 * 4096 / 64 * 196L, (6144, 4096, "Felucca") },
        // map2 Ilshenar: 2304×1600
        { 2304 * 1600 / 64 * 196L, (2304, 1600, "Ilshenar") },
        // map3 Malas: 2560×2048
        { 2560 * 2048 / 64 * 196L, (2560, 2048, "Malas") },
        // map4 Tokuno: 1448×1448
        { 1448 * 1448 / 64 * 196L, (1448, 1448, "Tokuno") },
        // map5 TerMur: 1280×4096
        { 1280 * 4096 / 64 * 196L, (1280, 4096, "TerMur") },
    };

    /// <summary>
    /// Attempts to identify map dimensions and facet name from file size.
    /// Falls back to a square estimate when size is unrecognised.
    /// </summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException("Map file not found.", filePath);

        if (KnownSizes.TryGetValue(info.Length, out var known))
            return new MapDimensions(known.Width, known.Height, known.Facet);

        // Unknown size — estimate square from block count
        var blockCount = info.Length / 196;
        var side = (int)Math.Sqrt(blockCount * 64);
        side = (side / 8) * 8; // snap to block boundary
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
            // 4-byte block header (unused)
            br.ReadUInt32();

            for (var ty = 0; ty < 8; ty++)
            for (var tx = 0; tx < 8; tx++)
            {
                var tileId = br.ReadUInt16();
                var z      = br.ReadSByte();

                var worldX = bx * 8 + tx;
                var worldY = by * 8 + ty;

                map.Terrain[worldX, worldY] = tileId;
                map.Height [worldX, worldY] = z;
            }
        }

        return map;
    }
}
