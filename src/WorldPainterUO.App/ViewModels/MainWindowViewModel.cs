using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorldPainterUO.Core;
using WorldPainterUO.FileFormats;
using WorldPainterUO.Rendering;

namespace WorldPainterUO.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
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

    public MainWindowViewModel()
    {
        Layers =
        [
            new LayerItem("Terrain", true),
            new LayerItem("Height", true),
        ];
    }

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

    public int TileX
    {
        get => _tileX;
        private set => SetProperty(ref _tileX, value);
    }

    public int TileY
    {
        get => _tileY;
        private set => SetProperty(ref _tileY, value);
    }

    public ushort TileId
    {
        get => _tileId;
        private set => SetProperty(ref _tileId, value);
    }

    public sbyte TileZ
    {
        get => _tileZ;
        private set => SetProperty(ref _tileZ, value);
    }

    public MapRenderService RenderService { get; private set; } = new();
    public MinimapRenderer MinimapRenderer { get; private set; } = new();

    public ViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                RenderService.ViewMode = value;
                RenderService.InvalidateAll();
            }
        }
    }

    public ObservableCollection<LayerItem> Layers { get; }

    public string StatusText => _map is null
        ? "No map loaded"
        : $"Tile: ({_tileX}, {_tileY})  ID: 0x{_tileId:X4}  Z: {_tileZ,4}  Zoom: {ZoomPercent}";

    public event Action? RequestNewMap;
    public event Action? RequestOpenMap;

    [RelayCommand]
    private void NewMap()
    {
        RequestNewMap?.Invoke();
    }

    [RelayCommand]
    private void OpenMap()
    {
        RequestOpenMap?.Invoke();
    }

    public void LoadMap(WorldMap map, double viewportWidth = 0, double viewportHeight = 0)
    {
        Map = map;
        RenderService.ClearCache();
        ResetView();

        // Auto-fit zoom if we know viewport dimensions
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

        // Center the map
        OffsetX = -(map.Dimensions.Width - viewportWidth / _zoom) / 2;
        OffsetY = -(map.Dimensions.Height - viewportHeight / _zoom) / 2;

        RenderService.Zoom = (float)_zoom;
        MinimapRenderer.Invalidate();
        OnPropertyChanged(nameof(StatusText));
    }

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

    public void UpdateMousePosition(double canvasX, double canvasY)
    {
        if (_map is null)
            return;

        var tileX = (int)((canvasX / _zoom) - _offsetX);
        var tileY = (int)((canvasY / _zoom) - _offsetY);

        if (tileX < 0 || tileY < 0 || tileX >= _map.Dimensions.Width || tileY >= _map.Dimensions.Height)
        {
            TileX = tileX;
            TileY = tileY;
            return;
        }

        TileX = tileX;
        TileY = tileY;
        TileId = _map.Terrain[tileX, tileY];
        TileZ = _map.Height[tileX, tileY];
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
        OffsetX = 0;
        OffsetY = 0;
        Zoom = 1.0;
        TileX = 0;
        TileY = 0;
        TileId = 0;
        TileZ = 0;
        OnPropertyChanged(nameof(StatusText));
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
