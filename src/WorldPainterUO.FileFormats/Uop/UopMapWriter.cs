using Microsoft.Extensions.Logging;
using Ultima.Helpers;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats.Uop;

/// <summary>
/// Writes land-map data into a per-block UOP container readable by the Ultima
/// SDK's <c>TileMatrix</c>.  Each 8×8 land block is stored as a separate hash
/// table entry with the naming pattern
/// <c>build/{pattern}/{blockIndex:D8}.dat</c>.
///
/// Optional zlib compression is available (see <c>compress</c> parameter).
/// When disabled, blocks are stored uncompressed (flag=0).
/// </summary>
public sealed class UopMapWriter : IMapFileWriter
{
    private readonly int _mapIndex;
    private readonly bool _compress;

    /// <param name="mapIndex">Map index used for the entry name pattern (default 0).</param>
    /// <param name="compress">When true, compress each block with zlib via <see cref="UopUtils.Compress"/>.</param>
    public UopMapWriter(int mapIndex = 0, bool compress = false)
    {
        _mapIndex = mapIndex;
        _compress = compress;
    }

    public void Write(string filePath, WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(map);

        if (filePath.Length == 0)
            throw new ArgumentException("Path must not be empty.", nameof(filePath));

        var logger = Log.For("UopMapWriter");
        logger.LogInformation("Writing UOP to {Path} (compress={Compress})", filePath, _compress);

        var dims = map.Dimensions;
        // SDK's TileMatrix uses floor division (>> 3) for block count.  Must
        // match so the MUL-offset-to-UOP-offset calculation lines up.
        var blockWidth = dims.Width >> 3;
        var blockHeight = dims.Height >> 3;
        var totalBlocks = blockWidth * blockHeight;

        // ── Build entry data: each block → 196 bytes (4 zero header + 192 tile data) ──
        var entryData = new byte[totalBlocks][];
        const int entrySize = 196;
        const int tileDataOffset = 4;

        var blockIndex = 0;
        for (var blockX = 0; blockX < blockWidth; blockX++)
        for (var blockY = 0; blockY < blockHeight; blockY++)
        {
            var buf = new byte[entrySize];
            // First 4 bytes: block header (zeros)
            // Following 192 bytes: 64 tiles x 3 bytes each
            var offset = tileDataOffset;

            for (var tileY = 0; tileY < 8; tileY++)
            {
                var globalY = blockY * 8 + tileY;
                for (var tileX = 0; tileX < 8; tileX++)
                {
                    var globalX = blockX * 8 + tileX;
                    var id = map.Terrain[globalX, globalY];
                    var z = map.Height[globalX, globalY];

                    buf[offset] = (byte)(id & 0xFF);
                    buf[offset + 1] = (byte)((id >> 8) & 0xFF);
                    buf[offset + 2] = unchecked((byte)z);
                    offset += 3;
                }
            }

            entryData[blockIndex++] = buf;
        }

        // ── Layout calculation ──────────────────────────────────────────────
        var blocksNeeded = UopFormat.BlockCount(totalBlocks);
        var hashTableSize = 0;
        for (var b = 0; b < blocksNeeded; b++)
        {
            var entriesInBlock = Math.Min(UopFormat.DefaultBlockSize, totalBlocks - b * UopFormat.DefaultBlockSize);
            hashTableSize += UopFormat.PerBlockHeaderSize + entriesInBlock * UopFormat.PerEntrySize;
        }

        var dataStart = UopFormat.HeaderSize + hashTableSize;

        // ── Compute each entry's compressed/mapped size ────────────────────
        var entryOffsets = new long[totalBlocks];
        var entryCompLengths = new int[totalBlocks];
        var entryDecompLengths = new int[totalBlocks];
        var entryFlags = new short[totalBlocks];
        var filePos = dataStart;

        for (var i = 0; i < totalBlocks; i++)
        {
            entryOffsets[i] = filePos;

            if (_compress && UopUtils.Compress(entryData[i]) is (true, var compressed))
            {
                entryCompLengths[i] = compressed.Length;
                entryDecompLengths[i] = entrySize;
                entryFlags[i] = 1;
                entryData[i] = compressed;
                filePos += compressed.Length;
            }
            else
            {
                entryCompLengths[i] = 0;
                entryDecompLengths[i] = entrySize;
                entryFlags[i] = 0;
                filePos += entrySize;
            }
        }

        var totalSize = filePos;
        var uopBytes = new byte[totalSize];
        // Pattern must match what the SDK's ReadLandBlock derives from the filename:
        //   Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant()
        var pattern = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

        // ── Write header ───────────────────────────────────────────────────
        // Layout matches what the SDK's TileMatrix expects:
        //   0-3:   signature (int32)
        //   4-11:  version + padding (int64, skipped by SDK)
        //   12-19: nextBlock (int64)
        //   20-23: block capacity (int32)
        //   24-27: total file count (int32)
        WriteU32(uopBytes, 0, UopFormat.Signature);
        WriteU32(uopBytes, 4, (uint)UopFormat.Version);
        WriteU32(uopBytes, 8, 0); // padding for SDK's skipped int64
        WriteU64(uopBytes, 12, (ulong)UopFormat.HeaderSize); // nextBlock → first hash table block
        WriteU32(uopBytes, 20, (uint)UopFormat.DefaultBlockSize);
        WriteU32(uopBytes, 24, (uint)totalBlocks);

        // ── Write hash table blocks ────────────────────────────────────────
        var tablePos = UopFormat.HeaderSize;
        var remaining = totalBlocks;
        var entryIdx = 0;

        for (var block = 0; block < blocksNeeded; block++)
        {
            var entriesInBlock = Math.Min(UopFormat.DefaultBlockSize, remaining);
            var blockStart = tablePos;

            // entryCount
            WriteU32(uopBytes, tablePos, (uint)entriesInBlock);
            tablePos += 4;

            // nextBlock (0 for last block, otherwise next block position)
            if (block < blocksNeeded - 1)
            {
                var nextBlockPos = blockStart +
                    UopFormat.PerBlockHeaderSize +
                    entriesInBlock * UopFormat.PerEntrySize;
                WriteU64(uopBytes, tablePos, (ulong)nextBlockPos);
            }
            else
            {
                WriteU64(uopBytes, tablePos, 0);
            }
            tablePos += 8;

            // Entries
            for (var e = 0; e < entriesInBlock; e++)
            {
                var idx = entryIdx++;
                var name = UopFormat.BlockEntryName(pattern, idx);
                var hash = UopFormat.HashFileName(name);

                WriteU64(uopBytes, tablePos, (ulong)entryOffsets[idx]);
                tablePos += 8;

                WriteU32(uopBytes, tablePos, 0); // headerLength = 0
                tablePos += 4;

                WriteU32(uopBytes, tablePos, (uint)entryCompLengths[idx]);
                tablePos += 4;

                WriteU32(uopBytes, tablePos, (uint)entryDecompLengths[idx]);
                tablePos += 4;

                WriteU64(uopBytes, tablePos, hash);
                tablePos += 8;

                WriteU32(uopBytes, tablePos, 0); // Adler32
                tablePos += 4;

                WriteU16(uopBytes, tablePos, (ushort)entryFlags[idx]);
                tablePos += 2;
            }

            remaining -= entriesInBlock;
        }

        // ── Write entry data ───────────────────────────────────────────────
        for (var i = 0; i < totalBlocks; i++)
        {
            Array.Copy(entryData[i], 0, uopBytes, entryOffsets[i], entryData[i].Length);
        }

        File.WriteAllBytes(filePath, uopBytes);
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

    private static void WriteU16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}