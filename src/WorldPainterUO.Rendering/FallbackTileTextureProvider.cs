using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Fallback tile texture provider that generates procedural textures.
/// Used when art.mul / artLegacyMUL.uop is not loaded.
/// </summary>
public sealed class FallbackTileTextureProvider : ITileTextureProvider
{
    public bool HasArtwork => false;

    public SKBitmap? GetLandTileTexture(ushort tileId)
    {
        // No real artwork — return null to signal caller should use fallback rendering
        return null;
    }

    /// <summary>
    /// Draws a terrain-style preview for a single tile (for use in Terrain View fallback mode).
    /// </summary>
    public void RenderFallbackTile(SKCanvas canvas, float x, float y, float size,
        ushort tileId, sbyte z)
    {
        var baseColor = RadarColorPalette.GetColor(tileId);
        var heightFactor = (z + 100) / 227.0f;
        heightFactor = Math.Clamp(heightFactor, 0.4f, 1.0f);

        using var paint = new SKPaint
        {
            Color = new SKColor(
                (byte)(baseColor.Red * heightFactor),
                (byte)(baseColor.Green * heightFactor),
                (byte)(baseColor.Blue * heightFactor)),
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
        };

        canvas.DrawRect(x, y, size, size, paint);

        // Subtle internal pattern for terrain feel
        if (size >= 8)
        {
            var tint = (tileId & 0x3) switch
            {
                0 => new SKColor(0, 0, 0, 8),
                1 => new SKColor(255, 255, 255, 6),
                _ => SKColors.Transparent,
            };

            if (tint.Alpha > 0)
            {
                paint.Color = tint;
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawRect(x + size * 0.25f, y + size * 0.25f,
                    size * 0.5f, size * 0.5f, paint);
            }
        }
    }
}
