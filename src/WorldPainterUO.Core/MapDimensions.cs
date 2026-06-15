namespace WorldPainterUO.Core;

public readonly record struct MapDimensions(int Width, int Height)
{
    public const int DefaultChunkSize = 64;

    public int ChunkSize { get; init; } = DefaultChunkSize;

    public int ChunksX => (Width + ChunkSize - 1) / ChunkSize;
    public int ChunksY => (Height + ChunkSize - 1) / ChunkSize;

    public int TotalChunks => ChunksX * ChunksY;
    public int TotalTiles => Width * Height;

    public int BlockWidth => (Width + 7) / 8;
    public int BlockHeight => (Height + 7) / 8;
    public int TotalBlocks => BlockWidth * BlockHeight;

    public readonly void GetChunkCoord(
        int tileX, int tileY,
        out int chunkX, out int chunkY,
        out int localX, out int localY)
    {
        chunkX = tileX / ChunkSize;
        chunkY = tileY / ChunkSize;
        localX = tileX % ChunkSize;
        localY = tileY % ChunkSize;
    }

    public readonly int ChunkIndex(int chunkX, int chunkY) =>
        chunkY * ChunksX + chunkX;
}
