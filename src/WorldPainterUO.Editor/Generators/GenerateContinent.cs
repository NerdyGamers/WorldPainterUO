using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Generators;

public sealed class GenerateContinent : IMapGenerator
{
    public WorldMap Generate(MapDimensions dimensions, int seed, BiomeStyle biomeStyle)
    {
        var map = new WorldMap(dimensions, new MapMetadata("Generated", SourceFileType.Mul));
        var width = dimensions.Width;
        var height = dimensions.Height;

        var heightMap = NoiseHelper.GenerateHeightMap(width, height, seed, 0.015f, 5, 0.55f);
        var detailMap = NoiseHelper.GenerateHeightMap(width, height, seed + 100, 0.04f, 3, 0.4f);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var elevation = heightMap[x, y] * 0.7f + detailMap[x, y] * 0.3f;

                // Sharpen: more land than ocean
                elevation = (elevation - 0.35f) * 1.5f;
                elevation = Math.Clamp(elevation, 0, 1);

                var z = (sbyte)Math.Clamp(elevation * 60, -128, 127);
                map.Height[x, y] = z;

                map.Terrain[x, y] = ResolveContinentTile(elevation, biomeStyle);
            }
        }

        return map;
    }

    private static ushort ResolveContinentTile(float elevation, BiomeStyle biome)
    {
        if (elevation < 0.05f) return 0x0001;
        if (elevation < 0.12f) return 0x0000;
        if (elevation < 0.18f) return 0x00A8;

        if (biome == BiomeStyle.Arctic)
        {
            if (elevation < 0.35f) return 0x01A0;
            if (elevation < 0.55f) return 0x0155;
            if (elevation < 0.75f) return 0x01A5;
            return 0x0205;
        }

        if (biome == BiomeStyle.Desert)
        {
            if (elevation < 0.35f) return 0x00A8;
            if (elevation < 0.55f) return 0x00A9;
            if (elevation < 0.75f) return 0x0220;
            return 0x01AE;
        }

        if (biome == BiomeStyle.Volcanic)
        {
            if (elevation < 0.35f) return 0x0044;
            if (elevation < 0.55f) return 0x01AD;
            if (elevation < 0.75f) return 0x01AE;
            return 0x01AF;
        }

        if (biome == BiomeStyle.Tropical)
        {
            if (elevation < 0.35f) return 0x00A8;
            if (elevation < 0.55f) return 0x000C;
            if (elevation < 0.75f) return 0x0057;
            return 0x01AE;
        }

        // Temperate / Mixed
        if (elevation < 0.25f) return 0x0003;
        if (elevation < 0.40f) return 0x0004;
        if (elevation < 0.55f) return 0x000C;
        if (elevation < 0.70f) return 0x0056;
        if (elevation < 0.85f) return 0x01A5;
        return 0x01AE;
    }
}
