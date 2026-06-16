using System;
using System.Collections.Generic;
using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>GPU-accelerated map renderer using SkiaSharp.</summary>
public sealed class MapRenderService
{
    private readonly RadarColorPalette             _palette     = new();
    private readonly FallbackTileTextureProvider   _fallback;
    private readonly Dictionary<(int cx, int cy), SKBitmap> _chunkCache  = new();
    private readonly HashSet<(int cx, int cy)>               _dirtyChunks = new();

    // Render chunks are 32x32 tiles each (visual grouping for the bitmap cache).
    // UO data chunks are 64x64 tiles (MapChunk.Size = 64).
    // Each data chunk therefore covers a 2x2 block of render chunks.
    private const int ChunkSize     = 32; // render-chunk tile width/height
    private const int RenderPerData = 2;  // DataChunkSize(64) / ChunkSize(32)

    // SKSamplingOptions replaces the obsolete SKPaint.FilterQuality API.
    // DrawImage (not DrawBitmap) accepts these directly.
    private static readonly SKSamplingOptions NearestSampling =
        new(SKFilterMode.Nearest, SKMipmapMode.None);

    private static readonly SKSamplingOptions LinearSampling =
        new(SKFilterMode.Linear, SKMipmapMode.None);

    public MapRenderService()
    {
        _fallback = new FallbackTileTextureProvider(_palette);
    }

    public float    Zoom          { get; set; } = 1f;
    public float    OffsetX       { get; set; }
    public float    OffsetY       { get; set; }
    public ViewMode ViewMode      { get; set; } = ViewMode.Radar;
    public bool     ShowTileGrid  { get; set; }
    public bool     ShowChunkGrid { get; set; }

    /// <summary>Whether the Terrain layer is visible.</summary>
    public bool TerrainVisible { get; set; } = true;

    /// <summary>Whether the Height shading overlay is visible.</summary>
    public bool HeightVisible { get; set; } = true;

    /// <summary>Loads radar colors from the given UO data folder.</summary>
    public bool TryLoadRadarColors(string? uoDataPath)
    {
        if (string.IsNullOrWhiteSpace(uoDataPath)) return false;
        var loaded = _palette.TryLoad(uoDataPath);
        if (loaded) InvalidateAll();
        return loaded;
    }

    /// <summary>
    /// Converts dirty data-chunk coordinates (64-tile grid) emitted by WorldMap
    /// into the corresponding render-chunk coordinates (32-tile grid) and queues
    /// those render chunks for bitmap rebuild on the next Render call.
    /// One UO data chunk (64x64) covers a 2x2 block of render chunks (32x32 each).
    /// </summary>
    public void SyncDirtyChunks(WorldMap map)
    {
        foreach (var (dcx, dcy) in map.ConsumeAndClearDirtyChunks())
        {
            // Translate data-chunk origin to render-chunk origin
            var rcx = dcx * RenderPerData;
            var rcy = dcy * RenderPerData;
            // Invalidate every render chunk covered by this data chunk
            for (var dy = 0; dy < RenderPerData; dy++)
            for (var dx = 0; dx < RenderPerData; dx++)
                _dirtyChunks.Add((rcx + dx, rcy + dy));
        }
    }

    public void InvalidateAll()
    {
        foreach (var bmp in _chunkCache.Values) bmp.Dispose();
        _chunkCache.Clear();
        _dirtyChunks.Clear();
    }

    public void ClearCache() => InvalidateAll();

