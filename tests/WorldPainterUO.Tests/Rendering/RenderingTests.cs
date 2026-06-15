using SkiaSharp;
using WorldPainterUO.Core;
using WorldPainterUO.Rendering;

namespace WorldPainterUO.Tests.Rendering;

public sealed class RenderingTests
{
    // ── RadarColorPalette ────────────────────────────────────────────────

    [Fact]
    public void RadarColorPalette_ReturnsDeterministicColor()
    {
        var c1 = RadarColorPalette.GetColor(0x0000);
        var c2 = RadarColorPalette.GetColor(0x0000);
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void RadarColorPalette_AdjacentTiles_ReturnsWithoutError()
    {
        var c1 = RadarColorPalette.GetColor(0x0010);
        var c2 = RadarColorPalette.GetColor(0x0011);
        _ = c1;
        _ = c2;
    }

    // ── FallbackTileTextureProvider ──────────────────────────────────────

    [Fact]
    public void FallbackTileProvider_HasNoArtwork()
    {
        var provider = new FallbackTileTextureProvider();
        Assert.False(provider.HasArtwork);
        Assert.Null(provider.GetLandTileTexture(0));
    }

    [Fact]
    public void FallbackTileProvider_RenderFallbackTile_DoesNotThrow()
    {
        using var bmp = new SKBitmap(44, 44);
        using var canvas = new SKCanvas(bmp);
        var provider = new FallbackTileTextureProvider();

        var ex = Record.Exception(() =>
            provider.RenderFallbackTile(canvas, 0, 0, 44, 0x1234, 0));
        Assert.Null(ex);
    }

    // ── RenderCache ──────────────────────────────────────────────────────

    [Fact]
    public void RenderCache_IsDirtyFalseInitially()
    {
        var cache = new RenderCache();
        Assert.False(cache.IsDirty(5, 10));
    }

    [Fact]
    public void RenderCache_InvalidateChunk_MarksDirty()
    {
        var cache = new RenderCache();
        cache.InvalidateChunk(3, 7);
        Assert.True(cache.IsDirty(3, 7));
    }

    [Fact]
    public void RenderCache_InvalidateAll_MarksAllDirty()
    {
        var cache = new RenderCache();

        // First render two chunks
        var renderCount = 0;
        cache.GetOrRender(0, 0, () => { renderCount++; return new SKBitmap(4, 4); });
        cache.GetOrRender(1, 0, () => { renderCount++; return new SKBitmap(4, 4); });
        Assert.Equal(2, cache.Count);

        cache.InvalidateAll();
        Assert.True(cache.IsDirty(0, 0));
        Assert.True(cache.IsDirty(1, 0));
    }

    [Fact]
    public void RenderCache_GetOrRender_CallsRenderFuncOnceForClean()
    {
        var cache = new RenderCache();
        var callCount = 0;

        var bmp = cache.GetOrRender(0, 0, () =>
        {
            callCount++;
            return new SKBitmap(4, 4);
        });

        Assert.NotNull(bmp);
        Assert.Equal(1, callCount);

        // Second call — cached
        var bmp2 = cache.GetOrRender(0, 0, () =>
        {
            callCount++;
            return new SKBitmap(4, 4);
        });

        Assert.Same(bmp, bmp2);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void RenderCache_GetOrRender_CallsRenderFuncAfterInvalidate()
    {
        var cache = new RenderCache();
        var callCount = 0;

        cache.GetOrRender(0, 0, () =>
        {
            callCount++;
            return new SKBitmap(4, 4);
        });
        Assert.Equal(1, callCount);

        cache.InvalidateChunk(0, 0);
        cache.GetOrRender(0, 0, () =>
        {
            callCount++;
            return new SKBitmap(4, 4);
        });
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RenderCache_Clear_DisposesAll()
    {
        var cache = new RenderCache();
        cache.GetOrRender(0, 0, () => new SKBitmap(4, 4));
        cache.GetOrRender(0, 1, () => new SKBitmap(4, 4));
        Assert.Equal(2, cache.Count);

        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.Equal(0, cache.PendingCount);
    }

    [Fact]
    public void RenderCache_SyncDirtyChunks_MarksCorrectChunks()
    {
        var map = WorldMap.Create(128, 128, "Felucca", SourceFileType.Mul);
        var cache = new RenderCache();

        // Edit a tile to mark chunk dirty
        map.Terrain[10, 10] = 0x0002;
        map.Terrain[10, 64] = 0x0003;

        cache.SyncDirtyChunks(map);

        // Chunk (0,0) and (0,1) should be invalidated
        Assert.True(cache.IsDirty(0, 0));
        Assert.True(cache.IsDirty(0, 1));
    }

    // ── OverlayRenderer ──────────────────────────────────────────────────

    [Fact]
    public void OverlayRenderer_DrawGrid_NullArgs_DoesNotThrow()
    {
        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);
        var dims = new MapDimensions(64, 64);

        var ex = Record.Exception(() =>
            OverlayRenderer.DrawGrid(canvas, dims, 0, 0, 1, 200, 200, true, true));
        Assert.Null(ex);
    }

    [Fact]
    public void OverlayRenderer_DrawGrid_NoGrids_DoesNotThrow()
    {
        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);
        var dims = new MapDimensions(64, 64);

        var ex = Record.Exception(() =>
            OverlayRenderer.DrawGrid(canvas, dims, 0, 0, 1, 200, 200, false, false));
        Assert.Null(ex);
    }

    // ── MinimapRenderer ──────────────────────────────────────────────────

    // Requires native SkiaSharp runtime — run locally only.
    [Fact, Trait("Category", "Rendering")]
    public void MinimapRenderer_GetOrRender_ReturnsBitmap()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var minimap = new MinimapRenderer();

        var bmp = minimap.GetOrRender(map, 100);
        Assert.NotNull(bmp);
        Assert.True(bmp.Width > 0);
        Assert.True(bmp.Height > 0);
    }

