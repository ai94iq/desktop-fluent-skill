namespace __Product__.Presentation.Hosting;

public static class Culture
{
    public static bool IsRtl { get; private set; }

    // Not checked on Avalonia yet, so the setting stays hidden (ui-frameworks.md).
    public static bool ArabicIndicDigits => false;

    public static void Configure(AppSettings settings)
    {
        // Empty Language = follow the system locale (dates, numbers, calendar, direction).
        var culture = string.IsNullOrWhiteSpace(settings.Language)
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(settings.Language);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        IsRtl = culture.TextInfo.IsRightToLeft;
    }
}
