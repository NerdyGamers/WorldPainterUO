using SkiaSharp;
using WorldPainterUO.Core;

namespace WorldPainterUO.Rendering;

/// <summary>Renders a full-map minimap at reduced resolution.</summary>
public sealed class MinimapRenderer
{
    private SKBitmap? _cached;

    /// <summary>Gets or renders the minimap bitmap. Cached until <see cref="Invalidate"/>.</summary>
    public SKBitmap GetOrRender(WorldMap map, int maxSize = 200)
    {
        var dims = map.Dimensions;
        var w = Math.Max(1, Math.Min(maxSize, dims.Width));
        var h = Math.Max(1, Math.Min(maxSize, dims.Height));

        if (_cached is null || _cached.Width != w || _cached.Height != h)
        {
            _cached?.Dispose();
            _cached = new SKBitmap(w, h);

            for (var py = 0; py < h; py++)
            {
                var tileY = Math.Clamp((int)((float)py / h * dims.Height), 0, dims.Height - 1);

                for (var px = 0; px < w; px++)
                {
                    var tileX = Math.Clamp((int)((float)px / w * dims.Width), 0, dims.Width - 1);

                    var id = map.Terrain[tileX, tileY];
                    var z = map.Height[tileX, tileY];
                    var baseColor = RadarColorPalette.GetColor(id);
                    var heightFactor = (z + 100) / 227.0f;
                    heightFactor = Math.Clamp(heightFactor, 0.3f, 1.0f);

                    _cached.SetPixel(px, py, new SKColor(
                        (byte)(baseColor.Red * heightFactor),
                        (byte)(baseColor.Green * heightFactor),
                        (byte)(baseColor.Blue * heightFactor)));
                }
            }
        }

        return _cached;
    }

    /// <summary>Forces the minimap to regenerate on the next call.</summary>
    public void Invalidate()
    {
        _cached?.Dispose();
        _cached = null;
    }
}
