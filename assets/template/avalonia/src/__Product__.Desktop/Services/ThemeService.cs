using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using __Product__.Core.Theming;

namespace __Product__.Desktop.Services;

// Maps the saved theme and accent onto Fluent, live. No accent means Fluent keeps the system accent.
public sealed class ThemeService : IThemeService
{
    private static readonly string[] AccentKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    public void Apply(AppSettings settings)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        foreach (var key in AccentKeys) app.Resources.Remove(key);
        if (settings.Accent is null) return;

        var shades = AccentPalette.Shades(settings.Accent);
        string[] values = [shades.Base, shades.Light1, shades.Light2, shades.Light3, shades.Dark1, shades.Dark2, shades.Dark3];
        for (var i = 0; i < AccentKeys.Length; i++)
            app.Resources[AccentKeys[i]] = Color.Parse(values[i]);
    }
}
