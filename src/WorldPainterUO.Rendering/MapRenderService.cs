using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>GPU-accelerated map renderer using SkiaSharp.</summary>
public sealed class MapRenderService
{
    private readonly RadarColorPalette _palette = new();
    private FallbackTileTextureProvider _fallback;
    private readonly Dictionary<(int cx, int cy), SKBitmap> _chunkCache = new();
    private readonly HashSet<(int cx, int cy)> _dirtyChunks = new();

    private const int ChunkSize = 32;

    public MapRenderService()
    {
        _fallback = new FallbackTileTextureProvider(_palette);
    }

    public float Zoom { get; set; } = 1f;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public ViewMode ViewMode { get; set; } = ViewMode.Radar;
    public bool ShowTileGrid { get; set; }
    public bool ShowChunkGrid { get; set; }

    /// <summary>Whether the Terrain layer is visible (controlled by layer panel).</summary>
    public bool TerrainVisible { get; set; } = true;

    /// <summary>Whether the Height shading overlay is visible (controlled by layer panel).</summary>
    public bool HeightVisible { get; set; } = true;

    /// <summary>Loads radar colors from the given UO data folder.</summary>
    public bool TryLoadRadarColors(string? uoDataPath)
    {
        if (string.IsNullOrWhiteSpace(uoDataPath))
            return false;
        var loaded = _palette.TryLoad(uoDataPath);
        if (loaded) InvalidateAll();
        return loaded;
    }

    public void SyncDirtyChunks(WorldMap map)
    {
        foreach (var key in map.ConsumeAndClearDirtyChunks())
            _dirtyChunks.Add(key);
    }

    public void InvalidateAll()
    {
        foreach (var bmp in _chunkCache.Values)
            bmp.Dispose();
        _chunkCache.Clear();
        _dirtyChunks.Clear();
    }

    public void ClearCache() => InvalidateAll();

    public void Render(SKCanvas canvas, WorldMap map, int viewW, int viewH)
    {
        canvas.Clear(new SKColor(20, 20, 30));

        var dims = map.Dimensions;
        var tileSize = Zoom;

        // Visible tile range
        var startX = Math.Max(0, (int)(-OffsetX));
        var startY = Math.Max(0, (int)(-OffsetY));
        var endX = Math.Min(dims.Width,  startX + (int)(viewW / tileSize) + 2);
        var endY = Math.Min(dims.Height, startY + (int)(viewH / tileSize) + 2);

        // Render by chunk
        var startCX = startX / ChunkSize;
        var startCY = startY / ChunkSize;
        var endCX = (endX + ChunkSize - 1) / ChunkSize;
        var endCY = (endY + ChunkSize - 1) / ChunkSize;

        for (var cy = startCY; cy < endCY; cy++)
        for (var cx = startCX; cx < endCX; cx++)
        {
            var key = (cx, cy);
            if (_dirtyChunks.Remove(key) && _chunkCache.TryGetValue(key, out var old))
            {
                old.Dispose();
                _chunkCache.Remove(key);
            }

            if (!_chunkCache.TryGetValue(key, out var chunkBmp))
            {
                chunkBmp = RenderChunk(map, cx, cy);
                _chunkCache[key] = chunkBmp;
            }

            var screenX = (cx * ChunkSize + OffsetX) * tileSize;
            var screenY = (cy * ChunkSize + OffsetY) * tileSize;
            var chunkPx = ChunkSize * tileSize;

            using var paint = new SKPaint { FilterQuality = SKFilterQuality.None };
            canvas.DrawBitmap(chunkBmp,
                new SKRect(0, 0, chunkBmp.Width, chunkBmp.Height),
                new SKRect(screenX, screenY, screenX + chunkPx, screenY + chunkPx),
                paint);
        }

        if (ShowTileGrid && Zoom >= 4f)
            DrawTileGrid(canvas, dims, viewW, viewH);

        if (ShowChunkGrid)
            DrawChunkGrid(canvas, dims, viewW, viewH);
    }

    private SKBitmap RenderChunk(WorldMap map, int cx, int cy)
    {
        var dims = map.Dimensions;
        var bmp = new SKBitmap(ChunkSize, ChunkSize);

        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Black);

        for (var ty = 0; ty < ChunkSize; ty++)
        for (var tx = 0; tx < ChunkSize; tx++)
        {
            var tileX = cx * ChunkSize + tx;
            var tileY = cy * ChunkSize + ty;

            if (tileX >= dims.Width || tileY >= dims.Height)
                continue;

            var id = map.Terrain[tileX, tileY];
            var z  = map.Height[tileX, tileY];

            SKColor color;

            if (!TerrainVisible)
            {
                color = new SKColor(40, 40, 40);
            }
            else
            {
                color = _palette.GetColor(id);

                if (HeightVisible)
                {
                    var heightFactor = (z + 100) / 227.0f;
                    heightFactor = Math.Clamp(heightFactor, 0.3f, 1.0f);
                    color = new SKColor(
                        (byte)(color.Red   * heightFactor),
                        (byte)(color.Green * heightFactor),
                        (byte)(color.Blue  * heightFactor));
                }
            }

            bmp.SetPixel(tx, ty, color);
        }

        return bmp;
    }

    private void DrawTileGrid(SKCanvas canvas, MapDimensions dims, int viewW, int viewH)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 25),
            StrokeWidth = 0.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
        };

        var ts = Zoom;
        var startX = Math.Max(0, (int)(-OffsetX));
        var startY = Math.Max(0, (int)(-OffsetY));
        var endX = Math.Min(dims.Width,  startX + (int)(viewW / ts) + 2);
        var endY = Math.Min(dims.Height, startY + (int)(viewH / ts) + 2);

        for (var x = startX; x <= endX; x++)
        {
            var sx = (x + OffsetX) * ts;
            canvas.DrawLine(sx, 0, sx, viewH, paint);
        }
        for (var y = startY; y <= endY; y++)
        {
            var sy = (y + OffsetY) * ts;
            canvas.DrawLine(0, sy, viewW, sy, paint);
        }
    }

    private void DrawChunkGrid(SKCanvas canvas, MapDimensions dims, int viewW, int viewH)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(255, 200, 50, 40),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
        };

        var ts = Zoom;
        var startX = Math.Max(0, (int)(-OffsetX) / ChunkSize * ChunkSize);
        var startY = Math.Max(0, (int)(-OffsetY) / ChunkSize * ChunkSize);
        var endX = Math.Min(dims.Width,  startX + (int)(viewW / ts) + ChunkSize + 1);
        var endY = Math.Min(dims.Height, startY + (int)(viewH / ts) + ChunkSize + 1);

        for (var x = startX; x <= endX; x += ChunkSize)
        {
            var sx = (x + OffsetX) * ts;
            canvas.DrawLine(sx, 0, sx, viewH, paint);
        }
        for (var y = startY; y <= endY; y += ChunkSize)
        {
            var sy = (y + OffsetY) * ts;
            canvas.DrawLine(0, sy, viewW, sy, paint);
        }
    }
}
