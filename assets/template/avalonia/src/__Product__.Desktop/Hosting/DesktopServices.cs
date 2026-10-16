using __Product__.Desktop.Services;
using __Product__.Desktop.Shell;

namespace __Product__.Desktop.Hosting;

// The UI side of the services. OS-specific ones go behind an interface with one class per OS under
// Platform/<OS>/, picked here with OperatingSystem.Is*() (cross-platform.md section 3).
public static class DesktopServices
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();

        // Shell: Singleton. Pages: Transient.
        services.AddSingleton<MainWindow>();
        return services;
    }
}
