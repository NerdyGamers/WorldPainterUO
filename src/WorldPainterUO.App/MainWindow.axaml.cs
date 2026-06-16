using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.App.ViewModels;
using WorldPainterUO.App.Views;
using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.FileFormats.Uop;
using WorldPainterUO.Editor.Selection;
using WorldPainterUO.Project;
using WorldPainterUO.Rendering;

namespace WorldPainterUO.App;

public partial class MainWindow : Window
{
    // Pan state
    private bool _isPanning;
    private Point _lastPanPoint;

    // Paint-drag state (apply tool continuously while dragging)
    private bool _isPainting;
    private int _lastPaintTileX = -1;
    private int _lastPaintTileY = -1;

    // Selection state
    private bool _isSelecting;
    private double _selStartX, _selStartY, _selEndX, _selEndY;
    private readonly List<(int X, int Y)> _lassoPoints = [];

    private bool _needsRender = true;
    private Bitmap? _prevViewport;
    private Bitmap? _prevMinimap;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;

        DragDrop.SetAllowDrop(ViewportBorder, true);
        ViewportBorder.AddHandler(DragDrop.DragEnterEvent, OnViewportDragEnter);
        ViewportBorder.AddHandler(DragDrop.DropEvent, OnViewportDrop);

        ViewModel.RequestNewMap    += OnRequestNewMap;
        ViewModel.RequestOpenMap   += OnRequestOpenMap;
        ViewModel.RequestSaveMap   += OnRequestSaveMap;
        ViewModel.RequestSaveAsMap += OnRequestSaveAsMap;
        ViewModel.RequestExportMap += OnRequestExportMap;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.Map)
                or nameof(MainWindowViewModel.OffsetX)
                or nameof(MainWindowViewModel.OffsetY)
                or nameof(MainWindowViewModel.Zoom)
                or nameof(MainWindowViewModel.ShowTileGrid)
                or nameof(MainWindowViewModel.ShowChunkGrid)
                or nameof(MainWindowViewModel.ViewMode))
            {
                _needsRender = true;
            }
        };

        // Keyboard shortcuts for tools
        KeyDown += OnWindowKeyDown;

        Loaded += async (_, _) =>
        {
            _needsRender = true;

            var prefs = AppPreferences.Load();
            // Apply saved UO data path on startup — this initializes the SDK bridge         // with the Settings path before any map is opened, so radar colors and         // future art/tiledata loading all use the correct folder regardless of         // where the user stores their map files.         if (!string.IsNullOrWhiteSpace(prefs.UoDataPath))             ViewModel.ApplyUoDataPath(prefs.UoDataPath);          if (string.IsNullOrWhiteSpace(prefs.UoDataPath))
            {
                await MessageBox.ShowDialog(
                    this,
                    "Welcome to WorldPainterUO!\n\n" +
                    "To display correct terrain colors, please set your UO data folder " +
                    "(the directory containing radarcol.mul, tiledata.mul, etc.).\n\n" +
                    "This will open the Settings dialog now.",
                    "Set UO Data Path");

                await OpenSettingsAsync();
            }
        };

        var timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Render,
            (_, _) =>
            {
                if (_needsRender)
                {
                    RenderViewport();
                    UpdateMinimap();
                }
            });
        timer.Start();
    }

    public MainWindowViewModel ViewModel { get; }

    // ── Keyboard shortcuts ────────────────────────────────────────────────────

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        // Tool hotkeys (only when no text field is focused)
        if (FocusManager?.GetFocusedElement() is TextBox or NumericUpDown)
            return;

        switch (e.Key)
        {
            case Key.V: ViewModel.Tools.SelectPanCommand.Execute(null);     break;
            case Key.B: ViewModel.Tools.SelectPaintCommand.Execute(null);   break;
            case Key.F: ViewModel.Tools.SelectFillCommand.Execute(null);    break;
            case Key.R: ViewModel.Tools.SelectRaiseCommand.Execute(null);   break;
            case Key.L: ViewModel.Tools.SelectLowerCommand.Execute(null);   break;
            case Key.S: ViewModel.Tools.SelectSmoothCommand.Execute(null);  break;
            case Key.G: ViewModel.Tools.SelectFlattenCommand.Execute(null); break;
            case Key.N: ViewModel.Tools.SelectNoiseCommand.Execute(null);   break;
            case Key.E: ViewModel.Tools.SelectReplaceCommand.Execute(null); break;
            case Key.M: ViewModel.Tools.SelectRectCommand.Execute(null);    break;
            case Key.O: ViewModel.Tools.SelectLassoCommand.Execute(null);   break;
            case Key.Escape:
                ViewModel.ActiveSelection = null;
                _lassoPoints.Clear();
                _needsRender = true;
                break;
        }
    }

    // ── Menu handlers ─────────────────────────────────────────────────────────

    private async void OnOpenSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => await OpenSettingsAsync();

    private async System.Threading.Tasks.Task OpenSettingsAsync()
    {
        var result = await SettingsDialog.ShowDialog(this);
        if (result is not null)
        {
            ViewModel.ApplyPreferences(result);
            _needsRender = true;
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!ViewModel.IsDirty) return;
        e.Cancel = true;
        var result = await MessageBox.ShowDialog(this,
            "Save changes before closing?", "Unsaved Changes",
            MessageBoxButtons.YesNoCancel);
        if (result == MessageBoxResult.Yes)
        {
            ViewModel.SaveMapCommand.Execute(null);
            if (!ViewModel.IsDirty) Close();
        }
        else if (result == MessageBoxResult.No)
        {
            ViewModel.IsDirty = false;
            Close();
        }
        // Cancel — do nothing
    }

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnSetRadarMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.ViewMode = ViewMode.Radar;

    private void OnSetTerrainMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.ViewMode = ViewMode.Terrain;

    private void OnSetHybridMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.ViewMode = ViewMode.Hybrid;

    private void OnToggleTileGrid(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.ShowTileGrid = !ViewModel.ShowTileGrid;
        _needsRender = true;
    }

    private void OnToggleChunkGrid(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.ShowChunkGrid = !ViewModel.ShowChunkGrid;
        _needsRender = true;
    }

    // ── Tool strip button handlers ────────────────────────────────────────────

    private void OnSelectPan(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectPanCommand.Execute(null);
    private void OnSelectPaint(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectPaintCommand.Execute(null);
    private void OnSelectFill(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectFillCommand.Execute(null);
    private void OnSelectReplace(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectReplaceCommand.Execute(null);
    private void OnSelectRect(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectRectCommand.Execute(null);
    private void OnSelectLasso(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectLassoCommand.Execute(null);
    private void OnSelectRaise(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectRaiseCommand.Execute(null);
    private void OnSelectLower(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectLowerCommand.Execute(null);
    private void OnSelectSmooth(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectSmoothCommand.Execute(null);
    private void OnSelectFlatten(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectFlattenCommand.Execute(null);
    private void OnSelectNoise(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel.Tools.SelectNoiseCommand.Execute(null);

    // ── Biome selection ───────────────────────────────────────────────────────

    private void OnBiomeSelected(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: string biomeName })
            ViewModel.Tools.ActiveBiomeName = biomeName;
    }

    // ── Map open/new ──────────────────────────────────────────────────────────

    private async void OnRequestNewMap()
    {
        var dialog = new NewMapDialog();
        var dims = await dialog.RunDialogAsync(this);
        if (dims is null) return;

        ViewModel.LoadMap(
            WorldMap.Create(dims.Width, dims.Height, dims.Facet, SourceFileType.Mul),
            ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
    }

    private async void OnRequestOpenMap()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Map File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Map Files") { Patterns = ["*.mul", "*.uop", "*.uomap"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] },
            ],
        });

        if (files.Count == 0) return;

        try
        {
            var filePath = files[0].Path.LocalPath;
            ViewModel.FilePath = filePath;
            ViewModel.RecentFiles.Add(filePath);

            if (filePath.EndsWith(".uomap", StringComparison.OrdinalIgnoreCase))
            {
                var map = UomapSerializer.Load(filePath);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
            else
            {
                var dims   = UltimaMapReader.DetectDimensions(filePath);
                var reader = new UltimaMapReader();
                var map    = reader.Read(filePath, dims);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to open map:\n{ex.Message}", "Open Error");
        }
    }

    private async void OnRequestSaveMap(string filePath)
    {
        try
        {
            UomapSerializer.Save(filePath, ViewModel.Map!);
            UomapAutosave.DeleteSnapshot(filePath);
            ViewModel.IsDirty = false;
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to save map:\n{ex.Message}", "Save Error");
        }
    }

    private async void OnRequestSaveAsMap()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Map As",
            DefaultExtension = ".uomap",
            FileTypeChoices =
            [
                new FilePickerFileType("WorldPainter Map") { Patterns = ["*.uomap"] },
            ],
        });
        if (file is null) return;
        ViewModel.FilePath = file.Path.LocalPath;
        ViewModel.RecentFiles.Add(ViewModel.FilePath);
        OnRequestSaveMap(ViewModel.FilePath);
    }

    private async void OnRequestExportMap()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Map",
            DefaultExtension = ".mul",
            FileTypeChoices =
            [
                new FilePickerFileType("UO Classic Map") { Patterns = ["*.mul"] },
                new FilePickerFileType("UO Enhanced Map") { Patterns = ["*.uop"] },
            ],
        });
        if (file is null) return;

        try
        {
            var path = file.Path.LocalPath;
            if (path.EndsWith(".uop", StringComparison.OrdinalIgnoreCase))
                new UopMapWriter().Write(path, ViewModel.Map!);
            else
                new UltimaMapWriter().Write(path, ViewModel.Map!);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to export map:\n{ex.Message}", "Export Error");
        }
    }

    private async void OnOpenRecentFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: RecentFileEntry entry }) return;

        if (!System.IO.File.Exists(entry.Path))
        {
            await MessageBox.ShowDialog(this, $"File not found:\n{entry.Path}", "Open Error");
            ViewModel.RecentFiles.Entries.Remove(entry);
            return;
        }

        try
        {
            ViewModel.FilePath = entry.Path;
            ViewModel.RecentFiles.Add(entry.Path);

            if (entry.Path.EndsWith(".uomap", StringComparison.OrdinalIgnoreCase))
            {
                var map = UomapSerializer.Load(entry.Path);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
            else
            {
                var dims   = UltimaMapReader.DetectDimensions(entry.Path);
                var reader = new UltimaMapReader();
                var map    = reader.Read(entry.Path, dims);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to open map:\n{ex.Message}", "Open Error");
        }
    }

    // ── Render ────────────────────────────────────────────────────────────────

    private void RenderViewport()
    {
        var border = ViewportBorder;
        var w = (int)Math.Max(border.Bounds.Width, 1);
        var h = (int)Math.Max(border.Bounds.Height, 1);

        if (w < 2 || h < 2) return;

        _needsRender = false;

        if (ViewModel.Map is null) return;

        try
        {
            var renderService = ViewModel.RenderService;
            renderService.OffsetX = (float)ViewModel.OffsetX;
            renderService.OffsetY = (float)ViewModel.OffsetY;
            renderService.Zoom    = (float)ViewModel.Zoom;

            renderService.SyncDirtyChunks(ViewModel.Map);

            using var bitmap = new SKBitmap(w, h);
            using var canvas = new SKCanvas(bitmap);

            renderService.Render(canvas, ViewModel.Map, w, h);

            // Brush preview circle
            var tool = ViewModel.Tools;
            if (ViewModel.IsMapLoaded && tool.ActiveTool is ActiveTool.PaintBrush or ActiveTool.Raise
                or ActiveTool.Lower or ActiveTool.Smooth or ActiveTool.Flatten or ActiveTool.Noise)
            {
                var mx = (float)ViewModel.MouseScreenX;
                var my = (float)ViewModel.MouseScreenY;
                var radius = tool.BrushRadius * (float)ViewModel.Zoom;
                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = new SKColor(255, 255, 255, 180),
                    StrokeWidth = 2f,
                    IsAntialias = true,
                };
                canvas.DrawCircle(mx, my, radius, paint);
            }

            // Selection overlay
            var sel = ViewModel.ActiveSelection;
            var z = (float)ViewModel.Zoom;
            var ox = (float)ViewModel.OffsetX;
            var oy = (float)ViewModel.OffsetY;
            if (sel is RectangleSelection rectSel && rectSel.Bounds is { } rb)
            {
                var sx = (rb.MinX + ox) * z;
                var sy = (rb.MinY + oy) * z;
                var sw = (rb.MaxX - rb.MinX + 1) * z;
                var sh = (rb.MaxY - rb.MinY + 1) * z;
                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = new SKColor(0, 200, 255, 220),
                    StrokeWidth = 2f,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash([6f, 4f], 0),
                };
                canvas.DrawRect(sx, sy, sw, sh, paint);
            }
            else if (sel is LassoSelection lassoSel && lassoSel.Polygon.Count >= 3)
            {
                using var path = new SKPath();
                var pts = lassoSel.Polygon;
                path.MoveTo((pts[0].X + ox) * z, (pts[0].Y + oy) * z);
                for (var i = 1; i < pts.Count; i++)
                    path.LineTo((pts[i].X + ox) * z, (pts[i].Y + oy) * z);
                path.Close();
                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = new SKColor(0, 200, 255, 220),
                    StrokeWidth = 2f,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash([6f, 4f], 0),
                };
                canvas.DrawPath(path, paint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
            using var ms    = new MemoryStream(data.ToArray());

            _prevViewport?.Dispose();
            _prevViewport = new Bitmap(ms);
            ViewportImage.Source = _prevViewport;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Render error: {ex}");
        }
    }

    private void UpdateMinimap()
    {
        if (ViewModel.Map is null) return;

        try
        {
            var bmp = ViewModel.MinimapRenderer.GetOrRender(ViewModel.Map, 200);
            using var image = SKImage.FromBitmap(bmp);
            using var data  = image.Encode(SKEncodedImageFormat.Png, 80);
            using var ms    = new MemoryStream(data.ToArray());

            _prevMinimap?.Dispose();
            _prevMinimap = new Bitmap(ms);
            MinimapImage.Source = _prevMinimap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Minimap error: {ex}");
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel.Map is null) return;

        var pos = e.GetPosition(ViewportBorder);
        ViewModel.UpdateMousePosition(pos.X, pos.Y);

        if (_isPanning)
        {
            var dx = pos.X - _lastPanPoint.X;
            var dy = pos.Y - _lastPanPoint.Y;
            ViewModel.Pan(dx, dy);
            _lastPanPoint = pos;
            _needsRender  = true;
            return;
        }

        if (_isPainting)
        {
            var tileX = (int)((pos.X / ViewModel.Zoom) - ViewModel.OffsetX);
            var tileY = (int)((pos.Y / ViewModel.Zoom) - ViewModel.OffsetY);

            // Only fire if tile changed (avoid redundant edits on same tile)
            if (tileX != _lastPaintTileX || tileY != _lastPaintTileY)
            {
                _lastPaintTileX = tileX;
                _lastPaintTileY = tileY;
                if (ViewModel.ApplyTool(tileX, tileY))
                    _needsRender = true;
            }
        }

        if (_isSelecting)
        {
            _selEndX = pos.X;
            _selEndY = pos.Y;
            if (ViewModel.Tools.ActiveTool == ActiveTool.LassoSelect)
            {
                var tx = (int)((pos.X / ViewModel.Zoom) - ViewModel.OffsetX);
                var ty = (int)((pos.Y / ViewModel.Zoom) - ViewModel.OffsetY);
                var last = _lassoPoints[^1];
                if (tx != last.X || ty != last.Y)
                    _lassoPoints.Add((tx, ty));
            }
            _needsRender = true;
        }
    }

    private void OnViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel.Map is null) return;
        var pos = e.GetPosition(ViewportBorder);
        ViewModel.HandleScroll(e.Delta.X, e.Delta.Y, pos.X, pos.Y);
        _needsRender = true;
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.Map is null) return;

        var point = e.GetCurrentPoint(ViewportBorder);
        var pos   = point.Position;

        if (point.Properties.IsLeftButtonPressed)
        {
            var tool = ViewModel.Tools.ActiveTool;

            if (tool == ActiveTool.Pan)
            {
                // Pan mode
                _isPanning    = true;
                _lastPanPoint = pos;
                ViewportBorder.Cursor = new Cursor(StandardCursorType.Hand);
            }
            else if (tool is ActiveTool.RectSelect or ActiveTool.LassoSelect)
            {
                _isSelecting = true;
                _selStartX = _selEndX = pos.X;
                _selStartY = _selEndY = pos.Y;
                _lassoPoints.Clear();
                _lassoPoints.Add((
                    (int)((pos.X / ViewModel.Zoom) - ViewModel.OffsetX),
                    (int)((pos.Y / ViewModel.Zoom) - ViewModel.OffsetY)));
                ViewportBorder.Cursor = new Cursor(StandardCursorType.Cross);
            }
            else
            {
                // Editing mode — apply tool on first press
                _isPainting = true;
                var tileX = (int)((pos.X / ViewModel.Zoom) - ViewModel.OffsetX);
                var tileY = (int)((pos.Y / ViewModel.Zoom) - ViewModel.OffsetY);
                _lastPaintTileX = tileX;
                _lastPaintTileY = tileY;
                if (ViewModel.ApplyTool(tileX, tileY))
                    _needsRender = true;
                ViewportBorder.Cursor = new Cursor(StandardCursorType.Cross);
            }

            // Middle mouse or right mouse always pans
            if (point.Properties.IsMiddleButtonPressed)
            {
                _isPanning    = true;
                _lastPanPoint = pos;
                ViewportBorder.Cursor = new Cursor(StandardCursorType.Hand);
            }
        }
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning  = false;
        _isPainting = false;

        if (_isSelecting)
        {
            _isSelecting = false;
            var tool = ViewModel.Tools.ActiveTool;
            var zoom = ViewModel.Zoom;
            var ox = ViewModel.OffsetX;
            var oy = ViewModel.OffsetY;

            if (tool == ActiveTool.RectSelect)
            {
                var x0 = (int)((_selStartX / zoom) - ox);
                var y0 = (int)((_selStartY / zoom) - oy);
                var x1 = (int)((_selEndX / zoom) - ox);
                var y1 = (int)((_selEndY / zoom) - oy);
                ViewModel.ActiveSelection = new RectangleSelection(x0, y0, x1, y1);
            }
            else if (tool == ActiveTool.LassoSelect && _lassoPoints.Count >= 3)
            {
                ViewModel.ActiveSelection = new LassoSelection([.. _lassoPoints]);
            }
            _needsRender = true;
        }

        ViewportBorder.Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
        => _needsRender = true;

    private void OnViewportDragEnter(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null) return;
        foreach (var item in files)
        {
            var ext = Path.GetExtension(item.Path.LocalPath).ToLowerInvariant();
            if (ext is ".mul" or ".uop" or ".uomap")
            {
                e.DragEffects = DragDropEffects.Copy;
                return;
            }
        }
    }

    private async void OnViewportDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null || files.Length == 0) return;
        var filePath = files[0].Path.LocalPath;

        try
        {
            ViewModel.FilePath = filePath;
            ViewModel.RecentFiles.Add(filePath);

            if (filePath.EndsWith(".uomap", StringComparison.OrdinalIgnoreCase))
            {
                var map = UomapSerializer.Load(filePath);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
            else
            {
                var dims   = UltimaMapReader.DetectDimensions(filePath);
                var reader = new UltimaMapReader();
                var map    = reader.Read(filePath, dims);
                ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
            }
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to open dropped file:\n{ex.Message}", "Open Error");
        }
    }
}
