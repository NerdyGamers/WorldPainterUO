using System;
using System.IO;
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

    private bool _needsRender = true;
    private Bitmap? _prevViewport;
    private Bitmap? _prevMinimap;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;

        ViewModel.RequestNewMap  += OnRequestNewMap;
        ViewModel.RequestOpenMap += OnRequestOpenMap;

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
                new FilePickerFileType("Map Files") { Patterns = ["*.mul", "*.uop"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] },
            ],
        });

        if (files.Count == 0) return;

        try
        {
            var filePath = files[0].Path.LocalPath;
            var dims     = UltimaMapReader.DetectDimensions(filePath);
            var reader   = new UltimaMapReader();
            var map      = reader.Read(filePath, dims);
            ViewModel.RecentFiles.Add(filePath);
            ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to open map:\n{ex.Message}", "Open Error");
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
            var dims   = UltimaMapReader.DetectDimensions(entry.Path);
            var reader = new UltimaMapReader();
            var map    = reader.Read(entry.Path, dims);
            ViewModel.RecentFiles.Add(entry.Path);
            ViewModel.LoadMap(map, ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
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
        ViewportBorder.Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
        => _needsRender = true;
}
