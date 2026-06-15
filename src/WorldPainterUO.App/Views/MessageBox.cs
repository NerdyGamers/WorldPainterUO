using Avalonia.Controls;
using Avalonia.Layout;

namespace WorldPainterUO.App.Views;

public static class MessageBox
{
    public static async Task ShowDialog(Window owner, string message, string title)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(16),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var okButton = new Button { Content = "OK", Width = 80, IsDefault = true };
        okButton.Click += (_, _) => dialog.Close();

        var buttonPanel = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 0, 16, 12),
            Children = { okButton },
        };

        var buttonBar = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 48,
            Child = buttonPanel,
        };

        var layout = new Avalonia.Controls.Panel();
        layout.Children.Add(textBlock);
        layout.Children.Add(buttonBar);

        dialog.Content = layout;
        await dialog.ShowDialog(owner);
    }
}