    public void Render(SKCanvas canvas, WorldMap map, int viewW, int viewH)
    {
        canvas.Clear(new SKColor(20, 20, 30));

        if (map is null) return;
        var dims     = map.Dimensions;
        var tileSize = Zoom;

        // Visible tile range
        var startX = Math.Max(0, (int)(-OffsetX));
        var startY = Math.Max(0, (int)(-OffsetY));
        var endX   = Math.Min(dims.Width,  startX + (int)(viewW / tileSize) + 2);
        var endY   = Math.Min(dims.Height, startY + (int)(viewH / tileSize) + 2);

        // Evict dirty render chunks from cache before drawing
        foreach (var key in _dirtyChunks)
        {
            if (_chunkCache.TryGetValue(key, out var old))
            {
                old.Dispose();
                _chunkCache.Remove(key);
            }
        }
        _dirtyChunks.Clear();

        var chunkStartX = startX / ChunkSize;
        var chunkStartY = startY / ChunkSize;
        var chunkEndX   = (endX + ChunkSize - 1) / ChunkSize;
        var chunkEndY   = (endY + ChunkSize - 1) / ChunkSize;

        var sampling = tileSize < 1f ? LinearSampling : NearestSampling;

        for (var cy = chunkStartY; cy < chunkEndY; cy++)
        for (var cx = chunkStartX; cx < chunkEndX; cx++)
        {
            var bmp = GetOrBuildChunk(map, cx, cy);
            if (bmp is null) continue;

            var screenX  = (cx * ChunkSize + OffsetX) * tileSize;
            var screenY  = (cy * ChunkSize + OffsetY) * tileSize;
            var destRect = new SKRect(screenX, screenY,
                                      screenX + ChunkSize * tileSize,
                                      screenY + ChunkSize * tileSize);

            using var image = SKImage.FromBitmap(bmp);
            canvas.DrawImage(image, destRect, sampling);
        }

        if (ShowTileGrid)  DrawTileGrid (canvas, startX, startY, endX, endY, tileSize);
        if (ShowChunkGrid) DrawChunkGrid(canvas, chunkStartX, chunkStartY, chunkEndX, chunkEndY, tileSize);
    }

    // ── Chunk building ──────────────────────────────────────────────────────────

    private SKBitmap? GetOrBuildChunk(WorldMap map, int cx, int cy)
    {
        if (_chunkCache.TryGetValue((cx, cy), out var cached)) return cached;

        var dims       = map.Dimensions;
        var tileStartX = cx * ChunkSize;
        var tileStartY = cy * ChunkSize;

        if (tileStartX >= dims.Width || tileStartY >= dims.Height) return null;

        var bmp = new SKBitmap(ChunkSize, ChunkSize, SKColorType.Rgb888x, SKAlphaType.Opaque);
        using var chunkCanvas = new SKCanvas(bmp);

        for (var ty = 0; ty < ChunkSize; ty++)
        for (var tx = 0; tx < ChunkSize; tx++)
        {
            var wx = tileStartX + tx;
            var wy = tileStartY + ty;
            if (wx >= dims.Width || wy >= dims.Height) continue;

            var tileId = map.Terrain[wx, wy];
            var z      = map.Height [wx, wy];
            _fallback.RenderFallbackTile(chunkCanvas, tx, ty, 1f, tileId, z);
        }

        _chunkCache[(cx, cy)] = bmp;
        return bmp;
    }

    // ── Grid overlays ───────────────────────────────────────────────────────────

    private static void DrawTileGrid(SKCanvas canvas, int startX, int startY, int endX, int endY, float tileSize)
    {
        using var paint = new SKPaint
        {
            Color       = new SKColor(255, 255, 255, 18),
            StrokeWidth = 0.5f,
            Style       = SKPaintStyle.Stroke,
        };
        for (var x = startX; x <= endX; x++)
            canvas.DrawLine(x * tileSize, startY * tileSize, x * tileSize, endY * tileSize, paint);
        for (var y = startY; y <= endY; y++)
            canvas.DrawLine(startX * tileSize, y * tileSize, endX * tileSize, y * tileSize, paint);
    }

    private static void DrawChunkGrid(SKCanvas canvas, int cStartX, int cStartY, int cEndX, int cEndY, float tileSize)
    {
        using var paint = new SKPaint
        {
            Color       = new SKColor(255, 220, 80, 40),
            StrokeWidth = 1f,
            Style       = SKPaintStyle.Stroke,
        };
        var pxSize = ChunkSize * tileSize;
        for (var cx = cStartX; cx <= cEndX; cx++)
            canvas.DrawLine(cx * pxSize, cStartY * pxSize, cx * pxSize, cEndY * pxSize, paint);
        for (var cy = cStartY; cy <= cEndY; cy++)
            canvas.DrawLine(cStartX * pxSize, cy * pxSize, cEndX * pxSize, cy * pxSize, paint);
    }
}
