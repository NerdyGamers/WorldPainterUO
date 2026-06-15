using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Generators;

public sealed class GenerateArchipelago : IMapGenerator
{
    public WorldMap Generate(MapDimensions dimensions, int seed, BiomeStyle biomeStyle)
    {
        var map = new WorldMap(dimensions, new MapMetadata("Generated", SourceFileType.Mul));
        var width = dimensions.Width;
        var height = dimensions.Height;
        var rng = new Random(seed);

        var islandCount = Math.Clamp((int)(Math.Sqrt(width * height) / 80), 3, 40);
        var islands = new (float X, float Y, float Radius)[islandCount];

        for (var i = 0; i < islandCount; i++)
        {
            islands[i] = (
                (float)(rng.NextDouble() * (width - 80) + 40),
                (float)(rng.NextDouble() * (height - 80) + 40),
                (float)(rng.NextDouble() * 60 + 30)
            );
        }

        var heightMap = NoiseHelper.GenerateHeightMap(width, height, seed + 1, 0.04f, 3, 0.5f);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var minDist = float.MaxValue;
                foreach (var (ix, iy, ir) in islands)
                {
                    var dx = (x - ix) / ir;
                    var dy = (y - iy) / ir;
                    var d = dx * dx + dy * dy;
                    if (d < minDist) minDist = d;
                }

                var falloff = 1f - Math.Clamp((float)Math.Sqrt(minDist), 0, 1);
                falloff = falloff * falloff * (3 - 2 * falloff);

                var elevation = falloff * heightMap[x, y] * 2f;
                var z = (sbyte)Math.Clamp(elevation * 50, -128, 127);
                map.Height[x, y] = z;

                map.Terrain[x, y] = ResolveArchipelagoTile(elevation, falloff);
            }
        }

        return map;
    }

    private static ushort ResolveArchipelagoTile(float elevation, float falloff)
    {
        if (falloff < 0.02f) return 0x0001;
        if (falloff < 0.1f) return 0x0000;
        if (falloff < 0.2f) return 0x00A8;
        if (elevation < 0.4f) return 0x0003;
        if (elevation < 0.7f) return 0x000C;
        if (elevation < 1.0f) return 0x0056;
        return 0x01AE;
    }
}
