namespace WorldPainterUO.Core;

public readonly record struct MapBounds(int MinX, int MinY, int MaxX, int MaxY)
{
    public int Width => MaxX - MinX + 1;
    public int Height => MaxY - MinY + 1;

    public bool Contains(int x, int y) =>
        x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
}
