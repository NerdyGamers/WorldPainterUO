using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

public interface IMapFileWriter
{
    void Write(string filePath, WorldMap map);
}
