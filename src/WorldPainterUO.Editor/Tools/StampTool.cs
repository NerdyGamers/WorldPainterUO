using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;
using WorldPainterUO.Editor.Stamp;

namespace WorldPainterUO.Editor.Tools;

public sealed class StampTool
{
    public static ICommand? Execute(
        WorldMap map,
        StampTemplate template,
        int targetX, int targetY,
        int rotation = 0,
        ISelection? selection = null)
    {
        var (tw, th) = rotation is 90 or 270
            ? (template.Height, template.Width)
            : (template.Width, template.Height);

        var dims = map.Dimensions;
        var size = MapChunk<ushort>.Size;
        var countTiles = MapChunk<ushort>.TileCount;
        var grouped = new Dictionary<(int, int), List<(int MX, int MY, ushort NewId, sbyte NewZ)>>();

        for (var ty = 0; ty < th; ty++)
        {
            for (var tx = 0; tx < tw; tx++)
            {
                var mx = targetX + tx;
                var my = targetY + ty;

                if (mx < 0 || my < 0 || mx >= dims.Width || my >= dims.Height)
                    continue;

                if (selection is not null && !selection.Contains(mx, my))
                    continue;

                var (sx, sy) = GetSourceCoords(tx, ty, template.Width, template.Height, rotation);
                var newId = template.GetTile(sx, sy);
                var newZ = template.GetHeight(sx, sy);

                dims.GetChunkCoord(mx, my, out var cx, out var cy, out _, out _);
                var key = (cx, cy);

                if (!grouped.TryGetValue(key, out var list))
                {
                    list = [];
                    grouped[key] = list;
                }

                list.Add((mx, my, newId, newZ));
            }
        }

        if (grouped.Count == 0)
            return null;

        var diffs = new List<ChunkDiff>();

        foreach (var ((cx, cy), tileList) in grouped)
        {
            var terrainChunk = map.Terrain.GetChunk(cx, cy);
            var heightChunk = map.Height.GetChunk(cx, cy);

            var before = new MapTile[countTiles];
            var after = new MapTile[countTiles];

            for (var i = 0; i < countTiles; i++)
            {
                var lx = i % size;
                var ly = i / size;
                before[i] = new MapTile(terrainChunk[lx, ly], heightChunk[lx, ly]);
                after[i] = before[i];
            }

            foreach (var (mx, my, newId, newZ) in tileList)
            {
                dims.GetChunkCoord(mx, my, out _, out _, out var lx, out var ly);
                var idx = ly * size + lx;
                after[idx] = new MapTile(newId, newZ);
            }

            diffs.Add(new ChunkDiff(cx, cy, before, after));
        }

        return new MapEditCommand($"Stamp {template.Name}", diffs.ToArray(), true, true);
    }

    private static (int sx, int sy) GetSourceCoords(
        int dx, int dy, int w, int h, int rotation)
    {
        return rotation switch
        {
            90 => (dy, h - 1 - dx),
            180 => (w - 1 - dx, h - 1 - dy),
            270 => (w - 1 - dy, dx),
            _ => (dx, dy)
        };
    }
}
