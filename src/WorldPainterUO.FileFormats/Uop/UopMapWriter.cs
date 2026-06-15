using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats.Uop;

/// <summary>
/// Writes land-map data into a UOP container (map0LegacyMUL.uop).
///
/// Produces a single-entry legacy MUL UOP container (readable by <see cref="UltimaMapReader"/>):
/// - 40-byte header, version 5, block size 100, single entry.
/// - Hash table with one block containing the map entry.
/// - Raw (uncompressed) MUL data stored after the hash table.
///
/// Format assumptions are documented in <see cref="UopFormat"/>.
/// </summary>
public sealed class UopMapWriter : IMapFileWriter
{
    private readonly int _mapIndex;

    public UopMapWriter(int mapIndex = 0)
    {
        _mapIndex = mapIndex;
    }

    public void Write(string filePath, WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(map);

        if (filePath.Length == 0)
            throw new ArgumentException("Path must not be empty.", nameof(filePath));

        var logger = Log.For("UopMapWriter");
        logger.LogInformation("Writing UOP to {Path}", filePath);

        // Step 1: serialize the map to raw MUL bytes
        var mulData = SerializeToMul(map);
        var decompressedSize = mulData.Length;

        // Step 2: build UOP container
        const int fileCount = 1;
        var blockCount = UopFormat.BlockCount(fileCount);
        var dataOffset = UopFormat.DataOffset(blockCount);
        var totalSize = dataOffset + decompressedSize;

        var uopBytes = new byte[totalSize];
        var headerHash = UopFormat.HashFileName(UopFormat.MapDataPath(_mapIndex));

        // Write header
        WriteU32(uopBytes, 0, UopFormat.Signature);
        WriteU32(uopBytes, 4, UopFormat.Version);
        WriteU32(uopBytes, 8, UopFormat.DefaultBlockSize);
        WriteU32(uopBytes, 12, fileCount);
        WriteU32(uopBytes, 16, (uint)totalSize);
        // bytes 20-23: 0 (reserved)
        // bytes 24-31: 0 (header size / extended)
        // bytes 32-39: 0 (reserved)

        // Write hash table (single block)
        var blockOffset = UopFormat.HeaderSize;
        WriteU32(uopBytes, blockOffset, 0xFFFFFFFF); // nextBlock = -1 (none)
        WriteU32(uopBytes, blockOffset + 4, fileCount); // entryCount

        var entriesStart = blockOffset + UopFormat.BlockHeaderSize;
        WriteU64(uopBytes, entriesStart, headerHash);
        WriteU64(uopBytes, entriesStart + 8, (ulong)dataOffset);
        WriteU32(uopBytes, entriesStart + 16, 0); // compressedSize = 0 (uncompressed)
        WriteU32(uopBytes, entriesStart + 20, (uint)decompressedSize);

        // Write MUL data
        Array.Copy(mulData, 0, uopBytes, dataOffset, decompressedSize);

        File.WriteAllBytes(filePath, uopBytes);
    }

    private static byte[] SerializeToMul(WorldMap map)
    {
        var dims = map.Dimensions;
        var totalBlocks = dims.TotalBlocks;
        var blockWidth = dims.BlockWidth;
        var data = new byte[totalBlocks * 192];
        var globalOffset = 0;

        for (var blockIndex = 0; blockIndex < totalBlocks; blockIndex++)
        {
            var blockX = blockIndex % blockWidth;
            var blockY = blockIndex / blockWidth;

            for (var localX = 0; localX < 8; localX++)
            {
                var tileX = blockX * 8 + localX;

                for (var localY = 0; localY < 8; localY++)
                {
                    var tileY = blockY * 8 + localY;
                    var tileId = map.Terrain[tileX, tileY];
                    var z = map.Height[tileX, tileY];

                    data[globalOffset] = (byte)(tileId & 0xFF);
                    data[globalOffset + 1] = (byte)((tileId >> 8) & 0xFF);
                    data[globalOffset + 2] = unchecked((byte)z);

                    globalOffset += 3;
                }
            }
        }

        return data;
    }

    private static void WriteU32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteU64(byte[] data, int offset, ulong value)
    {
        WriteU32(data, offset, (uint)(value & 0xFFFFFFFF));
        WriteU32(data, offset + 4, (uint)((value >> 32) & 0xFFFFFFFF));
    }
}
