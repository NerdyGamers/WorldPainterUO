using Ultima.Helpers;

namespace WorldPainterUO.FileFormats.Uop;

/// <summary>
/// Low-level UOP container format definitions and helpers for the per-block
/// map format (as read by the Ultima SDK's TileMatrix).
///
/// Entry name pattern: <c>build/{pattern}/{index:D8}.dat</c>
/// Hash function: <see cref="UopUtils.HashFileName"/> (from the Ultima SDK)
/// </summary>
internal static class UopFormat
{
    /// <summary>UOP file signature bytes (0x50594D = "MYNP" little-endian).</summary>
    public const uint Signature = 0x50594D;

    /// <summary>UOP container version.</summary>
    public const int Version = 5;

    /// <summary>Size of the UOP file header in bytes.</summary>
    public const int HeaderSize = 40;

    /// <summary>Maximum entries per hash table block.</summary>
    public const int DefaultBlockSize = 100;

    /// <summary>Size of each hash table entry in bytes (per-block format).</summary>
    public const int PerEntrySize = 34;

    /// <summary>Size of each hash table block header in bytes (entryCount + nextBlock).</summary>
    public const int PerBlockHeaderSize = 12;

    /// <summary>Raw size of one land tile data block (64 tiles x 3 bytes).</summary>
    public const int LandBlockDataSize = 192;

    /// <summary>
    /// Delegates to <see cref="UopUtils.HashFileName"/> from the Ultima SDK.
    /// </summary>
    public static ulong HashFileName(string name) => UopUtils.HashFileName(name);

    /// <summary>
    /// Calculates the number of hash table blocks needed for a given
    /// number of entries.
    /// </summary>
    public static int BlockCount(int entryCount) =>
        (entryCount + DefaultBlockSize - 1) / DefaultBlockSize;

    /// <summary>
    /// Calculates the total size of the hash table in bytes.
    /// </summary>
    public static int HashTableSize(int entryCount)
    {
        var blocks = BlockCount(entryCount);
        var size = 0;
        var remaining = entryCount;

        for (var b = 0; b < blocks; b++)
        {
            var entriesInBlock = Math.Min(DefaultBlockSize, remaining);
            size += PerBlockHeaderSize + entriesInBlock * PerEntrySize;
            remaining -= entriesInBlock;
        }

        return size;
    }

    /// <summary>
    /// Entry name for a land block in per-block UOP format.
    /// </summary>
    public static string BlockEntryName(string pattern, int blockIndex) =>
        $"build/{pattern}/{blockIndex:D8}.dat";
}
