using System;
using System.IO;
using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Provides radar colors for land tile IDs sourced from radarcol.mul.
/// radarcol.mul is a flat array of 0x10000 16-bit RGB555 entries (2 bytes each,
/// little-endian). Bit layout: 0RRRRRGGGGGBBBBB
/// Index 0..0x3FFF are land tiles; 0x4000..0xFFFF are statics.
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
            // Each entry is 2 bytes: 16-bit RGB555 little-endian
            // Bit layout: 0RRRRRGGGGGBBBBB
            var count = Math.Min(bytes.Length / 2, _colors.Length);
            for (var i = 0; i < count; i++)
            {
                var raw = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                // RGB555: R=bits14-10, G=bits9-5, B=bits4-0
                var r = (byte)(((raw >> 10) & 0x1F) << 3);
                var g = (byte)(((raw >> 5)  & 0x1F) << 3);
                var b = (byte)(((raw)       & 0x1F) << 3);
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
        {
            // Do NOT mask with 0x3FFF — land tile IDs can legitimately exceed
            // that range in newer UO clients. Clamp to array bounds only.
            var idx = Math.Min((int)tileId, _colors.Length - 1);
            return _colors[idx];
        }

        return FallbackColor(tileId);
    }

    /// <summary>Gets the radar color for a static tile ID (offset by 0x4000 in radarcol).</summary>
    public SKColor GetStaticColor(ushort staticId)
    {
        if (_loaded)
        {
            var idx = Math.Min(0x4000 + (int)staticId, _colors.Length - 1);
            return _colors[idx];
        }

        return FallbackColor(staticId);
    }

    // Deterministic fallback when radarcol.mul is not available.
    // 32 buckets give better coverage of UO's wide tile ID range.
    private static SKColor FallbackColor(ushort tileId)
    {
        var groups = new (byte r, byte g, byte b)[]
        {
            ( 50,  80, 180),  //  0 deep water
            ( 70, 120, 200),  //  1 shallow water
            ( 90, 160, 210),  //  2 very shallow water
            (110, 170, 160),  //  3 wet sand / shoreline
            (180, 160, 100),  //  4 sand
            (200, 180, 130),  //  5 light sand
            (160, 140,  80),  //  6 dry sand
            ( 90, 140,  60),  //  7 grass
            (110, 155,  70),  //  8 light grass
            ( 70, 110,  45),  //  9 dark grass
            (130, 160,  80),  // 10 pale grass
            ( 60, 100,  50),  // 11 forest dark
            ( 80, 120,  60),  // 12 forest mid
            ( 50,  80,  40),  // 13 dense forest
            (100,  90,  50),  // 14 swamp
            ( 80,  70,  40),  // 15 dark swamp
            (140, 110,  60),  // 16 dirt
            (120,  95,  55),  // 17 dark dirt
            (160, 130,  90),  // 18 light dirt
            (110, 110, 110),  // 19 stone
            ( 90,  90,  90),  // 20 dark stone
            (140, 140, 140),  // 21 light stone
            (160, 150, 130),  // 22 rock face
            (180, 190, 200),  // 23 snow
            (200, 210, 220),  // 24 light snow
            (150, 160, 170),  // 25 icy ground
            (150,  80,  60),  // 26 dark earth / volcanic
            (100,  60,  50),  // 27 lava
            (130,  70,  40),  // 28 scorched earth
            ( 80,  60,  80),  // 29 dungeon floor
            ( 60,  50,  70),  // 30 dungeon wall
            (200, 170, 100),  // 31 road / cobble
        };

        var idx = (tileId >> 2) % groups.Length;
        var (r, g, b) = groups[idx];
        return new SKColor(r, g, b);
    }
}
