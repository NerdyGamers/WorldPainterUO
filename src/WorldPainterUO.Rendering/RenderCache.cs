using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Chunk-level render cache. Each dirty chunk is re-rendered to its own
/// <see cref="SKBitmap"/>; clean chunks reuse their cached output.
/// </summary>
public sealed class RenderCache
{
    private readonly Dictionary<(int, int), SKBitmap> _cache = new();
    private readonly HashSet<(int, int)> _invalidated = new();

    /// <summary>Marks a single chunk for re-render.</summary>
    public void InvalidateChunk(int chunkX, int chunkY)
    {
        _invalidated.Add((chunkX, chunkY));
    }

    /// <summary>Marks all chunks for re-render.</summary>
    public void InvalidateAll()
    {
        // Mark all cached entries as invalid
        foreach (var key in _cache.Keys)
            _invalidated.Add(key);
    }

    /// <summary>True if the chunk needs re-rendering.</summary>
    public bool IsDirty(int chunkX, int chunkY) =>
        _invalidated.Contains((chunkX, chunkY));

    /// <summary>
    /// Returns a cached bitmap for the chunk, or renders it if dirty/missing.
    /// The render function is only called when the chunk is dirty.
    /// </summary>
    public SKBitmap GetOrRender(int chunkX, int chunkY, Func<SKBitmap> renderFunc)
    {
        var key = (chunkX, chunkY);

        if (_invalidated.Remove(key) || !_cache.TryGetValue(key, out var bmp))
        {
            // Dispose old bitmap
            if (_cache.TryGetValue(key, out var old))
            {
                old.Dispose();
                _cache.Remove(key);
            }

            bmp = renderFunc();
            _cache[key] = bmp;
        }

        return bmp;
    }

    /// <summary>Removes and disposes all cached bitmaps.</summary>
    public void Clear()
    {
        foreach (var bmp in _cache.Values)
            bmp.Dispose();
        _cache.Clear();
        _invalidated.Clear();
    }

    /// <summary>Consumes dirty flags from the map's terrain and height layers,
    /// marking those chunks as invalidated.</summary>
    public void SyncDirtyChunks(WorldMap map)
    {
        // Terrain dirty chunks
        foreach (var chunk in map.Terrain.DirtyChunks)
        {
            _invalidated.Add(chunk.Index);
            chunk.MarkClean();
        }

        // Height dirty chunks
        foreach (var chunk in map.Height.DirtyChunks)
        {
            _invalidated.Add(chunk.Index);
            chunk.MarkClean();
        }
    }

    /// <summary>Number of entries in the cache.</summary>
    public int Count => _cache.Count;

    /// <summary>Number of entries pending re-render.</summary>
    public int PendingCount => _invalidated.Count;
}
