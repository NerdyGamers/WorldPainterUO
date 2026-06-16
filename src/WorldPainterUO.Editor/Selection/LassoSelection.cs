using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Selection;

public sealed class LassoSelection : ISelection
{
    private readonly (int X, int Y)[] _polygon;
    public MapBounds? Bounds { get; }
    public IReadOnlyList<(int X, int Y)> Polygon => _polygon;

    public LassoSelection(IEnumerable<(int X, int Y)> points)
    {
        _polygon = points.ToArray();

        if (_polygon.Length == 0)
        {
            Bounds = null;
            return;
        }

        var minX = _polygon.Min(p => p.X);
        var maxX = _polygon.Max(p => p.X);
        var minY = _polygon.Min(p => p.Y);
        var maxY = _polygon.Max(p => p.Y);
        Bounds = new MapBounds(minX, minY, maxX, maxY);
    }

    public bool Contains(int x, int y)
    {
        if (_polygon.Length < 3)
            return false;

        if (Bounds.HasValue && !Bounds.Value.Contains(x, y))
            return false;

        return PointInPolygon(x, y);
    }

    private bool PointInPolygon(int px, int py)
    {
        var inside = false;
        var j = _polygon.Length - 1;

        for (var i = 0; i < _polygon.Length; i++)
        {
            var xi = _polygon[i].X;
            var yi = _polygon[i].Y;
            var xj = _polygon[j].X;
            var yj = _polygon[j].Y;

            if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (double)(yj - yi) + xi)
                inside = !inside;

            j = i;
        }

        return inside;
    }
}
