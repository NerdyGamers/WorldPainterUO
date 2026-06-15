using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>Increases Z values within a circular brush area.</summary>
public static class RaiseTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        int radius,
        sbyte amount = 1,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0) return null;

        return MapEditCommand.Create(
            $"Raise +{amount}",
            map, tiles,
            (id, z) => (id, (sbyte)Math.Clamp(z + amount, -128, 127)),
            modifiesTerrain: false,
            modifiesHeight: true);
    }
}
