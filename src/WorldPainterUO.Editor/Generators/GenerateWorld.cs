using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Generators;

public sealed class GenerateWorld : IMapGenerator
{
    public WorldMap Generate(MapDimensions dimensions, int seed, BiomeStyle biomeStyle)
    {
        var map = new WorldMap(dimensions, new MapMetadata("Generated", SourceFileType.Mul));
        var width = dimensions.Width;
        var height = dimensions.Height;

        var continentNoise = NoiseHelper.GenerateHeightMap(width, height, seed, 0.012f, 6, 0.55f);
        var detailNoise = NoiseHelper.GenerateHeightMap(width, height, seed + 200, 0.03f, 3, 0.45f);
        var archipelagoNoise = NoiseHelper.GenerateHeightMap(width, height, seed + 400, 0.05f, 2, 0.5f);

        var rng = new Random(seed + 1000);
        var archipelagoCount = Math.Clamp((int)(Math.Sqrt(width * height) / 100), 5, 30);
        var archipelagoSpots = new (float X, float Y, float R)[archipelagoCount];

        for (var i = 0; i < archipelagoCount; i++)
        {
            archipelagoSpots[i] = (
                (float)(rng.NextDouble() * (width - 60) + 30),
                (float)(rng.NextDouble() * (height - 60) + 30),
                (float)(rng.NextDouble() * 40 + 15)
            );
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var continentVal = (continentNoise[x, y] - 0.30f) * 1.8f;
                continentVal = Math.Clamp(continentVal, 0, 1);

                var detailVal = detailNoise[x, y] * 0.25f;
                var archipelagoVal = archipelagoNoise[x, y] * 0.15f;

                var archipelagoFalloff = 0f;
                foreach (var (ax, ay, ar) in archipelagoSpots)
                {
                    var dx = (x - ax) / ar;
                    var dy = (y - ay) / ar;
                    var dist = dx * dx + dy * dy;
                    if (dist < 1)
                    {
                        var f = 1f - (float)Math.Sqrt(dist);
                        archipelagoFalloff = Math.Max(archipelagoFalloff, f * f * (3 - 2 * f));
                    }
                }

                var elevation = continentVal + detailVal + archipelagoFalloff * 0.5f;
                elevation = Math.Clamp(elevation, 0, 1);

                var zRaw = elevation < 0.15f
                    ? (elevation / 0.15f) * 5
                    : 5 + (elevation - 0.15f) / 0.85f * 55;
                var z = (sbyte)Math.Clamp(zRaw, -128, 127);
                map.Height[x, y] = z;

                map.Terrain[x, y] = ResolveWorldTile(elevation, biomeStyle, continentVal > 0.1f);
            }
        }

        return map;
    }

    private static ushort ResolveWorldTile(float elevation, BiomeStyle biome, bool isContinent)
    {
        if (elevation < 0.03f) return 0x0001;
        if (elevation < 0.08f) return 0x0000;
        if (elevation < 0.12f) return 0x00A8;

        if (!isContinent && elevation < 0.3f)
        {
            return 0x00A8;
        }

        if (biome == BiomeStyle.Arctic)
        {
            if (elevation < 0.3f) return 0x01A0;
            if (elevation < 0.5f) return 0x0155;
            if (elevation < 0.7f) return 0x01A5;
            return 0x0205;
        }

        if (biome == BiomeStyle.Desert)
        {
            if (elevation < 0.3f) return 0x00A8;
            if (elevation < 0.5f) return 0x00A9;
            if (elevation < 0.7f) return 0x0220;
            return 0x01AE;
        }

        if (biome == BiomeStyle.Volcanic)
        {
            if (elevation < 0.3f) return 0x0044;
            if (elevation < 0.5f) return 0x01AD;
            if (elevation < 0.7f) return 0x01AE;
            return 0x01AF;
        }

        if (biome == BiomeStyle.Tropical)
        {
            if (elevation < 0.3f) return 0x00A8;
            if (elevation < 0.5f) return 0x000C;
            if (elevation < 0.7f) return 0x0057;
            return 0x01AE;
        }

        if (elevation < 0.2f) return 0x0003;
        if (elevation < 0.35f) return 0x0004;
        if (elevation < 0.50f) return 0x000C;
        if (elevation < 0.65f) return 0x0056;
        if (elevation < 0.80f) return 0x01A5;
        return 0x01AE;
    }
}
