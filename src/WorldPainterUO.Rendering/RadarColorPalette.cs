using System;
using System.IO;
using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Provides radar colors for land tile IDs sourced from radarcol.mul.
/// radarcol.mul is a flat array of 0x10000 16-bit BGR555 entries (2 bytes each,
/// little-endian). Index 0..0x3FFF are land tiles; 0x4000..0xFFFF are statics.
/// Falls back to a deterministic palette when the file is unavailable.
/// </summary>
public class RadarColorPalette
{
    private readonly SKColor[] _colors = new SKColor[0x10000];
    private bool _loaded;

    /// <summary>
    /// Attempts to load radarcol.mul from the given UO data path.
    /// Safe to call multiple times; re-loads on each call.
    /// </summary>
    public bool TryLoad(string uoDataPath)
    {
        var path = Path.Combine(uoDataPath, "radarcol.mul");
        if (!File.Exists(path))
            return false;

        try
        {
            var bytes = File.ReadAllBytes(path);
            // Each entry is 2 bytes: 16-bit BGR555 little-endian
            // bit layout: 0BBBBBGGGGGRRRRR
            var count = Math.Min(bytes.Length / 2, _colors.Length);
            for (var i = 0; i < count; i++)
            {
                var raw = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                var r = (byte)(((raw)       & 0x1F) << 3);
                var g = (byte)(((raw >> 5)  & 0x1F) << 3);
                var b = (byte)(((raw >> 10) & 0x1F) << 3);
                _colors[i] = new SKColor(r, g, b);
            }

            _loaded = true;
            return true;
        }
        catch
        {
            _loaded = false;
            return false;
        }
    }

    /// <summary>Gets the radar color for a land tile ID.</summary>
    public SKColor GetColor(ushort tileId)
    {
        if (_loaded)
            return _colors[tileId & 0x3FFF]; // land tiles are 0..0x3FFF

        return FallbackColor(tileId);
    }

    /// <summary>Gets the radar color for a static tile ID (offset by 0x4000 in radarcol).</summary>
    public SKColor GetStaticColor(ushort staticId)
    {
        if (_loaded)
            return _colors[0x4000 + (staticId & 0x3FFF)];

        return FallbackColor(staticId);
    }

    // Deterministic fallback when radarcol.mul is not available.
    private static SKColor FallbackColor(ushort tileId)
    {
        var groups = new (byte r, byte g, byte b)[]
        {
            ( 90, 140,  60),  // grass
            (140, 110,  60),  // dirt
            (110, 110, 110),  // stone
            ( 50,  80, 180),  // water  ← corrected to blue
            ( 60, 100,  50),  // forest dark
            (180, 190, 200),  // snow
            (100,  90,  50),  // swamp
            (180, 160, 100),  // sand
            (150,  80,  60),  // dark earth
            ( 80,  70,  50),  // muddy
            (130, 130,  90),  // light grass
            ( 70, 120, 180),  // shallow water  ← blue
            (160, 140, 110),  // light dirt
            ( 90,  90,  90),  // dark stone
            (200, 180, 150),  // light sand
            ( 40,  60, 160),  // deep water  ← blue
        };

        var idx = (tileId >> 3) % groups.Length;
        var (r, g, b) = groups[idx];
        return new SKColor(r, g, b);
    }
}
