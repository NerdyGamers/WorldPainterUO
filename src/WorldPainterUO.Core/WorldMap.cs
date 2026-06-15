namespace WorldPainterUO.Core;

public sealed class WorldMap
{
    public WorldMap(MapDimensions dimensions, MapMetadata metadata)
    {
        Dimensions = dimensions;
        Metadata = metadata;
        Terrain = new TerrainLayer(dimensions);
        Height = new HeightLayer(dimensions);
    }

    public MapDimensions Dimensions { get; }
    public MapMetadata Metadata { get; }
    public TerrainLayer Terrain { get; }
    public HeightLayer Height { get; }

    public static WorldMap Create(int width, int height, string facet, SourceFileType sourceType) =>
        new(new MapDimensions(width, height), new MapMetadata(facet, sourceType));
}
