using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    // ============================================================
    // UO-style Color Palette
    // ============================================================
    static class C
    {
        public static readonly SKColor Outline   = SKColor.Parse("#2D2216");
        public static readonly SKColor Khaki     = SKColor.Parse("#C4B28A");
        public static readonly SKColor KhakiD    = SKColor.Parse("#A8966E");
        public static readonly SKColor Grass     = SKColor.Parse("#5B8C3E");
        public static readonly SKColor GrassL    = SKColor.Parse("#7CB454");
        public static readonly SKColor GrassD    = SKColor.Parse("#3D6B26");
        public static readonly SKColor Earth     = SKColor.Parse("#8B6B42");
        public static readonly SKColor EarthL    = SKColor.Parse("#A88555");
        public static readonly SKColor EarthD    = SKColor.Parse("#6B4F2E");
        public static readonly SKColor Stone     = SKColor.Parse("#8A8A8A");
        public static readonly SKColor StoneL    = SKColor.Parse("#A5A5A5");
        public static readonly SKColor StoneD    = SKColor.Parse("#6B6B6B");
        public static readonly SKColor Gold      = SKColor.Parse("#D4A017");
        public static readonly SKColor Amber     = SKColor.Parse("#C8961E");
        public static readonly SKColor Silver    = SKColor.Parse("#B0B0B0");
        public static readonly SKColor Water     = SKColor.Parse("#3B6E8C");
        public static readonly SKColor WaterL    = SKColor.Parse("#5A8EB0");
        public static readonly SKColor Cyan      = SKColor.Parse("#00B5B5");
        public static readonly SKColor Skin      = SKColor.Parse("#D4A574");
        public static readonly SKColor SkinD     = SKColor.Parse("#B8895E");
        public static readonly SKColor Wood      = SKColor.Parse("#8B6914");
        public static readonly SKColor WoodD     = SKColor.Parse("#6B4F10");
        public static readonly SKColor Iron      = SKColor.Parse("#705050");
        public static readonly SKColor IronL     = SKColor.Parse("#8A6A6A");
        public static readonly SKColor Red       = SKColor.Parse("#A03030");
        public static readonly SKColor Crimson   = SKColor.Parse("#CC3333");
        public static readonly SKColor Orange    = SKColor.Parse("#CC7722");
        public static readonly SKColor White     = SKColor.Parse("#E8E0D0");
        public static readonly SKColor Bone      = SKColor.Parse("#D4C8A8");
        public static readonly SKColor Mud       = SKColor.Parse("#5C4A32");
    }

    // ============================================================
    // Drawing Helpers
    // ============================================================
    static void Fill(SKCanvas c, SKColor color)
    {
        using var p = new SKPaint { Color = color, IsAntialias = false, Style = SKPaintStyle.Fill };
        c.DrawRect(0, 0, 36, 36, p);
    }

    static void FCircle(SKCanvas c, float cx, float cy, float r, SKColor fill)
    {
        using var p = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        c.DrawCircle(cx, cy, r, p);
    }

    static void SCircle(SKCanvas c, float cx, float cy, float r, SKColor stroke, float sw = 1.5f)
    {
        using var p = new SKPaint { Color = stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw };
        c.DrawCircle(cx, cy, r, p);
    }

    static void FRect(SKCanvas c, float x, float y, float w, float h, SKColor fill)
    {
        using var p = new SKPaint { Color = fill, IsAntialias = false, Style = SKPaintStyle.Fill };
        c.DrawRect(x, y, w, h, p);
    }

    static void SRect(SKCanvas c, float x, float y, float w, float h, SKColor stroke, float sw = 1.5f)
    {
        using var p = new SKPaint { Color = stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw };
        c.DrawRect(x, y, w, h, p);
    }

    static void FPath(SKCanvas c, SKPoint[] pts, SKColor fill)
    {
        using var path = new SKPath();
        path.MoveTo(pts[0]);
        for (int i = 1; i < pts.Length; i++) path.LineTo(pts[i]);
        path.Close();
        using var p = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        c.DrawPath(path, p);
    }

    static void SPath(SKCanvas c, SKPoint[] pts, SKColor stroke, float sw = 1.5f)
    {
        using var path = new SKPath();
        path.MoveTo(pts[0]);
        for (int i = 1; i < pts.Length; i++) path.LineTo(pts[i]);
        using var p = new SKPaint { Color = stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        c.DrawPath(path, p);
    }

    static void Line(SKCanvas c, float x1, float y1, float x2, float y2, SKColor stroke, float sw = 1.5f)
    {
        using var p = new SKPaint { Color = stroke, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = sw, StrokeCap = SKStrokeCap.Round };
        c.DrawLine(x1, y1, x2, y2, p);
    }

    static void ArrowUp(SKCanvas c, float cx, float cy, float sz, SKColor fill, SKColor outline)
    {
        float h = sz * 0.5f;
        var pts = new SKPoint[] { new(cx, cy - h), new(cx - h, cy + h * 0.4f), new(cx + h, cy + h * 0.4f) };
        FPath(c, pts, fill);
        SPath(c, pts, outline, 1.5f);
    }

    static void ArrowDown(SKCanvas c, float cx, float cy, float sz, SKColor fill, SKColor outline)
    {
        float h = sz * 0.5f;
        var pts = new SKPoint[] { new(cx, cy + h), new(cx - h, cy - h * 0.4f), new(cx + h, cy - h * 0.4f) };
        FPath(c, pts, fill);
        SPath(c, pts, outline, 1.5f);
    }

    // ============================================================
    // Toolbar Icons (36x36)
    // ============================================================
    static void IconPan(SKCanvas c, int s)
    {
        // Palm
        FCircle(c, 18, 20, 8, C.Skin);
        SCircle(c, 18, 20, 8, C.Outline, 1.5f);
        // Thumb (left)
        FCircle(c, 9, 24, 4, C.Skin);
        SCircle(c, 9, 24, 4, C.Outline, 1.5f);
        // Fingers
        for (int i = 0; i < 4; i++)
        {
            float fx = 18 + (i - 1.5f) * 3.5f;
            float fy = 12;
            FCircle(c, fx, fy - 1, 3f, C.Skin);
            SCircle(c, fx, fy - 1, 3f, C.Outline, 1f);
        }
    }

    static void IconPaintBrush(SKCanvas c, int s)
    {
        float cx = 18, cy = 20;
        // Handle (brown rectangle, angled)
        c.Save();
        c.RotateDegrees(-35, cx, cy);
        FRect(c, cx - 2.5f, cy - 10, 5, 16, C.Wood);
        SRect(c, cx - 2.5f, cy - 10, 5, 16, C.Outline, 1f);
        // Ferrule (silver band)
        FRect(c, cx - 3, cy - 10, 6, 4, C.Silver);
        SRect(c, cx - 3, cy - 10, 6, 4, C.Outline, 1f);
        // Bristles (green wedge)
        var bristles = new SKPoint[] { new(cx - 3, cy - 14), new(cx + 3, cy - 14), new(cx + 1, cy - 18), new(cx - 1, cy - 18) };
        FPath(c, bristles, C.Grass);
        SPath(c, bristles, C.Outline, 1f);
        c.Restore();
    }

    static void IconFill(SKCanvas c, int s)
    {
        float cx = 18, cy = 16;
        // Bucket body
        var bucket = new SKPoint[] { new(cx - 8, cy - 2), new(cx + 8, cy - 2), new(cx + 6, cy + 10), new(cx - 6, cy + 10) };
        FPath(c, bucket, C.Iron);
        SPath(c, bucket, C.Outline, 1.5f);
        // Bucket rim
        FRect(c, cx - 8.5f, cy - 3, 17, 3, C.IronL);
        SRect(c, cx - 8.5f, cy - 3, 17, 3, C.Outline, 1f);
        // Handle (arc)
        using var hp = new SKPaint { Color = C.IronL, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round };
        c.DrawArc(new SKRect(cx - 6, cy - 8, cx + 6, cy), 20, 140, false, hp);
        // Spilling liquid
        var spill = new SKPoint[] { new(cx - 4, cy + 10), new(cx - 6, cy + 14), new(cx - 2, cy + 16), new(cx + 3, cy + 15), new(cx + 6, cy + 13), new(cx + 4, cy + 10) };
        FPath(c, spill, C.GrassL);
        SPath(c, spill, C.Outline, 1f);
    }

    static void IconRaise(SKCanvas c, int s)
    {
        float cx = 18, cy = 22;
        // Three stepped layers (bottom: widest)
        FRect(c, cx - 10, cy - 2, 20, 6, C.EarthD);
        SRect(c, cx - 10, cy - 2, 20, 6, C.Outline, 1f);
        FRect(c, cx - 7, cy - 8, 14, 6, C.Earth);
        SRect(c, cx - 7, cy - 8, 14, 6, C.Outline, 1f);
        FRect(c, cx - 4, cy - 14, 8, 6, C.Grass);
        SRect(c, cx - 4, cy - 14, 8, 6, C.Outline, 1f);
        // Arrow above
        ArrowUp(c, cx, cy - 18, 8, C.Gold, C.Outline);
    }

    static void IconLower(SKCanvas c, int s)
    {
        float cx = 18, cy = 14;
        // Three stepped layers (top: widest, going down)
        FRect(c, cx - 10, cy + 8, 20, 6, C.EarthD);
        SRect(c, cx - 10, cy + 8, 20, 6, C.Outline, 1f);
        FRect(c, cx - 7, cy + 2, 14, 6, C.Earth);
        SRect(c, cx - 7, cy + 2, 14, 6, C.Outline, 1f);
        FRect(c, cx - 4, cy - 4, 8, 6, C.Grass);
        SRect(c, cx - 4, cy - 4, 8, 6, C.Outline, 1f);
        // Arrow below
        ArrowDown(c, cx, cy + 20, 8, C.Gold, C.Outline);
    }

    static void IconSmooth(SKCanvas c, int s)
    {
        float cy = 18;
        // Left side: wavy line
        var wavePts = new List<SKPoint>();
        for (int i = 0; i <= 8; i++)
        {
            float x = 4 + i * 2.5f;
            float y = cy + 8f * MathF.Sin(i * 0.8f);
            wavePts.Add(new SKPoint(x, y));
        }
        SPath(c, wavePts.ToArray(), C.Earth, 2f);
        // Right side: flat line
        Line(c, 24, cy, 32, cy, C.GrassL, 2.5f);
        // Connecting arc
        var connPts = new SKPoint[] { wavePts[^1], new(24, cy) };
        SPath(c, connPts, C.Amber, 1.5f);
        // Dots along wave
        for (int i = 0; i <= 8; i++)
        {
            float x = 4 + i * 2.5f;
            float y = cy + 8f * MathF.Sin(i * 0.8f);
            FCircle(c, x, y, 1.5f, C.Gold);
        }
    }

    static void IconFlatten(SKCanvas c, int s)
    {
        float cy = 18;
        // Flat top surface
        FRect(c, 2, cy - 3, 32, 5, C.Grass);
        SRect(c, 2, cy - 3, 32, 5, C.Outline, 1.5f);
        // Earth below (cross-section)
        FRect(c, 2, cy + 2, 32, 10, C.Earth);
        SRect(c, 2, cy + 2, 32, 10, C.Outline, 1.5f);
        // Grass texture lines on top
        Line(c, 6, cy - 1, 10, cy - 1, C.GrassL, 1f);
        Line(c, 16, cy - 2, 22, cy - 1, C.GrassL, 1f);
        Line(c, 26, cy - 1, 30, cy - 1, C.GrassL, 1f);
    }

    static void IconNoise(SKCanvas c, int s)
    {
        float cx = 18, cy = 20;
        // Spiky noise line
        var noisePts = new SKPoint[] {
            new(4, cy + 4), new(7, cy - 8), new(10, cy + 2), new(13, cy - 10),
            new(16, cy + 0), new(19, cy - 12), new(22, cy - 2), new(25, cy - 14), new(28, cy + 2), new(31, cy - 6)
        };
        SPath(c, noisePts, C.Earth, 2f);
        // Spark / star at a peak
        float sx = 19, sy = cy - 14;
        Line(c, sx - 3, sy, sx + 3, sy, C.Gold, 2f);
        Line(c, sx, sy - 3, sx, sy + 3, C.Gold, 2f);
        Line(c, sx - 2, sy - 2, sx + 2, sy + 2, C.Gold, 1.5f);
        Line(c, sx - 2, sy + 2, sx + 2, sy - 2, C.Gold, 1.5f);
    }

    static void IconReplace(SKCanvas c, int s)
    {
        float cx = 18, cy = 18;
        // Left tile (grass)
        FRect(c, 1, cy - 8, 14, 16, C.Grass);
        SRect(c, 1, cy - 8, 14, 16, C.Outline, 1.5f);
        Line(c, 4, cy - 5, 8, cy - 5, C.GrassL, 1f);
        Line(c, 6, cy - 1, 10, cy - 1, C.GrassL, 1f);
        // Right tile (dirt)
        FRect(c, 21, cy - 8, 14, 16, C.Earth);
        SRect(c, 21, cy - 8, 14, 16, C.Outline, 1.5f);
        // Swap arrow between them
        using var ap = new SKPaint { Color = C.Amber, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round };
        c.DrawArc(new SKRect(cx - 5, cy - 10, cx + 5, cy + 2), 340, 220, false, ap);
        // Arrowhead on left end
        var ahead = new SKPoint[] { new(cx - 6, cy - 6), new(cx - 3, cy - 10), new(cx - 1, cy - 5) };
        FPath(c, ahead, C.Amber);
    }

    static void IconRectSelect(SKCanvas c, int s)
    {
        float cx = 18, cy = 18;
        // Dashed rectangle outline
        using var p = new SKPaint { Color = C.Cyan, IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, PathEffect = SKPathEffect.CreateDash(new[] { 3f, 2f }, 0) };
        c.DrawRect(cx - 12, cy - 12, 24, 24, p);
        // Corner dots
        FCircle(c, cx - 12, cy - 12, 2, C.Cyan);
        FCircle(c, cx + 12, cy - 12, 2, C.Cyan);
        FCircle(c, cx - 12, cy + 12, 2, C.Cyan);
        FCircle(c, cx + 12, cy + 12, 2, C.Cyan);
    }

    static void IconLassoSelect(SKCanvas c, int s)
    {
        float cx = 18, cy = 18;
        // Lasso loop using cubic bezier
        using var p = new SKPaint { Color = C.Cyan, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        using var path = new SKPath();
        path.MoveTo(cx - 8, cy + 2);
        path.CubicTo(cx - 12, cy - 8, cx - 4, cy - 14, cx + 4, cy - 12);
        path.CubicTo(cx + 12, cy - 10, cx + 14, cy - 2, cx + 8, cy + 4);
        path.CubicTo(cx + 2, cy + 10, cx - 6, cy + 8, cx - 8, cy + 2);
        c.DrawPath(path, p);
        // Lasso handle (small loop at start)
        SCircle(c, cx - 8, cy + 2, 2.5f, C.Cyan, 1.5f);
    }

    // ============================================================
    // Menu Icons (24x24)
    // ============================================================
    static void MenuNewMap(SKCanvas c, int s)
    {
        // Parchment scroll
        FRect(c, 3, 3, 18, 18, C.Bone);
        SRect(c, 3, 3, 18, 18, C.Outline, 1f);
        // Rolled top
        FPath(c, new SKPoint[] { new(3, 3), new(7, 1), new(17, 1), new(21, 3) }, C.Khaki);
        SPath(c, new SKPoint[] { new(3, 3), new(7, 1), new(17, 1), new(21, 3) }, C.Outline, 1f);
        // Quill
        Line(c, 14, 5, 8, 16, C.Khaki, 1.5f);
        // Ink dot
        FCircle(c, 8, 17, 1.5f, C.Outline);
    }

    static void MenuOpen(SKCanvas c, int s)
    {
        // Chest body
        FRect(c, 2, 8, 20, 12, C.Wood);
        SRect(c, 2, 8, 20, 12, C.Outline, 1f);
        // Lid
        var lid = new SKPoint[] { new(2, 8), new(0, 2), new(24, 2), new(22, 8) };
        FPath(c, lid, C.WoodD);
        SPath(c, lid, C.Outline, 1f);
        // Iron bands
        FRect(c, 2, 8, 20, 2, C.Iron);
        FRect(c, 2, 16, 20, 2, C.Iron);
        // Gold glow inside
        FRect(c, 6, 10, 12, 4, C.Gold);
    }

    static void MenuSave(SKCanvas c, int s)
    {
        // Stone tablet body
        FRect(c, 2, 2, 20, 20, C.Stone);
        SRect(c, 2, 2, 20, 20, C.Outline, 1f);
        // Label area
        FRect(c, 4, 4, 16, 12, C.Bone);
        SRect(c, 4, 4, 16, 12, C.Outline, 0.8f);
        // Text lines
        Line(c, 6, 7, 14, 7, C.Outline, 1f);
        Line(c, 6, 10, 12, 10, C.Outline, 1f);
        Line(c, 6, 13, 16, 13, C.Outline, 1f);
    }

    static void MenuSaveAs(SKCanvas c, int s)
    {
        MenuSave(c, s);
        // Second tablet slightly offset
        FRect(c, 5, 5, 20, 20, C.StoneL);
        SRect(c, 5, 5, 20, 20, C.Outline, 1f);
        FRect(c, 7, 7, 16, 12, C.Bone);
        SRect(c, 7, 7, 16, 12, C.Outline, 0.8f);
    }

    static void MenuExport(SKCanvas c, int s)
    {
        // Crate
        FRect(c, 2, 5, 18, 16, C.Wood);
        SRect(c, 2, 5, 18, 16, C.Outline, 1.5f);
        // Iron bands
        FRect(c, 2, 5, 18, 2, C.Iron);
        FRect(c, 2, 12, 18, 2, C.Iron);
        FRect(c, 10, 5, 2, 16, C.Iron);
        // Arrow pointing out
        var arrow = new SKPoint[] { new(20, 10), new(24, 7), new(24, 13) };
        FPath(c, arrow, C.Gold);
        SPath(c, arrow, C.Outline, 1f);
    }

    static void MenuSettings(SKCanvas c, int s)
    {
        float cx = 12, cy = 12;
        SCircle(c, cx, cy, 8, C.Iron, 2f);
        SCircle(c, cx, cy, 4, C.Iron, 2f);
        // Teeth (4 squares around edge)
        FRect(c, cx - 2, 1, 4, 4, C.Iron);
        FRect(c, cx - 2, 19, 4, 4, C.Iron);
        FRect(c, 1, cy - 2, 4, 4, C.Iron);
        FRect(c, 19, cy - 2, 4, 4, C.Iron);
        // Center gem
        FCircle(c, cx, cy, 2.5f, C.Cyan);
    }

    static void MenuExit(SKCanvas c, int s)
    {
        // Door
        FRect(c, 3, 2, 18, 20, C.WoodD);
        SRect(c, 3, 2, 18, 20, C.Outline, 1.5f);
        // Door frame
        FRect(c, 1, 2, 2, 20, C.Iron);
        FRect(c, 21, 2, 2, 20, C.Iron);
        // Hinges
        FCircle(c, 5, 5, 1.5f, C.Iron);
        FCircle(c, 5, 19, 1.5f, C.Iron);
        // Crack/darkness
        FRect(c, 10, 4, 4, 16, C.Outline);
    }

    static void MenuUndo(SKCanvas c, int s)
    {
        // Scroll arrow curling back
        var arrow = new SKPoint[] { new(9, 4), new(4, 8), new(8, 12) };
        SPath(c, arrow, C.Amber, 2f);
        using var ap = new SKPaint { Color = C.Amber, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round };
        c.DrawArc(new SKRect(2, 6, 14, 18), 340, 200, false, ap);
    }

    static void MenuRedo(SKCanvas c, int s)
    {
        var arrow = new SKPoint[] { new(15, 4), new(20, 8), new(16, 12) };
        SPath(c, arrow, C.Amber, 2f);
        using var ap = new SKPaint { Color = C.Amber, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round };
        c.DrawArc(new SKRect(10, 6, 22, 18), 200, -200, false, ap);
    }

    static void MenuZoomIn(SKCanvas c, int s)
    {
        // Spyglass body
        FPath(c, new SKPoint[] { new(3, 8), new(11, 12), new(11, 16), new(3, 14) }, C.Iron);
        SPath(c, new SKPoint[] { new(3, 8), new(11, 12), new(11, 16), new(3, 14) }, C.Outline, 1f);
        // Lens
        FCircle(c, 14, 14, 5, C.WaterL);
        SCircle(c, 14, 14, 5, C.Outline, 1.5f);
        // Plus
        Line(c, 14, 11, 14, 17, C.White, 1.5f);
        Line(c, 11, 14, 17, 14, C.White, 1.5f);
    }

    static void MenuZoomOut(SKCanvas c, int s)
    {
        FPath(c, new SKPoint[] { new(3, 8), new(11, 12), new(11, 16), new(3, 14) }, C.Iron);
        SPath(c, new SKPoint[] { new(3, 8), new(11, 12), new(11, 16), new(3, 14) }, C.Outline, 1f);
        FCircle(c, 14, 14, 5, C.WaterL);
        SCircle(c, 14, 14, 5, C.Outline, 1.5f);
        Line(c, 11, 14, 17, 14, C.White, 1.5f);
    }

    static void MenuResetZoom(SKCanvas c, int s)
    {
        float cx = 12, cy = 12;
        SCircle(c, cx, cy, 7, C.Cyan, 1.5f);
        SCircle(c, cx, cy, 3, C.Cyan, 1.5f);
        Line(c, cx, 3, cx, 9, C.Cyan, 1.5f);
        Line(c, cx, 15, cx, 21, C.Cyan, 1.5f);
        Line(c, 3, cy, 9, cy, C.Cyan, 1.5f);
        Line(c, 15, cy, 21, cy, C.Cyan, 1.5f);
        // "1:1" indicator lines
        Line(c, 3, 3, 6, 3, C.White, 1f);
        Line(c, 3, 3, 3, 6, C.White, 1f);
    }

    static void MenuRadarView(SKCanvas c, int s)
    {
        float cx = 12, cy = 12;
        FCircle(c, cx, cy, 10, C.GrassD);
        SCircle(c, cx, cy, 10, C.Outline, 1.5f);
        Line(c, cx, cy, cx + 7, cy - 7, C.Amber, 1f);
        FCircle(c, cx - 2, cy - 4, 1.5f, C.Gold);
        FCircle(c, cx + 4, cy + 2, 1f, C.Gold);
        FCircle(c, cx - 5, cy + 3, 1f, C.Cyan);
    }

    static void MenuTerrainView(SKCanvas c, int s)
    {
        float cx = 12, cy = 12;
        var top = new SKPoint[] { new(cx, 3), new(21, 8), new(cx, 13), new(3, 8) };
        FPath(c, top, C.Grass);
        SPath(c, top, C.Outline, 1f);
        var right = new SKPoint[] { new(21, 8), new(cx, 13), new(cx, 20), new(21, 15) };
        FPath(c, right, C.Earth);
        SPath(c, right, C.Outline, 1f);
        var left = new SKPoint[] { new(3, 8), new(cx, 13), new(cx, 20), new(3, 15) };
        FPath(c, left, C.EarthD);
        SPath(c, left, C.Outline, 1f);
    }

    static void MenuHybridView(SKCanvas c, int s)
    {
        MenuTerrainView(c, s);
        using var p = new SKPaint { Color = C.Cyan.WithAlpha(80), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var path = new SKPath();
        path.MoveTo(3, 8);
        path.LineTo(12, 13);
        path.LineTo(21, 8);
        path.LineTo(12, 3);
        path.Close();
        c.DrawPath(path, p);
    }

    static void MenuGrid(SKCanvas c, int s)
    {
        FRect(c, 2, 2, 20, 20, C.EarthD);
        SRect(c, 2, 2, 20, 20, C.Outline, 1.5f);
        Line(c, 2 + 6.67f, 2, 2 + 6.67f, 22, C.White, 0.8f);
        Line(c, 2 + 13.33f, 2, 2 + 13.33f, 22, C.White, 0.8f);
        Line(c, 2, 2 + 6.67f, 22, 2 + 6.67f, C.White, 0.8f);
        Line(c, 2, 2 + 13.33f, 22, 2 + 13.33f, C.White, 0.8f);
    }

    // ============================================================
    // Section Header Icons (20x20)
    // ============================================================
    static void SectionBrush(SKCanvas c, int s)
    {
        FRect(c, 5, 2, 10, 6, C.Wood);
        FRect(c, 5, 2, 10, 2, C.Silver);
        FRect(c, 4, 5, 12, 4, C.WoodD);
        FPath(c, new SKPoint[] { new(4, 5), new(4, 9), new(6, 12), new(8, 9), new(10, 12), new(12, 9), new(14, 12), new(16, 9), new(16, 5) }, C.Grass);
        SRect(c, 4, 5, 12, 4, C.Outline, 0.8f);
    }

    static void SectionZoom(SKCanvas c, int s)
    {
        SCircle(c, 10, 10, 6, C.Outline, 1.5f);
        FCircle(c, 10, 10, 6, C.WaterL);
        Line(c, 15, 15, 19, 19, C.Outline, 2f);
    }

    static void SectionTerrain(SKCanvas c, int s)
    {
        FRect(c, 1, 1, 18, 6, C.Grass);
        FRect(c, 1, 7, 18, 6, C.Earth);
        FRect(c, 1, 13, 18, 6, C.Stone);
        SRect(c, 1, 1, 18, 18, C.Outline, 1f);
    }

    static void SectionLayers(SKCanvas c, int s)
    {
        FRect(c, 1, 1, 18, 18, C.WaterL);
        SRect(c, 1, 1, 18, 18, C.Outline, 0.8f);
        FRect(c, 4, 4, 18, 18, C.GrassL);
        SRect(c, 4, 4, 18, 18, C.Outline, 0.8f);
        FRect(c, 7, 7, 18, 18, C.Amber);
        SRect(c, 7, 7, 18, 18, C.Outline, 0.8f);
    }

    static void SectionMinimap(SKCanvas c, int s)
    {
        float cx = 10, cy = 10;
        SCircle(c, cx, cy, 8, C.Outline, 1f);
        FCircle(c, cx, cy, 8, C.Bone);
        var n = new SKPoint[] { new(cx, 2), new(cx - 2, cy), new(cx + 2, cy) };
        FPath(c, n, C.Crimson);
        SPath(c, n, C.Outline, 0.8f);
        Line(c, cx - 6, cy, cx + 6, cy, C.Outline, 0.8f);
        Line(c, cx, cy - 6, cx, cy + 6, C.Outline, 0.8f);
    }

    static void SectionTileInfo(SKCanvas c, int s)
    {
        FRect(c, 1, 1, 18, 18, C.Stone);
        SRect(c, 1, 1, 18, 18, C.Outline, 1f);
        Line(c, 10, 5, 10, 9, C.Cyan, 1.5f);
        FCircle(c, 10, 12, 1.5f, C.Cyan);
    }

    // ============================================================
    // Status Bar Icons (14x14)
    // ============================================================
    static void StatusCoords(SKCanvas c, int s)
    {
        float cx = 7, cy = 7;
        SCircle(c, cx, cy, 4, C.Amber, 1f);
        Line(c, cx, 1, cx, 5, C.Amber, 1f);
        Line(c, cx, 9, cx, 13, C.Amber, 1f);
        Line(c, 1, cy, 5, cy, C.Amber, 1f);
        Line(c, 9, cy, 13, cy, C.Amber, 1f);
    }

    static void StatusTileID(SKCanvas c, int s)
    {
        FRect(c, 1, 2, 12, 10, C.StoneL);
        SRect(c, 1, 2, 12, 10, C.Outline, 0.8f);
        Line(c, 3, 5, 6, 5, C.Outline, 0.8f);
        Line(c, 3, 5, 3, 8, C.Outline, 0.8f);
        Line(c, 6, 5, 6, 8, C.Outline, 0.8f);
        Line(c, 3, 8, 6, 8, C.Outline, 0.8f);
        Line(c, 8, 5, 8, 8, C.Outline, 0.8f);
        FCircle(c, 10, 6.5f, 1, C.Outline);
    }

    static void StatusHeight(SKCanvas c, int s)
    {
        float cy = 9;
        FRect(c, 1, cy - 2, 4, 5, C.Earth);
        FRect(c, 5, cy - 5, 4, 8, C.Earth);
        FRect(c, 9, cy - 8, 4, 11, C.Earth);
        SRect(c, 1, cy - 2, 4, 5, C.Outline, 0.6f);
        SRect(c, 5, cy - 5, 4, 8, C.Outline, 0.6f);
        SRect(c, 9, cy - 8, 4, 11, C.Outline, 0.6f);
    }

    static void StatusZoom(SKCanvas c, int s)
    {
        SCircle(c, 7, 7, 5, C.WaterL);
        SCircle(c, 7, 7, 5, C.Outline, 1f);
        Line(c, 11, 11, 14, 14, C.Outline, 1.2f);
    }

    static void StatusTool(SKCanvas c, int s)
    {
        float cx = 7, cy = 7;
        var gem = new SKPoint[] { new(cx, 1), new(13, cy), new(cx, 13), new(1, cy) };
        FPath(c, gem, C.Gold);
        SPath(c, gem, C.Outline, 1f);
    }

    // ============================================================
    // Dialog Icons (24x24)
    // ============================================================
    static void DialogBrowse(SKCanvas c, int s)
    {
        FRect(c, 2, 5, 20, 16, C.Wood);
        SRect(c, 2, 5, 20, 16, C.Outline, 1f);
        FRect(c, 2, 3, 8, 4, C.WoodD);
        SRect(c, 2, 3, 8, 4, C.Outline, 1f);
        FRect(c, 16, 5, 3, 6, C.Crimson);
    }

    static void DialogWarning(SKCanvas c, int s)
    {
        float cx = 12, cy = 12;
        var tri = new SKPoint[] { new(cx, 1), new(23, 22), new(1, 22) };
        FPath(c, tri, C.Amber);
        SPath(c, tri, C.Outline, 1.5f);
        FRect(c, cx - 1.5f, 6, 3, 8, C.Outline);
        FCircle(c, cx, 17, 2, C.Outline);
    }

    static void DialogOK(SKCanvas c, int s)
    {
        var check = new SKPoint[] { new(4, 12), new(10, 18), new(20, 5) };
        SPath(c, check, C.GrassL, 3f);
        Line(c, 10, 18, 12, 16, C.GrassL, 2f);
    }

    static void DialogCancel(SKCanvas c, int s)
    {
        SPath(c, new SKPoint[] { new(4, 4), new(20, 20) }, C.Crimson, 3f);
        SPath(c, new SKPoint[] { new(20, 4), new(4, 20) }, C.Crimson, 3f);
    }

    // ============================================================
    // MAIN
    // ============================================================
    static void Main()
    {
        string outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/WorldPainterUO.App/Assets/Icons"));
        Directory.CreateDirectory(outputDir);

        var icons = new (string Name, int Size, Action<SKCanvas, int> Draw)[]
        {
            // Toolbar (36x36)
            ("icon_pan", 36, IconPan),
            ("icon_paintbrush", 36, IconPaintBrush),
            ("icon_fill", 36, IconFill),
            ("icon_raise", 36, IconRaise),
            ("icon_lower", 36, IconLower),
            ("icon_smooth", 36, IconSmooth),
            ("icon_flatten", 36, IconFlatten),
            ("icon_noise", 36, IconNoise),
            ("icon_replace", 36, IconReplace),
            ("icon_rectselect", 36, IconRectSelect),
            ("icon_lassoselect", 36, IconLassoSelect),

            // Menu (24x24)
            ("menu_new", 24, MenuNewMap),
            ("menu_open", 24, MenuOpen),
            ("menu_save", 24, MenuSave),
            ("menu_saveas", 24, MenuSaveAs),
            ("menu_export", 24, MenuExport),
            ("menu_settings", 24, MenuSettings),
            ("menu_exit", 24, MenuExit),
            ("menu_undo", 24, MenuUndo),
            ("menu_redo", 24, MenuRedo),
            ("menu_zoomin", 24, MenuZoomIn),
            ("menu_zoomout", 24, MenuZoomOut),
            ("menu_resetzoom", 24, MenuResetZoom),
            ("menu_radar", 24, MenuRadarView),
            ("menu_terrain", 24, MenuTerrainView),
            ("menu_hybrid", 24, MenuHybridView),
            ("menu_grid", 24, MenuGrid),

            // Section headers (20x20)
            ("section_brush", 20, SectionBrush),
            ("section_zoom", 20, SectionZoom),
            ("section_terrain", 20, SectionTerrain),
            ("section_layers", 20, SectionLayers),
            ("section_minimap", 20, SectionMinimap),
            ("section_tileinfo", 20, SectionTileInfo),

            // Status bar (14x14)
            ("status_coords", 14, StatusCoords),
            ("status_tileid", 14, StatusTileID),
            ("status_height", 14, StatusHeight),
            ("status_zoom", 14, StatusZoom),
            ("status_tool", 14, StatusTool),

            // Dialog (24x24)
            ("dialog_browse", 24, DialogBrowse),
            ("dialog_warning", 24, DialogWarning),
            ("dialog_ok", 24, DialogOK),
            ("dialog_cancel", 24, DialogCancel),
        };

        foreach (var (name, size, draw) in icons)
        {
            using var bmp = new SKBitmap(size, size);
            using var canvas = new SKCanvas(bmp);
            canvas.Clear(SKColors.Transparent);
            draw(canvas, size);
            using var image = bmp.Encode(SKEncodedImageFormat.Png, 100);
            string path = Path.Combine(outputDir, $"{name}.png");
            using var stream = File.OpenWrite(path);
            image.SaveTo(stream);
            Console.WriteLine($"  Generated {name}.png ({size}x{size})");
        }

        Console.WriteLine($"\nDone! {icons.Length} icons generated in {outputDir}");
    }
}
