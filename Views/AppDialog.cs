using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace PortBan.Views;

internal static class AppDialog
{
    public static async Task<bool> ConfirmAsync(Window owner, string message)
    {
        var confirmed = false;
        var dialog = Create(owner, "プロセスを終了", message);
        var cancel = new Button { Content = "キャンセル", MinWidth = 104, IsCancel = true };
        var confirm = new Button
        {
            Content = "終了する",
            MinWidth = 104,
            IsDefault = true,
            Margin = new Thickness(8, 0, 0, 0),
        };
        cancel.Click += (_, _) => dialog.Close();
        confirm.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };
        AddButtons(dialog, cancel, confirm);
        await dialog.ShowDialog(owner);
        return confirmed;
    }

    public static async Task AlertAsync(Window owner, string title, string message)
    {
        var dialog = Create(owner, title, message);
        var close = new Button { Content = "閉じる", MinWidth = 104, IsDefault = true, IsCancel = true };
        close.Click += (_, _) => dialog.Close();
        AddButtons(dialog, close);
        await dialog.ShowDialog(owner);
    }

    private static Window Create(Window owner, string title, string message)
    {
        return new Window
        {
            Title = title,
            Width = 460,
            MinWidth = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Icon = owner.Icon,
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    Spacing = 0,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            FontSize = 14,
                            LineHeight = 22,
                        },
                    },
                },
            },
        };
    }

    private static void AddButtons(Window dialog, params Button[] buttons)
    {
        if (dialog.Content is not Border { Child: StackPanel panel })
            return;

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
        };
        foreach (var button in buttons)
            row.Children.Add(button);

        panel.Children.Add(row);
    }
}
