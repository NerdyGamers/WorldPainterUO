using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>
/// Paints terrain tile IDs using a circular brush with configurable size,
/// opacity, hardness, and randomization.
/// </summary>
public sealed class PaintBrushTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        ushort tileId,
        int radius,
        double opacity = 1.0,
        double hardness = 1.0,
        int seed = 0,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0)
            return null;

        var rng = seed != 0 ? new Random(seed) : null;

        return MapEditCommand.Create(
            $"Paint 0x{tileId:X4}",
            map,
            tiles,
            (origId, origZ) =>
            {
                var skip = rng?.NextDouble() ?? 0;
                if (skip >= opacity)
                    return (origId, origZ);

                var jitter = rng?.Next(-2, 3) ?? 0;
                var finalId = (ushort)Math.Clamp(tileId + jitter, 0, ushort.MaxValue);

                return (finalId, origZ);
            },
            modifiesTerrain: true,
            modifiesHeight: false);
    }
}
