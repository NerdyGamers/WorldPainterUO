using WorldPainterUO.Core;

namespace WorldPainterUO.Editor.Selection;

public interface ISelection
{
    bool Contains(int x, int y);
    MapBounds? Bounds { get; }
}
