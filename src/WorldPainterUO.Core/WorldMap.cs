using System.Collections.Generic;

namespace WorldPainterUO.Core;

public sealed class WorldMap
{
    private readonly HashSet<(int cx, int cy)> _dirtyChunks = new();

    public WorldMap(MapDimensions dimensions, MapMetadata metadata)
    {
        Dimensions = dimensions;
        Metadata = metadata;
        Terrain = new TerrainLayer(dimensions);
        Height = new HeightLayer(dimensions);
    }

    public MapDimensions Dimensions { get; }
    public MapMetadata Metadata { get; }
    public TerrainLayer Terrain { get; }
    public HeightLayer Height { get; }

    /// <summary>
    /// Marks a chunk as dirty so the renderer knows to redraw it.
    /// Called by edit tools after modifying tiles.
    /// </summary>
    public void MarkChunkDirty(int chunkX, int chunkY) =>
        _dirtyChunks.Add((chunkX, chunkY));

    /// <summary>
    /// Returns all dirty chunk coordinates and clears the dirty set.
    /// Called once per render frame by <see cref="WorldPainterUO.Rendering.MapRenderService"/>.
    /// </summary>
    public IReadOnlyCollection<(int cx, int cy)> ConsumeAndClearDirtyChunks()
    {
        if (_dirtyChunks.Count == 0)
            return System.Array.Empty<(int, int)>();

        var snapshot = new HashSet<(int cx, int cy)>(_dirtyChunks);
        _dirtyChunks.Clear();
        return snapshot;
    }

    public static WorldMap Create(int width, int height, string facet, SourceFileType sourceType) =>
        new(new MapDimensions(width, height, facet), new MapMetadata(facet, sourceType));
}
