using System.Text.Json.Serialization;

namespace WorldPainterUO.Editor;

/// <summary>
/// A tile ID with an optional weight for weighted random selection.
/// </summary>
public readonly record struct WeightedTileEntry(ushort TileId, float Weight = 1.0f)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Weight { get; init; } = Weight;
}
