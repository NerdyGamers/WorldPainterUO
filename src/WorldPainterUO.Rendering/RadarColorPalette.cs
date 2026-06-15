using System;
using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Provides radar colors for land and static tile IDs.
/// Delegates to <see cref="Ultima.RadarCol"/> from the Ultima SDK when a
/// UO data path has been set, which correctly handles radarcol.mul loading
/// and the BGR555 → RGB conversion internally.
/// Falls back to a deterministic palette when no data path is available.
/// </summary>
public class RadarColorPalette
{
    private bool _loaded;

    /// <summary>
    /// Points the Ultima SDK at the given data folder and loads radarcol.mul.
    /// Returns true if the file was found and loaded successfully.
    /// </summary>
    public bool TryLoad(string? uoDataPath)
    {
        if (string.IsNullOrWhiteSpace(uoDataPath))
            return false;

        try
        {
            // The SDK uses a global data path; set it before accessing RadarCol.
            Ultima.Files.SetMulPath(uoDataPath);
            // Force the SDK to load radarcol.mul now.
            _ = Ultima.RadarCol.GetLandColor(0);
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
            var c = Ultima.RadarCol.GetLandColor(tileId);
            return new SKColor(c.R, c.G, c.B);
        }
        return FallbackColor(tileId);
    }

    /// <summary>Gets the radar color for a static tile ID.</summary>
    public SKColor GetStaticColor(ushort staticId)
    {
        if (_loaded)
        {
            var c = Ultima.RadarCol.GetStaticColor(staticId);
            return new SKColor(c.R, c.G, c.B);
        }
        return FallbackColor(staticId);
    }

    private static SKColor FallbackColor(ushort tileId)
    {
        var groups = new (byte r, byte g, byte b)[]
        {
            ( 50,  80, 180),  // deep water
            ( 70, 120, 200),  // shallow water
            ( 90, 160, 210),  // very shallow water
            (110, 170, 160),  // wet sand / shoreline
            (180, 160, 100),  // sand
            (200, 180, 130),  // light sand
            (160, 140,  80),  // dry sand
            ( 90, 140,  60),  // grass
            (110, 155,  70),  // light grass
            ( 70, 110,  45),  // dark grass
            (130, 160,  80),  // pale grass
            ( 60, 100,  50),  // forest dark
            ( 80, 120,  60),  // forest mid
            ( 50,  80,  40),  // dense forest
            (100,  90,  50),  // swamp
            ( 80,  70,  40),  // dark swamp
            (140, 110,  60),  // dirt
            (120,  95,  55),  // dark dirt
            (160, 130,  90),  // light dirt
            (110, 110, 110),  // stone
            ( 90,  90,  90),  // dark stone
            (140, 140, 140),  // light stone
            (160, 150, 130),  // rock face
            (180, 190, 200),  // snow
            (200, 210, 220),  // light snow
            (150, 160, 170),  // icy ground
            (150,  80,  60),  // volcanic
            (100,  60,  50),  // lava
            (130,  70,  40),  // scorched earth
            ( 80,  60,  80),  // dungeon floor
            ( 60,  50,  70),  // dungeon wall
            (200, 170, 100),  // road / cobble
        };
        var idx = (tileId >> 2) % groups.Length;
        var (r, g, b) = groups[idx];
        return new SKColor(r, g, b);
    }
}
