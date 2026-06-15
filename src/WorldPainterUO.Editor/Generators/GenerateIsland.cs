using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Generators;

public sealed class GenerateIsland : IMapGenerator
{
    public WorldMap Generate(MapDimensions dimensions, int seed, BiomeStyle biomeStyle)
    {
        var map = new WorldMap(dimensions, new MapMetadata("Generated", SourceFileType.Mul));
        var width = dimensions.Width;
        var height = dimensions.Height;
        var cx = width / 2f;
        var cy = height / 2f;
        var maxDist = Math.Min(cx, cy) * 0.85f;

        var heightMap = NoiseHelper.GenerateHeightMap(width, height, seed, 0.03f, 4, 0.5f);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = (x - cx) / maxDist;
                var dy = (y - cy) / maxDist;
                var dist = (float)Math.Sqrt(dx * dx + dy * dy);
                var falloff = 1f - Math.Clamp(dist, 0, 1);
                falloff = falloff * falloff * (3 - 2 * falloff);

                var noiseVal = heightMap[x, y];
                var elevation = falloff * noiseVal * 2f;

                var z = (sbyte)Math.Clamp(elevation * 50, -128, 127);
                map.Height[x, y] = z;

                map.Terrain[x, y] = ResolveTile(elevation, falloff, biomeStyle);
            }
        }

        return map;
    }

    private static ushort ResolveTile(float elevation, float falloff, BiomeStyle biome)
    {
        if (falloff < 0.02f)
            return 0x0001; // deep water
        if (falloff < 0.05f)
            return 0x0000; // shallow water
        if (falloff < 0.1f)
            return 0x00A8; // sand

        if (biome == BiomeStyle.Arctic)
        {
            if (elevation < 0.5f) return 0x01A0;
            if (elevation < 1.0f) return 0x0155;
            return 0x0205;
        }

        if (biome == BiomeStyle.Desert)
        {
            if (elevation < 0.5f) return 0x00A8;
            if (elevation < 1.0f) return 0x00A9;
            return 0x0220;
        }

        if (biome == BiomeStyle.Volcanic)
        {
            if (elevation < 0.5f) return 0x0044;
            if (elevation < 1.0f) return 0x01AD;
            return 0x01AE;
        }

        if (biome == BiomeStyle.Tropical)
        {
            if (elevation < 0.3f) return 0x00A8;
            if (elevation < 0.7f) return 0x000C;
            if (elevation < 1.0f) return 0x0057;
            return 0x01AE;
        }

        // Temperate / Mixed
        if (elevation < 0.3f) return 0x0003;
        if (elevation < 0.5f) return 0x0004;
        if (elevation < 0.8f) return 0x000C;
        if (elevation < 1.0f) return 0x0056;
        return 0x01AE;
    }
}
