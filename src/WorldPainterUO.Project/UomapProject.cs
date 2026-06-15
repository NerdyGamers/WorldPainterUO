using System.Text.Json.Serialization;

namespace WorldPainterUO.Project;

public sealed class UomapProject
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("chunkSize")]
    public int ChunkSize { get; set; } = 64;

    [JsonPropertyName("facet")]
    public string Facet { get; set; } = "Unknown";

    [JsonPropertyName("sourceFileType")]
    public string SourceFileType { get; set; } = "Mul";

    [JsonPropertyName("chunks")]
    public List<UomapChunkData> Chunks { get; set; } = [];
}

public sealed class UomapChunkData
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("terrain")]
    public string Terrain { get; set; } = "";

    [JsonPropertyName("height")]
    public string Height { get; set; } = "";
}
