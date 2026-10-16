using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.Hosting;
using Serilog;
using __Product__.Desktop.Hosting;
using __Product__.Desktop.Services;
using __Product__.Desktop.Shell;

namespace __Product__.Desktop;

public sealed partial class App : Application
{
    private IHost? _host;

    // Read by Program before Avalonia starts; the tests leave it at the defaults.
    internal static AppSettings Settings { get; set; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Fluent controls read this key. An Arabic UI uses the bundled Noto Sans Arabic; $Default
        // falls back to the system UI font (and covers a missing font file).
        if (Culture.IsRtl)
            Resources["ContentControlThemeFontFamily"] = new FontFamily("avares://__Product__/Assets/Fonts#Noto Sans Arabic, $Default");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            _ = StartAsync(desktop);
    }

    private async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.ShutdownRequested += (_, _) => _host?.Dispose();   // the app registers no hosted services
        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSerilog();        // uses the static Log.Logger
            builder.Services.AddAppServices(Settings);
            builder.Services.AddDesktopServices();
            _host = builder.Build();
            await _host.StartAsync();

            _host.Services.GetRequiredService<IThemeService>().Apply(Settings);
            var database = _host.Services.GetRequiredService<DatabaseInitializer>();
            await Task.Run(database.BackupAndMigrate);

            var window = _host.Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            await new DialogService().ShowErrorAsync(Tr.Get("Error_Startup"));
            desktop.Shutdown(1);
        }
    }
}
