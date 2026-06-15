using System.Text;

namespace WorldPainterUO.FileFormats.Uop;

/// <summary>
/// Low-level UOP container format definitions and helpers.
///
/// FORMAT ASSUMPTIONS:
/// The UOP (Ultima Online Package) format is a container that wraps one or
/// more files (typically MUL data) into a single archive.  The format details
/// are partially reverse-engineered from the UO client and community tools.
///
/// Known uncertainties:
/// - The exact header layout may vary between UOP versions.  We assume a
///   40-byte header common in version-5 UOP containers.
/// - The hash function used for entry lookup is a 32-bit rolling hash with
///   multiplier 0x0103.  Upper 32 bits of the stored ulong are zero.
/// - The hash-table block size is typically 100 entries per block.
/// - Data within the UOP may be compressed (zlib/deflate) or raw.
///   We assume raw (uncompressed) storage for the legacy MUL wrapper.
/// - No checksum or integrity validation is performed on the container.
///
/// These assumptions are sufficient for self-consistent round-trip (write then
/// read back).  Compatibility with UOP files produced by the official UO client
/// or third-party tools may require adjustments.
/// </summary>
internal static class UopFormat
{
    public const int HeaderSize = 40;
    public const int DefaultBlockSize = 100;
    public const int EntrySize = 24;
    public const int BlockHeaderSize = 8; // nextBlock (4) + entryCount (4)
    public const int Signature = 0;
    public const int Version = 5;

    /// <summary>
    /// Hash function for UOP internal file paths.  Lowercased, forward-slash
    /// normalized, then a 32-bit rolling hash with multiplier 0x0103.
    /// </summary>
    public static ulong HashFileName(string name)
    {
        name = name.ToLowerInvariant().Replace('\\', '/');

        if (name.Length > 0 && name[0] == '/')
            name = name[1..];

        ulong hash = 0;

        foreach (var c in name)
        {
            hash = (hash * 0x0103) + c;
            hash &= 0xFFFFFFFF;
        }

        return hash;
    }

    /// <summary>
    /// The internal path used for the legacy MUL map data inside the UOP.
    /// </summary>
    public static string MapDataPath(int mapIndex) =>
        $"build/map/{mapIndex}/legacymul";

    /// <summary>
    /// Calculates the byte offset where file data begins, based on the
    /// header size and the number of hash table blocks.
    /// </summary>
    public static int DataOffset(int blockCount) =>
        HeaderSize + blockCount * (BlockHeaderSize + DefaultBlockSize * EntrySize);

    /// <summary>
    /// Calculates the number of hash table blocks needed for a given
    /// number of files.
    /// </summary>
    public static int BlockCount(int fileCount) =>
        (fileCount + DefaultBlockSize - 1) / DefaultBlockSize;
}
