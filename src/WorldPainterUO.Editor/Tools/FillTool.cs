using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>
/// Flood-fills connected tiles matching a source tile ID with a new tile ID.
/// Uses 4-directional connectivity.
/// </summary>
public sealed class FillTool
{
    public static ICommand? Execute(
        WorldMap map,
        int startX, int startY,
        ushort? findTileId = null,
        ushort replaceTileId = 0,
        ISelection? selection = null)
    {
        if (startX < 0 || startY < 0 || startX >= map.Dimensions.Width || startY >= map.Dimensions.Height)
            return null;

        if (selection is not null && !selection.Contains(startX, startY))
            return null;

        var targetId = findTileId ?? map.Terrain[startX, startY];
        var visited = new HashSet<long>();
        var queue = new Queue<(int X, int Y)>();
        var fillTiles = new List<(int X, int Y)>();

        queue.Enqueue((startX, startY));
        visited.Add(Encode(startX, startY));

        while (queue.TryDequeue(out var pos))
        {
            if (map.Terrain[pos.X, pos.Y] != targetId)
                continue;

            fillTiles.Add(pos);

            foreach (var (nx, ny) in GetNeighbors4(pos.X, pos.Y, map.Dimensions, selection))
            {
                if (visited.Add(Encode(nx, ny)))
                    queue.Enqueue((nx, ny));
            }
        }

        if (fillTiles.Count == 0)
            return null;

        return MapEditCommand.Create(
            $"Fill 0x{replaceTileId:X4}",
            map,
            fillTiles,
            (origId, origZ) => (replaceTileId, origZ),
            modifiesTerrain: true,
            modifiesHeight: false);
    }

    private static long Encode(int x, int y) => ((long)x << 32) | (uint)y;

    private static IEnumerable<(int X, int Y)> GetNeighbors4(int x, int y, MapDimensions dims, ISelection? selection = null)
    {
        if (x > 0 && (selection is null || selection.Contains(x - 1, y))) yield return (x - 1, y);
        if (y > 0 && (selection is null || selection.Contains(x, y - 1))) yield return (x, y - 1);
        if (x < dims.Width - 1 && (selection is null || selection.Contains(x + 1, y))) yield return (x + 1, y);
        if (y < dims.Height - 1 && (selection is null || selection.Contains(x, y + 1))) yield return (x, y + 1);
    }
}
