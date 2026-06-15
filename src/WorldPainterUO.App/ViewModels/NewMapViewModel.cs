using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WorldPainterUO.App.ViewModels;

public sealed partial class NewMapViewModel : ObservableObject
{
    [ObservableProperty]
    private int _width = 256;

    [ObservableProperty]
    private int _height = 256;

    [ObservableProperty]
    private string _facet = "Unknown";

    public record MapDims(int Width, int Height, string Facet);
}
