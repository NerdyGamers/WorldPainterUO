using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private bool _isPanning;
    private Point _lastPanPoint;
    private bool _needsRender = true;
    private Bitmap? _prevViewport;
    private Bitmap? _prevMinimap;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;

        ViewModel.RequestNewMap += OnRequestNewMap;
        ViewModel.RequestOpenMap += OnRequestOpenMap;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.Map)
                or nameof(MainWindowViewModel.OffsetX)
                or nameof(MainWindowViewModel.OffsetY)
                or nameof(MainWindowViewModel.Zoom))
            {
                _needsRender = true;
            }
        };

        Loaded += (_, _) => _needsRender = true;

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

    private async void OnRequestNewMap()
    {
        var dialog = new NewMapDialog();
        var dims = await dialog.RunDialogAsync(this);
        if (dims is null)
            return;

        ViewModel.LoadMap(WorldMap.Create(dims.Width, dims.Height, dims.Facet, SourceFileType.Mul),
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

        if (files.Count == 0)
            return;

        try
        {
            var filePath = files[0].Path.LocalPath;
            var dims = UltimaMapReader.DetectDimensions(filePath);
            var reader = new UltimaMapReader();
            var map = reader.Read(filePath, dims);
            ViewModel.RecentFiles.Add(filePath);
            ViewModel.LoadMap(map,
                ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(
                this,
                $"Failed to open map:\n{ex.Message}",
                "Open Error");
        }
    }

    private async void OnOpenRecentFile(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: RecentFileEntry entry })
            return;

        if (!File.Exists(entry.Path))
        {
            await MessageBox.ShowDialog(this, $"File not found:\n{entry.Path}", "Open Error");
            ViewModel.RecentFiles.Entries.Remove(entry);
            return;
        }

        try
        {
            var dims = UltimaMapReader.DetectDimensions(entry.Path);
            var reader = new UltimaMapReader();
            var map = reader.Read(entry.Path, dims);
            ViewModel.RecentFiles.Add(entry.Path);
            ViewModel.LoadMap(map,
                ViewportBorder.Bounds.Width, ViewportBorder.Bounds.Height);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowDialog(this, $"Failed to open map:\n{ex.Message}", "Open Error");
        }
    }

    private void RenderViewport()
    {
        _needsRender = false;

        if (ViewModel.Map is null)
            return;

        try
        {
            var border = ViewportBorder;
            var w = (int)Math.Max(border.Bounds.Width, 1);
            var h = (int)Math.Max(border.Bounds.Height, 1);

            if (w < 2 || h < 2)
                return;

            // Sync view params
            var renderService = ViewModel.RenderService;
            renderService.OffsetX = (float)ViewModel.OffsetX;
            renderService.OffsetY = (float)ViewModel.OffsetY;
            renderService.Zoom = (float)ViewModel.Zoom;

            // Sync dirty chunks from map edits
            renderService.SyncDirtyChunks(ViewModel.Map);

            using var bitmap = new SKBitmap(w, h);
            using var canvas = new SKCanvas(bitmap);

            renderService.Render(canvas, ViewModel.Map, w, h);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());

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
        if (ViewModel.Map is null)
            return;

        try
        {
            var bmp = ViewModel.MinimapRenderer.GetOrRender(ViewModel.Map, 180);
            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(SKEncodedImageFormat.Png, 80);
            using var ms = new MemoryStream(data.ToArray());

            _prevMinimap?.Dispose();
            _prevMinimap = new Bitmap(ms);
            MinimapImage.Source = _prevMinimap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Minimap error: {ex}");
        }
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel.Map is null)
            return;

        var pos = e.GetPosition(ViewportBorder);

        if (_isPanning)
        {
            var dx = pos.X - _lastPanPoint.X;
            var dy = pos.Y - _lastPanPoint.Y;
            ViewModel.Pan(dx, dy);
            _lastPanPoint = pos;
            return;
        }

        ViewModel.UpdateMousePosition(pos.X, pos.Y);
    }

    private void OnViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel.Map is null)
            return;

        var pos = e.GetPosition(ViewportBorder);
        ViewModel.HandleScroll(e.Delta.X, e.Delta.Y, pos.X, pos.Y);
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.Map is null)
            return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(ViewportBorder);
            ViewportBorder.Cursor = new Cursor(StandardCursorType.Hand);
        }
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ViewportBorder.Cursor = new Cursor(StandardCursorType.Arrow);
        }
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _needsRender = true;
    }
}
