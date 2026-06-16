using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.Tests.Fixtures;

namespace WorldPainterUO.Tests.FileFormats;

[Collection("UltimaSDK")]
public class UltimaMapReaderTests : IDisposable
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

    private string WriteTemp(byte[] data)
    {
        var path = MulFixtureBuilder.WriteTempFile(data);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void Read_single_block_8x8()
    {
        var dims = new MapDimensions(8, 8);
        var data = MulFixtureBuilder.BuildMulFile(8, 8,
            (x, y) => (ushort)(x * 8 + y + 1),
            (x, y) => (sbyte)(x + y));
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Equal(8, map.Dimensions.Width);
        Assert.Equal(8, map.Dimensions.Height);

        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                Assert.Equal((ushort)(x * 8 + y + 1), map.Terrain[x, y]);
                Assert.Equal((sbyte)(x + y), map.Height[x, y]);
            }
        }
    }

    [Fact]
    public void Read_four_blocks_16x16()
    {
        var dims = new MapDimensions(16, 16);
        var data = MulFixtureBuilder.BuildMulFile(16, 16,
            (x, y) => (ushort)(0x100 + x * 16 + y),
            (x, y) => (sbyte)(x - y));
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                Assert.Equal((ushort)(0x100 + x * 16 + y), map.Terrain[x, y]);
                Assert.Equal((sbyte)(x - y), map.Height[x, y]);
            }
        }
    }

    [Fact]
    public void Read_exactly_one_chunk_64x64()
    {
        var dims = new MapDimensions(64, 64);
        var data = MulFixtureBuilder.BuildSimpleMap(64, 64, 0x0ABC, 42);
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Equal(0x0ABC, map.Terrain[0, 0]);
        Assert.Equal(0x0ABC, map.Terrain[63, 63]);
        Assert.Equal(42, map.Height[0, 0]);
        Assert.Equal(42, map.Height[63, 63]);
    }

    [Fact]
    public void Read_non_aligned_dimensions_20x20()
    {
        // SDK's TileMatrix uses floor division (>> 3) for block count, so
        // a 20×20 file only covers 16×16 tiles.  Tiles outside that come
        // back as 0.
        var dims = new MapDimensions(20, 20);
        var data = MulFixtureBuilder.BuildSimpleMap(20, 20, 0x1234, -5);
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        var sdkW = dims.Width & ~7;  // 16
        var sdkH = dims.Height & ~7; // 16
        Assert.Equal(0x1234, map.Terrain[0, 0]);
        Assert.Equal(0x1234, map.Terrain[sdkW - 1, sdkH - 1]);
        Assert.Equal(-5, map.Height[sdkW - 1, sdkH - 1]);
        // Tiles beyond the SDK range are zero
        Assert.Equal(0, map.Terrain[dims.Width - 1, dims.Height - 1]);
        Assert.Equal(0, map.Height[dims.Width - 1, dims.Height - 1]);
    }

    [Fact]
    public void Negative_z_values_preserved()
    {
        var dims = new MapDimensions(8, 8);
        var data = MulFixtureBuilder.BuildMulFile(8, 8,
            (_, _) => (ushort)1,
            (x, y) => (sbyte)(-128 + x + y));
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Equal(-128, map.Height[0, 0]);
        Assert.Equal(-114, map.Height[7, 7]);
    }

    [Fact]
    public void Zero_size_map_produces_no_chunks()
    {
        var dims = new MapDimensions(0, 0);
        var data = MulFixtureBuilder.BuildSimpleMap(0, 0);
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Equal(0, map.Terrain.Dimensions.TotalChunks);
    }

    [Fact]
    public void Truncated_file_throws()
    {
        var dims = new MapDimensions(8, 8);
        var data = new byte[100]; // need 196 bytes
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var ex = Assert.Throws<MulFormatException>(() => reader.Read(path, dims));
        Assert.Contains("too short", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_path_throws()
    {
        var reader = new UltimaMapReader();
        var dims = new MapDimensions(8, 8);
        Assert.Throws<ArgumentException>(() => reader.Read("", dims));
    }

    [Fact]
    public void Null_path_throws()
    {
        var reader = new UltimaMapReader();
        Assert.Throws<ArgumentNullException>(() => reader.Read(null!, new MapDimensions(8, 8)));
    }

    [Fact]
    public void After_read_both_layers_marked_clean()
    {
        var dims = new MapDimensions(8, 8);
        var data = MulFixtureBuilder.BuildSimpleMap(8, 8);
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Empty(map.Terrain.DirtyChunks);
        Assert.Empty(map.Height.DirtyChunks);
    }

    [Fact]
    public void Golden_file_round_trip()
    {
        var dims = new MapDimensions(16, 16);
        var sourceData = MulFixtureBuilder.BuildMulFile(16, 16,
            (x, y) => (ushort)(x * y + 1),
            (x, y) => (sbyte)(x * 2 - y * 3));
        var path = WriteTemp(sourceData);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                Assert.Equal((ushort)(x * y + 1), map.Terrain[x, y]);
                Assert.Equal((sbyte)(x * 2 - y * 3), map.Height[x, y]);
            }
        }
    }

    [Fact]
    public void Britannia_scale_smoke_test()
    {
        var dims = new MapDimensions(6144, 4096);
        var data = MulFixtureBuilder.BuildSimpleMap(6144, 4096, 0x0001, 0);
        var path = WriteTemp(data);

        var reader = new UltimaMapReader();
        var map = reader.Read(path, dims);

        Assert.Equal(96 * 64, map.Dimensions.TotalChunks);
        Assert.Equal(0x0001, map.Terrain[0, 0]);
        Assert.Equal(0x0001, map.Terrain[6143, 4095]);
        Assert.Equal(0, map.Height[6143, 4095]);
        Assert.Empty(map.Terrain.DirtyChunks);
        Assert.Empty(map.Height.DirtyChunks);
    }
}
