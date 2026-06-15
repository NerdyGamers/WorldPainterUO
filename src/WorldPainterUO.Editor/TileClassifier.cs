namespace WorldPainterUO.Editor;

/// <summary>
/// Classifies UO land tile IDs into named biomes, using
/// tiledata.mul flags and radarcol.mul colors when available.
/// Falls back to well-known tile ID ranges when files are absent.
/// </summary>
public static class TileClassifier
{
    private static readonly Lazy<Dictionary<ushort, string>> DefaultClassMap = new(BuildDefaultClassMap);

    /// <summary>
    /// Gets the biome name for a tile ID using the default classification.
    /// </summary>
    public static string Classify(ushort tileId)
    {
        return DefaultClassMap.Value.TryGetValue(tileId, out var biome)
            ? biome
            : ClassifyByRange(tileId);
    }

    /// <summary>
    /// Builds a complete biome map for all 0x4000 land tiles using
    /// data from tiledata.mul and radarcol.mul. Falls back to range-based
    /// defaults for tiles that cannot be classified from the source files.
    /// </summary>
    public static Dictionary<ushort, string> BuildBiomeMap(
        string? tiledataPath, string? radarcolPath)
    {
        var map = new Dictionary<ushort, string>();

        RgbaColor[]? colors = null;
        TileFlagsEntry[]? flags = null;

        if (tiledataPath is not null && File.Exists(tiledataPath))
            flags = ReadTiledataMul(tiledataPath);

        if (radarcolPath is not null && File.Exists(radarcolPath))
            colors = ReadRadarColMul(radarcolPath);

        for (ushort id = 0; id < 0x4000; id++)
        {
            var flagEntry = flags is not null && id < flags.Length ? flags[id] : (TileFlagsEntry?)null;
            var colorEntry = colors is not null && id < colors.Length ? colors[id] : (RgbaColor?)null;
            var biome = ClassifyFromData(id, flagEntry, colorEntry);
            map[id] = biome;
        }

        return map;
    }

    /// <summary>Biome names returned by classification.</summary>
    public static string[] BiomeNames =>
        ["Ocean", "Grass", "Forest", "Swamp", "Snow", "Desert",
         "Mountain", "Volcanic", "Marsh", "Road", "Rock"];

    #region tiledata.mul reader

    /// <summary>Parsed tiledata.mul land tile entry.</summary>
    public readonly record struct TileFlagsEntry(uint Flags, string Name);

    /// <summary>
    /// Reads land tile entries from tiledata.mul (legacy format).
    /// Each group: 4-byte header + 32 × 26-byte entries.
    /// Total: 512 groups covering 0x0000-0x3FFF.
    /// </summary>
    public static TileFlagsEntry[] ReadTiledataMul(string path)
    {
        if (!File.Exists(path))
            return [];

        const int groupSize = 32;
        const int entrySize = 26;
        const int groupHeader = 4;
        const int totalGroups = 0x4000 / groupSize; // 512
        const int expectedSize = totalGroups * (groupHeader + groupSize * entrySize);

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < expectedSize)
            return [];

        var result = new TileFlagsEntry[0x4000];

        for (var g = 0; g < totalGroups; g++)
        {
            var groupOffset = g * (groupHeader + groupSize * entrySize) + groupHeader;

            for (var i = 0; i < groupSize; i++)
            {
                var entryOffset = groupOffset + i * entrySize;
                if (entryOffset + entrySize > bytes.Length)
                    break;

                var flags = (uint)(bytes[entryOffset] |
                                   (bytes[entryOffset + 1] << 8) |
                                   (bytes[entryOffset + 2] << 16) |
                                   (bytes[entryOffset + 3] << 24));

                // Name is 20 bytes, null-terminated
                var nameBytes = bytes.AsSpan(entryOffset + 6, 20);
                var nullIdx = nameBytes.IndexOf((byte)0);
                var name = nullIdx >= 0
                    ? System.Text.Encoding.ASCII.GetString(nameBytes[..nullIdx])
                    : System.Text.Encoding.ASCII.GetString(nameBytes);

                var tileId = (ushort)(g * groupSize + i);
                result[tileId] = new TileFlagsEntry(flags, name);
            }
        }

