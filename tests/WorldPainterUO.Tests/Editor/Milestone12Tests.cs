using WorldPainterUO.Core;
using WorldPainterUO.Editor;
using WorldPainterUO.Editor.Generators;
using WorldPainterUO.Editor.Selection;
using WorldPainterUO.Editor.Stamp;
using WorldPainterUO.Editor.Tools;

namespace WorldPainterUO.Tests.Editor;

public sealed class Milestone12Tests
{
    private const int TestWidth = 128;
    private const int TestHeight = 128;

    // ──────────────────────────────────────────────
    // Generator tests
    // ──────────────────────────────────────────────

    [Fact]
    public void GenerateIsland_creates_valid_map()
    {
        var dims = new MapDimensions(TestWidth, TestHeight);
        var gen = new GenerateIsland();
        var map = gen.Generate(dims, 42, BiomeStyle.Temperate);

        Assert.NotNull(map);
        Assert.Equal(TestWidth, map.Dimensions.Width);
        Assert.Equal(TestHeight, map.Dimensions.Height);
        Assert.All(Enumerable.Range(0, TestWidth), x =>
            Assert.All(Enumerable.Range(0, TestHeight), y =>
            {
                Assert.InRange(map.Terrain[x, y], (ushort)0, (ushort)0x3FFF);
                Assert.InRange(map.Height[x, y], (sbyte)-128, (sbyte)127);
            }));
    }

    [Fact]
    public void GenerateIsland_all_biome_styles_produce_valid_output()
    {
        var dims = new MapDimensions(64, 64);
        var gen = new GenerateIsland();

        foreach (BiomeStyle style in Enum.GetValues<BiomeStyle>())
        {
            var map = gen.Generate(dims, 42, style);
            Assert.All(Enumerable.Range(0, 64), x =>
                Assert.All(Enumerable.Range(0, 64), y =>
                    Assert.InRange(map.Terrain[x, y], (ushort)0, (ushort)0x3FFF)));
        }
    }

    [Fact]
    public void GenerateArchipelago_creates_valid_map()
    {
        var dims = new MapDimensions(TestWidth, TestHeight);
        var gen = new GenerateArchipelago();
        var map = gen.Generate(dims, 42, BiomeStyle.Temperate);

        Assert.NotNull(map);
        Assert.All(Enumerable.Range(0, TestWidth), x =>
            Assert.All(Enumerable.Range(0, TestHeight), y =>
                Assert.InRange(map.Terrain[x, y], (ushort)0, (ushort)0x3FFF)));
    }

    [Fact]
    public void GenerateContinent_creates_valid_map()
    {
        var dims = new MapDimensions(TestWidth, TestHeight);
        var gen = new GenerateContinent();
        var map = gen.Generate(dims, 42, BiomeStyle.Temperate);

        Assert.NotNull(map);
        Assert.All(Enumerable.Range(0, TestWidth), x =>
            Assert.All(Enumerable.Range(0, TestHeight), y =>
                Assert.InRange(map.Terrain[x, y], (ushort)0, (ushort)0x3FFF)));
    }

    [Fact]
    public void GenerateWorld_creates_valid_map()
    {
        var dims = new MapDimensions(TestWidth, TestHeight);
        var gen = new GenerateWorld();
        var map = gen.Generate(dims, 42, BiomeStyle.Temperate);

        Assert.NotNull(map);
        Assert.All(Enumerable.Range(0, TestWidth), x =>
            Assert.All(Enumerable.Range(0, TestHeight), y =>
                Assert.InRange(map.Terrain[x, y], (ushort)0, (ushort)0x3FFF)));
    }