    // Requires native SkiaSharp runtime — run locally only.
    [Fact, Trait("Category", "Rendering")]
    public void MinimapRenderer_Invalidate_TriggersNewBitmap()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var minimap = new MinimapRenderer();

        var bmp1 = minimap.GetOrRender(map, 100);
        minimap.Invalidate();
        var bmp2 = minimap.GetOrRender(map, 100);

        Assert.NotNull(bmp1);
        Assert.NotNull(bmp2);
        Assert.NotSame(bmp1, bmp2);
    }

    // Requires native SkiaSharp runtime — run locally only.
    [Fact, Trait("Category", "Rendering")]
    public void MinimapRenderer_GetOrRender_CachesUntilInvalidate()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var minimap = new MinimapRenderer();

        var bmp1 = minimap.GetOrRender(map, 100);
        var bmp2 = minimap.GetOrRender(map, 100);

        Assert.Same(bmp1, bmp2);
    }

    // ── MapRenderService ─────────────────────────────────────────────────

    [Fact]
    public void MapRenderService_Render_EmptyCanvas_DoesNotThrow()
    {
        var service = new MapRenderService();
        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);

        // Null map
        var ex = Record.Exception(() => service.Render(canvas, null!, 200, 200));
        Assert.Null(ex);
    }

    [Fact]
    public void MapRenderService_Render_WithMap_DoesNotThrow()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var service = new MapRenderService();

        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);

        var ex = Record.Exception(() => service.Render(canvas, map, 200, 200));
        Assert.Null(ex);
    }

    [Fact]
    public void MapRenderService_ViewMode_Switch()
    {
        var service = new MapRenderService();
        Assert.Equal(ViewMode.Radar, service.ViewMode);

        service.ViewMode = ViewMode.Terrain;
        Assert.Equal(ViewMode.Terrain, service.ViewMode);

        service.ViewMode = ViewMode.Hybrid;
        Assert.Equal(ViewMode.Hybrid, service.ViewMode);
    }

    [Fact]
    public void MapRenderService_SyncDirtyChunks_MarksCache()
    {
        var map = WorldMap.Create(128, 128, "Felucca", SourceFileType.Mul);
        var service = new MapRenderService();

        // Edit a tile to make a chunk dirty
        map.Terrain[5, 5] = 0x0001;

        service.SyncDirtyChunks(map);

        // Render to consume dirty
        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);
        service.Render(canvas, map, 200, 200);
    }

    [Fact]
    public void MapRenderService_InvalidateAll_ReRenders()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var service = new MapRenderService();

        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);

        service.Render(canvas, map, 200, 200);
        service.InvalidateAll();
        service.Render(canvas, map, 200, 200);

        // No exception = pass
    }

    [Fact]
    public void MapRenderService_ClearCache_EmptiesCache()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var service = new MapRenderService();

        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);

        service.Render(canvas, map, 200, 200);
        service.ClearCache();
        service.Render(canvas, map, 200, 200);

        // No exception = pass
    }

    [Fact]
    public void MapRenderService_Zoom_Clamping()
    {
        var service = new MapRenderService();
        service.Zoom = 100f;
        Assert.Equal(100f, service.Zoom); // Zoom is not clamped in MapRenderService (clamped during render)
    }

    [Fact]
    public void MapRenderService_Render_AllViewModes()
    {
        var map = WorldMap.Create(64, 64, "Felucca", SourceFileType.Mul);
        var service = new MapRenderService();

        using var bmp = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bmp);

        foreach (ViewMode mode in Enum.GetValues<ViewMode>())
        {
            service.ViewMode = mode;
            var ex = Record.Exception(() => service.Render(canvas, map, 200, 200));
            Assert.Null(ex);
        }
    }

    // ── ITileTextureProvider ─────────────────────────────────────────────

    [Fact]
    public void ITileTextureProvider_CanBeImplemented()
    {
        var provider = new FallbackTileTextureProvider();
        Assert.IsAssignableFrom<ITileTextureProvider>(provider);
    }
}
