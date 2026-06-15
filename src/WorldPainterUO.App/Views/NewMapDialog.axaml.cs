using Avalonia.Controls;
using Avalonia.Interactivity;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.App.ViewModels;

namespace WorldPainterUO.App.Views;

public partial class NewMapDialog : Window
{
    public NewMapDialog()
    {
        InitializeComponent();

        // Seed fields from saved preferences
        var prefs = AppPreferences.Load();
        WidthBox.Text  = prefs.DefaultMapWidth.ToString();
        HeightBox.Text = prefs.DefaultMapHeight.ToString();
        FacetBox.Text  = prefs.DefaultFacet;
    }

    public Task<NewMapViewModel.MapDims?> RunDialogAsync(Window owner)
    {
        return ShowDialog<NewMapViewModel.MapDims?>(owner);
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthBox.Text, out var w) || w < 1)
        {
            WidthBox.Focus();
            return;
        }

        if (!int.TryParse(HeightBox.Text, out var h) || h < 1)
        {
            HeightBox.Focus();
            return;
        }

        Close(new NewMapViewModel.MapDims(w, h, FacetBox.Text ?? "Unknown"));
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
