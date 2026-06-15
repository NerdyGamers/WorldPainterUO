using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class MapTileTests
{
    [Fact]
    public void Default_has_tile_id_zero_and_z_zero()
    {
        var tile = default(MapTile);
        Assert.Equal(0, tile.LandTileId);
        Assert.Equal(0, tile.Z);
    }

    [Fact]
    public void Constructor_sets_properties()
    {
        var tile = new MapTile(0x1234, -5);
        Assert.Equal(0x1234, tile.LandTileId);
        Assert.Equal(-5, tile.Z);
    }

    [Fact]
    public void Equality_based_on_fields()
    {
        var a = new MapTile(1, 10);
        var b = new MapTile(1, 10);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Inequality_detected()
    {
        var a = new MapTile(1, 10);
        var b = new MapTile(2, 10);
        Assert.NotEqual(a, b);
    }
}
