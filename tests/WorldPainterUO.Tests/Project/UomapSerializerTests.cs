using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.Project;
using WorldPainterUO.Tests.Fixtures;

namespace WorldPainterUO.Tests.Project;

public class UomapSerializerTests : IDisposable
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

    private string WriteTemp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"uomap_test_{Guid.NewGuid():N}.uomap");
        _tempFiles.Add(path);
        return path;
    }

    private static WorldMap CreateTestMap(int width, int height, string facet = "Test")
    {
        var map = WorldMap.Create(width, height, facet, SourceFileType.Mul);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                map.Terrain[x, y] = (ushort)(x + y * width);
                map.Height[x, y] = (sbyte)((x + y) % 100);
            }
        }
        return map;
    }

    private static void AssertMapsEqual(WorldMap expected, WorldMap actual)
    {
        Assert.Equal(expected.Dimensions.Width, actual.Dimensions.Width);
        Assert.Equal(expected.Dimensions.Height, actual.Dimensions.Height);
        Assert.Equal(expected.Metadata.Facet, actual.Metadata.Facet);
        Assert.Equal(expected.Metadata.SourceFileType, actual.Metadata.SourceFileType);

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
    public void Save_and_load_single_chunk_64x64()
    {
        var original = CreateTestMap(64, 64);
        var path = WriteTemp();

        UomapSerializer.Save(path, original);
        var loaded = UomapSerializer.Load(path);

        AssertMapsEqual(original, loaded);
    }

    [Fact]
    public void Save_and_load_multi_chunk_128x64()
    {
        var original = CreateTestMap(128, 64, "Britannia");
        var path = WriteTemp();

        UomapSerializer.Save(path, original);
        var loaded = UomapSerializer.Load(path);

        AssertMapsEqual(original, loaded);
    }

    [Fact]
    public void Metadata_preserved()
    {
        var original = WorldMap.Create(64, 64, "Malas", SourceFileType.Uop);
        original.Terrain[10, 10] = 0x1234;
        original.Height[20, 20] = -15;
        var path = WriteTemp();

        UomapSerializer.Save(path, original);
        var loaded = UomapSerializer.Load(path);

        Assert.Equal("Malas", loaded.Metadata.Facet);
        Assert.Equal(SourceFileType.Uop, loaded.Metadata.SourceFileType);
        Assert.Equal(1, loaded.Metadata.Version);
        Assert.Equal(0x1234, loaded.Terrain[10, 10]);
        Assert.Equal(-15, loaded.Height[20, 20]);
    }

    [Fact]
    public void After_load_layers_marked_clean()
    {
        var original = CreateTestMap(64, 64);
        var path = WriteTemp();

        UomapSerializer.Save(path, original);
        var loaded = UomapSerializer.Load(path);

        Assert.Empty(loaded.Terrain.DirtyChunks);
        Assert.Empty(loaded.Height.DirtyChunks);
    }

    [Fact]
    public void Round_trip_with_mul_import()
    {
        var dims = new MapDimensions(16, 16);
        var data = MulFixtureBuilder.BuildMulFile(16, 16,
            (x, y) => (ushort)(x * 16 + y),
            (x, y) => (sbyte)(x - y));
        var mulPath = Path.Combine(Path.GetTempPath(), $"mul_{Guid.NewGuid():N}.mul");
        _tempFiles.Add(mulPath);
        File.WriteAllBytes(mulPath, data);

        var reader = new UltimaMapReader();
        var imported = reader.Read(mulPath, dims);

        var uomapPath = WriteTemp();
        UomapSerializer.Save(uomapPath, imported);
        var reloaded = UomapSerializer.Load(uomapPath);

        AssertMapsEqual(imported, reloaded);
    }

    [Fact]
    public void Save_and_load_non_power_of_two()
    {
        var original = CreateTestMap(20, 30);
        var path = WriteTemp();

        UomapSerializer.Save(path, original);
        var loaded = UomapSerializer.Load(path);

        AssertMapsEqual(original, loaded);
    }

    [Fact]
    public void Null_map_throws()
    {
        var path = WriteTemp();
        Assert.Throws<ArgumentNullException>(() => UomapSerializer.Save(path, null!));
    }

    [Fact]
    public void Load_nonexistent_file_throws()
    {
        var badPath = Path.Combine(Path.GetTempPath(), "nonexistent_file_xyz.uomap");
        Assert.Throws<FileNotFoundException>(() =>
            UomapSerializer.Load(badPath));
    }
}
