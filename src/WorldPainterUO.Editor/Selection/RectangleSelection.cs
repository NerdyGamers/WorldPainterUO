using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Selection;

public sealed class RectangleSelection : ISelection
{
    public MapBounds? Bounds { get; }

    public RectangleSelection(int x0, int y0, int x1, int y1)
    {
        var minX = Math.Min(x0, x1);
        var maxX = Math.Max(x0, x1);
        var minY = Math.Min(y0, y1);
        var maxY = Math.Max(y0, y1);
        Bounds = new MapBounds(minX, minY, maxX, maxY);
    }

    public RectangleSelection(MapBounds bounds)
    {
        Bounds = bounds;
    }

    public bool Contains(int x, int y) => Bounds?.Contains(x, y) ?? false;
}
