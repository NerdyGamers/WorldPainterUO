using SkiaSharp;

namespace WorldPainterUO.Rendering;

/// <summary>
/// Provides per-tile visual data for rendering.
/// The default implementation generates procedural textures;
/// a future implementation may read from art.mul / artLegacyMUL.uop.
/// </summary>
public interface ITileTextureProvider
{
    /// <summary>Gets the 44×44 land tile texture (null if using fallback rendering).</summary>
    SKBitmap? GetLandTileTexture(ushort tileId);

    /// <summary>True if real artwork is available (vs. fallback).</summary>
    bool HasArtwork { get; }
}
