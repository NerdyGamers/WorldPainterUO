using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;

namespace WorldPainterUO.Tests.FileFormats;

public class MapExportValidatorTests
{
    [Fact]
    public void Valid_map_returns_no_diagnostics()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        var diagnostics = MapExportValidator.Validate(map);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void All_valid_tile_ids_are_accepted()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        map.Terrain[0, 0] = 0x0000;
        map.Terrain[1, 1] = 0x3FFF;

        var diagnostics = MapExportValidator.Validate(map);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Tile_id_above_0x3FFF_emits_warning()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        map.Terrain[3, 3] = 0x4000;

        var diagnostics = MapExportValidator.Validate(map);
        var tileDiag = Assert.Single(diagnostics);
        Assert.Equal(ExportDiagnosticSeverity.Warning, tileDiag.Severity);
        Assert.Equal("TILE_OOB", tileDiag.Code);
        Assert.Equal(3, tileDiag.TileX);
        Assert.Equal(3, tileDiag.TileY);
    }

    [Fact]
    public void Zero_width_map_emits_error()
    {
        var map = WorldMap.Create(0, 64, "Test", SourceFileType.Mul);
        var diagnostics = MapExportValidator.Validate(map);
        Assert.Contains(diagnostics, d =>
            d.Severity == ExportDiagnosticSeverity.Error &&
            d.Code == "DIM_ZERO");
    }

    [Fact]
    public void Zero_height_map_emits_error()
    {
        var map = WorldMap.Create(64, 0, "Test", SourceFileType.Mul);
        var diagnostics = MapExportValidator.Validate(map);
        Assert.Contains(diagnostics, d =>
            d.Severity == ExportDiagnosticSeverity.Error &&
            d.Code == "DIM_ZERO");
    }

    [Fact]
    public void IsExportable_returns_true_for_valid_map()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        var diagnostics = MapExportValidator.Validate(map);
        Assert.True(MapExportValidator.IsExportable(diagnostics));
    }

    [Fact]
    public void IsExportable_returns_true_for_warnings_only()
    {
        var map = WorldMap.Create(8, 8, "Test", SourceFileType.Mul);
        map.Terrain[0, 0] = 0x4000;
        var diagnostics = MapExportValidator.Validate(map);
        Assert.True(MapExportValidator.IsExportable(diagnostics));
    }

    [Fact]
    public void IsExportable_returns_false_for_errors()
    {
        var map = WorldMap.Create(0, 64, "Test", SourceFileType.Mul);
        var diagnostics = MapExportValidator.Validate(map);
        Assert.False(MapExportValidator.IsExportable(diagnostics));
    }

    [Fact]
    public void Null_map_throws()
    {
        Assert.Throws<ArgumentNullException>(() => MapExportValidator.Validate(null!));
    }

    [Fact]
    public void Max_tile_warnings_suppressed()
    {
        var map = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 8; x++)
                map.Terrain[x, y] = 0x4000;

        var diagnostics = MapExportValidator.Validate(map);
        var tileWarnings = diagnostics.Where(d => d.Code == "TILE_OOB").Count();
        Assert.True(tileWarnings <= 100);
    }
}
