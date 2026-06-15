using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Main rendering orchestrator. Manages view mode, zoom/pan, chunk-cached
/// tile rendering, grid/region overlays, and dirty-chunk invalidation.
/// </summary>
public sealed class MapRenderService
{
    private readonly FallbackTileTextureProvider _tileProvider = new();
    private readonly RenderCache _cache = new();

    public ViewMode ViewMode { get; set; } = ViewMode.Radar;

    /// <summary>Zoom in pixels per tile (clamped 0.1–8).</summary>
    public float Zoom { get; set; } = 1.0f;

    /// <summary>Pan offset in tile units.</summary>
    public float OffsetX { get; set; }

    /// <summary>Pan offset in tile units.</summary>
    public float OffsetY { get; set; }

    public bool ShowTileGrid { get; set; }
    public bool ShowChunkGrid { get; set; }

    /// <summary>Consumes dirty flags from the map and marks chunks for re-render.</summary>
    public void SyncDirtyChunks(WorldMap map) => _cache.SyncDirtyChunks(map);

    /// <summary>Marks a specific chunk for re-render.</summary>
    public void InvalidateChunk(int cx, int cy) => _cache.InvalidateChunk(cx, cy);

    /// <summary>Marks all chunks for re-render.</summary>
    public void InvalidateAll() => _cache.InvalidateAll();

    /// <summary>Clears all cached chunk bitmaps.</summary>
    public void ClearCache() => _cache.Clear();

    /// <summary>Renders the current view of the map to the canvas.</summary>
    public void Render(SKCanvas canvas, WorldMap map, int width, int height)
    {
        if (map is null)
        {
            canvas.Clear(new SKColor(0x1a, 0x1a, 0x2e));
            return;
        }

        var dims = map.Dimensions;
        var zoom = Math.Clamp(Zoom, 0.1f, 8f);

        canvas.Clear(new SKColor(0x1a, 0x1a, 0x2e));

        // Compute visible chunk range
        var chunkSize = MapChunk<ushort>.Size;
        var startCX = Math.Max(0, (int)Math.Floor(-OffsetX / chunkSize));
        var startCY = Math.Max(0, (int)Math.Floor(-OffsetY / chunkSize));
        var endCX = Math.Min(dims.ChunksX,
            (int)Math.Ceiling((width / zoom - OffsetX) / chunkSize) + 1);
        var endCY = Math.Min(dims.ChunksY,
            (int)Math.Ceiling((height / zoom - OffsetY) / chunkSize) + 1);

        // Render each visible chunk
        for (var cy = startCY; cy < endCY; cy++)
        {
            for (var cx = startCX; cx < endCX; cx++)
            {
                var chunkBmp = _cache.GetOrRender(cx, cy, () =>
                    RenderChunk(map, cx, cy));

                // Position the cached chunk bitmap
                var sx = (cx * chunkSize + OffsetX) * zoom;
                var sy = (cy * chunkSize + OffsetY) * zoom;
                var sw = chunkSize * zoom;
                var sh = chunkSize * zoom;

                canvas.DrawBitmap(chunkBmp,
                    new SKRect(0, 0, chunkBmp.Width, chunkBmp.Height),
                    new SKRect(sx, sy, sx + sw, sy + sh));
            }
        }

        // Overlays
        OverlayRenderer.DrawGrid(canvas, dims,
            OffsetX, OffsetY, zoom,
            width, height,
            ShowTileGrid, ShowChunkGrid);
    }

    private SKBitmap RenderChunk(WorldMap map, int cx, int cy)
    {
        var size = MapChunk<ushort>.Size;
        var bmp = new SKBitmap(size * 4, size * 4); // Each tile rendered at 4x4 for quality
        var tilePx = 4;

        using var chunkCanvas = new SKCanvas(bmp);
        var terrainChunk = map.Terrain.GetChunk(cx, cy);
        var heightChunk = map.Height.GetChunk(cx, cy);
        var tileX = cx * size;
        var tileY = cy * size;

        for (var ly = 0; ly < size; ly++)
        {
            for (var lx = 0; lx < size; lx++)
            {
                var id = terrainChunk[lx, ly];
                var z = heightChunk[lx, ly];
                var px = lx * tilePx;
                var py = ly * tilePx;

                switch (ViewMode)
                {
                    case ViewMode.Terrain:
                        if (_tileProvider.HasArtwork)
                        {
                            var tex = _tileProvider.GetLandTileTexture(id);
                            if (tex is not null)
                                chunkCanvas.DrawBitmap(tex, px, py);
                            else
                                _tileProvider.RenderFallbackTile(chunkCanvas, px, py, tilePx, id, z);
                        }
                        else
                        {
                            _tileProvider.RenderFallbackTile(chunkCanvas, px, py, tilePx, id, z);
                        }
                        break;

                    case ViewMode.Hybrid:
                    {
                        // Terrain fallback, then radar tint
                        _tileProvider.RenderFallbackTile(chunkCanvas, px, py, tilePx, id, z);
                        var radar = RadarColorPalette.GetColor(id);
                        using var tint = new SKPaint
                        {
                            Color = new SKColor(radar.Red, radar.Green, radar.Blue, 60),
                            Style = SKPaintStyle.Fill,
                        };
                        chunkCanvas.DrawRect(px, py, tilePx, tilePx, tint);
                        break;
                    }

                    default: // Radar
                    {
                        var color = RadarColorPalette.GetColor(id);
                        var heightFactor = (z + 100) / 227.0f;
                        heightFactor = Math.Clamp(heightFactor, 0.3f, 1.0f);
                        using var paint = new SKPaint
                        {
                            Color = new SKColor(
                                (byte)(color.Red * heightFactor),
                                (byte)(color.Green * heightFactor),
                                (byte)(color.Blue * heightFactor)),
                            Style = SKPaintStyle.Fill,
                            IsAntialias = false,
                        };
                        chunkCanvas.DrawRect(px, py, tilePx, tilePx, paint);
                        break;
                    }
                }
            }
        }

        return bmp;
    }
}
