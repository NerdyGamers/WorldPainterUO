using System;
using System.Collections.ObjectModel;
using System.Timers;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.Rendering;

namespace WorldPainterUO.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private WorldMap? _map;
    private double _offsetX;
    private double _offsetY;
    private double _zoom = 1.0;
    private int _tileX;
    private int _tileY;
    private ushort _tileId;
    private sbyte _tileZ;
    private ViewMode _viewMode = ViewMode.Radar;
    private bool _showTileGrid;
    private bool _showChunkGrid;
    private Timer? _autosaveTimer;

    public MainWindowViewModel()
    {
        RecentFiles = RecentFiles.Load();
        Layers =
        [
            new LayerItem("Terrain", true),
            new LayerItem("Height",  true),
        ];

        // Sync layer visibility changes to the render service
        foreach (var layer in Layers)
            layer.PropertyChanged += OnLayerVisibilityChanged;

        var prefs = AppPreferences.Load();
        ApplyPreferences(prefs);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    public RecentFiles RecentFiles { get; }

    public string Title => _map is null
        ? "WorldPainterUO"
        : $"WorldPainterUO — {_map.Dimensions.Width}\u00d7{_map.Dimensions.Height} ({_map.Metadata.Facet})";

    public WorldMap? Map
    {
        get => _map;
        private set
        {
            SetProperty(ref _map, value);
            OnPropertyChanged(nameof(IsMapLoaded));
            OnPropertyChanged(nameof(MapDimensions));
            OnPropertyChanged(nameof(Title));
        }
    }

    public bool IsMapLoaded => _map is not null;
    public MapDimensions? MapDimensions => _map?.Dimensions;

    public double OffsetX
    {
        get => _offsetX;
        set => SetProperty(ref _offsetX, value);
    }

    public double OffsetY
    {
        get => _offsetY;
        set => SetProperty(ref _offsetY, value);
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, Math.Clamp(value, 0.03125, 16.0)))
                OnPropertyChanged(nameof(ZoomPercent));
        }
    }

    public string ZoomPercent => $"{_zoom * 100:F0}%";

    public int TileX { get => _tileX; private set => SetProperty(ref _tileX, value); }
    public int TileY { get => _tileY; private set => SetProperty(ref _tileY, value); }
    public ushort TileId { get => _tileId; private set => SetProperty(ref _tileId, value); }
    public sbyte TileZ { get => _tileZ; private set => SetProperty(ref _tileZ, value); }

    public MapRenderService RenderService { get; } = new();
    public MinimapRenderer MinimapRenderer { get; } = new();

    public ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                RenderService.ViewMode = value;
                RenderService.InvalidateAll();
                MinimapRenderer.Invalidate();
                OnPropertyChanged(nameof(IsRadarMode));
                OnPropertyChanged(nameof(IsTerrainMode));
                OnPropertyChanged(nameof(IsHybridMode));
            }
        }
    }

    // Used by AXAML checkmarks
    public bool IsRadarMode   => _viewMode == ViewMode.Radar;
    public bool IsTerrainMode => _viewMode == ViewMode.Terrain;
    public bool IsHybridMode  => _viewMode == ViewMode.Hybrid;

    public bool ShowTileGrid
    {
        get => _showTileGrid;
        set
        {
            if (SetProperty(ref _showTileGrid, value))
                RenderService.ShowTileGrid = value;
        }
    }

    public bool ShowChunkGrid
    {
        get => _showChunkGrid;
        set
        {
            if (SetProperty(ref _showChunkGrid, value))
                RenderService.ShowChunkGrid = value;
        }
    }

    public ObservableCollection<LayerItem> Layers { get; }

    public string StatusText => _map is null
        ? "No map loaded"
        : $"Tile: ({_tileX}, {_tileY})  ID: 0x{_tileId:X4}  Z: {_tileZ,4}  Zoom: {ZoomPercent}";

    public event Action? RequestNewMap;
    public event Action? RequestOpenMap;

    // ── Public Methods ────────────────────────────────────────────────────────

    /// <summary>Apply all preferences in one call (startup + after settings dialog).</summary>
    public void ApplyPreferences(AppPreferences prefs)
    {
        ApplyUoDataPath(prefs.UoDataPath);
        ApplyTheme(prefs.Theme);
        RestartAutosave(prefs.AutosaveIntervalSeconds);
    }

    public void ApplyUoDataPath(string? path)
    {
        RenderService.TryLoadRadarColors(path);
        MinimapRenderer.TryLoadRadarColors(path);
        RenderService.InvalidateAll();
        MinimapRenderer.Invalidate();
    }

    public static void ApplyTheme(string? theme)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = theme == "Light"
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    // ── Layers ────────────────────────────────────────────────────────────────

    private void OnLayerVisibilityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LayerItem.Visible)) return;

        foreach (var layer in Layers)
        {
            switch (layer.Name)
            {
                case "Terrain": RenderService.TerrainVisible = layer.Visible; break;
                case "Height":  RenderService.HeightVisible  = layer.Visible; break;
            }
        }

        RenderService.InvalidateAll();
        MinimapRenderer.Invalidate();
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

    private void RestartAutosave(int intervalSeconds)
    {
        _autosaveTimer?.Stop();
        _autosaveTimer?.Dispose();

        if (intervalSeconds <= 0) return;

        _autosaveTimer = new Timer(intervalSeconds * 1000.0)
        {
            AutoReset = true,
        };
        _autosaveTimer.Elapsed += (_, _) => TriggerAutosave();
        _autosaveTimer.Start();
    }

    private void TriggerAutosave()
    {
        if (_map is null) return;
        // TODO V1: save to .uomap project file
        System.Diagnostics.Debug.WriteLine($"[Autosave] {DateTime.Now:HH:mm:ss}");
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand] private void NewMap()  => RequestNewMap?.Invoke();
    [RelayCommand] private void OpenMap() => RequestOpenMap?.Invoke();

    [RelayCommand]
    private void ZoomIn()
    {
        Zoom *= 2.0;
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Zoom /= 2.0;
        OnPropertyChanged(nameof(StatusText));
    }

    [RelayCommand]
    private void ResetZoom()
    {
        Zoom = 1.0;
        OffsetX = 0;
        OffsetY = 0;
        OnPropertyChanged(nameof(StatusText));
    }

    // ── Map interaction ───────────────────────────────────────────────────────

    public void LoadMap(WorldMap map, double viewportWidth = 0, double viewportHeight = 0)
    {
        Map = map;
        RenderService.ClearCache();
        ResetView();

        if (viewportWidth > 0 && viewportHeight > 0)
        {
            var fitZoom = Math.Min(viewportWidth / map.Dimensions.Width,
                                   viewportHeight / map.Dimensions.Height);
            Zoom = Math.Clamp(fitZoom * 0.9, 0.03125, 1.0);
        }
        else
        {
            Zoom = 1.0;
        }

        OffsetX = -(map.Dimensions.Width  - viewportWidth  / _zoom) / 2;
        OffsetY = -(map.Dimensions.Height - viewportHeight / _zoom) / 2;

        RenderService.Zoom = (float)_zoom;
        MinimapRenderer.Invalidate();
        OnPropertyChanged(nameof(StatusText));
    }

    public void UpdateMousePosition(double canvasX, double canvasY)
    {
        if (_map is null) return;

        var tileX = (int)((canvasX / _zoom) - _offsetX);
        var tileY = (int)((canvasY / _zoom) - _offsetY);

        TileX = tileX;
        TileY = tileY;

        if (tileX >= 0 && tileY >= 0 && tileX < _map.Dimensions.Width && tileY < _map.Dimensions.Height)
        {
            TileId = _map.Terrain[tileX, tileY];
            TileZ  = _map.Height[tileX, tileY];
        }

        OnPropertyChanged(nameof(StatusText));
    }

    public void HandleScroll(double deltaX, double deltaY, double mouseX, double mouseY)
    {
        var oldZoom = _zoom;
        Zoom = deltaY > 0 ? _zoom * 2.0 : _zoom * 0.5;
        var ratio = _zoom / oldZoom;

        OffsetX = mouseX / _zoom - (mouseX / oldZoom - _offsetX) * ratio;
        OffsetY = mouseY / _zoom - (mouseY / oldZoom - _offsetY) * ratio;

        OnPropertyChanged(nameof(StatusText));
    }

    public void Pan(double deltaX, double deltaY)
    {
        OffsetX += deltaX / _zoom;
        OffsetY += deltaY / _zoom;
    }

    private void ResetView()
    {
        OffsetX = 0; OffsetY = 0; Zoom = 1.0;
        TileX = 0; TileY = 0; TileId = 0; TileZ = 0;
        OnPropertyChanged(nameof(StatusText));
    }

    public void Dispose()
    {
        _autosaveTimer?.Stop();
        _autosaveTimer?.Dispose();
    }
}

public sealed class LayerItem : ObservableObject
{
    private bool _visible;

    public LayerItem(string name, bool visible = true)
    {
        Name = name;
        _visible = visible;
    }

    public string Name { get; }

    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }
}
