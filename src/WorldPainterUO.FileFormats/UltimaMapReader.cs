using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;
using WorldPainterUO.FileFormats.Ultima;
using WorldPainterUO.FileFormats.Uop;

namespace WorldPainterUO.FileFormats;

public sealed class UltimaMapReader : IMapFileReader
{
    private static readonly (int Width, int Height, string Name)[] KnownMapSizes =
    [
        (6144, 4096, "Felucca/Trammel (map0/map1)"),
        (2304, 1600, "Ilshenar (map2)"),
        (2560, 2048, "Malas (map3)"),
        (1448, 1448, "Tokuno (map4)"),
        (1280, 1024, "Ter Mur (map5)"),
    ];

    /// <summary>
    /// Detects map dimensions from a MUL or UOP file by matching file size
    /// against known UO map sizes or inferring from block count.
    /// </summary>
    public static MapDimensions DetectDimensions(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var isUop = filePath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase);

        if (isUop)
        {
            var bytes = File.ReadAllBytes(filePath);
            return DetectUopDimensions(bytes);
        }

        // MUL file: detect from file size
        var fileSize = fileInfo.Length;
        var expectedBlockSize = 196;
        var totalBlocks = (int)(fileSize / expectedBlockSize);

        // Try known sizes first
        foreach (var (w, h, _) in KnownMapSizes)
        {
            var dims = new MapDimensions(w, h);
            if (dims.TotalBlocks == totalBlocks)
                return dims;
        }

        // Guess: try to find an arrangement close to square
        var guessWidth = (int)Math.Sqrt(totalBlocks) * 8;
        var guessHeight = totalBlocks * 64 / guessWidth;
        // Align to 8-tile blocks
        guessWidth = ((guessWidth + 7) / 8) * 8;
        guessHeight = ((guessHeight + 7) / 8) * 8;

