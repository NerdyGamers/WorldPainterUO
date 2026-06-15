using WorldPainterUO.Editor;

namespace WorldPainterUO.Tests.Editor;

public sealed class TerrainRulesTests
{
    // ── WeightedTileEntry ────────────────────────────────────────────────

    [Fact]
    public void WeightedTileEntry_DefaultWeight_IsOne()
    {
        var entry = new WeightedTileEntry(0x0003);
        Assert.Equal(1.0f, entry.Weight);
    }

    [Fact]
    public void WeightedTileEntry_ExplicitWeight_Stored()
    {
        var entry = new WeightedTileEntry(0x0004, 5.0f);
        Assert.Equal(0x0004, entry.TileId);
        Assert.Equal(5.0f, entry.Weight);
    }

    // ── BiomeDefinition ──────────────────────────────────────────────────

    [Fact]
    public void BiomeDefinition_IsValid_WhenNameAndTilesExist()
    {
        var biome = new BiomeDefinition
        {
            Name = "Grass",
            Tiles = [new(0x0003, 1f)],
        };
        Assert.True(biome.IsValid);
    }

    [Fact]
    public void BiomeDefinition_IsInvalid_WithoutTiles()
    {
        var biome = new BiomeDefinition { Name = "Empty" };
        Assert.False(biome.IsValid);
    }

    [Fact]
    public void BiomeDefinition_IsInvalid_ZeroWeight()
    {
        var biome = new BiomeDefinition
        {
            Name = "Test",
            Tiles = [new(0x0003, 0f)],
        };
        Assert.False(biome.IsValid);
    }

    [Fact]
    public void BiomeDefinition_GetTilesForNeighbor_ReturnsTransition_WhenDefined()
    {
        var biome = new BiomeDefinition
        {
            Name = "Grass",
            Tiles = [new(0x0003, 1f)],
            NeighborTransitions = new()
            {
                ["Ocean"] = [new(0x0008, 1f)],
            },
        };

        var transition = biome.GetTilesForNeighbor("Ocean");
        Assert.Single(transition);
        Assert.Equal(0x0008, transition[0].TileId);
    }

    [Fact]
    public void BiomeDefinition_GetTilesForNeighbor_FallsBackToBase_WhenNoTransition()
    {
        var biome = new BiomeDefinition
        {
            Name = "Grass",
            Tiles = [new(0x0003, 1f)],
        };

        var tiles = biome.GetTilesForNeighbor("Ocean");
        Assert.Equal(0x0003, tiles[0].TileId);
    }

    // ── TerrainPalette ───────────────────────────────────────────────────

    [Fact]
    public void TerrainPalette_Default_HasAllBiomes()
    {
        var palette = TerrainPalette.CreateDefault();
        Assert.Equal(11, palette.Biomes.Count);
    }

    [Fact]
    public void TerrainPalette_Default_OceanHasWaterTiles()
    {
        var palette = TerrainPalette.CreateDefault();
        var ocean = palette.GetBiome("Ocean");
        Assert.NotNull(ocean);
        Assert.Contains(ocean.Tiles, t => t.TileId == 0x0000);
    }

    [Fact]
    public void TerrainPalette_GetBiome_IsCaseSensitive()
    {
        var palette = TerrainPalette.CreateDefault();
        Assert.Null(palette.GetBiome("grass"));
    }

    [Fact]
    public void TerrainPalette_BiomeNames_ReturnsAll()
    {
        var palette = TerrainPalette.CreateDefault();
        var names = palette.BiomeNames.ToList();
        Assert.Contains("Grass", names);
        Assert.Contains("Forest", names);
        Assert.Contains("Ocean", names);
    }

    [Fact]
    public void TerrainPalette_SaveAndLoad_RoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = TerrainPalette.CreateDefault();
            original.Save(path);
            var loaded = TerrainPalette.Load(path);

            Assert.Equal(original.Biomes.Count, loaded.Biomes.Count);

