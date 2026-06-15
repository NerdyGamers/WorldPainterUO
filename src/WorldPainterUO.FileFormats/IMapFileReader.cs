using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

public interface IMapFileReader
{
    WorldMap Read(string filePath, MapDimensions dimensions);
}
