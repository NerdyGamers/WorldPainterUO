using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class HeightLayerTests
{
    [Fact]
    public void Default_z_is_zero()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new HeightLayer(dims);
        Assert.Equal(0, layer[0, 0]);
        Assert.Equal(0, layer.DefaultZ);
    }

    [Fact]
    public void Custom_default_z()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new HeightLayer(dims, 10);
        Assert.Equal(10, layer[0, 0]);
        Assert.Equal(10, layer.DefaultZ);
    }

    [Fact]
    public void Negative_z_values()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new HeightLayer(dims);
        layer[5, 5] = -10;
        Assert.Equal(-10, layer[5, 5]);
    }

    [Fact]
    public void Set_and_get_height()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new HeightLayer(dims);
        layer[30, 40] = 15;
        Assert.Equal(15, layer[30, 40]);
    }

    [Fact]
    public void Dirty_chunks_tracked()
    {
        var dims = new MapDimensions(128, 64);
        var layer = new HeightLayer(dims);
        Assert.Empty(layer.DirtyChunks);

        layer[10, 10] = 5;
        Assert.Single(layer.DirtyChunks);
    }

    [Fact]
    public void MarkAllClean_resets_dirty_state()
    {
        var dims = new MapDimensions(64, 64);
        var layer = new HeightLayer(dims);
        layer[0, 0] = 1;
        Assert.True(layer.GetChunk(0, 0).IsDirty);

        layer.MarkAllClean();
        Assert.False(layer.GetChunk(0, 0).IsDirty);
    }
}
