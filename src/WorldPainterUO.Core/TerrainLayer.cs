namespace WorldPainterUO.Core;

public sealed class TerrainLayer
{
    private readonly MapChunk<ushort>[] _chunks;

    public TerrainLayer(MapDimensions dimensions, ushort defaultTileId = 0)
    {
        Dimensions = dimensions;
        DefaultTileId = defaultTileId;
        _chunks = new MapChunk<ushort>[dimensions.TotalChunks];

        for (var cy = 0; cy < dimensions.ChunksY; cy++)
        {
            for (var cx = 0; cx < dimensions.ChunksX; cx++)
            {
                var index = dimensions.ChunkIndex(cx, cy);
                _chunks[index] = new MapChunk<ushort>((cx, cy), defaultTileId);
            }
        }
    }

    public MapDimensions Dimensions { get; }
    public ushort DefaultTileId { get; }

    public ushort this[int x, int y]
    {
        get
        {
            Dimensions.GetChunkCoord(x, y, out var cx, out var cy, out var lx, out var ly);
            return _chunks[Dimensions.ChunkIndex(cx, cy)][lx, ly];
        }
        set
        {
            Dimensions.GetChunkCoord(x, y, out var cx, out var cy, out var lx, out var ly);
            _chunks[Dimensions.ChunkIndex(cx, cy)][lx, ly] = value;
        }
    }

    public MapChunk<ushort> GetChunk(int chunkX, int chunkY) =>
        _chunks[Dimensions.ChunkIndex(chunkX, chunkY)];

    public IEnumerable<MapChunk<ushort>> DirtyChunks =>
        _chunks.Where(c => c.IsDirty);

    public void MarkAllClean()
    {
        foreach (var chunk in _chunks)
        {
            chunk.MarkClean();
        }
    }
}
