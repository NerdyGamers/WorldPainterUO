using System.Runtime.InteropServices;

namespace WorldPainterUO.FileFormats.Ultima;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Tile
{
    public ushort Id;
    public sbyte Z;

    public Tile(ushort id, sbyte z)
    {
        Id = id;
        Z = z;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HuedTile
{
    public ushort Id { get; }
    public int Hue { get; }
    public sbyte Z { get; }

    public HuedTile(ushort id, short hue, sbyte z)
    {
        Id = id;
        Hue = hue;
        Z = z;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StaticTile
{
    public ushort Id;
    public byte X;
    public byte Y;
    public sbyte Z;
    public short Hue;
}
