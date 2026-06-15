using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class MapMetadataTests
{
    [Fact]
    public void Default_metadata()
    {
        var meta = new MapMetadata("Britannia", SourceFileType.Mul);
        Assert.Equal("Britannia", meta.Facet);
        Assert.Equal(SourceFileType.Mul, meta.SourceFileType);
        Assert.Equal(1, meta.Version);
    }

    [Fact]
    public void Explicit_version()
    {
        var meta = new MapMetadata("Malas", SourceFileType.Uop, 2);
        Assert.Equal("Malas", meta.Facet);
        Assert.Equal(SourceFileType.Uop, meta.SourceFileType);
        Assert.Equal(2, meta.Version);
    }
}
