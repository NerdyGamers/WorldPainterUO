using System.Text.Json;

namespace WorldPainterUO.Editor;

/// <summary>
/// Holds a collection of biome definitions and provides default
/// biome groupings based on known UO tile ID ranges.
/// </summary>
public sealed class TerrainPalette
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Dictionary<string, BiomeDefinition> Biomes { get; set; } = [];

    /// <summary>
    /// Creates the default palette of biome definitions based on
    /// well-known UO land tile ID ranges.
    /// </summary>
    public static TerrainPalette CreateDefault()
    {
        return new TerrainPalette
        {
            Biomes = new Dictionary<string, BiomeDefinition>
            {
                ["Ocean"] = new()
                {
                    Name = "Ocean",
                    Tiles =
                    [
                        new(0x0000, 40f),   // water dark
                        new(0x0001, 30f),   // water med
                        new(0x0002, 20f),   // water light
                        new(0x0003, 10f),   // water shallow
                        new(0x00A4, 5f),    // dark water
                        new(0x00A5, 5f),    // med-dark water
                        new(0x00A6, 5f),    // med water
                        new(0x00A7, 5f),    // med-light water
                        new(0x00A8, 5f),    // light water
                    ],
                },
                ["Grass"] = new()
                {
                    Name = "Grass",
                    Tiles =
                    [
                        new(0x0003, 40f),
                        new(0x0004, 30f),
                        new(0x0005, 20f),
                        new(0x0006, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Ocean"] = [new(0x0008, 50f), new(0x0009, 50f)],   // beach/sand edge
                        ["Swamp"] = [new(0x00E8, 50f), new(0x00E9, 50f)],   // grassy swamp edge
                        ["Desert"] = [new(0x0010, 50f), new(0x0011, 50f)],  // sand transition
                    },
                },
                ["Forest"] = new()
                {
                    Name = "Forest",
                    Tiles =
                    [
                        new(0x000C, 30f),   // grass + tree
                        new(0x000D, 25f),
                        new(0x000E, 20f),
                        new(0x000F, 15f),
                        new(0x0010, 10f),   // darker forest floor
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x000C, 40f), new(0x000D, 40f), new(0x0003, 20f)],
                        ["Ocean"] = [new(0x0008, 50f), new(0x0009, 50f)],
                    },
                },
                ["Swamp"] = new()
                {
                    Name = "Swamp",
                    Tiles =
                    [
                        new(0x00D8, 30f),
                        new(0x00D9, 25f),
                        new(0x00DA, 20f),
                        new(0x00DB, 15f),
                        new(0x00DC, 10f),
                    ],
                },
                ["Snow"] = new()
                {
                    Name = "Snow",
                    Tiles =
                    [
                        new(0x01AC, 40f),
                        new(0x01AD, 30f),
                        new(0x01AE, 20f),
                        new(0x01AF, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x01AC, 50f), new(0x0003, 50f)],  // snowy grass edge
                        ["Mountain"] = [new(0x01AC, 60f), new(0x01AD, 40f)],
                    },
                },
                ["Desert"] = new()
                {
                    Name = "Desert",
                    Tiles =
                    [
                        new(0x0012, 40f),
                        new(0x0013, 30f),
                        new(0x0014, 20f),
                        new(0x0015, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x0012, 40f), new(0x0013, 40f), new(0x0003, 20f)],
                        ["Ocean"] = [new(0x0015, 50f), new(0x0012, 50f)],
                    },
                },
                ["Mountain"] = new()
                {
                    Name = "Mountain",
                    Tiles =
                    [
                        new(0x0035, 30f),
                        new(0x0036, 25f),
                        new(0x0037, 20f),
                        new(0x0038, 15f),
                        new(0x0039, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x0035, 40f), new(0x0003, 30f), new(0x0004, 30f)],
                        ["Snow"] = [new(0x01AC, 40f), new(0x0035, 30f), new(0x0036, 30f)],
                    },
                },
                ["Volcanic"] = new()
                {
                    Name = "Volcanic",
                    Tiles =
                    [
                        new(0x001E, 40f),   // lava
                        new(0x001F, 30f),
                        new(0x0020, 20f),
                        new(0x0021, 10f),
                        new(0x0022, 5f),    // dark rock
                        new(0x0023, 5f),    // scorched
                    ],
                    NeighborTransitions = new()
                    {
                        ["Ocean"] = [new(0x001E, 30f), new(0x0022, 30f), new(0x0000, 40f)],
                        ["Grass"] = [new(0x0022, 50f), new(0x0023, 30f), new(0x0003, 20f)],
                    },
                },
                ["Marsh"] = new()
                {
                    Name = "Marsh",
                    Tiles =
                    [
                        new(0x00E8, 30f),
                        new(0x00E9, 25f),
                        new(0x00EA, 20f),
                        new(0x00EB, 15f),
                        new(0x00EC, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Ocean"] = [new(0x00E8, 40f), new(0x0001, 30f), new(0x0002, 30f)],
                        ["Grass"] = [new(0x00E8, 50f), new(0x0003, 30f), new(0x0004, 20f)],
                    },
                },
                ["Road"] = new()
                {
                    Name = "Road",
                    Tiles =
                    [
                        new(0x0071, 25f),   // cobblestone
                        new(0x0072, 20f),
                        new(0x0073, 20f),
                        new(0x0074, 15f),
                        new(0x0075, 10f),
                        new(0x0076, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x0071, 40f), new(0x0072, 40f), new(0x0003, 20f)],
                        ["Desert"] = [new(0x0071, 30f), new(0x0012, 40f), new(0x0013, 30f)],
                    },
                },
                ["Rock"] = new()
                {
                    Name = "Rock",
                    Tiles =
                    [
                        new(0x003C, 30f),
                        new(0x003D, 25f),
                        new(0x003E, 20f),
                        new(0x003F, 15f),
                        new(0x0040, 10f),
                    ],
                    NeighborTransitions = new()
                    {
                        ["Grass"] = [new(0x003C, 40f), new(0x0003, 30f), new(0x0004, 30f)],
                        ["Mountain"] = [new(0x003C, 30f), new(0x0035, 40f), new(0x0036, 30f)],
                    },
                },
            },
        };
    }

    /// <summary>Gets a biome by name, case-insensitive.</summary>
    public BiomeDefinition? GetBiome(string name) =>
        Biomes.TryGetValue(name, out var biome) ? biome : null;

    /// <summary>Saves the palette to a JSON file.</summary>
    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>Loads a palette from a JSON file. Returns empty palette if file not found.</summary>
    public static TerrainPalette Load(string path)
    {
        if (!File.Exists(path))
            return new();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TerrainPalette>(json, JsonOptions) ?? new();
    }

    /// <summary>Returns all biome names in the palette.</summary>
    public IEnumerable<string> BiomeNames => Biomes.Keys;
}
