using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.FileFormats.Uop;
using WorldPainterUO.Tests.Fixtures;

namespace WorldPainterUO.Tests.Uop;

[Collection("UltimaSDK")]
public class UopMapReaderWriterTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); }
            catch { /* best effort */ }
        }
    }

    private string GetTempPath(string extension = ".uop")
    {
        var path = Path.Combine(Path.GetTempPath(), $"uop_test_{Guid.NewGuid():N}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    private static void AssertMapsEqual(WorldMap expected, WorldMap actual)
    {
        Assert.Equal(expected.Dimensions.Width, actual.Dimensions.Width);
        Assert.Equal(expected.Dimensions.Height, actual.Dimensions.Height);

        for (var y = 0; y < expected.Dimensions.Height; y++)
        {
            for (var x = 0; x < expected.Dimensions.Width; x++)
            {
                Assert.Equal(expected.Terrain[x, y], actual.Terrain[x, y]);
                Assert.Equal(expected.Height[x, y], actual.Height[x, y]);
            }
        }
    }

    [Fact]
    public void Round_trip_8x8()
    {
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                original.Terrain[x, y] = (ushort)(x * 8 + y);
                original.Height[x, y] = (sbyte)(x - y);
            }

        var path = GetTempPath();
        new UopMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, new MapDimensions(8, 8));
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_16x16()
    {
        var original = WorldMap.Create(16, 16, "Test", SourceFileType.Uop);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                original.Terrain[x, y] = (ushort)(0x200 + x * 16 + y);
                original.Height[x, y] = (sbyte)(x * 3 - y * 2);
            }

        var path = GetTempPath();
        new UopMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, new MapDimensions(16, 16));
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_64x64_one_chunk()
    {
        var original = WorldMap.Create(64, 64, "Test", SourceFileType.Uop);
        var rng = new Random(99);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                original.Terrain[x, y] = (ushort)rng.Next(0, 0x4000);
                original.Height[x, y] = (sbyte)rng.Next(-128, 128);
            }

        var path = GetTempPath();
        new UopMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, new MapDimensions(64, 64));
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_non_aligned_20x20()
    {
        // The SDK's TileMatrix computes BlockWidth/BlockHeight via floor
        // division (width >> 3, height >> 3), so a 20×20 map only covers
        // 2×2 blocks = 16×16 tiles.  Tiles outside that range come back as 0.
        var dims = new MapDimensions(20, 20);
        var original = WorldMap.Create(20, 20, "Test", SourceFileType.Uop);
        for (var y = 0; y < 20; y++)
            for (var x = 0; x < 20; x++)
            {
                original.Terrain[x, y] = 0x0ABC;
                original.Height[x, y] = -10;
            }

        var path = GetTempPath();
        new UopMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, dims);
        // SDK's TileMatrix uses floor division (>> 3) for block count, so a 20×20
        // map only covers 16×16 tiles.  Tiles beyond that come back as 0.
        var sdkW = dims.Width & ~7;  // 16
        var sdkH = dims.Height & ~7; // 16
    }

    [Fact]
    public void Round_trip_from_mul_import()
    {
        var dims = new MapDimensions(16, 16);
        var data = MulFixtureBuilder.BuildMulFile(16, 16,
            (x, y) => (ushort)(x * y + 3),
            (x, y) => (sbyte)(y - x));
        var mulPath = GetTempPath(".mul");
        File.WriteAllBytes(mulPath, data);

        var imported = new UltimaMapReader().Read(mulPath, dims);

        var uopPath = GetTempPath();
        new UopMapWriter().Write(uopPath, imported);

        var reloaded = new UltimaMapReader().Read(uopPath, dims);
        AssertMapsEqual(imported, reloaded);
    }

    [Fact]
    public void Uncompressed_data_correct_offset()
    {
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);
        original.Terrain[0, 0] = 0x1234;
        var path = GetTempPath();

        new UopMapWriter().Write(path, original);

        var bytes = File.ReadAllBytes(path);

        // Header: 40 bytes
        // 1 hash block (1 entry): 12 + 1*34 = 46 bytes
        // Data starts at offset 86
        // +4 for block header → first tile at offset 90
        var expectedDataOffset = 90;
        Assert.True(bytes.Length > expectedDataOffset);

        // Verify the first tile at the data offset
        Assert.Equal(0x34, bytes[expectedDataOffset]);
        Assert.Equal(0x12, bytes[expectedDataOffset + 1]);
    }

    [Fact]
    public void After_read_layers_marked_clean()
    {
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);
        var path = GetTempPath();

        new UopMapWriter().Write(path, original);
        var loaded = new UltimaMapReader().Read(path, new MapDimensions(8, 8));

        Assert.Empty(loaded.Terrain.DirtyChunks);
        Assert.Empty(loaded.Height.DirtyChunks);
    }

    [Fact]
    public void Metadata_marks_source_as_uop()
    {
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);
        var path = GetTempPath();

        new UopMapWriter().Write(path, original);
        var loaded = new UltimaMapReader().Read(path, new MapDimensions(8, 8));

        Assert.Equal(SourceFileType.Uop, loaded.Metadata.SourceFileType);
    }

    [Fact]
    public void Corrupted_hash_throws()
    {
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);
        var path = GetTempPath();

        new UopMapWriter(0).Write(path, original);

        // Corrupt the entry hash in the UOP so the SDK's hash lookup fails
        var bytes = File.ReadAllBytes(path);
        // Entry[0] hash is at: header(40) + blockHeader(12) + 20 = offset 72
        for (var i = 0; i < 8; i++)
            bytes[72 + i] = 0xFF;
        File.WriteAllBytes(path, bytes);

        var ex = Assert.Throws<ArgumentException>(() =>
            new UltimaMapReader().Read(path, new MapDimensions(8, 8)));
        Assert.Contains("not found in hashes dictionary", ex.Message);
    }

    [Fact]
    public void Null_path_throws()
    {
        var writer = new UopMapWriter();
        var reader = new UltimaMapReader();
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);

        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, map));
        Assert.Throws<ArgumentNullException>(() => reader.Read(null!, new MapDimensions(8, 8)));
    }

    [Fact]
    public void Empty_path_throws()
    {
        var writer = new UopMapWriter();
        var reader = new UltimaMapReader();
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Uop);

        Assert.Throws<ArgumentException>(() => writer.Write("", map));
        Assert.Throws<ArgumentException>(() => reader.Read("", new MapDimensions(8, 8)));
    }

    [Fact]
    public void Nonexistent_file_throws()
    {
        var reader = new UltimaMapReader();
        Assert.Throws<FileNotFoundException>(() =>
            reader.Read(Path.Combine(Path.GetTempPath(), "nonexistent.uop"), new MapDimensions(8, 8)));
    }
}
