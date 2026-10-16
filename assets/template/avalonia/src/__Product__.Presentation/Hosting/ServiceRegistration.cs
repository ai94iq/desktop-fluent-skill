using __Product__.Presentation.Shell;

namespace __Product__.Presentation.Hosting;

// The portable services. The Desktop project adds the UI ones (AddDesktopServices).
public static class ServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, AppSettings settings)
    {
        services.AddSingleton(settings);
        services.AddMemoryCache(o => o.SizeLimit = 2_000);
        services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

        // Data: factory, initializer and repositories are Singletons.
        services.AddSingleton(new DataOptions(AppPaths.Database, AppPaths.Backups));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<IDateFormatter, DateFormatter>();

        // Shell: Singleton. Pages' ViewModels: Transient.
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}
