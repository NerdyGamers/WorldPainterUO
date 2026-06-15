using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>
/// Averages Z values of neighboring tiles within a brush radius.
/// Computes all target values from the pre-edit map state to avoid
/// cascading artifacts.
/// </summary>
public sealed class SmoothTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        int radius,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0)
            return null;

        // First pass: compute averaged Z for every tile using pre-edit state
        var targetZ = new Dictionary<(int X, int Y), sbyte>();
        var dims = map.Dimensions;

        foreach (var (tx, ty) in tiles)
        {
            if (tx < 0 || ty < 0 || tx >= dims.Width || ty >= dims.Height)
                continue;

            var sum = 0;
            var count = 0;

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = tx + dx;
                    var ny = ty + dy;

                    if (nx < 0 || ny < 0 || nx >= dims.Width || ny >= dims.Height)
                        continue;

                    sum += map.Height[nx, ny];
                    count++;
                }
            }

            targetZ[(tx, ty)] = (sbyte)(count > 0 ? sum / count : map.Height[tx, ty]);
        }

        if (targetZ.Count == 0)
            return null;

        // Group by chunk, build before/after arrays
        var size = MapChunk<ushort>.Size;
        var countTiles = MapChunk<ushort>.TileCount;
        var grouped = new Dictionary<(int, int), List<(int X, int Y, sbyte NewZ)>>();

        foreach (var ((tx, ty), newZ) in targetZ)
        {
            dims.GetChunkCoord(tx, ty, out var cx, out var cy, out _, out _);
            var key = (cx, cy);

            if (!grouped.TryGetValue(key, out var list))
            {
                list = [];
                grouped[key] = list;
            }

            list.Add((tx, ty, newZ));
        }

        var diffs = new List<ChunkDiff>();

        foreach (var ((cx, cy), tileList) in grouped)
        {
            var heightChunk = map.Height.GetChunk(cx, cy);
            var before = new MapTile[countTiles];
            var after = new MapTile[countTiles];

            for (var i = 0; i < countTiles; i++)
            {
                var lx = i % size;
                var ly = i / size;
                var z = heightChunk[lx, ly];
                before[i] = new MapTile(0, z);
                after[i] = before[i];
            }

            var changed = false;

            foreach (var (tx, ty, newZ) in tileList)
            {
                dims.GetChunkCoord(tx, ty, out _, out _, out var lx, out var ly);
                var idx = ly * size + lx;

                if (after[idx].Z != newZ)
                {
                    after[idx] = after[idx] with { Z = newZ };
                    changed = true;
                }
            }

            if (changed)
                diffs.Add(new ChunkDiff(cx, cy, before, after));
        }

        if (diffs.Count == 0)
            return null;

        return new MapEditCommand("Smooth height", diffs.ToArray(), false, true);
    }
}