        return result;
    }

    #endregion

    #region radarcol.mul reader

    /// <summary>Simple RGBA color for radarcol classification.</summary>
    public readonly record struct RgbaColor(byte R, byte G, byte B, byte A);

    /// <summary>
    /// Reads land tile colors from radarcol.mul.
    /// File contains 0x4000 × 4-byte BGRA colors.
    /// </summary>
    public static RgbaColor[] ReadRadarColMul(string path)
    {
        if (!File.Exists(path))
            return [];

        var bytes = File.ReadAllBytes(path);
        var count = Math.Min(bytes.Length / 4, 0x4000);
        var colors = new RgbaColor[0x4000];

        for (var i = 0; i < count; i++)
        {
            var offset = i * 4;
            colors[i] = new RgbaColor(
                bytes[offset + 2], // R
                bytes[offset + 1], // G
                bytes[offset],     // B
                bytes[offset + 3]  // A
            );
        }

        return colors;
    }

    #endregion

    #region Classification logic

    private static string ClassifyFromData(ushort id, TileFlagsEntry? flags, RgbaColor? color)
    {
        if (flags.HasValue)
        {
            var f = flags.Value.Flags;
            var name = flags.Value.Name;

            // Water (Wet flag 0x01)
            if ((f & 0x01) != 0)
                return "Ocean";

            // Lava tiles (specific tile IDs)
            if (id is >= 0x001E and <= 0x0023)
                return "Volcanic";

            // Swamp (Foliage flag 0x20000 on specific low tiles)
            if ((f & 0x20000) != 0 && id is >= 0x00C0 and <= 0x00FF)
                return "Swamp";

            // Road/Path tiles by name
            if (name.Contains("path", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("road", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("cobble", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("stone", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("pavement", StringComparison.OrdinalIgnoreCase))
                return "Road";

            // Snow by name or flag
            if (name.Contains("snow", StringComparison.OrdinalIgnoreCase) ||
                (color.HasValue && IsSnowColor(color.Value)))
                return "Snow";

            // Sand/Desert by name
            if (name.Contains("sand", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("dune", StringComparison.OrdinalIgnoreCase) ||
                (color.HasValue && IsDesertColor(color.Value)))
                return "Desert";

            // Rock/Mountain by flags
            if ((f & 0x40) != 0 && (f & 0x400) != 0) // Impassable + Surface
            {
                // Higher rock/mountain tiles
                if (id is >= 0x0035 and <= 0x0040 or >= 0x01AC and <= 0x01AF)
                    return "Mountain";
                return "Rock";
            }

            // Forest (Foliage flag)
            if ((f & 0x20000) != 0)
                return "Forest";

            // Marsh
            if (name.Contains("swamp", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("marsh", StringComparison.OrdinalIgnoreCase) ||
                id is >= 0x00D8 and <= 0x00EC)
                return "Marsh";
        }

        return ClassifyByRange(id);
    }

    private static string ClassifyByRange(ushort id)
    {
        // Order from most specific to most general (last match wins in switch,
        // but we use distinct, non-overlapping ranges).
        return id switch
        {
            >= 0x0003 and <= 0x0006 => "Grass",
            >= 0x0008 and <= 0x0009 => "Grass",
            >= 0x000C and <= 0x0010 => "Forest",
            >= 0x0012 and <= 0x0015 => "Desert",
            >= 0x001E and <= 0x0023 => "Volcanic",
            >= 0x0035 and <= 0x003B => "Mountain",
            >= 0x003C and <= 0x0040 => "Rock",
            >= 0x0071 and <= 0x0076 => "Road",
            >= 0x00A4 and <= 0x00A8 => "Ocean",
            >= 0x00C4 and <= 0x00D7 => "Forest",
            >= 0x00D8 and <= 0x00EC => "Marsh",
            >= 0x00E8 and <= 0x00EF => "Swamp",
            >= 0x01AC and <= 0x01AF => "Snow",
            // Ocean (water tiles below 0xA4, individual known IDs)
            0x0000 or 0x0001 or 0x0002 => "Ocean",
            _ => "Grass",
        };
    }

    private static bool IsSnowColor(RgbaColor c) =>
        c.R > 180 && c.G > 180 && c.B > 180;

    private static bool IsDesertColor(RgbaColor c) =>
        c.R > 160 && c.G > 140 && c.B < 100;

    private static Dictionary<ushort, string> BuildDefaultClassMap()
    {
        // Precise known tile IDs to biome mapping
        return new Dictionary<ushort, string>
        {
            // Water
            [0x0000] = "Ocean", [0x0001] = "Ocean", [0x0002] = "Ocean",
            [0x00A4] = "Ocean", [0x00A5] = "Ocean", [0x00A6] = "Ocean",
            [0x00A7] = "Ocean", [0x00A8] = "Ocean",

            // Grass
            [0x0003] = "Grass", [0x0004] = "Grass", [0x0005] = "Grass", [0x0006] = "Grass",
            [0x0008] = "Grass", [0x0009] = "Grass",  // beach edges

            // Forest
            [0x000C] = "Forest", [0x000D] = "Forest", [0x000E] = "Forest",
            [0x000F] = "Forest", [0x0010] = "Forest",

            // Desert
            [0x0012] = "Desert", [0x0013] = "Desert", [0x0014] = "Desert", [0x0015] = "Desert",

            // Volcanic
            [0x001E] = "Volcanic", [0x001F] = "Volcanic",
            [0x0020] = "Volcanic", [0x0021] = "Volcanic",
            [0x0022] = "Volcanic", [0x0023] = "Volcanic",

            // Mountain
            [0x0035] = "Mountain", [0x0036] = "Mountain", [0x0037] = "Mountain",
            [0x0038] = "Mountain", [0x0039] = "Mountain",

            // Rock
            [0x003C] = "Rock", [0x003D] = "Rock", [0x003E] = "Rock",
            [0x003F] = "Rock", [0x0040] = "Rock",

            // Road
            [0x0071] = "Road", [0x0072] = "Road", [0x0073] = "Road",
            [0x0074] = "Road", [0x0075] = "Road", [0x0076] = "Road",

            // Swamp
            [0x00D8] = "Swamp", [0x00D9] = "Swamp", [0x00DA] = "Swamp",
            [0x00DB] = "Swamp", [0x00DC] = "Swamp",

            // Marsh
            [0x00E8] = "Marsh", [0x00E9] = "Marsh", [0x00EA] = "Marsh",
            [0x00EB] = "Marsh", [0x00EC] = "Marsh",

            // Snow
            [0x01AC] = "Snow", [0x01AD] = "Snow", [0x01AE] = "Snow", [0x01AF] = "Snow",
        };
    }

    #endregion
}
