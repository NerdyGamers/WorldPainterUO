

namespace WorldPainterUO.FileFormats.Ultima;

public sealed class TileMatrix : IDisposable
{
    private const int LandBlockSize = 196;
    private const int LandBlockDataSize = 192;
    private const int LandTileSize = 3;

    public int BlockWidth { get; }
    public int BlockHeight { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly string _mapPath;
    private FileStream? _mapStream;
    private bool _isUop;
    private bool _uopRead;
    private UopFile[]? _uopFiles;

    private byte[]? _buffer;

    public TileMatrix(string mapFilePath, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(mapFilePath);

        _mapPath = mapFilePath;
        Width = width;
        Height = height;
        BlockWidth = width >> 3;
        BlockHeight = height >> 3;
        _isUop = mapFilePath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsUopFormat => _isUop;

    private void EnsureMapOpen()
    {
        if (_mapStream?.CanRead == true && _mapStream.CanSeek)
            return;

        _mapStream?.Dispose();
        _mapStream = new FileStream(_mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (_isUop && !_uopRead)
        {
            var fi = new FileInfo(_mapPath);
            var uopPattern = fi.Name.Replace(fi.Extension, "").ToLowerInvariant();
            ReadUopFiles(uopPattern);
            _uopRead = true;
        }
    }

    public Tile[] GetLandBlock(int x, int y)
    {
        if (x < 0 || y < 0 || x >= BlockWidth || y >= BlockHeight)
        {
            return [];
        }

        return ReadLandBlock(x, y);
    }

    public Tile GetLandTile(int x, int y)
    {
        return GetLandBlock(x >> 3, y >> 3)[((y & 0x7) << 3) + (x & 0x7)];
    }

    private Tile[] ReadLandBlock(int x, int y)
    {
        EnsureMapOpen();

        var tiles = new Tile[64];
        if (_mapStream == null)
            return tiles;

        long offset;
        if (_isUop)
        {
            var mulOffset = ((x * BlockHeight) + y) * LandBlockSize + 4;
            offset = CalculateOffsetFromUop(mulOffset);
        }
        else
        {
            offset = ((x * BlockHeight) + y) * LandBlockSize + 4;
        }

        _mapStream.Seek(offset, SeekOrigin.Begin);

        if (_buffer == null || _buffer.Length < LandBlockDataSize)
            _buffer = new byte[LandBlockDataSize];

        var bytesRead = _mapStream.Read(_buffer, 0, LandBlockDataSize);
        if (bytesRead < LandBlockDataSize)
            return tiles;

        for (var i = 0; i < 64; i++)
        {
            var tileOffset = i * LandTileSize;
            tiles[i] = new Tile
            {
                Id = (ushort)(_buffer[tileOffset] | (_buffer[tileOffset + 1] << 8)),
                Z = (sbyte)_buffer[tileOffset + 2]
            };
        }

        return tiles;
    }

    private void ReadUopFiles(string pattern)
    {
        if (_mapStream == null)
            throw new InvalidOperationException("Map stream is not open.");

        var reader = new BinaryReader(_mapStream);

        reader.BaseStream.Seek(0, SeekOrigin.Begin);

        if (reader.ReadInt32() != 0x50594D)
            throw new InvalidDataException($"Not a valid UOP file: {_mapPath}");

        reader.ReadInt64();
        var nextBlock = reader.ReadInt64();
        reader.ReadInt32();
        var count = reader.ReadInt32();

        _uopFiles = new UopFile[count];

        var hashes = new Dictionary<ulong, int>(count);
        for (var i = 0; i < count; i++)
        {
            var file = $"build/{pattern}/{i:D8}.dat";
            var hash = UopUtils.HashFileName(file);
            hashes.TryAdd(hash, i);
        }

        reader.BaseStream.Seek(nextBlock, SeekOrigin.Begin);

        do
        {
            var filesCount = reader.ReadInt32();
            nextBlock = reader.ReadInt64();

            for (var i = 0; i < filesCount; i++)
            {
                var offset = reader.ReadInt64();
                var headerLength = reader.ReadInt32();
                var compressedLength = reader.ReadInt32();
                var decompressedLength = reader.ReadInt32();
                var hash = reader.ReadUInt64();
                reader.ReadUInt32();
                var flag = reader.ReadInt16();

                if (offset == 0)
                    continue;

                if (!hashes.TryGetValue(hash, out var idx))
                    continue;

                if (idx < 0 || idx >= _uopFiles.Length)
                    throw new IndexOutOfRangeException("Hash index out of range.");

                var length = flag == 1 ? compressedLength : decompressedLength;
                _uopFiles[idx] = new UopFile(offset + headerLength, length);
            }
        }
        while (reader.BaseStream.Seek(nextBlock, SeekOrigin.Begin) != 0);

        // If no UOP entries matched the per-block pattern, this isn't a per-block UOP
        if (_uopFiles.All(f => f.Offset == 0))
            throw new InvalidDataException("UOP file does not contain per-block map entries.");
    }

    private long CalculateOffsetFromUop(long offset)
    {
        if (_uopFiles == null)
            return offset;

        long pos = 0;
        foreach (var f in _uopFiles)
        {
            var currentPosition = pos + f.Length;
            if (offset < currentPosition)
                return f.Offset + (offset - pos);
            pos = currentPosition;
        }

        return _mapStream?.Length ?? offset;
    }

    public void Dispose()
    {
        _mapStream?.Dispose();
    }

    private readonly struct UopFile(long offset, int length)
    {
        public readonly long Offset = offset;
        public readonly int Length = length;
    }
}
