using WorldPainterUO.Core;

namespace WorldPainterUO.Tests.Fixtures;

internal static class MulFixtureBuilder
{
    public static byte[] BuildMulFile(int tileWidth, int tileHeight, Func<int, int, ushort>? tileIdFn, Func<int, int, sbyte>? zFn)
    {
        var blockWidth = (tileWidth + 7) / 8;
        var blockHeight = (tileHeight + 7) / 8;
        var totalBlocks = blockWidth * blockHeight;
        var data = new byte[totalBlocks * 196];

        // Column-major block order: X varies slowest (matching Ultima MUL format)
        for (var bx = 0; bx < blockWidth; bx++)
        {
            for (var by = 0; by < blockHeight; by++)
            {
                var blockIndex = bx * blockHeight + by;
                var offset = blockIndex * 196 + 4; // skip 4-byte header

                for (var ly = 0; ly < 8; ly++)
                {
                    var tileY = by * 8 + ly;
                    for (var lx = 0; lx < 8; lx++)
                    {
                        var tileX = bx * 8 + lx;

                        if (tileX < tileWidth && tileY < tileHeight)
                        {
                            var tileId = tileIdFn?.Invoke(tileX, tileY) ?? 0;
                            var z = zFn?.Invoke(tileX, tileY) ?? 0;

                            data[offset] = (byte)(tileId & 0xFF);
                            data[offset + 1] = (byte)((tileId >> 8) & 0xFF);
                            data[offset + 2] = unchecked((byte)(sbyte)z);
                        }

                        offset += 3;
                    }
                }
            }
        }

        return data;
    }

    public static byte[] BuildSimpleMap(int tileWidth, int tileHeight, ushort tileId = 0x0001, sbyte z = 10) =>
        BuildMulFile(tileWidth, tileHeight, (_, _) => tileId, (_, _) => z);

    public static string WriteTempFile(byte[] data)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mul_test_{Guid.NewGuid():N}.mul");
        File.WriteAllBytes(path, data);
        return path;
    }
}