            foreach (var kvp in original.Biomes)
            {
                Assert.True(loaded.Biomes.ContainsKey(kvp.Key));
                Assert.Equal(kvp.Value.Tiles.Count, loaded.Biomes[kvp.Key].Tiles.Count);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TerrainPalette_Load_NonExistentFile_ReturnsEmpty()
    {
        var palette = TerrainPalette.Load("nonexistent_file.json");
        Assert.NotNull(palette);
        Assert.Empty(palette.Biomes);
    }

    // ── TerrainRulesEngine - Weighted Selection ──────────────────────────

    [Fact]
    public void TerrainRulesEngine_ResolveTileId_ReturnsBiomeTile()
    {
        var palette = TerrainPalette.CreateDefault();
        var engine = new TerrainRulesEngine(palette, seed: 42);

        var id = engine.ResolveTileId("Grass");
        Assert.InRange(id, 0x0003, 0x0006);
    }

    [Fact]
    public void TerrainRulesEngine_ResolveTileId_ReturnsZero_ForUnknownBiome()
    {
        var palette = TerrainPalette.CreateDefault();
        var engine = new TerrainRulesEngine(palette);

        Assert.Equal(0, engine.ResolveTileId("Nonexistent"));
    }

    [Fact]
    public void TerrainRulesEngine_ResolveTileId_DeterministicSeed()
    {
        var palette = TerrainPalette.CreateDefault();

        var engine1 = new TerrainRulesEngine(palette, seed: 100);
        var engine2 = new TerrainRulesEngine(palette, seed: 100);

        var results1 = Enumerable.Range(0, 50).Select(_ => engine1.ResolveTileId("Grass")).ToList();
        var results2 = Enumerable.Range(0, 50).Select(_ => engine2.ResolveTileId("Grass")).ToList();

        Assert.Equal(results1, results2);
    }

    [Fact]
    public void TerrainRulesEngine_DifferentSeeds_DifferentResults()
    {
        var palette = TerrainPalette.CreateDefault();

        var engine1 = new TerrainRulesEngine(palette, seed: 100);
        var engine2 = new TerrainRulesEngine(palette, seed: 999);

        var results1 = Enumerable.Range(0, 20).Select(_ => engine1.ResolveTileId("Grass")).ToList();
        var results2 = Enumerable.Range(0, 20).Select(_ => engine2.ResolveTileId("Grass")).ToList();

        // Very unlikely to be identical with different seeds
        Assert.NotEqual(results1, results2);
    }

    [Fact]
    public void WeightedSelection_ProducesCorrectDistribution()
    {
        // Single tile with 100% weight should always be selected
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Test"] = new()
                {
                    Name = "Test",
                    Tiles =
                    [
                        new(0x0001, 100f),
                    ],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);
        var distribution = engine.SampleDistribution("Test", 1000);

        Assert.Single(distribution);
        Assert.Equal(1000, distribution[0x0001]);
    }

    [Fact]
    public void WeightedSelection_EvenDistribution()
    {
        // Two tiles with equal weight should each appear roughly 50%
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Test"] = new()
                {
                    Name = "Test",
                    Tiles =
                    [
                        new(0x0001, 1f),
                        new(0x0002, 1f),
                    ],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 12345);
        var distribution = engine.SampleDistribution("Test", 10000);

        var count1 = distribution.GetValueOrDefault((ushort)0x0001);
        var count2 = distribution.GetValueOrDefault((ushort)0x0002);

        // Should be in roughly equal proportion with some tolerance
        var ratio = (double)count1 / count2;
        Assert.InRange(ratio, 0.8, 1.2);
    }

    [Fact]
    public void WeightedSelection_UnevenDistribution()
    {
        // Two tiles with weights 3:1
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Test"] = new()
                {
                    Name = "Test",
                    Tiles =
                    [
                        new(0x0001, 3f),
                        new(0x0002, 1f),
                    ],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);
        var distribution = engine.SampleDistribution("Test", 10000);

        var count1 = distribution.GetValueOrDefault((ushort)0x0001);
        var count2 = distribution.GetValueOrDefault((ushort)0x0002);
        var ratio = (double)count1 / count2;

        // Should be close to 3:1
        Assert.InRange(ratio, 2.0, 4.0);
    }

    // ── TerrainRulesEngine - Neighbor Transitions ────────────────────────

    [Fact]
    public void ResolveTileIdWithNeighbors_UsesTransition_WhenDefined()
    {
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Grass"] = new()
                {
                    Name = "Grass",
                    Tiles = [new(0x0003, 1f)],
                    NeighborTransitions = new()
                    {
                        ["Ocean"] = [new(0x0008, 1f)],
                    },
                },
                ["Ocean"] = new()
                {
                    Name = "Ocean",
                    Tiles = [new(0x0000, 1f)],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);

        var id = engine.ResolveTileIdWithNeighbors("Grass", "Ocean", null, null, null);
        Assert.Equal(0x0008, id); // Transition tile, not base grass
    }

    [Fact]
    public void ResolveTileIdWithNeighbors_UsesBase_WhenNoTransition()
    {
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Grass"] = new()
                {
                    Name = "Grass",
                    Tiles = [new(0x0003, 1f)],
                },
                ["Swamp"] = new()
                {
                    Name = "Swamp",
                    Tiles = [new(0x00D8, 1f)],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);

        var id = engine.ResolveTileIdWithNeighbors("Grass", "Swamp", null, null, null);
        Assert.Equal(0x0003, id); // No transition defined, fallback to base
    }

    [Fact]
    public void ResolveTileIdWithNeighbors_ReturnsBase_WhenNeighborIsSame()
    {
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Grass"] = new()
                {
                    Name = "Grass",
                    Tiles = [new(0x0003, 1f)],
                },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);

        var id = engine.ResolveTileIdWithNeighbors("Grass", "Grass", "Grass", "Grass", "Grass");
        Assert.Equal(0x0003, id); // Same biome, no transition
    }

