namespace WorldPainterUO.Editor;

internal static class BrushShapes
{
    /// <summary>Yields all tile coordinates within a filled circle (inclusive).</summary>
    public static IEnumerable<(int X, int Y)> Circle(int centerX, int centerY, int radius)
    {
        if (radius < 0)
            yield break;

        if (radius == 0)
        {
            yield return (centerX, centerY);
            yield break;
        }

        var r2 = radius * radius;

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= r2)
                    yield return (centerX + dx, centerY + dy);
            }
        }
    }

    /// <summary>Yields all tile coordinates within a square brush (inclusive).</summary>
    public static IEnumerable<(int X, int Y)> Square(int centerX, int centerY, int halfSize)
    {
        for (var dy = -halfSize; dy <= halfSize; dy++)
        {
            for (var dx = -halfSize; dx <= halfSize; dx++)
                yield return (centerX + dx, centerY + dy);
        }
    }
}