    [Fact]
    public void Same_seed_produces_identical_output()
    {
        var dims = new MapDimensions(64, 64);
        var gen = new GenerateIsland();

        var map1 = gen.Generate(dims, 12345, BiomeStyle.Temperate);
        var map2 = gen.Generate(dims, 12345, BiomeStyle.Temperate);

        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                Assert.Equal(map1.Terrain[x, y], map2.Terrain[x, y]);
                Assert.Equal(map1.Height[x, y], map2.Height[x, y]);
            }
        }
    }

    [Fact]
    public void Different_seed_produces_different_output()
    {
        var dims = new MapDimensions(64, 64);
        var gen = new GenerateIsland();

        var map1 = gen.Generate(dims, 100, BiomeStyle.Temperate);
        var map2 = gen.Generate(dims, 200, BiomeStyle.Temperate);

        var different = false;
        for (var y = 0; y < 64 && !different; y++)
        {
            for (var x = 0; x < 64 && !different; x++)
            {
                if (map1.Terrain[x, y] != map2.Terrain[x, y] || map1.Height[x, y] != map2.Height[x, y])
                    different = true;
            }
        }

        Assert.True(different, "Different seeds should produce different terrain");
    }

    [Fact]
    public void Generators_support_alternative_biome_styles()
    {
        var dims = new MapDimensions(64, 64);
        var temperate = new GenerateIsland().Generate(dims, 42, BiomeStyle.Temperate);
        var arctic = new GenerateIsland().Generate(dims, 42, BiomeStyle.Arctic);
        var desert = new GenerateIsland().Generate(dims, 42, BiomeStyle.Desert);

        // Different biomes should produce different tile distributions
        var temperateSums = SumTerrain(temperate);
        var arcticSums = SumTerrain(arctic);
        var desertSums = SumTerrain(desert);

        Assert.NotEqual(temperateSums, arcticSums);
        Assert.NotEqual(temperateSums, desertSums);
    }

    // ──────────────────────────────────────────────
    // Selection tests
    // ──────────────────────────────────────────────

    [Fact]
    public void RectangleSelection_contains_correct_tiles()
    {
        var sel = new RectangleSelection(5, 5, 15, 15);

        Assert.True(sel.Contains(10, 10));
        Assert.True(sel.Contains(5, 5));
        Assert.True(sel.Contains(15, 15));
        Assert.False(sel.Contains(4, 10));
        Assert.False(sel.Contains(10, 4));
        Assert.False(sel.Contains(16, 10));
        Assert.False(sel.Contains(10, 16));
    }

    [Fact]
    public void RectangleSelection_reversed_coordinates()
    {
        var sel = new RectangleSelection(15, 15, 5, 5);
        // Should normalize to (5,5)-(15,15)
        Assert.True(sel.Contains(10, 10));
        Assert.True(sel.Contains(5, 5));
        Assert.False(sel.Contains(4, 4));
    }

    [Fact]
    public void RectangleSelection_has_correct_bounds()
    {
        var sel = new RectangleSelection(5, 10, 20, 30);
        Assert.NotNull(sel.Bounds);
        Assert.Equal(5, sel.Bounds.Value.MinX);
        Assert.Equal(10, sel.Bounds.Value.MinY);
        Assert.Equal(20, sel.Bounds.Value.MaxX);
        Assert.Equal(30, sel.Bounds.Value.MaxY);
    }

    [Fact]
    public void LassoSelection_empty_returns_false()
    {
        var sel = new LassoSelection([]);
        Assert.False(sel.Contains(0, 0));
        Assert.Null(sel.Bounds);
    }

    [Fact]
    public void LassoSelection_single_point_rejected()
    {
        var sel = new LassoSelection([(5, 5)]);
        Assert.False(sel.Contains(5, 5));
    }

    [Fact]
    public void LassoSelection_rectangle_shape()
    {
        var sel = new LassoSelection([
            (0, 0), (10, 0), (10, 10), (0, 10)
        ]);

        Assert.True(sel.Contains(5, 5));
        Assert.True(sel.Contains(1, 1));
        Assert.True(sel.Contains(9, 9));
        Assert.False(sel.Contains(-1, 5));
        Assert.False(sel.Contains(11, 5));
        Assert.False(sel.Contains(5, -1));
        Assert.False(sel.Contains(5, 11));
    }

    [Fact]
    public void LassoSelection_has_bounds()
    {
        var sel = new LassoSelection([(3, 7), (15, 7), (15, 20), (3, 20)]);
        Assert.NotNull(sel.Bounds);
        Assert.Equal(3, sel.Bounds.Value.MinX);
        Assert.Equal(7, sel.Bounds.Value.MinY);
        Assert.Equal(15, sel.Bounds.Value.MaxX);
        Assert.Equal(20, sel.Bounds.Value.MaxY);
    }

    [Fact]
    public void PaintBrushTool_respects_selection()
    {
        var map = CreateTestMap();
        var sel = new RectangleSelection(2, 2, 5, 5);

        var cmd = PaintBrushTool.Execute(map, 4, 4, 0x1234, 4, selection: sel);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        // Tiles inside selection should be painted
        Assert.Equal(0x1234, map.Terrain[3, 3]);
        Assert.Equal(0x1234, map.Terrain[4, 4]);

        // Tiles outside selection should NOT be painted (default is 0)
        Assert.Equal(0, map.Terrain[0, 4]);
        Assert.Equal(0, map.Terrain[4, 0]);
    }

    [Fact]
    public void RaiseTool_respects_selection()
    {
        var map = CreateTestMap();
        var sel = new RectangleSelection(0, 0, 3, 3);

        var cmd = RaiseTool.Execute(map, 4, 4, 4, 5, selection: sel);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        Assert.Equal(5, map.Height[2, 2]);
        Assert.Equal(0, map.Height[4, 4]); // outside selection, unchanged
    }

    [Fact]
    public void FlattenTool_respects_selection()
    {
        var map = CreateTestMap();
        var sel = new RectangleSelection(0, 0, 5, 5);

        var cmd = FlattenTool.Execute(map, 4, 4, 6, 42, selection: sel);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        Assert.Equal(42, map.Height[3, 3]); // inside
        // (7,7) is outside selection (bounds 0,0-5,5)
        Assert.Equal(0, map.Height[7, 7]);
    }

    // ──────────────────────────────────────────────
    // Stamp tool tests
    // ──────────────────────────────────────────────

    [Fact]
    public void StampTemplate_create_and_validate()
    {
        var tiles = new ushort[16];
        var heights = new sbyte[16];
        for (var i = 0; i < 16; i++)
        {
            tiles[i] = (ushort)(0x100 + i);
            heights[i] = (sbyte)(i);
        }

        var template = StampTemplate.Create("Test", 4, 4, tiles, heights);
        Assert.True(template.IsValid);
        Assert.Equal("Test", template.Name);
        Assert.Equal(0x100, template.GetTile(0, 0));
        Assert.Equal(0x10F, template.GetTile(3, 3));
        Assert.Equal(0, template.GetHeight(0, 0));
        Assert.Equal(15, template.GetHeight(3, 3));
    }

    [Fact]
    public void StampTemplate_invalid_when_mismatched()
    {
        var template = StampTemplate.Create("Bad", 4, 4, new ushort[8], new sbyte[8]);
        Assert.False(template.IsValid);
    }

    [Fact]
    public void StampTemplate_save_and_load_round_trip()
    {
        var tiles = new ushort[] { 0x1, 0x2, 0x3, 0x4 };
        var heights = new sbyte[] { 10, 20, 30, 40 };
        var original = StampTemplate.Create("RoundTrip", 2, 2, tiles, heights);

        var path = Path.GetTempFileName();
        try
        {
            original.Save(path);
            var loaded = StampTemplate.Load(path)!;

            Assert.NotNull(loaded);
            Assert.True(loaded.IsValid);
            Assert.Equal("RoundTrip", loaded.Name);
            Assert.Equal(0x1, loaded.GetTile(0, 0));
            Assert.Equal(0x4, loaded.GetTile(1, 1));
            Assert.Equal(40, loaded.GetHeight(1, 1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void StampTool_places_template_correctly()
    {
        var map = CreateTestMap();
        var tiles = new ushort[] { 0x1111, 0x2222, 0x3333, 0x4444 };
        var heights = new sbyte[] { 10, 20, 30, 40 };
        var template = StampTemplate.Create("Small", 2, 2, tiles, heights);

        var cmd = StampTool.Execute(map, template, 5, 5);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        Assert.Equal(0x1111, map.Terrain[5, 5]);
        Assert.Equal(0x2222, map.Terrain[6, 5]);
        Assert.Equal(0x3333, map.Terrain[5, 6]);
        Assert.Equal(0x4444, map.Terrain[6, 6]);
        Assert.Equal(10, map.Height[5, 5]);
        Assert.Equal(20, map.Height[6, 5]);
        Assert.Equal(30, map.Height[5, 6]);
        Assert.Equal(40, map.Height[6, 6]);
    }

    [Fact]
    public void StampTool_respects_rotation()
    {
        var map = CreateTestMap();
        // Use a non-square template so rotation is clearly visible
        var tiles = new ushort[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6 };
        var heights = new sbyte[] { 1, 2, 3, 4, 5, 6 };
        var template = StampTemplate.Create("Rotate", 3, 2, tiles, heights);

        // Place with 0 rotation
        var cmd0 = StampTool.Execute(map, template, 0, 0, rotation: 0);
        Assert.NotNull(cmd0);
        cmd0.Execute(map);
        Assert.Equal(0x1, map.Terrain[0, 0]);
        Assert.Equal(0x2, map.Terrain[1, 0]);

        // Place rotated 90° CW at same location on a fresh map
        var map2 = CreateTestMap();
        var cmd90 = StampTool.Execute(map2, template, 0, 0, rotation: 90);
        Assert.NotNull(cmd90);
        cmd90.Execute(map2);

        // Rotated template should place different tiles
        Assert.NotEqual(map.Terrain[0, 0], map2.Terrain[0, 0]);
    }

    [Fact]
    public void StampTool_respects_selection()
    {
        var map = CreateTestMap();
        var tiles = new ushort[] { 0xA, 0xB, 0xC, 0xD };
        var heights = new sbyte[] { 1, 2, 3, 4 };
        var template = StampTemplate.Create("Select", 2, 2, tiles, heights);
        var sel = new RectangleSelection(5, 5, 5, 5); // single tile

        var cmd = StampTool.Execute(map, template, 5, 5, selection: sel);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        // Only (5,5) is in selection, so only that tile should change
        Assert.Equal(0xA, map.Terrain[5, 5]);
        // Tile (5,6) is in the stamp area but not in selection — should remain unchanged
        Assert.Equal(0, map.Terrain[5, 6]);
    }

    [Fact]
    public void StampTool_clamps_to_map_bounds()
    {
        var map = new WorldMap(new MapDimensions(4, 4), new MapMetadata("Test", SourceFileType.Mul));
        var tiles = new ushort[36]; // 6x6 template
        var heights = new sbyte[36];
        var template = StampTemplate.Create("Large", 6, 6, tiles, heights);

        var cmd = StampTool.Execute(map, template, 0, 0);
        // Should not throw despite template exceeding map bounds
        Assert.NotNull(cmd);
        cmd.Execute(map); // should not throw
    }

    [Fact]
    public void StampTemplateManager_loads_templates_from_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stamps_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var template = StampTemplate.Create("TestStamp", 2, 2,
                [0x1, 0x2, 0x3, 0x4],
                [1, 2, 3, 4]);
            template.Save(Path.Combine(tempDir, "test.json"));

            var manager = new StampTemplateManager(tempDir);
            manager.LoadAll();

            Assert.Single(manager.Templates);
            Assert.Equal("TestStamp", manager.Templates[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void StampTemplateManager_skips_invalid_files()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "stamps_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "bad.json"), "{invalid}");
            File.WriteAllText(Path.Combine(tempDir, "not.json.dat"), "hello");

            var manager = new StampTemplateManager(tempDir);
            manager.LoadAll();

            Assert.Empty(manager.Templates);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static WorldMap CreateTestMap(int w = 16, int h = 16)
    {
        var map = new WorldMap(new MapDimensions(w, h), new MapMetadata("Test", SourceFileType.Mul));
        return map;
    }

    private static ulong SumTerrain(WorldMap map)
    {
        ulong sum = 0;
        for (var y = 0; y < map.Dimensions.Height; y++)
            for (var x = 0; x < map.Dimensions.Width; x++)
                sum += map.Terrain[x, y];
        return sum;
    }
}
