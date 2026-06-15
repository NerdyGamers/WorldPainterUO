namespace WorldPainterUO.Core;

public sealed class HeightLayer
{
    private readonly MapChunk<sbyte>[] _chunks;

    public HeightLayer(MapDimensions dimensions, sbyte defaultZ = 0)
    {
        Dimensions = dimensions;
        DefaultZ = defaultZ;
        _chunks = new MapChunk<sbyte>[dimensions.TotalChunks];

        for (var cy = 0; cy < dimensions.ChunksY; cy++)
        {
            for (var cx = 0; cx < dimensions.ChunksX; cx++)
            {
                var index = dimensions.ChunkIndex(cx, cy);
                _chunks[index] = new MapChunk<sbyte>((cx, cy), defaultZ);
            }
        }
    }

    public MapDimensions Dimensions { get; }
    public sbyte DefaultZ { get; }

    public sbyte this[int x, int y]
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

    public MapChunk<sbyte> GetChunk(int chunkX, int chunkY) =>
        _chunks[Dimensions.ChunkIndex(chunkX, chunkY)];

    public IEnumerable<MapChunk<sbyte>> DirtyChunks =>
        _chunks.Where(c => c.IsDirty);

    public void MarkAllClean()
    {
        foreach (var chunk in _chunks)
        {
            chunk.MarkClean();
        }
    }
}
