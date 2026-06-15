namespace WorldPainterUO.Core;

public sealed class MapChunk<T> where T : struct
{
    public const int Size = 64;
    public const int TileCount = Size * Size;

    private readonly T[] _data;

    public MapChunk((int X, int Y) index, T defaultValue = default)
    {
        Index = index;
        _data = new T[TileCount];

        if (EqualityComparer<T>.Default.Equals(defaultValue, default(T)) is false)
        {
            Array.Fill(_data, defaultValue);
        }
    }

    public (int X, int Y) Index { get; }
    public bool IsDirty { get; private set; }

    public T this[int localX, int localY]
    {
        get => _data[localY * Size + localX];
        set
        {
            _data[localY * Size + localX] = value;
            IsDirty = true;
        }
    }

    public ReadOnlySpan<T> Data => _data;

    public void MarkClean() => IsDirty = false;
}
