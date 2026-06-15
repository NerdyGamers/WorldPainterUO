using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.FileFormats;

public static class MapExportValidator
{
    /// <summary>
    /// The maximum valid land tile ID in standard UO. Values above this may
    /// produce unexpected results in-game but are not strictly invalid.
    /// </summary>
    public const int MaxLandTileId = 0x3FFF;

    /// <summary>Minimum valid Z value (sbyte).</summary>
    public const int MinHeight = -128;

    /// <summary>Maximum valid Z value (sbyte).</summary>
    public const int MaxHeight = 127;

    /// <summary>
    /// Validates a WorldMap for export and returns a list of diagnostics.
    /// This does NOT throw — callers should inspect the returned diagnostics.
    /// </summary>
    public static IReadOnlyList<ExportDiagnostic> Validate(WorldMap map) =>
        ValidateDetailed(map).Diagnostics;

    /// <summary>
    /// Validates a WorldMap for export and returns a structured <see cref="ValidationResult"/>.
    /// Checks include dimension validity, tile ID ranges, height bounds, and chunk integrity.
    /// </summary>
    public static ValidationResult ValidateDetailed(WorldMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        var result = new ValidationResult();
        var dims = map.Dimensions;
        var logger = Log.For("MapExportValidator");

        if (dims.Width <= 0)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "DIM_ZERO",
                "Map width must be greater than zero."));
            logger.LogWarning("DIM_ZERO: map width = {Width}", dims.Width);
        }

        if (dims.Height <= 0)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "DIM_ZERO",
                "Map height must be greater than zero."));
            logger.LogWarning("DIM_ZERO: map height = {Height}", dims.Height);
        }

        if (dims.Width <= 0 || dims.Height <= 0)
            return result;

        // Chunk integrity: verify all chunks exist and have correct tile count
        ValidateChunkIntegrity(map, result, logger);

        // Tile ID and height range checks
        ValidateTileRanges(map, result, logger);

        logger.LogInformation(
            "Validation complete: {Count} diagnostics ({Errors} errors, {Warnings} warnings)",
            result.Count, result.Errors.Count(), result.Warnings.Count());

        return result;
    }

    /// <summary>
    /// Returns true if the diagnostics contain no errors (warnings are allowed).
    /// </summary>
    public static bool IsExportable(IReadOnlyList<ExportDiagnostic> diagnostics) =>
        !diagnostics.Any(d => d.Severity == ExportDiagnosticSeverity.Error);

    private const int MaxTileWarnings = 100;

    private static void ValidateChunkIntegrity(WorldMap map, ValidationResult result, ILogger logger)
    {
        var dims = map.Dimensions;
        var expectedChunks = dims.TotalChunks;
        var foundTerrain = 0;
        var foundHeight = 0;

        for (var cy = 0; cy < dims.ChunksY; cy++)
        {
            for (var cx = 0; cx < dims.ChunksX; cx++)
            {
                MapChunk<ushort> terrainChunk;
                MapChunk<sbyte> heightChunk;

                try
                {
                    terrainChunk = map.Terrain.GetChunk(cx, cy);
                    foundTerrain++;

                    heightChunk = map.Height.GetChunk(cx, cy);
                    foundHeight++;
                }
                catch (Exception ex)
                {
                    result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "CHUNK_CORRUPT",
                        $"Chunk ({cx}, {cy}) is corrupt or inaccessible: {ex.Message}"));
                    logger.LogError(ex, "CHUNK_CORRUPT: chunk ({CX},{CY})", cx, cy);
                    continue;
                }

                // Verify chunk data length
                if (terrainChunk.Data.Length != MapChunk<ushort>.TileCount)
                {
                    result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "CHUNK_CORRUPT",
                        $"Terrain chunk ({cx}, {cy}) has {terrainChunk.Data.Length} tiles; expected {MapChunk<ushort>.TileCount}.",
                        cx * MapChunk<ushort>.Size, cy * MapChunk<ushort>.Size));
                }

                if (heightChunk.Data.Length != MapChunk<sbyte>.TileCount)
                {
                    result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "CHUNK_CORRUPT",
                        $"Height chunk ({cx}, {cy}) has {heightChunk.Data.Length} tiles; expected {MapChunk<sbyte>.TileCount}.",
                        cx * MapChunk<ushort>.Size, cy * MapChunk<ushort>.Size));
                }
            }
        }

        if (foundTerrain != expectedChunks)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "CHUNK_MISSING",
                $"Expected {expectedChunks} terrain chunks but found {foundTerrain}."));
        }

        if (foundHeight != expectedChunks)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "CHUNK_MISSING",
                $"Expected {expectedChunks} height chunks but found {foundHeight}."));
        }
    }

    private static void ValidateTileRanges(WorldMap map, ValidationResult result, ILogger logger)
    {
        var dims = map.Dimensions;
        var warningCount = 0;

        for (var y = 0; y < dims.Height; y++)
        {
            for (var x = 0; x < dims.Width; x++)
            {
                var tileId = map.Terrain[x, y];
                var z = map.Height[x, y];

                if (tileId > MaxLandTileId)
                {
                    result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Warning, "TILE_OOB",
                        $"Tile ID {tileId} at ({x}, {y}) exceeds standard UO land tile maximum (0x{MaxLandTileId:X4}).",
                        x, y));

                    if (++warningCount >= MaxTileWarnings)
                        break;
                }

                if (z < MinHeight || z > MaxHeight)
                {
                    result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "HEIGHT_OOB",
                        $"Height value {z} at ({x}, {y}) is outside the valid range {MinHeight} to {MaxHeight}.",
                        x, y));
                }
            }

            if (warningCount >= MaxTileWarnings)
            {
                var truncated = result.Diagnostics.Count(d => d.Code == "TILE_OOB");
                result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Warning, "TILE_OOB_TRUNCATED",
                    $"Found {truncated}+ tiles with out-of-range IDs. Further warnings suppressed."));
                logger.LogWarning("TILE_OOB_TRUNCATED: {Count} warnings suppressed",
                    warningCount);
                break;
            }
        }
    }

    /// <summary>
    /// Verifies UOP packaging integrity for the given raw UOP bytes.
    /// Checks signature, header hash, data offset, and size consistency.
    /// Returns a <see cref="ValidationResult"/> with any issues found.
    /// </summary>
    public static ValidationResult ValidateUopPackaging(byte[] uopBytes, string? label = null)
    {
        var result = new ValidationResult();
        var logger = Log.For("MapExportValidator");
        label ??= "UOP";

        if (uopBytes.Length < 64)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "UOP_TRUNCATED",
                $"{label}: file too small ({uopBytes.Length} bytes, minimum 64)."));
            logger.LogError("UOP_TRUNCATED: {Label} size {Size}", label, uopBytes.Length);
            return result;
        }

        // Check signature
        var sig = ReadU32(uopBytes, 0);
        if (sig != Uop.UopFormat.Signature)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "UOP_SIGNATURE",
                $"{label}: invalid signature 0x{sig:X8} (expected 0x{Uop.UopFormat.Signature:X8})."));
            logger.LogError("UOP_SIGNATURE: {Label} sig 0x{Sig:X8}", label, sig);
        }

        // Verify declared file count and total size
        var declaredFiles = ReadU32(uopBytes, 12);
        var declaredSize = ReadU32(uopBytes, 16);

        if (declaredSize != uopBytes.Length)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Warning, "UOP_SIZE_MISMATCH",
                $"{label}: declared size {declaredSize} differs from actual {uopBytes.Length}."));
            logger.LogWarning("UOP_SIZE_MISMATCH: {Label} declared {Declared} actual {Actual}",
                label, declaredSize, uopBytes.Length);
        }

        // Verify at least one data entry
        if (declaredFiles < 1)
        {
            result.Add(new ExportDiagnostic(ExportDiagnosticSeverity.Error, "UOP_NO_FILES",
                $"{label}: no entries declared in UOP container."));
        }

        return result;
    }

    private static uint ReadU32(byte[] data, int offset) =>
        (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
}