        var guessed = new MapDimensions(Math.Max(8, guessWidth), Math.Max(8, guessHeight));
        return guessed;
    }

    private static MapDimensions DetectUopDimensions(byte[] bytes)
    {
        // Try per-block UOP: read total entries from header
        if (bytes.Length >= 20)
        {
            var version = ReadU32(bytes, 4);
            if (version == UopFormat.Version || version == 4)
            {
                var totalEntries = (int)ReadU32(bytes, 12);
                if (totalEntries > 1)
                {
                    // Per-block format: totalEntries = blocks = blockWidth * blockHeight
                    foreach (var (w, h, _) in KnownMapSizes)
                    {
                        var dims = new MapDimensions(w, h);
                        if (dims.TotalBlocks == totalEntries)
                            return dims;
                    }
                }
            }
        }

        // Fall back to legacy single-entry: compute from raw MUL data size
        // Try extracting the first entry's decompressed size from hash table
        try
        {
            var totalEntries = (int)ReadU32(bytes, 12);
            var blockCount = UopFormat.BlockCount(totalEntries);
            if (blockCount > 0)
            {
                var entriesStart = UopFormat.HeaderSize + UopFormat.BlockHeaderSize;
                var decompressedSize = (int)ReadU32(bytes, entriesStart + 20);
                if (decompressedSize > 0)
                {
                    var totalMulTiles = decompressedSize / 3;
                    var totalMulBlocks = totalMulTiles / 64;
                    foreach (var (w, h, _) in KnownMapSizes)
                    {
                        var dims = new MapDimensions(w, h);
                        if (dims.TotalBlocks == totalMulBlocks)
                            return dims;
                    }
                }
            }
        }
        catch
        {
            // Fall through
        }

        // Default to Britannia if nothing matched
        return new MapDimensions(6144, 4096);
    }

    public WorldMap Read(string filePath, MapDimensions dimensions)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (filePath.Length == 0)
            throw new ArgumentException("Path must not be empty.", nameof(filePath));

        var logger = Log.For("UltimaMapReader");
        var isUop = filePath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase);

        logger.LogInformation("Reading map from {Path} ({Width}x{Height}, {Format})",
            filePath, dimensions.Width, dimensions.Height, isUop ? "UOP" : "MUL");

        WorldMap result;
        if (isUop)
        {
            result = ReadUop(filePath, dimensions);
        }
        else
        {
            result = ReadMul(filePath, dimensions);
        }

        logger.LogInformation("Map loaded: {Chunks} chunks, {Tiles} tiles",
            dimensions.TotalChunks, dimensions.Width * dimensions.Height);
        return result;
    }

    private static WorldMap ReadMul(string filePath, MapDimensions dimensions)
    {
        var worldMap = new WorldMap(dimensions, new MapMetadata("Unknown", SourceFileType.Mul));
        var blockSize = 196;
        var blockDataSize = 192;
        var tileSize = 3;
        var expectedSize = dimensions.TotalBlocks * blockSize;
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length < expectedSize)
        {
            throw new MulFormatException(
                $"File is too short. Expected at least {expectedSize} bytes, got {fileInfo.Length} bytes.");
        }

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var buffer = new byte[blockDataSize];
        var blockWidth = dimensions.BlockWidth;

        for (var blockIndex = 0; blockIndex < dimensions.TotalBlocks; blockIndex++)
        {
            var blockX = blockIndex % blockWidth;
            var blockY = blockIndex / blockWidth;

            fileStream.Seek(((long)blockX * dimensions.BlockHeight + blockY) * blockSize + 4, SeekOrigin.Begin);

            var bytesRead = fileStream.Read(buffer, 0, blockDataSize);
            if (bytesRead < blockDataSize)
                break;

            var offset = 0;
            for (var localY = 0; localY < 8; localY++)
            {
                var tileY = blockY * 8 + localY;
                for (var localX = 0; localX < 8; localX++)
                {
                    var tileX = blockX * 8 + localX;
                    worldMap.Terrain[tileX, tileY] = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
                    worldMap.Height[tileX, tileY] = (sbyte)buffer[offset + 2];
                    offset += tileSize;
                }
            }
        }

        worldMap.Terrain.MarkAllClean();
        worldMap.Height.MarkAllClean();

        return worldMap;
    }

    private static WorldMap ReadUop(string filePath, MapDimensions dimensions)
    {
        var bytes = File.ReadAllBytes(filePath);

        // Try per-block UOP format (TileMatrix style) first
        try
        {
            return ReadPerBlockUop(bytes, filePath, dimensions);
        }
        catch (InvalidDataException)
        {
            // Fall through to legacy single-entry format
        }

        // Legacy single-entry UOP format (matching UopMapWriter output)
        return ReadLegacyUop(bytes, dimensions);
    }

    private static WorldMap ReadPerBlockUop(byte[] fileBytes, string filePath, MapDimensions dimensions)
    {
        using var matrix = new TileMatrix(filePath, dimensions.Width, dimensions.Height);
        if (!matrix.IsUopFormat)
        {
            throw new InvalidDataException("Not a UOP file.");
        }

        var worldMap = new WorldMap(dimensions, new MapMetadata("Unknown", SourceFileType.Uop));
        var blockWidth = dimensions.BlockWidth;

        for (var blockY = 0; blockY < dimensions.BlockHeight; blockY++)
        {
            for (var blockX = 0; blockX < blockWidth; blockX++)
            {
                var block = matrix.GetLandBlock(blockX, blockY);
                if (block.Length == 0)
                    continue;

                for (var localY = 0; localY < 8; localY++)
                {
                    var tileY = blockY * 8 + localY;
                    for (var localX = 0; localX < 8; localX++)
                    {
                        var tileX = blockX * 8 + localX;
                        var idx = (localY * 8) + localX;
                        worldMap.Terrain[tileX, tileY] = block[idx].Id;
                        worldMap.Height[tileX, tileY] = block[idx].Z;
                    }
                }
            }
        }

        worldMap.Terrain.MarkAllClean();
        worldMap.Height.MarkAllClean();

        return worldMap;
    }

    private static WorldMap ReadLegacyUop(byte[] fileBytes, MapDimensions dimensions)
    {
        var targetHash = UopFormat.HashFileName(UopFormat.MapDataPath(0));

        var version = ReadU32(fileBytes, 4);
        if (version != UopFormat.Version)
            throw new UopFormatException($"Unsupported UOP version {version}. Expected {UopFormat.Version}.");

        var totalEntries = ReadU32(fileBytes, 12);
        var blockCount = UopFormat.BlockCount((int)totalEntries);
        var dataOffset = UopFormat.DataOffset(blockCount);

        var entryOffset = UopFormat.HeaderSize;

        for (var block = 0; block < blockCount; block++)
        {
            var entryCount = ReadU32(fileBytes, entryOffset + 4);
            var entriesStart = entryOffset + UopFormat.BlockHeaderSize;

            for (var i = 0; i < entryCount && i < UopFormat.DefaultBlockSize; i++)
            {
                var entryHash = ReadU64(fileBytes, entriesStart + i * UopFormat.EntrySize);

                if (entryHash == targetHash)
                {
                    var dataPos = (int)ReadU64(fileBytes, entriesStart + i * UopFormat.EntrySize + 8);
                    var compressedSize = ReadU32(fileBytes, entriesStart + i * UopFormat.EntrySize + 16);
                    var decompressedSize = ReadU32(fileBytes, entriesStart + i * UopFormat.EntrySize + 20);

                    byte[] mulData;
                    if (compressedSize == 0)
                    {
                        mulData = new byte[decompressedSize];
                        Array.Copy(fileBytes, dataPos, mulData, 0, decompressedSize);
                    }
                    else
                    {
                        mulData = DecompressLegacy(fileBytes, dataPos, (int)compressedSize, (int)decompressedSize);
                    }

                    return ParseLegacyMulData(mulData, dimensions);
                }
            }

            entryOffset = entriesStart + UopFormat.DefaultBlockSize * UopFormat.EntrySize;
        }

        throw new UopFormatException($"No entry found with hash {targetHash} in UOP file.");
    }

    private static WorldMap ParseLegacyMulData(byte[] mulData, MapDimensions dimensions)
    {
        var worldMap = new WorldMap(dimensions, new MapMetadata("Unknown", SourceFileType.Uop));
        var blockWidth = dimensions.BlockWidth;
        var totalBlocks = dimensions.TotalBlocks;
        var expectedMulSize = totalBlocks * 192;

        if (mulData.Length < expectedMulSize)
        {
            throw new UopFormatException(
                $"MUL data inside UOP is too short. Expected at least {expectedMulSize} bytes, got {mulData.Length}.");
        }

        for (var blockIndex = 0; blockIndex < totalBlocks; blockIndex++)
        {
            var blockOffset = blockIndex * 192;
            var blockX = blockIndex % blockWidth;
            var blockY = blockIndex / blockWidth;
            var tileOffset = blockOffset;

            for (var localX = 0; localX < 8; localX++)
            {
                var tileX = blockX * 8 + localX;
                for (var localY = 0; localY < 8; localY++)
                {
                    var tileY = blockY * 8 + localY;
                    worldMap.Terrain[tileX, tileY] = (ushort)(mulData[tileOffset] | (mulData[tileOffset + 1] << 8));
                    worldMap.Height[tileX, tileY] = (sbyte)mulData[tileOffset + 2];
                    tileOffset += 3;
                }
            }
        }

        worldMap.Terrain.MarkAllClean();
        worldMap.Height.MarkAllClean();
        return worldMap;
    }

    private static byte[] DecompressLegacy(byte[] source, int offset, int compressedSize, int decompressedSize)
    {
        throw new UopFormatException(
            $"Compressed UOP entries are not yet supported " +
            $"(compressedSize={compressedSize}, decompressedSize={decompressedSize}).");
    }

    private static uint ReadU32(byte[] data, int offset) =>
        (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

    private static ulong ReadU64(byte[] data, int offset) =>
        ReadU32(data, offset) | ((ulong)ReadU32(data, offset + 4) << 32);
}
