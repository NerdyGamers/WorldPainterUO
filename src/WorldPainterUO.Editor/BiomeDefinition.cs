using System.Text.Json.Serialization;

namespace WorldPainterUO.Editor;

/// <summary>
/// Defines a named biome with weighted tile IDs and optional
/// neighbor-aware transition overrides.
/// </summary>
public sealed class BiomeDefinition
{
    /// <summary>Unique name (e.g. "Grass", "Forest").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Weighted tile entries used for painting this biome.</summary>
    public List<WeightedTileEntry> Tiles { get; set; } = [];

    /// <summary>
    /// Optional transition overrides keyed by "NeighborBiomeName".
    /// When the center tile's biome has a neighbor of the keyed biome,
    /// these tiles are used instead of the base <see cref="Tiles"/> list.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<WeightedTileEntry>>? NeighborTransitions { get; set; }

    /// <summary>
    /// Validates that total weight > 0 and every entry has a valid weight.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        Tiles.Count > 0 &&
        Tiles.Sum(t => t.Weight) > 0;

    /// <summary>
    /// Gets the transition tiles for a given neighbor biome name,
    /// or the base <see cref="Tiles"/> if no transition is defined.
    /// </summary>
    public List<WeightedTileEntry> GetTilesForNeighbor(string neighborBiome) =>
        NeighborTransitions is not null &&
        NeighborTransitions.TryGetValue(neighborBiome, out var overrides) &&
        overrides.Count > 0
            ? overrides
            : Tiles;
}
