using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Generates stable radar-style colors from land tile IDs.
/// In production this would read radarcol.mul; for now uses a deterministic
/// palette that gives distinct colors to common UO terrain groups.
/// </summary>
public static class RadarColorPalette
{
    private static readonly SKColor[] Colors;

    static RadarColorPalette()
    {
        // 64-entry palette covering major UO land-tile groups
        Colors = new SKColor[64];
        for (var i = 0; i < 64; i++)
        {
            Colors[i] = GenerateStableColor(i);
        }
    }

    /// <summary>Gets the radar color for a land tile ID.</summary>
    public static SKColor GetColor(ushort tileId)
    {
        var idx = (tileId >> 4) & 0x3F;     // bits 4-9 for palette index
        return Colors[idx];
    }

    private static SKColor GenerateStableColor(int idx)
    {
        // Common UO terrain group hues (grass, dirt, stone, water, forest, snow, swamp, sand)
        var groups = new (byte r, byte g, byte b)[]
        {
            ( 90, 140,  60),   // grass
            (140, 110,  60),   // dirt
            (110, 110, 110),   // stone
            ( 50,  80, 140),   // water
            ( 60, 100,  50),   // forest dark
            (180, 190, 200),   // snow
            (100,  90,  50),   // swamp
            (180, 160, 100),   // sand
            (150,  80,  60),   // dark earth
            ( 80,  70,  50),   // muddy
            (130, 130,  90),   // light grass
            ( 70, 120, 130),   // shallow water
            (160, 140, 110),   // light dirt
            ( 90,  90,  90),   // dark stone
            (200, 180, 150),   // light sand
            ( 60,  60,  90),   // deep water
            (120, 100,  80),   // brown earth
            (170, 200, 130),   // bright grass
            (100, 130, 100),   // moss
            ( 80, 100, 120),   // wet stone
            (200, 170, 130),   // tan sand
            (110,  70,  50),   // dark brown
            (150, 160, 120),   // dry grass
            ( 70,  90, 110),   // rocky water
            (130, 120, 100),   // gravel
            ( 90, 130,  80),   // lush grass
            (160, 110,  70),   // orange dirt
            (180, 180, 170),   // light stone
            ( 70,  70,  50),   // dark swamp
            (210, 190, 160),   // pale sand
            ( 40,  70, 120),   // ocean
            (110, 150,  90),   // light forest
        };

        var (r, g, b) = groups[idx % groups.Length];
        // Vary within group for more texture
        var variation = (idx * 7 + idx * idx * 3) % 21 - 10;
        return new SKColor(
            (byte)Math.Clamp(r + variation, 0, 255),
            (byte)Math.Clamp(g + variation, 0, 255),
            (byte)Math.Clamp(b + variation, 0, 255));
    }
}
