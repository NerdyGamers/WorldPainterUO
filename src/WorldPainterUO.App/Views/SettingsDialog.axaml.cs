using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.App.ViewModels;

namespace WorldPainterUO.App.Views;

public partial class SettingsDialog : Window
{
    private readonly AppPreferences _prefs;

    public SettingsDialog(AppPreferences prefs)
    {
        InitializeComponent();
        _prefs = prefs;

        UoDataPathBox.Text     = prefs.UoDataPath ?? string.Empty;
        ThemeBox.SelectedIndex = prefs.Theme == "Light" ? 1 : 0;
        DefaultWidthBox.Text   = prefs.DefaultMapWidth.ToString();
        DefaultHeightBox.Text  = prefs.DefaultMapHeight.ToString();
        DefaultFacetBox.Text   = prefs.DefaultFacet;
        AutosaveBox.Text       = prefs.AutosaveIntervalSeconds.ToString();

        UoDataPathBox.TextChanged += (_, _) => ValidatePath();
        ValidatePath();
    }

    /// <summary>The saved preferences after the user clicks OK. Null if cancelled.</summary>
    public AppPreferences? Result { get; private set; }

    private void ValidatePath()
    {
        var path = UoDataPathBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            PathStatusText.Text       = "⚠ No path set — radar colors will use fallback palette.";
            PathStatusText.Foreground = Brushes.Orange;
            PathStatusText.IsVisible  = true;
            return;
        }

        var radarPath = Path.Combine(path, "radarcol.mul");
        if (!File.Exists(radarPath))
        {
            PathStatusText.Text       = "⚠ radarcol.mul not found in that folder.";
            PathStatusText.Foreground = Brushes.Orange;
            PathStatusText.IsVisible  = true;
        }
        else
        {
            PathStatusText.Text       = "✓ radarcol.mul found.";
            PathStatusText.Foreground = Brushes.LightGreen;
            PathStatusText.IsVisible  = true;
        }
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select UO Data Folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
            UoDataPathBox.Text = folders[0].Path.LocalPath;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _prefs.UoDataPath = string.IsNullOrWhiteSpace(UoDataPathBox.Text)
            ? null
            : UoDataPathBox.Text.Trim();

        _prefs.Theme = ThemeBox.SelectedIndex == 1 ? "Light" : "Dark";

        if (int.TryParse(DefaultWidthBox.Text, out var dw) && dw > 0)
            _prefs.DefaultMapWidth = dw;
        if (int.TryParse(DefaultHeightBox.Text, out var dh) && dh > 0)
            _prefs.DefaultMapHeight = dh;
        if (!string.IsNullOrWhiteSpace(DefaultFacetBox.Text))
            _prefs.DefaultFacet = DefaultFacetBox.Text.Trim();
        if (int.TryParse(AutosaveBox.Text, out var secs) && secs >= 0)
            _prefs.AutosaveIntervalSeconds = secs;

        _prefs.Save();

        // Apply theme immediately
        MainWindowViewModel.ApplyTheme(_prefs.Theme);

        Result = _prefs;
        Close(Result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }

    // 'new' suppresses CS0108: this intentionally hides Window.ShowDialog(Window)
    // with a typed convenience overload that returns AppPreferences? directly.
    public static new async Task<AppPreferences?> ShowDialog(Window owner)
    {
        var prefs = AppPreferences.Load();
        var dialog = new SettingsDialog(prefs);
        return await dialog.ShowDialog<AppPreferences?>(owner);
    }
}
