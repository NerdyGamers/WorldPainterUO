using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

public sealed class UltimaMapWriter : IMapFileWriter
{
    private const int BlockSize = 196;
    private const int BlockDataSize = 192;
    private const int TileSize = 3;

    public void Write(string filePath, WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(map);

        if (filePath.Length == 0)
            throw new ArgumentException("Path must not be empty.", nameof(filePath));

        var logger = Log.For("UltimaMapWriter");
        logger.LogInformation("Writing map to {Path} ({Width}x{Height}, MUL)",
            filePath, map.Dimensions.Width, map.Dimensions.Height);

        var dims = map.Dimensions;
        // SDK's TileMatrix uses floor division (>> 3) for block count.  Must
        // match so MUL offset formula ((x * BlockHeight) + y) * 196 + 4 works.
        var blockWidth = dims.Width >> 3;
        var blockHeight = dims.Height >> 3;
        var blockCount = blockWidth * blockHeight;
        var data = new byte[blockCount * BlockSize];

        // Column-major block order: X varies slowest (matching real UO MUL format)
        for (var blockX = 0; blockX < blockWidth; blockX++)
            for (var blockY = 0; blockY < blockHeight; blockY++)
            {
                var blockIndex = blockX * blockHeight + blockY;
                var blockOffset = blockIndex * BlockSize;

                // Write block header (4 bytes, zero)
                data[blockOffset] = 0;
                data[blockOffset + 1] = 0;
                data[blockOffset + 2] = 0;
                data[blockOffset + 3] = 0;

                var tileOffset = blockOffset + 4;

                for (var localY = 0; localY < 8; localY++)
                {
                    var tileY = blockY * 8 + localY;

                    for (var localX = 0; localX < 8; localX++)
                    {
                        var tileX = blockX * 8 + localX;
                        var tileId = map.Terrain[tileX, tileY];
                        var z = map.Height[tileX, tileY];

                        data[tileOffset] = (byte)(tileId & 0xFF);
                        data[tileOffset + 1] = (byte)((tileId >> 8) & 0xFF);
                        data[tileOffset + 2] = unchecked((byte)z);

                        tileOffset += TileSize;
                    }
            }
        }

        File.WriteAllBytes(filePath, data);
    }
}
