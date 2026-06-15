using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Generators;

public interface IMapGenerator
{
    WorldMap Generate(MapDimensions dimensions, int seed, BiomeStyle biomeStyle);
}
