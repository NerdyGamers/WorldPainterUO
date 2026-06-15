using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>Forces all tiles in the brush area to a specific Z value.</summary>
public static class FlattenTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        int radius,
        sbyte targetZ = 0,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0) return null;

        return MapEditCommand.Create(
            $"Flatten Z={targetZ}",
            map, tiles,
            (id, _) => (id, targetZ),
            modifiesTerrain: false,
            modifiesHeight: true);
    }
}
