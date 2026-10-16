using Avalonia;
using Serilog;

namespace __Product__.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var settings = SettingsStore.Load();
        Culture.Configure(settings);              // before any window or Tr call
        AppLogging.Configure();
        // A crash otherwise ends the process before the async log sink writes anything.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
            Log.CloseAndFlush();
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        // Named mutexes work on Windows, Linux and macOS; "Local\" keeps it per user session.
        using var singleInstance = new Mutex(initiallyOwned: true, @"Local\__Product__.SingleInstance", out var owns);
        if (!owns)
        {
            Log.Information("Second instance blocked");
            Log.CloseAndFlush();
            return 0;
        }

        try
        {
            App.Settings = settings;
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            singleInstance.ReleaseMutex();
            Log.CloseAndFlush();
        }
    }

    // Also used by the designer and the headless tests.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
