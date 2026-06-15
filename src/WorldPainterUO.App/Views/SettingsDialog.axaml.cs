using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WorldPainterUO.App.Configuration;

namespace WorldPainterUO.App.Views;

public partial class SettingsDialog : Window
{
    private AppPreferences _prefs;

    public SettingsDialog(AppPreferences prefs)
    {
        InitializeComponent();
        _prefs = prefs;

        UoDataPathBox.Text = prefs.UoDataPath ?? string.Empty;

        ThemeBox.SelectedIndex = prefs.Theme == "Light" ? 1 : 0;

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
            PathStatusText.Text = "⚠ No path set — radar colors will use fallback palette.";
            PathStatusText.IsVisible = true;
            return;
        }

        var radarPath = Path.Combine(path, "radarcol.mul");
        if (!File.Exists(radarPath))
        {
            PathStatusText.Text = $"⚠ radarcol.mul not found in that folder.";
            PathStatusText.IsVisible = true;
        }
        else
        {
            PathStatusText.Text = "✓ radarcol.mul found.";
            PathStatusText.Foreground = Avalonia.Media.Brushes.LightGreen;
            PathStatusText.IsVisible = true;
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
        {
            UoDataPathBox.Text = folders[0].Path.LocalPath;
        }
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _prefs.UoDataPath = string.IsNullOrWhiteSpace(UoDataPathBox.Text)
            ? null
            : UoDataPathBox.Text.Trim();

        _prefs.Theme = ThemeBox.SelectedIndex == 1 ? "Light" : "Dark";
        _prefs.Save();

        Result = _prefs;
        Close(Result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(null);
    }

    public static async Task<AppPreferences?> ShowDialog(Window owner)
    {
        var prefs = AppPreferences.Load();
        var dialog = new SettingsDialog(prefs);
        return await dialog.ShowDialog<AppPreferences?>(owner);
    }
}
