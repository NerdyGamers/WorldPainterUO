using WorldPainterUO.Core;
using WorldPainterUO.Editor;
using WorldPainterUO.Editor.Tools;

namespace WorldPainterUO.Tests.Editor;

public sealed class EditorToolTests
{
    private static WorldMap CreateMap(int w = 16, int h = 16, ushort defaultTile = 0, sbyte defaultZ = 0)
    {
        var map = new WorldMap(new MapDimensions(w, h), new MapMetadata("Test", SourceFileType.Mul));
        // Fill with known values
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                map.Terrain[x, y] = (ushort)(defaultTile + (x + y * w) % 64);
                map.Height[x, y] = (sbyte)(defaultZ + (x + y * w) % 32);
            }
        }
        return map;
    }

    // ──────────────────────────────────────────────
    // CommandHistory basics
    // ──────────────────────────────────────────────

    [Fact]
    public void CommandHistory_empty_cannot_undo_or_redo()
    {
        var history = new CommandHistory();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void CommandHistory_execute_undo_redo_round_trip()
    {
        var map = CreateMap();
        var history = new CommandHistory();

        var cmd = PaintBrushTool.Execute(map, 0, 0, 0x1234, 0);
        Assert.NotNull(cmd);

        history.Execute(cmd, map);
        Assert.True(history.CanUndo);
        Assert.Equal(0x1234, map.Terrain[0, 0]);

        history.Undo(map);
        Assert.True(history.CanRedo);
        Assert.Equal(0, map.Terrain[0, 0]);

        history.Redo(map);
        Assert.True(history.CanUndo);
        Assert.Equal(0x1234, map.Terrain[0, 0]);
    }

    [Fact]
    public void CommandHistory_clear_empties_both_stacks()
    {
        var map = CreateMap();
        var history = new CommandHistory();
        history.Execute(PaintBrushTool.Execute(map, 0, 0, 0x1234, 0)!, map);
        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    // ──────────────────────────────────────────────
    // PaintBrushTool
    // ──────────────────────────────────────────────

    [Fact]
    public void PaintBrushTool_paints_single_tile()
    {
        var map = CreateMap();
        var cmd = PaintBrushTool.Execute(map, 5, 5, 0xABCD, 0);

        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(0xABCD, map.Terrain[5, 5]);

        cmd.Undo(map);
        Assert.NotEqual(0xABCD, map.Terrain[5, 5]);
    }

    [Fact]
    public void PaintBrushTool_undo_redo_round_trip()
    {
        var map = CreateMap();
        var orig = map.Terrain[8, 8];
        var history = new CommandHistory();

        history.Execute(PaintBrushTool.Execute(map, 8, 8, 0xBEEF, 0)!, map);
        Assert.Equal(0xBEEF, map.Terrain[8, 8]);

        history.Undo(map);
        Assert.Equal(orig, map.Terrain[8, 8]);

        history.Redo(map);
        Assert.Equal(0xBEEF, map.Terrain[8, 8]);
    }

    [Fact]
    public void PaintBrushTool_radius_affects_multiple_tiles()
    {
        var map = CreateMap();
        var cmd = PaintBrushTool.Execute(map, 8, 8, 0xCAFE, 2);

        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));

        // Center should be painted
        Assert.Equal(0xCAFE, map.Terrain[8, 8]);

        // Far corner should not be affected
        Assert.NotEqual(0xCAFE, map.Terrain[0, 0]);

        cmd.Undo(map);
        // Spot-check center tile restored
        Assert.NotEqual(0xCAFE, map.Terrain[8, 8]);
    }

    // ──────────────────────────────────────────────
    // FillTool (flood fill)
    // ──────────────────────────────────────────────

    [Fact]
    public void FillTool_fills_connected_region()
    {
        var map = CreateMap(16, 16, 0x1111);
        // Carve out a region of a different tile
        map.Terrain[3, 3] = 0x2222;
        map.Terrain[3, 4] = 0x2222;
        map.Terrain[4, 3] = 0x2222;

        var cmd = FillTool.Execute(map, 3, 3, 0x2222, 0x3333);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(0x3333, map.Terrain[3, 3]);
        Assert.Equal(0x3333, map.Terrain[3, 4]);
        Assert.Equal(0x3333, map.Terrain[4, 3]);

        cmd.Undo(map);
        Assert.Equal(0x2222, map.Terrain[3, 3]);
    }

    [Fact]
    public void FillTool_undo_redo_round_trip()
    {
        var map = CreateMap(16, 16, 0xAAAA);
        map.Terrain[1, 1] = 0xBBBB;
        map.Terrain[1, 2] = 0xBBBB;

        var history = new CommandHistory();
        history.Execute(FillTool.Execute(map, 1, 1, 0xBBBB, 0xCCCC)!, map);
        Assert.Equal(0xCCCC, map.Terrain[1, 1]);
        Assert.Equal(0xCCCC, map.Terrain[1, 2]);

        history.Undo(map);
        Assert.Equal(0xBBBB, map.Terrain[1, 1]);
        Assert.Equal(0xBBBB, map.Terrain[1, 2]);

        history.Redo(map);
        Assert.Equal(0xCCCC, map.Terrain[1, 1]);
        Assert.Equal(0xCCCC, map.Terrain[1, 2]);
    }

    [Fact]
    public void FillTool_out_of_bounds_start_returns_null()
    {
        var map = CreateMap();
        Assert.Null(FillTool.Execute(map, -1, -1, 0, 0));
    }

    // ──────────────────────────────────────────────
    // ReplaceTool
    // ──────────────────────────────────────────────

    [Fact]
    public void ReplaceTool_replaces_all_matching_tiles()
    {
        var map = CreateMap(8, 8, 0x1111);
        map.Terrain[0, 0] = 0x2222;
        map.Terrain[2, 3] = 0x2222;
        map.Terrain[7, 7] = 0x2222;

        var cmd = ReplaceTool.Execute(map, 0x2222, 0x3333);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(0x3333, map.Terrain[0, 0]);
        Assert.Equal(0x3333, map.Terrain[2, 3]);
        Assert.Equal(0x3333, map.Terrain[7, 7]);

        cmd.Undo(map);
        Assert.Equal(0x2222, map.Terrain[0, 0]);
    }

    [Fact]
    public void ReplaceTool_undo_redo_round_trip()
    {
        var map = CreateMap(8, 8, 0x1111);
        map.Terrain[1, 1] = 0x2222;
        map.Terrain[5, 5] = 0x2222;

        var history = new CommandHistory();
        history.Execute(ReplaceTool.Execute(map, 0x2222, 0x3333)!, map);
        Assert.Equal(0x3333, map.Terrain[1, 1]);

        history.Undo(map);
        Assert.Equal(0x2222, map.Terrain[1, 1]);

        history.Redo(map);
        Assert.Equal(0x3333, map.Terrain[1, 1]);
    }

    [Fact]
    public void ReplaceTool_no_match_returns_null()
    {
        var map = CreateMap(8, 8, 0x1111);
        Assert.Null(ReplaceTool.Execute(map, 0x9999, 0xAAAA));
    }

    // ──────────────────────────────────────────────
    // RaiseTool
    // ──────────────────────────────────────────────

    [Fact]
    public void RaiseTool_increases_Z()
    {
        var map = CreateMap();
        map.Height[4, 4] = 10;
        var origZ = map.Height[4, 4];

        var cmd = RaiseTool.Execute(map, 4, 4, 0, 3);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(origZ + 3, map.Height[4, 4]);

        cmd.Undo(map);
        Assert.Equal(origZ, map.Height[4, 4]);
    }

    [Fact]
    public void RaiseTool_undo_redo_round_trip()
    {
        var map = CreateMap();
        map.Height[2, 2] = 5;
        var history = new CommandHistory();

        history.Execute(RaiseTool.Execute(map, 2, 2, 0, 2)!, map);
        Assert.Equal(7, map.Height[2, 2]);

        history.Undo(map);
        Assert.Equal(5, map.Height[2, 2]);

        history.Redo(map);
        Assert.Equal(7, map.Height[2, 2]);
    }

    [Fact]
    public void RaiseTool_clamps_to_sbyte_max()
    {
        var map = CreateMap();
        map.Height[0, 0] = 126;

        var cmd = RaiseTool.Execute(map, 0, 0, 0, 10);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(127, map.Height[0, 0]);
    }

    // ──────────────────────────────────────────────
    // LowerTool
    // ──────────────────────────────────────────────

    [Fact]
    public void LowerTool_decreases_Z()
    {
        var map = CreateMap();
        map.Height[6, 6] = 10;

        var cmd = LowerTool.Execute(map, 6, 6, 0, 4);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(6, map.Height[6, 6]);

        cmd.Undo(map);
        Assert.Equal(10, map.Height[6, 6]);
    }

    [Fact]
    public void LowerTool_undo_redo_round_trip()
    {
        var map = CreateMap();
        map.Height[3, 3] = 0;
        var history = new CommandHistory();

        history.Execute(LowerTool.Execute(map, 3, 3, 0, 1)!, map);
        Assert.Equal(-1, map.Height[3, 3]);

        history.Undo(map);
        Assert.Equal(0, map.Height[3, 3]);

        history.Redo(map);
        Assert.Equal(-1, map.Height[3, 3]);
    }

    // ──────────────────────────────────────────────
    // SmoothTool
    // ──────────────────────────────────────────────

    [Fact]
    public void SmoothTool_averages_neighbors()
    {
        var map = CreateMap(8, 8, defaultZ: 0);
        // Set all 9 neighbor values explicitly
        var neighbors = new[] {
            (2,2,0), (2,3,0), (2,4,0),
            (3,2,0), (3,3,10), (3,4,30),
            (4,2,0), (4,3,20), (4,4,40),
        };
        foreach (var (x, y, z) in neighbors)
            map.Height[x, y] = (sbyte)z;

        // Sum = 0+0+0+0+10+30+0+20+40 = 100, avg = 100/9 = 11
        var cmd = SmoothTool.Execute(map, 3, 3, 0);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(11, map.Height[3, 3]);

        cmd.Undo(map);
        Assert.Equal(10, map.Height[3, 3]);
    }

    [Fact]
    public void SmoothTool_undo_redo_round_trip()
    {
        var map = CreateMap(8, 8, defaultZ: 0);
        map.Height[4, 4] = 50;
        var history = new CommandHistory();

        history.Execute(SmoothTool.Execute(map, 4, 4, 0)!, map);
        var afterZ = map.Height[4, 4];
        Assert.NotEqual(50, afterZ);

        history.Undo(map);
        Assert.Equal(50, map.Height[4, 4]);

        history.Redo(map);
        Assert.Equal(afterZ, map.Height[4, 4]);
    }

    // ──────────────────────────────────────────────
    // FlattenTool
    // ──────────────────────────────────────────────

    [Fact]
    public void FlattenTool_sets_target_Z()
    {
        var map = CreateMap();
        map.Height[5, 5] = 20;

        var cmd = FlattenTool.Execute(map, 5, 5, 0, -10);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(-10, map.Height[5, 5]);

        cmd.Undo(map);
        Assert.Equal(20, map.Height[5, 5]);
    }

    [Fact]
    public void FlattenTool_undo_redo_round_trip()
    {
        var map = CreateMap();
        map.Height[7, 7] = 15;
        var history = new CommandHistory();

        history.Execute(FlattenTool.Execute(map, 7, 7, 0, 0)!, map);
        Assert.Equal(0, map.Height[7, 7]);

        history.Undo(map);
        Assert.Equal(15, map.Height[7, 7]);

        history.Redo(map);
        Assert.Equal(0, map.Height[7, 7]);
    }

    [Fact]
    public void FlattenTool_radius_affects_area()
    {
        var map = CreateMap(16, 16);
        var cmd = FlattenTool.Execute(map, 8, 8, 3, 100);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        Assert.Equal(100, map.Height[8, 8]);
        Assert.Equal(100, map.Height[6, 6]);
        // Outside brush
        Assert.NotEqual(100, map.Height[0, 0]);
    }

    // ──────────────────────────────────────────────
    // NoiseTool
    // ──────────────────────────────────────────────

    [Fact]
    public void NoiseTool_changes_Z_within_range()
    {
        var map = CreateMap();
        map.Height[2, 2] = 0;

        var cmd = NoiseTool.Execute(map, 2, 2, 0, -2, 2, seed: 42);
        Assert.NotNull(cmd);
        Assert.True(cmd.Execute(map));
        var newZ = map.Height[2, 2];
        Assert.InRange(newZ, -2, 2);
        Assert.NotEqual(0, newZ); // Non-zero delta within small range with fixed seed

        cmd.Undo(map);
        Assert.Equal(0, map.Height[2, 2]);
    }

    [Fact]
    public void NoiseTool_undo_redo_round_trip()
    {
        var map = CreateMap();
        var history = new CommandHistory();

        history.Execute(NoiseTool.Execute(map, 5, 5, 0, -10, 10, seed: 99)!, map);
        var afterZ = map.Height[5, 5];

        history.Undo(map);
        Assert.NotEqual(afterZ, map.Height[5, 5]);

        history.Redo(map);
        Assert.Equal(afterZ, map.Height[5, 5]);
    }

    [Fact]
    public void NoiseTool_deterministic_seed()
    {
        var map1 = CreateMap();
        var map2 = CreateMap();

        NoiseTool.Execute(map1, 5, 5, 2, -5, 5, seed: 123)!.Execute(map1);
        NoiseTool.Execute(map2, 5, 5, 2, -5, 5, seed: 123)!.Execute(map2);

        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                Assert.Equal(map1.Height[x, y], map2.Height[x, y]);
    }

    // ──────────────────────────────────────────────
    // Multi-step undo/redo chain
    // ──────────────────────────────────────────────

    [Fact]
    public void Multiple_commands_undo_in_reverse_order()
    {
        var map = CreateMap(8, 8);
        var history = new CommandHistory();

        history.Execute(PaintBrushTool.Execute(map, 0, 0, 0x1111, 0)!, map);
        history.Execute(PaintBrushTool.Execute(map, 1, 1, 0x2222, 0)!, map);
        history.Execute(PaintBrushTool.Execute(map, 2, 2, 0x3333, 0)!, map);

        Assert.Equal(0x1111, map.Terrain[0, 0]);
        Assert.Equal(0x2222, map.Terrain[1, 1]);
        Assert.Equal(0x3333, map.Terrain[2, 2]);

        // Undo third only
        history.Undo(map);
        Assert.NotEqual(0x3333, map.Terrain[2, 2]);
        Assert.Equal(0x2222, map.Terrain[1, 1]);

        // Undo second
        history.Undo(map);
        Assert.NotEqual(0x2222, map.Terrain[1, 1]);

        // Undo first
        history.Undo(map);
        Assert.NotEqual(0x1111, map.Terrain[0, 0]);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void New_execute_clears_redo_stack()
    {
        var map = CreateMap();
        var history = new CommandHistory();

        history.Execute(PaintBrushTool.Execute(map, 0, 0, 0x1111, 0)!, map);
        history.Undo(map);
        Assert.True(history.CanRedo);

        history.Execute(PaintBrushTool.Execute(map, 1, 1, 0x2222, 0)!, map);
        Assert.False(history.CanRedo);
    }

    // ──────────────────────────────────────────────
    // Chunk dirty flags
    // ──────────────────────────────────────────────

    [Fact]
    public void Edit_sets_chunk_dirty_flag()
    {
        var map = CreateMap(8, 8);
        map.Terrain.MarkAllClean();
        map.Height.MarkAllClean();

        var cmd = PaintBrushTool.Execute(map, 0, 0, 0xABCD, 0);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        Assert.Single(map.Terrain.DirtyChunks);
        Assert.Empty(map.Height.DirtyChunks);
    }

    [Fact]
    public void Height_edit_sets_height_chunk_dirty()
    {
        var map = CreateMap(8, 8);
        map.Terrain.MarkAllClean();
        map.Height.MarkAllClean();

        var cmd = RaiseTool.Execute(map, 0, 0, 0, 5);
        Assert.NotNull(cmd);
        cmd.Execute(map);

        Assert.Empty(map.Terrain.DirtyChunks);
        Assert.Single(map.Height.DirtyChunks);
    }
}
