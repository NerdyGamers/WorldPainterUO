using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>Draws grid lines and region borders over the viewport.</summary>
public static class OverlayRenderer
{
    /// <summary>Draws a tile grid and/or chunk grid.</summary>
    public static void DrawGrid(
        SKCanvas canvas,
        MapDimensions dims,
        float offsetX, float offsetY, float zoom,
        int canvasWidth, int canvasHeight,
        bool showTileGrid, bool showChunkGrid,
        SKColor? tileColor = null, SKColor? chunkColor = null)
    {
        if (!showTileGrid && !showChunkGrid)
            return;

        var tileGridColor = tileColor ?? new SKColor(255, 255, 255, 20);
        var chunkGridColor = chunkColor ?? new SKColor(255, 255, 255, 60);

        if (showChunkGrid)
        {
            using var paint = new SKPaint
            {
                Color = chunkGridColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = false,
            };

            var chunkSize = dims.ChunkSize;
            var startCX = Math.Max(0, (int)Math.Floor(-offsetX / chunkSize));
            var startCY = Math.Max(0, (int)Math.Floor(-offsetY / chunkSize));
            var endCX = Math.Min(dims.ChunksX,
                (int)Math.Ceiling((canvasWidth / zoom - offsetX) / chunkSize) + 1);

            for (var cx = startCX; cx < endCX; cx++)
            {
                var x = (cx * chunkSize + offsetX) * zoom;
                canvas.DrawLine(x, 0, x, canvasHeight, paint);
            }

            var endCY = Math.Min(dims.ChunksY,
                (int)Math.Ceiling((canvasHeight / zoom - offsetY) / chunkSize) + 1);

            for (var cy = startCY; cy < endCY; cy++)
            {
                var y = (cy * chunkSize + offsetY) * zoom;
                canvas.DrawLine(0, y, canvasWidth, y, paint);
            }
        }

        if (showTileGrid && zoom >= 8)
        {
            using var paint = new SKPaint
            {
                Color = tileGridColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.5f,
                IsAntialias = false,
            };

            var startX = Math.Max(0, (int)Math.Floor(-offsetX));
            var startY = Math.Max(0, (int)Math.Floor(-offsetY));
            var endX = Math.Min(dims.Width, (int)Math.Ceiling((canvasWidth / zoom) - offsetX) + 1);
            var endY = Math.Min(dims.Height, (int)Math.Ceiling((canvasHeight / zoom) - offsetY) + 1);

            for (var x = startX; x <= endX; x++)
            {
                var sx = (x + offsetX) * zoom;
                canvas.DrawLine(sx, 0, sx, canvasHeight, paint);
            }

            for (var y = startY; y <= endY; y++)
            {
                var sy = (y + offsetY) * zoom;
                canvas.DrawLine(0, sy, canvasWidth, sy, paint);
            }
        }
    }
}
