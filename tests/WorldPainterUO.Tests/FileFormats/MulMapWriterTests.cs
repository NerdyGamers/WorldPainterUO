using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.Tests.Fixtures;

namespace WorldPainterUO.Tests.FileFormats;

[Collection("UltimaSDK")]
public class UltimaMapWriterTests : IDisposable
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

    private string GetTempPath(string extension = ".mul")
    {
        var path = Path.Combine(Path.GetTempPath(), $"mul_test_{Guid.NewGuid():N}{extension}");
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
        var dims = new MapDimensions(8, 8);
        var original = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                original.Terrain[x, y] = (ushort)(x * 8 + y);
                original.Height[x, y] = (sbyte)(x - y);
            }

        var path = GetTempPath();
        new UltimaMapWriter().Write(path, original);

        var reader = new UltimaMapReader();
        var reloaded = reader.Read(path, dims);

        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_16x16()
    {
        var dims = new MapDimensions(16, 16);
        var original = WorldMap.Create(16, 16, "Test", SourceFileType.Mul);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                original.Terrain[x, y] = (ushort)(0x100 + x * 16 + y);
                original.Height[x, y] = (sbyte)(x * 2 - y);
            }

        var path = GetTempPath();
        new UltimaMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, dims);
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_64x64_one_chunk()
    {
        var original = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        var rng = new Random(42);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                original.Terrain[x, y] = (ushort)rng.Next(0, 0x4000);
                original.Height[x, y] = (sbyte)rng.Next(-128, 128);
            }

        var path = GetTempPath();
        new UltimaMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, new MapDimensions(64, 64));
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Round_trip_non_aligned_20x20()
    {
        // SDK's TileMatrix floor-divides dimensions, so a 20×20 map only
        // covers 16×16 tiles.  Tiles beyond that come back as 0.
        var dims = new MapDimensions(20, 20);
        var original = WorldMap.Create(20, 20, "Test", SourceFileType.Mul);
        for (var y = 0; y < 20; y++)
            for (var x = 0; x < 20; x++)
            {
                original.Terrain[x, y] = 0x0ABC;
                original.Height[x, y] = -10;
            }

        var path = GetTempPath();
        new UltimaMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, dims);
        var sdkW = dims.Width & ~7;  // 16
        var sdkH = dims.Height & ~7; // 16
        for (var y = 0; y < sdkH; y++)
            for (var x = 0; x < sdkW; x++)
            {
                Assert.Equal(original.Terrain[x, y], reloaded.Terrain[x, y]);
                Assert.Equal(original.Height[x, y], reloaded.Height[x, y]);
            }
    }

    [Fact]
    public void Round_trip_after_mul_import()
    {
        var dims = new MapDimensions(16, 16);
        var data = MulFixtureBuilder.BuildMulFile(16, 16,
            (x, y) => (ushort)(x * y + 7),
            (x, y) => (sbyte)(y - x));
        var importPath = GetTempPath();
        File.WriteAllBytes(importPath, data);

        var imported = new UltimaMapReader().Read(importPath, dims);

        var exportPath = GetTempPath();
        new UltimaMapWriter().Write(exportPath, imported);

        var reimported = new UltimaMapReader().Read(exportPath, dims);
        AssertMapsEqual(imported, reimported);
    }

    [Fact]
    public void Round_trip_britannia_scale()
    {
        var dims = new MapDimensions(64, 64);
        var original = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                original.Terrain[x, y] = (ushort)(x + y);
                original.Height[x, y] = (sbyte)((x + y) % 100);
            }

        var path = GetTempPath();
        new UltimaMapWriter().Write(path, original);

        var reloaded = new UltimaMapReader().Read(path, dims);
        AssertMapsEqual(original, reloaded);
    }

    [Fact]
    public void Write_then_read_produces_correct_file_size()
    {
        var original = WorldMap.Create(16, 8, "Test", SourceFileType.Mul);
        var path = GetTempPath();

        new UltimaMapWriter().Write(path, original);

        var fileInfo = new FileInfo(path);
        var expectedSize = ((16 + 7) / 8) * ((8 + 7) / 8) * 196;
        Assert.Equal(expectedSize, fileInfo.Length);
    }

    [Fact]
    public void Null_path_throws()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        var writer = new UltimaMapWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!, map));
    }

    [Fact]
    public void Null_map_throws()
    {
        var writer = new UltimaMapWriter();
        Assert.Throws<ArgumentNullException>(() => writer.Write("test.mul", null!));
    }

    [Fact]
    public void Empty_path_throws()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        var writer = new UltimaMapWriter();
        Assert.Throws<ArgumentException>(() => writer.Write("", map));
    }
}
