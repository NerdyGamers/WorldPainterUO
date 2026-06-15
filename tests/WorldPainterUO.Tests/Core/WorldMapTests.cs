using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class WorldMapTests
{
    [Fact]
    public void Create_with_factory()
    {
        var map = WorldMap.Create(64, 64, "Britannia", SourceFileType.Mul);
        Assert.Equal(64, map.Dimensions.Width);
        Assert.Equal(64, map.Dimensions.Height);
        Assert.Equal("Britannia", map.Metadata.Facet);
        Assert.Equal(SourceFileType.Mul, map.Metadata.SourceFileType);
    }

    [Fact]
    public void WorldMap_has_terrain_and_height_layers()
    {
        var map = WorldMap.Create(128, 64, "Malas", SourceFileType.Uop);
        Assert.NotNull(map.Terrain);
        Assert.NotNull(map.Height);
        Assert.Equal(map.Dimensions, map.Terrain.Dimensions);
        Assert.Equal(map.Dimensions, map.Height.Dimensions);
    }

    [Fact]
    public void Edit_terrain_then_read_back()
    {
        var map = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        map.Terrain[10, 10] = 0x1AB;
        Assert.Equal(0x1AB, map.Terrain[10, 10]);
    }

    [Fact]
    public void Edit_height_then_read_back()
    {
        var map = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        map.Height[20, 30] = 15;
        Assert.Equal(15, map.Height[20, 30]);
    }

    [Fact]
    public void Large_map_creates_correct_chunk_count()
    {
        var map = WorldMap.Create(6144, 4096, "Britannia", SourceFileType.Mul);
        Assert.Equal(96 * 64, map.Terrain.Dimensions.TotalChunks);
        Assert.Equal(96 * 64, map.Height.Dimensions.TotalChunks);
    }
}
