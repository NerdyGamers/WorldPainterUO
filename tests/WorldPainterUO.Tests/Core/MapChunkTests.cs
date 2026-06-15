using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class MapChunkTests
{
    [Fact]
    public void Constructor_initializes_all_tiles_to_default()
    {
        var chunk = new MapChunk<ushort>((0, 0));
        for (var ly = 0; ly < MapChunk<ushort>.Size; ly++)
        for (var lx = 0; lx < MapChunk<ushort>.Size; lx++)
            Assert.Equal(0, chunk[lx, ly]);
    }

    [Fact]
    public void Constructor_with_default_value()
    {
        var chunk = new MapChunk<ushort>((2, 3), 0x1234);
        Assert.Equal((2, 3), chunk.Index);
        Assert.Equal(0x1234, chunk[0, 0]);
    }

    [Fact]
    public void Get_and_set_tile_by_local_coordinates()
    {
        var chunk = new MapChunk<ushort>((0, 0));
        chunk[10, 20] = 42;
        Assert.Equal(42, chunk[10, 20]);
    }

    [Fact]
    public void Set_marks_chunk_dirty()
    {
        var chunk = new MapChunk<ushort>((0, 0));
        Assert.False(chunk.IsDirty);
        chunk[0, 0] = 1;
        Assert.True(chunk.IsDirty);
    }

    [Fact]
    public void MarkClean_resets_dirty_flag()
    {
        var chunk = new MapChunk<ushort>((0, 0));
        chunk[0, 0] = 1;
        chunk.MarkClean();
        Assert.False(chunk.IsDirty);
    }

    [Fact]
    public void TileCount_is_4096()
    {
        Assert.Equal(4096, MapChunk<ushort>.TileCount);
        Assert.Equal(64, MapChunk<ushort>.Size);
    }

    [Fact]
    public void Data_span_returns_all_tiles()
    {
        var chunk = new MapChunk<ushort>((0, 0), 7);
        Assert.Equal(4096, chunk.Data.Length);
        foreach (var v in chunk.Data)
            Assert.Equal(7, v);
    }
}
