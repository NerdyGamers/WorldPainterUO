using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>
/// Replaces all instances of one tile ID with another across the full map
/// or within a selection bounds.
/// </summary>
public sealed class ReplaceTool
{
    public static ICommand? Execute(
        WorldMap map,
        ushort findTileId,
        ushort replaceTileId,
        MapBounds? bounds = null,
        ISelection? selection = null)
    {
        var tiles = new List<(int X, int Y)>();
        var b = bounds ?? new MapBounds(0, 0, map.Dimensions.Width - 1, map.Dimensions.Height - 1);

        for (var y = b.MinY; y <= b.MaxY; y++)
        {
            for (var x = b.MinX; x <= b.MaxX; x++)
            {
                if (x >= 0 && x < map.Dimensions.Width && y >= 0 && y < map.Dimensions.Height
                    && map.Terrain[x, y] == findTileId
                    && (selection is null || selection.Contains(x, y)))
                {
                    tiles.Add((x, y));
                }
            }
        }

        if (tiles.Count == 0)
            return null;

        return MapEditCommand.Create(
            $"Replace 0x{findTileId:X4} \u2192 0x{replaceTileId:X4}",
            map,
            tiles,
            (origId, origZ) => (replaceTileId, origZ),
            modifiesTerrain: true,
            modifiesHeight: false);
    }
}
