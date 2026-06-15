using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorldPainterUO.Editor;

namespace WorldPainterUO.App.ViewModels;

/// <summary>Which editing tool is currently active.</summary>
public enum ActiveTool
{
    Pan,
    PaintBrush,
    Fill,
    Raise,
    Lower,
    Smooth,
    Flatten,
    Noise,
    Replace,
    Stamp,
}

/// <summary>
/// Holds transient tool state shared between the toolbar and the map canvas.
/// Does NOT own the command history – that lives in MainWindowViewModel.
/// </summary>
public sealed partial class ToolViewModel : ObservableObject
{
    private ActiveTool _activeTool = ActiveTool.Pan;
    private string? _activeBiomeName;
    private int _brushRadius = 4;
    private double _brushStrength = 1.0;
    private double _brushHardness = 1.0;
    private sbyte _flattenZ = 0;

    public ToolViewModel()
    {
        // Build biome list from default palette
        var palette = TerrainPalette.CreateDefault();
        foreach (var name in palette.BiomeNames)
            Biomes.Add(new BiomeItem(name, palette.GetBiome(name)!));

        _activeBiomeName = Biomes.Count > 0 ? Biomes[0].Name : null;
    }

    // ── Tool selection ────────────────────────────────────────────────────────

    public ActiveTool ActiveTool
    {
        get => _activeTool;
        set
        {
            if (SetProperty(ref _activeTool, value))
            {
                OnPropertyChanged(nameof(IsPanActive));
                OnPropertyChanged(nameof(IsPaintActive));
                OnPropertyChanged(nameof(IsFillActive));
                OnPropertyChanged(nameof(IsRaiseActive));
                OnPropertyChanged(nameof(IsLowerActive));
                OnPropertyChanged(nameof(IsSmoothActive));
                OnPropertyChanged(nameof(IsFlattenActive));
                OnPropertyChanged(nameof(IsNoiseActive));
                OnPropertyChanged(nameof(IsReplaceActive));
                OnPropertyChanged(nameof(IsStampActive));
                OnPropertyChanged(nameof(ShowBiomePalette));
                OnPropertyChanged(nameof(ShowBrushOptions));
                OnPropertyChanged(nameof(ShowFlattenOptions));
            }
        }
    }

    public bool IsPanActive     => _activeTool == ActiveTool.Pan;
    public bool IsPaintActive   => _activeTool == ActiveTool.PaintBrush;
    public bool IsFillActive    => _activeTool == ActiveTool.Fill;
    public bool IsRaiseActive   => _activeTool == ActiveTool.Raise;
    public bool IsLowerActive   => _activeTool == ActiveTool.Lower;
    public bool IsSmoothActive  => _activeTool == ActiveTool.Smooth;
    public bool IsFlattenActive => _activeTool == ActiveTool.Flatten;
    public bool IsNoiseActive   => _activeTool == ActiveTool.Noise;
    public bool IsReplaceActive => _activeTool == ActiveTool.Replace;
    public bool IsStampActive   => _activeTool == ActiveTool.Stamp;

    // Show biome palette when a terrain-painting tool is active
    public bool ShowBiomePalette => _activeTool is ActiveTool.PaintBrush or ActiveTool.Fill or ActiveTool.Replace;
    // Show brush size/strength for radius-based tools
    public bool ShowBrushOptions => _activeTool is ActiveTool.PaintBrush or ActiveTool.Raise
        or ActiveTool.Lower or ActiveTool.Smooth or ActiveTool.Flatten or ActiveTool.Noise;
    public bool ShowFlattenOptions => _activeTool == ActiveTool.Flatten;

    // ── Biomes ────────────────────────────────────────────────────────────────

    public ObservableCollection<BiomeItem> Biomes { get; } = [];

    public string? ActiveBiomeName
    {
        get => _activeBiomeName;
        set
        {
            if (SetProperty(ref _activeBiomeName, value))
                OnPropertyChanged(nameof(ActiveBiome));
        }
    }

    public BiomeItem? ActiveBiome =>
        _activeBiomeName is null ? null : Biomes.FirstOrDefault(b => b.Name == _activeBiomeName);

    // ── Brush options ─────────────────────────────────────────────────────────

    public int BrushRadius
    {
        get => _brushRadius;
        set => SetProperty(ref _brushRadius, Math.Clamp(value, 1, 64));
    }

    public double BrushStrength
    {
        get => _brushStrength;
        set => SetProperty(ref _brushStrength, Math.Clamp(value, 0.01, 1.0));
    }

    public double BrushHardness
    {
        get => _brushHardness;
        set => SetProperty(ref _brushHardness, Math.Clamp(value, 0.1, 1.0));
    }

    public sbyte FlattenZ
    {
        get => _flattenZ;
        set => SetProperty(ref _flattenZ, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand] public void SelectPan()     => ActiveTool = ActiveTool.Pan;
    [RelayCommand] public void SelectPaint()   => ActiveTool = ActiveTool.PaintBrush;
    [RelayCommand] public void SelectFill()    => ActiveTool = ActiveTool.Fill;
    [RelayCommand] public void SelectRaise()   => ActiveTool = ActiveTool.Raise;
    [RelayCommand] public void SelectLower()   => ActiveTool = ActiveTool.Lower;
    [RelayCommand] public void SelectSmooth()  => ActiveTool = ActiveTool.Smooth;
    [RelayCommand] public void SelectFlatten() => ActiveTool = ActiveTool.Flatten;
    [RelayCommand] public void SelectNoise()   => ActiveTool = ActiveTool.Noise;
    [RelayCommand] public void SelectReplace() => ActiveTool = ActiveTool.Replace;
}

/// <summary>A single biome entry shown in the palette panel.</summary>
public sealed class BiomeItem(string name, WorldPainterUO.Editor.BiomeDefinition definition)
{
    public string Name { get; } = name;
    public BiomeDefinition Definition { get; } = definition;

    /// <summary>Primary display color derived from the biome's first tile (fallback gray).</summary>
    public Avalonia.Media.Color DisplayColor { get; } = BiomeColors.For(name);
}

/// <summary>Hard-coded display colors for the palette swatches.</summary>
public static class BiomeColors
{
    private static readonly Dictionary<string, Avalonia.Media.Color> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ocean"]    = Avalonia.Media.Color.FromRgb(0x1A, 0x6B, 0xA0),
        ["Grass"]    = Avalonia.Media.Color.FromRgb(0x4C, 0xAF, 0x50),
        ["Forest"]   = Avalonia.Media.Color.FromRgb(0x1B, 0x5E, 0x20),
        ["Swamp"]    = Avalonia.Media.Color.FromRgb(0x55, 0x6B, 0x2F),
        ["Snow"]     = Avalonia.Media.Color.FromRgb(0xE0, 0xF7, 0xFA),
        ["Desert"]   = Avalonia.Media.Color.FromRgb(0xC8, 0xA0, 0x50),
        ["Mountain"] = Avalonia.Media.Color.FromRgb(0x78, 0x6C, 0x60),
        ["Volcanic"] = Avalonia.Media.Color.FromRgb(0xBF, 0x36, 0x0C),
        ["Marsh"]    = Avalonia.Media.Color.FromRgb(0x6A, 0x8F, 0x5A),
        ["Road"]     = Avalonia.Media.Color.FromRgb(0x90, 0x7A, 0x60),
        ["Rock"]     = Avalonia.Media.Color.FromRgb(0x70, 0x70, 0x70),
    };

    public static Avalonia.Media.Color For(string name) =>
        Map.TryGetValue(name, out var c) ? c : Avalonia.Media.Color.FromRgb(0x80, 0x80, 0x80);
}
