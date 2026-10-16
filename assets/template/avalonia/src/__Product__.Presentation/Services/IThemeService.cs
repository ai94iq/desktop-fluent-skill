namespace __Product__.Presentation.Services;

// Applies light/dark/system theme and accent to the whole app. A settings page calls Apply with the
// new settings (after SettingsStore.Save), so ViewModels never touch a window.
public interface IThemeService
{
    void Apply(AppSettings settings);
}
