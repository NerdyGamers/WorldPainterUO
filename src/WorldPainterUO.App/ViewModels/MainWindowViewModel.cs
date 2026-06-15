using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.Core;
using WorldPainterUO.Editor;
using WorldPainterUO.Editor.Tools;
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
    private System.Timers.Timer? _autosaveTimer;

    public CommandHistory History { get; } = new();
    public ToolViewModel Tools { get; } = new();

    public MainWindowViewModel()
    {
        RecentFiles = RecentFiles.Load();
        Layers =
        [
            new LayerItem("Terrain", true),
            new LayerItem("Height",  true),
        ];

        foreach (var layer in Layers)
            layer.PropertyChanged += OnLayerVisibilityChanged;

        History.StateChanged += () =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
        };

        var prefs = AppPreferences.Load();
        ApplyPreferences(prefs);
    }

    public RecentFiles RecentFiles { get; }

    public string Title => _map is null
        ? "WorldPainterUO"
        : $"WorldPainterUO \u2014 {_map.Dimensions.Width}\u00d7{_map.Dimensions.Height} ({_map.Metadata.Facet})";

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

    public int TileX     { get => _tileX;  private set => SetProperty(ref _tileX, value);  }
    public int TileY     { get => _tileY;  private set => SetProperty(ref _tileY, value);  }
    public ushort TileId { get => _tileId; private set => SetProperty(ref _tileId, value); }
    public sbyte TileZ   { get => _tileZ;  private set => SetProperty(ref _tileZ, value);  }

    public MapRenderService RenderService   { get; } = new();
    public MinimapRenderer  MinimapRenderer { get; } = new();

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

    public bool IsRadarMode   => _viewMode == ViewMode.Radar;
    public bool IsTerrainMode => _viewMode == ViewMode.Terrain;
    public bool IsHybridMode  => _viewMode == ViewMode.Hybrid;

    public bool ShowTileGrid
    {
        get => _showTileGrid;
        set { if (SetProperty(ref _showTileGrid, value)) RenderService.ShowTileGrid = value; }
    }

    public bool ShowChunkGrid
    {
        get => _showChunkGrid;
        set { if (SetProperty(ref _showChunkGrid, value)) RenderService.ShowChunkGrid = value; }
    }

    public ObservableCollection<LayerItem> Layers { get; }

    public string StatusText => _map is null
        ? "No map loaded"
        : $"Tile: ({_tileX}, {_tileY})  ID: 0x{_tileId:X4}  Z: {_tileZ,4}  Zoom: {ZoomPercent}";

    public bool CanUndo    => History.CanUndo;
    public bool CanRedo    => History.CanRedo;
    public string UndoLabel => History.UndoDescription is string d ? $"Undo: {d}" : "Undo";
    public string RedoLabel => History.RedoDescription is string d ? $"Redo: {d}" : "Redo";

    public event Action? RequestNewMap;
    public event Action? RequestOpenMap;

    // ── Tool execution ────────────────────────────────────────────────────────

    public bool ApplyTool(int tileX, int tileY)
    {
        if (_map is null) return false;
        if (tileX < 0 || tileY < 0 || tileX >= _map.Dimensions.Width || tileY >= _map.Dimensions.Height)
            return false;

        var t = Tools;
        var tileId = t.ActiveBiome is { } b && b.Definition.Tiles.Count > 0
            ? b.Definition.Tiles[0].TileId
            : (ushort)3;

        // Both Raise and Lower take a positive sbyte amount; Lower negates internally.
        var amount = (sbyte)Math.Clamp((int)(t.BrushStrength * 5), 1, 10);

        ICommand? cmd = t.ActiveTool switch
        {
            // PaintBrushTool: (map, cx, cy, tileId, radius, opacity, hardness, seed, selection?)
            ActiveTool.PaintBrush when t.ActiveBiome is not null =>
                PaintBrushTool.Execute(
                    _map, tileX, tileY,
                    tileId,
                    t.BrushRadius,
                    t.BrushStrength,
                    t.BrushHardness,
                    seed: 0),

            // FillTool: (map, cx, cy, tileId)
            ActiveTool.Fill when t.ActiveBiome is not null =>
                FillTool.Execute(_map, tileX, tileY, tileId),

            // RaiseTool: (map, cx, cy, radius, amount, selection?)
            ActiveTool.Raise =>
                RaiseTool.Execute(_map, tileX, tileY, t.BrushRadius, amount),

            // LowerTool: same signature — takes positive amount and subtracts internally
            ActiveTool.Lower =>
                LowerTool.Execute(_map, tileX, tileY, t.BrushRadius, amount),

            // SmoothTool: (map, cx, cy, radius, selection?) — no strength param
            ActiveTool.Smooth =>
                SmoothTool.Execute(_map, tileX, tileY, t.BrushRadius),

            // FlattenTool: (map, cx, cy, radius, targetZ)
            ActiveTool.Flatten =>
                FlattenTool.Execute(_map, tileX, tileY, t.BrushRadius, t.FlattenZ),

            // NoiseTool: (map, cx, cy, radius, magnitude)
            ActiveTool.Noise =>
                NoiseTool.Execute(_map, tileX, tileY, t.BrushRadius, (int)(t.BrushStrength * 10)),

            // ReplaceTool: (map, findTileId, replaceTileId, bounds?, selection?)
            ActiveTool.Replace when t.ActiveBiome is not null =>
                ReplaceTool.Execute(
                    _map,
                    _map.Terrain[tileX, tileY],
                    tileId),

            _ => null,
        };

        if (cmd is null) return false;

        History.Execute(cmd, _map);
        RenderService.InvalidateAll();
        MinimapRenderer.Invalidate();
        return true;
    }

    // ── Public Methods ────────────────────────────────────────────────────────

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
        _autosaveTimer = new System.Timers.Timer(intervalSeconds * 1000.0) { AutoReset = true };
        _autosaveTimer.Elapsed += (_, _) => TriggerAutosave();
        _autosaveTimer.Start();
    }

    private void TriggerAutosave()
    {
        if (_map is null) return;
        System.Diagnostics.Debug.WriteLine($"[Autosave] {DateTime.Now:HH:mm:ss}");
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand] private void NewMap()  => RequestNewMap?.Invoke();
    [RelayCommand] private void OpenMap() => RequestOpenMap?.Invoke();

    [RelayCommand]
    private void Undo()
    {
        if (_map is null) return;
        if (History.Undo(_map)) { RenderService.InvalidateAll(); MinimapRenderer.Invalidate(); }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_map is null) return;
        if (History.Redo(_map)) { RenderService.InvalidateAll(); MinimapRenderer.Invalidate(); }
    }

    [RelayCommand] private void ZoomIn()    { Zoom *= 2.0; OnPropertyChanged(nameof(StatusText)); }
    [RelayCommand] private void ZoomOut()   { Zoom /= 2.0; OnPropertyChanged(nameof(StatusText)); }
    [RelayCommand] private void ResetZoom() { Zoom = 1.0; OffsetX = 0; OffsetY = 0; OnPropertyChanged(nameof(StatusText)); }

    // ── Map interaction ───────────────────────────────────────────────────────

    public void LoadMap(WorldMap map, double viewportWidth = 0, double viewportHeight = 0)
    {
        Map = map;
        History.Clear();
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
        TileX = tileX; TileY = tileY;
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
    public LayerItem(string name, bool visible = true) { Name = name; _visible = visible; }
    public string Name { get; }
    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }
}
