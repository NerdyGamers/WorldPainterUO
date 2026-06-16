using Avalonia.Controls;
using Avalonia.Layout;

namespace WorldPainterUO.App.Views;

public enum MessageBoxResult { Ok, Yes, No, Cancel }
public enum MessageBoxButtons { Ok, YesNo, YesNoCancel }

public static class MessageBox
{
    public static Task ShowDialog(Window owner, string message, string title)
        => ShowDialog(owner, message, title, MessageBoxButtons.Ok);

    public static async Task<MessageBoxResult> ShowDialog(
        Window owner, string message, string title,
        MessageBoxButtons buttons = MessageBoxButtons.Ok)
    {
        var result = MessageBoxResult.Ok;

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

        var buttonPanel = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 0, 16, 12),
        };

        Button Make(string text, MessageBoxResult r, bool defaultBtn = false)
        {
            var btn = new Button { Content = text, Width = 80, IsDefault = defaultBtn };
            btn.Click += (_, _) => { result = r; dialog.Close(); };
            return btn;
        }

        buttonPanel.Children.AddRange(buttons switch
        {
            MessageBoxButtons.Ok =>         [ Make("OK", MessageBoxResult.Ok, true) ],
            MessageBoxButtons.YesNo =>      [ Make("Yes", MessageBoxResult.Yes, true), Make("No", MessageBoxResult.No) ],
            MessageBoxButtons.YesNoCancel => [ Make("Yes", MessageBoxResult.Yes, true), Make("No", MessageBoxResult.No), Make("Cancel", MessageBoxResult.Cancel) ],
            _ =>                            [ Make("OK", MessageBoxResult.Ok, true) ],
        });

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
        return result;
    }
}
