using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldPainterUO.Editor.Stamp;

public sealed class StampTemplate
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("tiles")]
    public ushort[] Tiles { get; set; } = [];

    [JsonPropertyName("heights")]
    public sbyte[] Heights { get; set; } = [];

    [JsonIgnore]
    public int TileCount => Width * Height;

    public bool IsValid => Width > 0 && Height > 0 && Tiles.Length == TileCount && Heights.Length == TileCount;

    public ushort GetTile(int x, int y) => Tiles[y * Width + x];
    public sbyte GetHeight(int x, int y) => Heights[y * Width + x];

    public static StampTemplate? Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<StampTemplate>(json);
    }

    public void Save(string filePath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static StampTemplate Create(string name, int width, int height, ushort[] tiles, sbyte[] heights)
    {
        return new StampTemplate
        {
            Name = name,
            Width = width,
            Height = height,
            Tiles = tiles,
            Heights = heights
        };
    }
}
