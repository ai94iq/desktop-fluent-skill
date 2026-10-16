using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace __Product__.Desktop.Services;

// Every info, error and confirm dialog goes through here so RTL is handled once. Fluent in Avalonia
// has no ContentDialog, so this is a small modal window; before the main window exists it shows on
// its own.
public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string message) => ShowAsync(message, Tr.Get("Common_Ok"), null);

    public Task ShowErrorAsync(string message) => ShowAsync(message, Tr.Get("Common_Ok"), null);

    public Task<bool> ConfirmAsync(string message) => ShowAsync(message, Tr.Get("Common_Yes"), Tr.Get("Common_No"));

    private static async Task<bool> ShowAsync(string message, string primary, string? secondary)
    {
        var result = false;
        var dialog = new Window
        {
            Title = Tr.Get("App_Name"),
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            FlowDirection = Culture.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var accept = new Button { Content = primary, IsDefault = secondary is null };
        accept.Classes.Add("accent");
        accept.Click += (_, _) => { result = true; dialog.Close(); };
        buttons.Children.Add(accept);
        if (secondary is not null)
        {
            // The safe choice is the default for a confirmation.
            var decline = new Button { Content = secondary, IsDefault = true, IsCancel = true };
            decline.Click += (_, _) => dialog.Close();
            buttons.Children.Add(decline);
        }

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is { IsVisible: true })
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            var closed = new TaskCompletionSource();
            dialog.Closed += (_, _) => closed.TrySetResult();
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.Show();
            await closed.Task;
        }

        return result;
    }
}
