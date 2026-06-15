namespace WorldPainterUO.Editor.Generators;

internal static class NoiseHelper
{
    public static float[,] GenerateHeightMap(int width, int height, int seed, float scale = 0.02f, int octaves = 4, float persistence = 0.5f)
    {
        var map = new float[width, height];
        var rng = new Random(seed);

        var offsets = new (float X, float Y)[octaves];
        for (var i = 0; i < octaves; i++)
            offsets[i] = ((float)rng.NextDouble() * 10000, (float)rng.NextDouble() * 10000);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var amplitude = 1f;
                var frequency = 1f;
                var value = 0f;
                var maxValue = 0f;

                for (var o = 0; o < octaves; o++)
                {
                    var sx = (x + offsets[o].X) * scale * frequency;
                    var sy = (y + offsets[o].Y) * scale * frequency;
                    value += SmoothNoise(sx, sy) * amplitude;
                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= 2;
                }

                map[x, y] = value / maxValue;
            }
        }

        return map;
    }

    private static float SmoothNoise(float x, float y)
    {
        var ix = (int)Math.Floor(x);
        var iy = (int)Math.Floor(y);
        var fx = x - ix;
        var fy = y - iy;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);

        var v00 = Hash(ix, iy);
        var v10 = Hash(ix + 1, iy);
        var v01 = Hash(ix, iy + 1);
        var v11 = Hash(ix + 1, iy + 1);

        var vx0 = Lerp(v00, v10, fx);
        var vx1 = Lerp(v01, v11, fx);
        return Lerp(vx0, vx1, fy);
    }

    private static float Hash(int x, int y)
    {
        var n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        n = n ^ (n >> 16);
        return (n & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
