using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class TerrainLayerTests
{
    [Fact]
    public void Layer_has_correct_chunk_count()
    {
        var dims = new MapDimensions(128, 64);
        var layer = new TerrainLayer(dims);
        Assert.Equal(2, layer.Dimensions.TotalChunks);
    }

    [Fact]
    public void Default_tile_id_is_zero()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new TerrainLayer(dims);
        Assert.Equal(0, layer[0, 0]);
        Assert.Equal(0, layer[63, 63]);
    }

    [Fact]
    public void Custom_default_tile_id()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new TerrainLayer(dims, 0x0001);
        Assert.Equal(0x0001, layer[0, 0]);
        Assert.Equal(0x0001, layer.DefaultTileId);
    }

    [Fact]
    public void Set_and_get_tile()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new TerrainLayer(dims);
        layer[10, 20] = 0x0123;
        Assert.Equal(0x0123, layer[10, 20]);
    }

    [Fact]
    public void Set_across_chunk_boundary()
    {
        var dims = new MapDimensions(128, 128);
        var layer = new TerrainLayer(dims);
        layer[64, 64] = 0x0ABC;

        var chunk = layer.GetChunk(1, 1);
        Assert.Equal(0x0ABC, chunk[0, 0]);
    }

    [Fact]
    public void Dirty_chunks_tracked()
    {
        var dims = new MapDimensions(128, 64);
        var layer = new TerrainLayer(dims);
        Assert.Empty(layer.DirtyChunks);

        layer[10, 10] = 5;
        Assert.Single(layer.DirtyChunks);

        layer[70, 10] = 6;
        Assert.Equal(2, layer.DirtyChunks.Count());
    }

    [Fact]
    public void MarkAllClean_resets_dirty_state()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new TerrainLayer(dims);
        layer[0, 0] = 1;
        Assert.True(layer.GetChunk(0, 0).IsDirty);

        layer.MarkAllClean();
        Assert.False(layer.GetChunk(0, 0).IsDirty);
    }
}
