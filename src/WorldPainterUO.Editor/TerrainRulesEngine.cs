namespace WorldPainterUO.Editor;

/// <summary>
/// Translates biome paint intent into weighted UO tile IDs.
/// Supports weighted random selection and neighbor-aware transitions.
/// This is an optional paint-assist layer; raw tile IDs are stored in TerrainLayer.
/// </summary>
public sealed class TerrainRulesEngine
{
    private readonly TerrainPalette _palette;
    private Random _rng;

    /// <summary>Creates the engine with a given palette.</summary>
    public TerrainRulesEngine(TerrainPalette palette, int? seed = null)
    {
        _palette = palette;
        Seed = seed ?? Environment.TickCount;
        _rng = new Random(Seed);
    }

    /// <summary>Current random seed. Changing this resets the RNG.</summary>
    public int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            _rng = new Random(value);
        }
    }
    private int _seed;

    /// <summary>The underlying palette.</summary>
    public TerrainPalette Palette => _palette;

    /// <summary>
    /// Resolves a weighted tile ID for the given biome.
    /// Returns 0 if the biome is not found or has no tiles.
    /// </summary>
    public ushort ResolveTileId(string biomeName)
    {
        var biome = _palette.GetBiome(biomeName);
        if (biome is null || biome.Tiles.Count == 0)
            return 0;

        return PickWeighted(biome.Tiles);
    }

    /// <summary>
    /// Resolves a tile ID for the center biome, taking neighbor biomes
    /// into account for transition edges. If a transition override exists
    /// for any neighbor, it is used in preference to the base biome tiles.
    /// The first matching neighbor direction wins.
    /// </summary>
    public ushort ResolveTileIdWithNeighbors(
        string biome,
        string? north, string? south,
        string? east, string? west)
    {
        var biomeDef = _palette.GetBiome(biome);
        if (biomeDef is null)
            return 0;

        // Check each neighbor direction for transition overrides
        var neighbors = new[] { north, south, east, west };
        foreach (var neighbor in neighbors)
        {
            if (string.IsNullOrWhiteSpace(neighbor) || neighbor == biome)
                continue;

            var transitionTiles = biomeDef.GetTilesForNeighbor(neighbor);
            if (transitionTiles.Count > 0 && transitionTiles.Sum(t => t.Weight) > 0)
                return PickWeighted(transitionTiles);
        }

        // No transition applies — use base tiles
        if (biomeDef.Tiles.Count == 0)
            return 0;

        return PickWeighted(biomeDef.Tiles);
    }

    /// <summary>
    /// Counts how many times each tile ID is selected over N rolls
    /// for a given biome. Used for testing distribution correctness.
    /// </summary>
    public Dictionary<ushort, int> SampleDistribution(string biomeName, int trials)
    {
        var counts = new Dictionary<ushort, int>();

        for (var i = 0; i < trials; i++)
        {
            var id = ResolveTileId(biomeName);
            counts[id] = counts.GetValueOrDefault(id) + 1;
        }

        return counts;
    }

    private ushort PickWeighted(List<WeightedTileEntry> tiles)
    {
        var totalWeight = tiles.Sum(t => t.Weight);
        if (totalWeight <= 0)
            return tiles.Count > 0 ? tiles[0].TileId : (ushort)0;

        var roll = _rng.NextDouble() * totalWeight;

        foreach (var tile in tiles)
        {
            roll -= tile.Weight;
            if (roll <= 0)
                return tile.TileId;
        }

        return tiles[^1].TileId;
    }
}
