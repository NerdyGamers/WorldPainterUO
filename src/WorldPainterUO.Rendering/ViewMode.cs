namespace WorldPainterUO.Rendering;

/// <summary>Viewport rendering mode.</summary>
public enum ViewMode
{
    /// <summary>Color-coded terrain by tile ID (simulates radarcol.mul).</summary>
    Radar,

    /// <summary>Actual UO land tile artwork (falls back to radar if art unavailable).</summary>
    Terrain,

    /// <summary>Terrain artwork with radar-color tint overlay.</summary>
    Hybrid,
}
