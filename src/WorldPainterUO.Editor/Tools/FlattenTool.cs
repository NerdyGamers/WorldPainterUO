using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>Sets all tiles under the brush to a target Z value.</summary>
public sealed class FlattenTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        int radius,
        sbyte targetZ,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0)
            return null;

        return MapEditCommand.Create(
            $"Flatten to {targetZ}",
            map,
            tiles,
            (origId, origZ) => (origId, targetZ),
            modifiesTerrain: false,
            modifiesHeight: true);
    }
}
