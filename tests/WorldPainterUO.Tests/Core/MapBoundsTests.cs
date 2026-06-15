using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class MapBoundsTests
{
    [Fact]
    public void Width_and_height_derived()
    {
        var bounds = new MapBounds(10, 20, 19, 29);
        Assert.Equal(10, bounds.Width);
        Assert.Equal(10, bounds.Height);
    }

    [Fact]
    public void Contains_inside_bounds()
    {
        var bounds = new MapBounds(0, 0, 63, 63);
        Assert.True(bounds.Contains(0, 0));
        Assert.True(bounds.Contains(63, 63));
        Assert.True(bounds.Contains(32, 32));
    }

    [Fact]
    public void Contains_outside_bounds()
    {
        var bounds = new MapBounds(10, 10, 20, 20);
        Assert.False(bounds.Contains(9, 10));
        Assert.False(bounds.Contains(10, 9));
        Assert.False(bounds.Contains(21, 10));
    }
}