    [Fact]
    public void ResolveTileIdWithNeighbors_ReturnsZero_ForUnknownBiome()
    {
        var palette = new TerrainPalette();
        var engine = new TerrainRulesEngine(palette);

        var id = engine.ResolveTileIdWithNeighbors("Unknown", null, null, null, null);
        Assert.Equal(0, id);
    }

    [Fact]
    public void ResolveTileIdWithNeighbors_FirstMatchingDirectionWins()
    {
        var palette = new TerrainPalette
        {
            Biomes = new()
            {
                ["Grass"] = new()
                {
                    Name = "Grass",
                    Tiles = [new(0x0003, 1f)],
                    NeighborTransitions = new()
                    {
                        ["Ocean"] = [new(0x0008, 1f)],
                        ["Desert"] = [new(0x0015, 1f)],
                    },
                },
                ["Ocean"] = new() { Name = "Ocean", Tiles = [new(0x0000, 1f)] },
                ["Desert"] = new() { Name = "Desert", Tiles = [new(0x0012, 1f)] },
            },
        };

        var engine = new TerrainRulesEngine(palette, seed: 42);

        // Both Ocean and Desert are neighbors; north should be checked first
        var id = engine.ResolveTileIdWithNeighbors("Grass", "Ocean", "Desert", null, null);
        Assert.Equal(0x0008, id); // Ocean (north) transition wins
    }

    [Fact]
    public void ResolveTileIdWithNeighbors_DeterministicSeed()
    {
        var palette = TerrainPalette.CreateDefault();
        var engine1 = new TerrainRulesEngine(palette, seed: 100);
        var engine2 = new TerrainRulesEngine(palette, seed: 100);

        var results1 = Enumerable.Range(0, 30)
            .Select(_ => engine1.ResolveTileIdWithNeighbors("Grass", null, null, "Ocean", null))
            .ToList();
        var results2 = Enumerable.Range(0, 30)
            .Select(_ => engine2.ResolveTileIdWithNeighbors("Grass", null, null, "Ocean", null))
            .ToList();

        Assert.Equal(results1, results2);
    }

    // ── TileClassifier ───────────────────────────────────────────────────

    [Fact]
    public void TileClassifier_Classify_KnownTileIds()
    {
        Assert.Equal("Ocean", TileClassifier.Classify(0x0000));
        Assert.Equal("Grass", TileClassifier.Classify(0x0003));
        Assert.Equal("Forest", TileClassifier.Classify(0x000C));
        Assert.Equal("Desert", TileClassifier.Classify(0x0012));
        Assert.Equal("Volcanic", TileClassifier.Classify(0x001E));
        Assert.Equal("Mountain", TileClassifier.Classify(0x0035));
        Assert.Equal("Rock", TileClassifier.Classify(0x003C));
        Assert.Equal("Road", TileClassifier.Classify(0x0071));
    }

    [Fact]
    public void TileClassifier_Classify_UnknownTile_DefaultsToGrass()
    {
        // Tile 0x00FF is outside defined ranges, should default to Grass
        Assert.Equal("Grass", TileClassifier.Classify(0x00FF));
    }

    [Fact]
    public void TileClassifier_BuildBiomeMap_WithoutFiles_ReturnsValidMap()
    {
        var map = TileClassifier.BuildBiomeMap(null, null);
        Assert.Equal(0x4000, map.Count);
        Assert.Equal("Ocean", map[0x0000]);
        Assert.Equal("Grass", map[0x0003]);
    }

    [Fact]
    public void TileClassifier_ReadTiledataMul_NonExistentFile_ReturnsEmpty()
    {
        var result = TileClassifier.ReadTiledataMul("nonexistent.mul");
        Assert.Empty(result);
    }

    [Fact]
    public void TileClassifier_ReadRadarColMul_NonExistentFile_ReturnsEmpty()
    {
        var result = TileClassifier.ReadRadarColMul("nonexistent.mul");
        Assert.Empty(result);
    }

    [Fact]
    public void TileClassifier_BiomeNames_ReturnsExpected()
    {
        var names = TileClassifier.BiomeNames;
        Assert.Contains("Ocean", names);
        Assert.Contains("Grass", names);
        Assert.Contains("Forest", names);
        Assert.Contains("Swamp", names);
        Assert.Contains("Snow", names);
        Assert.Contains("Desert", names);
        Assert.Contains("Mountain", names);
        Assert.Contains("Volcanic", names);
        Assert.Contains("Marsh", names);
        Assert.Contains("Road", names);
        Assert.Contains("Rock", names);
    }
}
