using Avalonia;
using Avalonia.Media;
using __Product__.Desktop.Services;
using __Product__.Desktop.Shell;
using __Product__.Presentation.Hosting;
using __Product__.Presentation.Localization;
using __Product__.Presentation.Shell;

namespace __Product__.Desktop.Tests;

public sealed class ShellTests
{
    [Fact]
    public Task Main_window_shows_the_app_name() => Ui.Run(() =>
    {
        var window = new MainWindow(new MainWindowViewModel());

        window.Show();

        Assert.Equal(Tr.Get("App_Name"), window.Title);
        window.Close();
    });

    [Fact]
    public Task A_preset_accent_replaces_the_fluent_accent() => Ui.Run(() =>
    {
        var theme = new ThemeService();

        theme.Apply(new AppSettings { Accent = "#0F6CBD" });

        Assert.Equal(Color.Parse("#0F6CBD"), Application.Current!.Resources["SystemAccentColor"]);
        theme.Apply(new AppSettings());
        Assert.False(Application.Current.Resources.ContainsKey("SystemAccentColor"));
    });
}
