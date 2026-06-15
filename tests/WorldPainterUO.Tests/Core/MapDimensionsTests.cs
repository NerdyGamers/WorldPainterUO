using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Core;

public class MapDimensionsTests
{
    [Fact]
    public void Chunk_count_for_exact_multiple()
    {
        var dims = new MapDimensions(64, 64);
        Assert.Equal(1, dims.ChunksX);
        Assert.Equal(1, dims.ChunksY);
        Assert.Equal(1, dims.TotalChunks);
    }

    [Fact]
    public void Chunk_count_for_partial_chunks()
    {
        var dims = new MapDimensions(65, 128);
        Assert.Equal(2, dims.ChunksX);
        Assert.Equal(2, dims.ChunksY);
        Assert.Equal(4, dims.TotalChunks);
    }

    [Fact]
    public void Block_count_derived_from_8x8_blocks()
    {
        var dims = new MapDimensions(64, 64);
        Assert.Equal(8, dims.BlockWidth);
        Assert.Equal(8, dims.BlockHeight);
        Assert.Equal(64, dims.TotalBlocks);
    }

    [Fact]
    public void Britannia_scale_dimensions()
    {
        var dims = new MapDimensions(6144, 4096);
        Assert.Equal(96, dims.ChunksX);
        Assert.Equal(64, dims.ChunksY);
        Assert.Equal(768, dims.BlockWidth);
        Assert.Equal(512, dims.BlockHeight);
    }

    [Fact]
    public void GetChunkCoord_returns_correct_mapping()
    {
        var dims = new MapDimensions(128, 128);
        dims.GetChunkCoord(0, 0, out var cx, out var cy, out var lx, out var ly);
        Assert.Equal(0, cx); Assert.Equal(0, cy); Assert.Equal(0, lx); Assert.Equal(0, ly);

        dims.GetChunkCoord(63, 63, out cx, out cy, out lx, out ly);
        Assert.Equal(0, cx); Assert.Equal(0, cy); Assert.Equal(63, lx); Assert.Equal(63, ly);

        dims.GetChunkCoord(64, 0, out cx, out cy, out lx, out ly);
        Assert.Equal(1, cx); Assert.Equal(0, cy); Assert.Equal(0, lx); Assert.Equal(0, ly);

        dims.GetChunkCoord(127, 127, out cx, out cy, out lx, out ly);
        Assert.Equal(1, cx); Assert.Equal(1, cy); Assert.Equal(63, lx); Assert.Equal(63, ly);
    }

    [Fact]
    public void ChunkIndex_flat_indexing()
    {
        var dims = new MapDimensions(128, 64);
        Assert.Equal(0, dims.ChunkIndex(0, 0));
        Assert.Equal(1, dims.ChunkIndex(1, 0));
        Assert.Equal(2, dims.ChunkIndex(0, 1));
        Assert.Equal(3, dims.ChunkIndex(1, 1));
    }
}
