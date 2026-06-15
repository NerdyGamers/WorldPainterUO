using WorldPainterUO.Core;

namespace WorldPainterUO.Editor;

/// <summary>
/// Before/after snapshot of a single chunk's tile data for undo/redo.
/// Arrays contain <see cref="MapChunk{T}.TileCount"/> entries in row-major order.
/// </summary>
public sealed record ChunkDiff(int ChunkX, int ChunkY, MapTile[] Before, MapTile[] After);
