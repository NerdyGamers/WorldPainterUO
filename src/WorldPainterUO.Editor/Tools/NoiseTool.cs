using WorldPainterUO.Core;
using WorldPainterUO.Editor.Selection;

namespace WorldPainterUO.Editor.Tools;

/// <summary>Applies random Z variation within a configurable range.</summary>
public sealed class NoiseTool
{
    public static ICommand? Execute(
        WorldMap map,
        int centerX, int centerY,
        int radius,
        sbyte minDelta = -5,
        sbyte maxDelta = 5,
        int seed = 0,
        ISelection? selection = null)
    {
        var tiles = BrushShapes.Circle(centerX, centerY, radius)
            .Where(t => selection is null || selection.Contains(t.X, t.Y))
            .ToList();

        if (tiles.Count == 0)
            return null;

        var rng = seed != 0 ? new Random(seed) : new Random();

        return MapEditCommand.Create(
            $"Noise [{minDelta}, {maxDelta}]",
            map,
            tiles,
            (origId, origZ) =>
            {
                var delta = rng.Next(minDelta, maxDelta + 1);
                return (origId, (sbyte)Math.Clamp(origZ + delta, sbyte.MinValue, sbyte.MaxValue));
            },
            modifiesTerrain: false,
            modifiesHeight: true);
    }
}
