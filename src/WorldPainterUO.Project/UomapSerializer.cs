using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.Project;

public static class UomapSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Save(string filePath, WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var logger = Log.For("UomapSerializer");
        logger.LogInformation("Saving project to {Path}", filePath);

        var project = new UomapProject
        {
            Width = map.Dimensions.Width,
            Height = map.Dimensions.Height,
            ChunkSize = map.Dimensions.ChunkSize,
            Facet = map.Metadata.Facet,
            SourceFileType = map.Metadata.SourceFileType.ToString(),
            Chunks = []
        };

        for (var cy = 0; cy < map.Dimensions.ChunksY; cy++)
        {
            for (var cx = 0; cx < map.Dimensions.ChunksX; cx++)
            {
                var terrainChunk = map.Terrain.GetChunk(cx, cy);
                var heightChunk = map.Height.GetChunk(cx, cy);

                project.Chunks.Add(new UomapChunkData
                {
                    X = cx,
                    Y = cy,
                    Terrain = SerializeTerrainChunk(terrainChunk),
                    Height = SerializeHeightChunk(heightChunk)
                });
            }
        }

        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static WorldMap Load(string filePath)
    {
        var logger = Log.For("UomapSerializer");
        logger.LogInformation("Loading project from {Path}", filePath);
        var json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<UomapProject>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize project file.");

        var dims = new MapDimensions(project.Width, project.Height)
        {
            ChunkSize = project.ChunkSize
        };

        var sourceType = Enum.TryParse<SourceFileType>(project.SourceFileType, ignoreCase: true, out var parsed)
            ? parsed
            : SourceFileType.Mul;

        var metadata = new MapMetadata(project.Facet, sourceType, project.Version);
        var map = new WorldMap(dims, metadata);

        foreach (var chunkData in project.Chunks)
        {
            var terrainChunk = map.Terrain.GetChunk(chunkData.X, chunkData.Y);
            var heightChunk = map.Height.GetChunk(chunkData.X, chunkData.Y);

            DeserializeTerrainChunk(chunkData.Terrain, terrainChunk);
            DeserializeHeightChunk(chunkData.Height, heightChunk);
        }

        map.Terrain.MarkAllClean();
        map.Height.MarkAllClean();

        return map;
    }

    private static string SerializeTerrainChunk(MapChunk<ushort> chunk)
    {
        var bytes = new byte[MapChunk<ushort>.TileCount * sizeof(ushort)];
        var data = chunk.Data;

        for (var i = 0; i < data.Length; i++)
        {
            bytes[i * 2] = (byte)(data[i] & 0xFF);
            bytes[i * 2 + 1] = (byte)((data[i] >> 8) & 0xFF);
        }

        return Convert.ToBase64String(bytes);
    }

    private static string SerializeHeightChunk(MapChunk<sbyte> chunk)
    {
        var bytes = new byte[MapChunk<sbyte>.TileCount];
        var data = chunk.Data;

        for (var i = 0; i < data.Length; i++)
        {
            bytes[i] = unchecked((byte)data[i]);
        }

        return Convert.ToBase64String(bytes);
    }

    private static void DeserializeTerrainChunk(string base64, MapChunk<ushort> chunk)
    {
        var bytes = Convert.FromBase64String(base64);
        for (var ly = 0; ly < MapChunk<ushort>.Size; ly++)
        {
            for (var lx = 0; lx < MapChunk<ushort>.Size; lx++)
            {
                var i = ly * MapChunk<ushort>.Size + lx;
                var value = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                chunk[lx, ly] = value;
            }
        }
    }

    private static void DeserializeHeightChunk(string base64, MapChunk<sbyte> chunk)
    {
        var bytes = Convert.FromBase64String(base64);
        for (var ly = 0; ly < MapChunk<sbyte>.Size; ly++)
        {
            for (var lx = 0; lx < MapChunk<sbyte>.Size; lx++)
            {
                var i = ly * MapChunk<sbyte>.Size + lx;
                chunk[lx, ly] = unchecked((sbyte)bytes[i]);
            }
        }
    }
}
