using WorldPainterUO.Core;
using WorldPainterUO.Project;

namespace WorldPainterUO.Tests.Project;

public class UomapAutosaveTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); }
            catch { /* best effort */ }
        }
    }

    private string GetProjectPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"uomap_autosave_{Guid.NewGuid():N}.uomap");
        _tempFiles.Add(path);
        _tempFiles.Add(UomapAutosave.GetAutosavePath(path));
        return path;
    }

    [Fact]
    public void Snapshot_does_not_exist_initially()
    {
        var path = GetProjectPath();
        Assert.False(UomapAutosave.SnapshotExists(path));
    }

    [Fact]
    public void Save_and_restore_snapshot()
    {
        var original = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        original.Terrain[10, 10] = 0x0ABC;
        original.Height[20, 20] = 30;

        var projectPath = GetProjectPath();
        UomapAutosave.SaveSnapshot(projectPath, original);

        Assert.True(UomapAutosave.SnapshotExists(projectPath));

        var loaded = UomapAutosave.TryLoadSnapshot(projectPath);
        Assert.NotNull(loaded);
        Assert.Equal(0x0ABC, loaded.Terrain[10, 10]);
        Assert.Equal(30, loaded.Height[20, 20]);
    }

    [Fact]
    public void Delete_snapshot_removes_file()
    {
        var original = WorldMap.Create(64, 64, "Test", SourceFileType.Mul);
        var projectPath = GetProjectPath();

        UomapAutosave.SaveSnapshot(projectPath, original);
        Assert.True(UomapAutosave.SnapshotExists(projectPath));

        UomapAutosave.DeleteSnapshot(projectPath);
        Assert.False(UomapAutosave.SnapshotExists(projectPath));
    }

    [Fact]
    public void TryLoad_returns_null_when_no_snapshot()
    {
        var projectPath = GetProjectPath();
        var loaded = UomapAutosave.TryLoadSnapshot(projectPath);
        Assert.Null(loaded);
    }

    [Fact]
    public void Save_snapshot_replaces_previous()
    {
        var map1 = WorldMap.Create(64, 64, "V1", SourceFileType.Mul);
        var map2 = WorldMap.Create(64, 64, "V2", SourceFileType.Mul);

        var projectPath = GetProjectPath();
        UomapAutosave.SaveSnapshot(projectPath, map1);
        UomapAutosave.SaveSnapshot(projectPath, map2);

        var loaded = UomapAutosave.TryLoadSnapshot(projectPath);
        Assert.NotNull(loaded);
        Assert.Equal("V2", loaded.Metadata.Facet);
    }

    [Fact]
    public void Autosave_path_suffix()
    {
        var path = @"C:\projects\test.uomap";
        Assert.Equal(@"C:\projects\test.uomap.autosave", UomapAutosave.GetAutosavePath(path));
    }
}
