using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>Decreases Z values for tiles under a circular brush.</summary>
public sealed class LowerTool
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

        if (tiles.Count == 0)
            return null;

        return MapEditCommand.Create(
            $"Lower -{amount}",
            map,
            tiles,
            (origId, origZ) => (origId, (sbyte)Math.Clamp(origZ - amount, sbyte.MinValue, sbyte.MaxValue)),
            modifiesTerrain: false,
            modifiesHeight: true);
    }
}
